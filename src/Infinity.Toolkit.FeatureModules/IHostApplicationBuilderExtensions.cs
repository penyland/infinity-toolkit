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
        var assemblies = new HashSet<Assembly>
        {
            typeof(Assembly).Assembly,
        };

        var entryAssembly = Assembly.GetEntryAssembly();
        var context = DependencyContext.Load(entryAssembly!)!;

        foreach (var assembly in context.RuntimeLibraries)
        {
            if (IsReferencingCurrentAssembly(assembly, typeof(IHostApplicationBuilderExtensions).Assembly.GetName().Name))
            {
                foreach (var assemblyName in assembly.GetDefaultAssemblyNames(context))
                {
                    assemblies.Add(Assembly.Load(assemblyName));
                }
            }
        }

        var typesAssignableTo = assemblies
            .SelectMany(x =>
                x.DefinedTypes
                .Where(type => type is { IsAbstract: false, IsInterface: false } &&
                                      type.IsAssignableTo(typeof(IFeatureModuleBase)) &&
                                      !options.ExcludedModules.Any(t => t == type.FullName)))
            .OrderBy(c => c.FullName);

        logger?.LogInformation(new EventId(1001, "ModulesFound"), "Found {moduleCount} feature modules.", typesAssignableTo.Count());
        return typesAssignableTo;
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
                logger?.LogInformation(new EventId(1002, "RegisteringModules"), "Registering feature module: {module} - v{version}", module.ModuleInfo?.Name ?? module.GetType().FullName, module.ModuleInfo?.Version ?? "1.0");
                featureModule?.RegisterModule(builder);
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
}
