using Infinity.Toolkit.LogFormatter;
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
                .AddConsole(options => options.FormatterName = "CodeThemeConsoleFormatter").AddConsoleFormatter<CodeThemeConsoleFormatter, CustomOptions>();
        });
        var logger = loggerFactory.CreateLogger("Infinity.Toolkit.FeatureModules");

        try
        {
            logger?.LogDebug(new EventId(1000, "Scanning"), "Scanning assemblies for feature modules...");

            var discoveredModules = DiscoverModules(options, logger);
            RegisterModules(discoveredModules, builder, logger);

            logger?.LogDebug(new EventId(1003, "ScanningComplete"), "Registering feature modules completed.");
        }
        catch (Exception ex)
        {
            logger?.LogError(new EventId(5000, "ScanningFailed"), "Failed to register feature modules. {ex}", ex.Message);
        }

        return builder;
    }

    /// <summary>
    /// Discover all modules that references IFeatureModule.
    /// </summary>
    /// <returns>A list of all feature modules in the solution.</returns>
    private static IEnumerable<TypeInfo> DiscoverModules(FeatureModuleOptions options, ILogger? logger)
    {
        var assemblies = GetCandidateAssemblies();

        var generatedModules = (TryGetGeneratedModules(assemblies, logger) ?? [])
            .Select(type => type.GetTypeInfo())
            .Where(type => type is { IsAbstract: false, IsInterface: false } &&
                          type.IsAssignableTo(typeof(IFeatureModuleBase)) &&
                          !ShouldModuleBeExcluded(type, options))
            .ToList();

        logger?.LogDebug(
            new EventId(1004, "UsingGeneratedModules"),
            "Discovered {count} modules at compile-time.",
            generatedModules.Count);

        var reflectionModules = assemblies
            .SelectMany(assembly =>
                assembly.DefinedTypes
                    .Where(type => type is { IsAbstract: false, IsInterface: false } &&
                                   type.IsAssignableTo(typeof(IFeatureModuleBase)) &&
                                   !ShouldModuleBeExcluded(type, options)))
            .ToList();

        var generatedFullNames = generatedModules
            .Select(type => type.FullName)
            .Where(fullName => !string.IsNullOrWhiteSpace(fullName))
            .ToHashSet(StringComparer.Ordinal);

        var reflectionOnlyCount = reflectionModules
            .Count(type => !generatedFullNames.Contains(type.FullName));

        logger?.LogDebug(
            new EventId(1007, "ReflectionModulesFound"),
            "Discovered {count} modules by reflection.",
            reflectionOnlyCount);

        var discoveredModules = generatedModules
            .Concat(reflectionModules)
            .GroupBy(type => type.FullName, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(type => type.FullName)
            .ToList();

        logger?.LogInformation(
            new EventId(1001, "ModulesFound"),
            "Discovered total {moduleCount} feature modules. {compileTimeModule}/{runtimeModule} (compile time/runtime)",
            discoveredModules.Count,
            generatedModules.Count,
            reflectionOnlyCount);

        return discoveredModules;
    }

    /// <summary>
    /// Attempts to retrieve module types from generated registries using reflection.
    /// Returns null if no registry type is found.
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
            logger?.LogDebug(
                new EventId(1006, "GeneratedRegistryError"),
                "Failed to load generated module registry: {message}",
                ex.Message);
            return null;
        }
    }

    private static IReadOnlyCollection<Assembly> GetCandidateAssemblies()
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

    private static bool ShouldModuleBeExcluded(TypeInfo type, FeatureModuleOptions options)
    {
        return options.ExcludedModules.Any(t => t == type.FullName);
    }

    private static bool IsReferencingCurrentAssembly(Library library, string? currentAssemblyName)
    {
        return library.Dependencies.Any(dependency => dependency.Name.Equals(currentAssemblyName));
    }

    /// <summary>
    /// Register all classes implementing IFeatureModule while scanning the project to IServiceCollection.
    /// </summary>
    /// <param name="discoveredModules">List of found feature modules.</param>
    /// <param name="builder">The <see cref="WebApplicationBuilder"/>.</param>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    /// <exception cref="InvalidOperationException">Thrown if no modules are found while scanning.</exception>
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

                logger?.LogInformation(
                    new EventId(1002, "RegisteringModules"),
                    "Registering feature module: {module} - v{version}",
                    moduleName,
                    moduleVersion);
                featureModule.RegisterModule(builder);
            }
            else
            {
                logger?.LogError(new EventId(1002, "RegisteringModules"), "Module {module} does not implement IFeatureModule or IWebFeatureModule.", module.GetType().FullName);
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
            logger?.LogDebug(
                new EventId(1008, "GeneratedMetadataRegistryError"),
                "Failed to load generated module metadata registry: {message}",
                ex.Message);
            return null;
        }
    }
}
