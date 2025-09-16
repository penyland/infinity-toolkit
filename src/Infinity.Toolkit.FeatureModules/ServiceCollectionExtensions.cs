using Infinity.Toolkit.LogFormatter;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Infinity.Toolkit.FeatureModules;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add all feature modules that are found in all assemblies.
    /// </summary>
    public static IServiceCollection AddFeatureModules(this IServiceCollection services, HostBuilderContext hostBuilderContext, ILoggerFactory? loggerFactory)
    {
        return services.AddFeatureModules(hostBuilderContext, options => { }, loggerFactory);
    }

    /// <summary>
    /// Add all feature modules that are found in all assemblies.
    /// </summary>
    public static IServiceCollection AddFeatureModules(this IServiceCollection services, HostBuilderContext hostBuilderContext, Action<FeatureModuleOptions> configure, ILoggerFactory? loggerFactory)
    {
        var options = new FeatureModuleOptions();
        services.TryAddSingleton(options);
        configure(options);

        return services.RegisterFeatureModules(hostBuilderContext.Configuration, hostBuilderContext.HostingEnvironment, options, loggerFactory);
    }

    internal static IServiceCollection RegisterFeatureModules(this IServiceCollection services, IConfiguration configuration, IHostEnvironment hostEnvironment, FeatureModuleOptions options, ILoggerFactory? loggerFactory)
    {
        loggerFactory ??= LoggerFactory.Create(builder =>
        {
            builder
                .AddConfiguration(configuration.GetSection("Logging"))
#if DEBUG
                .AddDebug()
#endif
                .AddConsole(options => options.FormatterName = "CodeThemeConsoleFormatter").AddConsoleFormatter<CodeThemeConsoleFormatter, CustomOptions>();
        });
        var logger = loggerFactory.CreateLogger("Infinity.Toolkit.FeatureModules");

        try
        {
            logger?.LogDebug(new EventId(1000, "Scanning"), "Scanning assemblies for feature modules...");

            var discoveredModules = ModuleUtilities.DiscoverModules(options, logger);
            RegisterModules(discoveredModules, services, configuration, hostEnvironment, logger);

            logger?.LogDebug(new EventId(1003, "ScanningComplete"), "Registering feature modules completed.");

            return services;
        }
        catch (Exception ex)
        {
            logger.LogError(new EventId(5000, "ScanningFailed"), "Failed to register feature modules. {ex}", ex.Message);
            return services;
        }
    }

    /// <summary>
    /// Register all classes implementing IFeatureModule while scanning the project to IServiceCollection.
    /// </summary>
    /// <param name="discoveredModules">List of found feature modules.</param>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/>.</param>
    /// <param name="hostEnvironment">The <see cref="IHostEnvironment"/>.</param>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    /// <exception cref="InvalidOperationException">Thrown if no modules are found while scanning.</exception>
    private static void RegisterModules(IEnumerable<TypeInfo> discoveredModules, IServiceCollection services, IConfiguration configuration, IHostEnvironment hostEnvironment, ILogger? logger)
    {
        ArgumentNullException.ThrowIfNull(discoveredModules, nameof(discoveredModules));
        ArgumentNullException.ThrowIfNull(services, nameof(services));

        Dictionary<Type, IFeatureModuleBase> registeredFeatureModules = [];

        var serviceDescriptors = discoveredModules
            .Select(type => ServiceDescriptor.Transient(typeof(IFeatureModuleBase), type));
        services.TryAddEnumerable(serviceDescriptors);

        var modules = discoveredModules
            .Select(Activator.CreateInstance)
            .Cast<IFeatureModuleBase>();

        foreach (var module in modules)
        {
            logger?.LogDebug(new EventId(1002, "RegisteringModules"), "Registering feature module: {module} - v{version}", module.GetType().FullName, module.ModuleInfo?.Version);
            registeredFeatureModules.Add(module.GetType(), module);

            if (module is IFeatureModule featureModule)
            {
                featureModule.RegisterModule(new()
                {
                    Configuration = configuration,
                    Environment = hostEnvironment,
                    Services = services
                });
            }
        }

        services.Configure<FeatureModuleOptions>(options =>
        {
            options.AdditionalAssemblies.AddRange([.. registeredFeatureModules.Keys.Select(x => x.Assembly)]);
        });
    }
}
