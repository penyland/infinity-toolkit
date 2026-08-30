using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyModel;

namespace Infinity.Toolkit.FeatureModules;

public static class IHostApplicationBuilderExtensions
{
    private const string FeatureModulesConfigKey = "FeatureModules";

    /// <summary>
    /// Add all feature modules that are found in the solution.
    /// </summary>
    public static IHostApplicationBuilder AddFeatureModules(
        this IHostApplicationBuilder builder,
        Action<FeatureModuleOptions> configure,
        string configKey = FeatureModulesConfigKey,
        ILoggerFactory? loggerFactory = null)
    {
        var options = new FeatureModuleOptions();
        builder.Configuration.GetSection(configKey).Bind(options);
        configure(options);

        return builder.RegisterFeatureModules(options, loggerFactory);
    }

    /// <summary>
    /// Add all feature modules that are found in the solution.
    /// </summary>
    public static IHostApplicationBuilder AddFeatureModules(
        this IHostApplicationBuilder builder,
        IConfiguration config)
    {
        var options = new FeatureModuleOptions();
        config.Bind(options);
        return builder.RegisterFeatureModules(options, null);
    }

    /// <summary>
    /// Add all feature modules that are found in the solution.
    /// </summary>
    public static IHostApplicationBuilder AddFeatureModules(this IHostApplicationBuilder builder)
    {
        return builder.AddFeatureModules(options => { });
    }

    internal static IHostApplicationBuilder RegisterFeatureModules(this IHostApplicationBuilder builder, FeatureModuleOptions options, ILoggerFactory? loggerFactory)
    {
        loggerFactory ??= LoggerFactory.Create(loggingBuilder =>
        {
            loggingBuilder
                .AddConfiguration(builder.Configuration.GetSection("Logging"))
#if DEBUG
                .AddDebug()
#endif
                .AddSimpleConsole(options => options.SingleLine = true);
        });
        var logger = loggerFactory.CreateLogger("Infinity.Toolkit.FeatureModules");

        try
        {
            logger?.ScanningAssembliesForFeatureModules();

            var discoveredModules = DiscoverModules(options, logger);
            RegisterModules(discoveredModules, builder, logger);

            logger?.RegisteringFeatureModulesCompleted();
        }
        catch (Exception ex)
        {
            logger?.FailedToRegisterFeatureModules(ex);
        }

        return builder;
    }

    /// <summary>
    /// Discover all modules that references IFeatureModule.
    /// </summary>
    /// <returns>A list of all feature modules in the solution.</returns>
    private static List<TypeInfo> DiscoverModules(FeatureModuleOptions options, ILogger? logger)
    {
        var assemblies = GetCandidateAssemblies();
        var excludedModules = GetExcludedModules(options, assemblies, logger);

        var generatedModules = (TryGetGeneratedModules(assemblies, logger) ?? [])
            .Select(type => type.GetTypeInfo())
            .Where(type => type is { IsAbstract: false, IsInterface: false } &&
                          type.IsAssignableTo(typeof(IFeatureModuleBase)) &&
                          !ShouldModuleBeExcluded(type, excludedModules))
            .ToList();

        logger?.DiscoveredCompileTimeModules(generatedModules.Count);

        var reflectionModules = assemblies
            .SelectMany(assembly =>
                assembly.DefinedTypes
                    .Where(type => type is { IsAbstract: false, IsInterface: false } &&
                                   type.IsAssignableTo(typeof(IFeatureModuleBase)) &&
                                   !ShouldModuleBeExcluded(type, excludedModules)))
            .ToList();

        var generatedFullNames = generatedModules
            .Select(type => type.FullName)
            .Where(fullName => !string.IsNullOrWhiteSpace(fullName))
            .ToHashSet(StringComparer.Ordinal);

        var reflectionOnlyCount = reflectionModules
            .Count(type => !generatedFullNames.Contains(type.FullName));

        logger?.DiscoveredReflectionModules(reflectionOnlyCount);

        var discoveredModules = generatedModules
            .Concat(reflectionModules)
            .GroupBy(type => type.FullName, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(type => type.FullName)
            .ToList();

        logger?.DiscoveredTotalModules(
            discoveredModules.Count,
            generatedModules.Count,
            reflectionOnlyCount);

        return discoveredModules;
    }

    /// <summary>
    /// Attempts to retrieve module types from generated registries using reflection. Returns null
    /// if no registry type is found.
    /// </summary>
    private static Type[]? TryGetGeneratedModules(
        IEnumerable<Assembly> assemblies,
        ILogger? logger)
    {
        try
        {
            const string registryTypeName =
                "Infinity.Toolkit.FeatureModules.GeneratedFeatureModuleRegistry";

            var generatedTypes = new List<Type>();

            foreach (var assembly in assemblies.Distinct())
            {
                var registryType = assembly.GetType(registryTypeName);
                if (registryType == null)
                {
                    continue;
                }

                var method = registryType.GetMethod(
                    "GetModuleTypes",
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                if (method == null)
                {
                    continue;
                }

                if (method.Invoke(null, null) is Type[] types)
                {
                    generatedTypes.AddRange(types);
                }
            }

            return generatedTypes.Count == 0
                ? null
                : generatedTypes.Distinct().ToArray();
        }
        catch (Exception ex)
        {
            logger?.FailedToLoadGeneratedModuleRegistry(ex.Message);
            return null;
        }
    }

    private static HashSet<Assembly> GetCandidateAssemblies()
    {
        var assemblies = new HashSet<Assembly>
        {
            typeof(Assembly).Assembly,
            typeof(IHostApplicationBuilderExtensions).Assembly,
        };

        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly == null)
        {
            return assemblies;
        }

        assemblies.Add(entryAssembly);

        var context = DependencyContext.Load(entryAssembly);
        if (context == null)
        {
            return assemblies;
        }

        foreach (var runtimeLibrary in context.RuntimeLibraries)
        {
            if (!IsReferencingCurrentAssembly(
                    runtimeLibrary,
                    typeof(IHostApplicationBuilderExtensions).Assembly.GetName().Name))
            {
                continue;
            }

            foreach (var assemblyName in runtimeLibrary.GetDefaultAssemblyNames(context))
            {
                assemblies.Add(Assembly.Load(assemblyName));
            }
        }

        return assemblies;
    }

    private static bool ShouldModuleBeExcluded(TypeInfo type, IReadOnlySet<Type> excludedModules)
    {
        return excludedModules.Contains(type.AsType());
    }

    private static HashSet<Type> GetExcludedModules(
        FeatureModuleOptions options,
        IEnumerable<Assembly> assemblies,
        ILogger? logger)
    {
        var excludedModules = new HashSet<Type>(options.ExcludedModules);

        foreach (var moduleName in options.ExcludedModuleNames)
        {
            if (string.IsNullOrWhiteSpace(moduleName))
            {
                continue;
            }

            var resolvedModule = Type.GetType(moduleName, throwOnError: false, ignoreCase: true);
            if (resolvedModule != null)
            {
                excludedModules.Add(resolvedModule);
                continue;
            }

            var matchingTypes = assemblies
                .SelectMany(assembly => assembly.DefinedTypes)
                .Where(type => type.IsAssignableTo(typeof(IFeatureModuleBase)) &&
                               (type.Name.Equals(moduleName, StringComparison.OrdinalIgnoreCase) ||
                                type.FullName?.Equals(
                                    moduleName,
                                    StringComparison.OrdinalIgnoreCase) == true))
                .Select(type => type.AsType())
                .Distinct()
                .ToList();

            if (matchingTypes.Count == 0)
            {
                logger?.ConfiguredExcludedModuleNotFound(moduleName);
                continue;
            }

            foreach (var matchingType in matchingTypes)
            {
                excludedModules.Add(matchingType);
            }

            if (matchingTypes.Count > 1)
            {
                logger?.ConfiguredExcludedModuleAmbiguous(moduleName, matchingTypes.Count);
            }
        }

        return excludedModules;
    }

    private static bool IsReferencingCurrentAssembly(Library library, string? currentAssemblyName)
    {
        return library.Dependencies.Any(dependency => dependency.Name.Equals(currentAssemblyName));
    }

    /// <summary>
    /// Register all classes implementing IFeatureModule while scanning the project to
    /// IServiceCollection.
    /// </summary>
    /// <param name="discoveredModules">List of found feature modules.</param>
    /// <param name="builder">The <see cref="WebApplicationBuilder"/>.</param>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if no modules are found while scanning.
    /// </exception>
    private static void RegisterModules(IEnumerable<TypeInfo> discoveredModules, IHostApplicationBuilder builder, ILogger? logger)
    {
        ArgumentNullException.ThrowIfNull(discoveredModules, nameof(discoveredModules));
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));

        Dictionary<Type, IFeatureModuleBase> registeredFeatureModules = [];
        var assemblies = GetCandidateAssemblies();

        var serviceDescriptors = discoveredModules
            .Select(type => ServiceDescriptor.Transient(typeof(IFeatureModuleBase), type));
        builder.Services.TryAddEnumerable(serviceDescriptors);

        var modules = discoveredModules
            .Select(Activator.CreateInstance)
            .Cast<IFeatureModuleBase>();

        foreach (var module in modules)
        {
            registeredFeatureModules.Add(module.GetType(), module);

            if (module is IFeatureModule featureModule)
            {
                var generatedModuleInfo = TryGetGeneratedModuleInfo(
                    assemblies,
                    module.GetType(),
                    logger);

                var moduleName = generatedModuleInfo?.Name
                    ?? module.ModuleInfo?.Name
                    ?? module.GetType().FullName;
                var moduleVersion = generatedModuleInfo?.Version
                    ?? module.ModuleInfo?.Version
                    ?? "1.0";

                logger?.RegisteringFeatureModule(moduleName, moduleVersion);
                featureModule.RegisterModule(builder);
            }
            else
            {
                logger?.ModuleDoesNotImplementFeatureModule(module.GetType().FullName);
            }
        }

        builder.Services.Configure<FeatureModuleOptions>(options =>
        {
            options.AdditionalAssemblies.AddRange([.. registeredFeatureModules.Keys.Select(x => x.Assembly)]);
        });
    }

    private static IModuleInfo? TryGetGeneratedModuleInfo(
        IEnumerable<Assembly> assemblies,
        Type moduleType,
        ILogger? logger)
    {
        try
        {
            const string registryTypeName =
                "Infinity.Toolkit.FeatureModules.GeneratedFeatureModuleMetadataRegistry";

            foreach (var assembly in assemblies.Distinct())
            {
                var registryType = assembly.GetType(registryTypeName);
                if (registryType == null)
                {
                    continue;
                }

                var method = registryType.GetMethod(
                    "TryGetModuleInfo",
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                if (method == null)
                {
                    continue;
                }

                var parameters = new object?[] { moduleType, null };
                if (method.Invoke(null, parameters) is true &&
                    parameters[1] is IModuleInfo moduleInfo)
                {
                    return moduleInfo;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            logger?.FailedToLoadGeneratedModuleMetadataRegistry(ex.Message);
            return null;
        }
    }
}
