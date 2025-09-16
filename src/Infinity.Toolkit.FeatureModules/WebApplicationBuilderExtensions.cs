using Infinity.Toolkit.LogFormatter;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Infinity.Toolkit.FeatureModules;

public static class WebApplicationBuilderExtensions
{
    private const string FeatureModulesConfigKey = "FeatureModules";

    /// <summary>
    /// Add all feature modules that are found in the solution.
    /// </summary>
    public static WebApplicationBuilder AddFeatureModules(
        this WebApplicationBuilder builder,
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
    public static WebApplicationBuilder AddFeatureModules(
        this WebApplicationBuilder builder,
        IConfiguration config)
    {
        var options = new FeatureModuleOptions();
        config.Bind(options);
        return builder.RegisterFeatureModules(options, null);
    }

    /// <summary>
    /// Add all feature modules that are found in the solution.
    /// </summary>
    public static WebApplicationBuilder AddFeatureModules(this WebApplicationBuilder builder)
    {
        return builder.AddFeatureModules(options => { });
    }

    internal static WebApplicationBuilder RegisterFeatureModules(this WebApplicationBuilder builder, FeatureModuleOptions options, ILoggerFactory? loggerFactory)
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

            var discoveredModules = ModuleUtilities.DiscoverModules(options, logger);
            RegisterModules(discoveredModules, builder, logger);

            logger?.LogDebug(new EventId(1003, "ScanningComplete"), "Registering feature modules completed.");
        }
        catch (Exception ex)
        {
            logger.LogError(new EventId(5000, "ScanningFailed"), "Failed to register feature modules. {ex}", ex.Message);
        }

        return builder;
    }

    /// <summary>
    /// Register all classes implementing IFeatureModule while scanning the project to IServiceCollection.
    /// </summary>
    /// <param name="discoveredModules">List of found feature modules.</param>
    /// <param name="builder">The <see cref="WebApplicationBuilder"/>.</param>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    /// <exception cref="InvalidOperationException">Thrown if no modules are found while scanning.</exception>
    private static void RegisterModules(IEnumerable<TypeInfo> discoveredModules, WebApplicationBuilder builder, ILogger? logger)
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
            logger?.LogDebug(new EventId(1002, "RegisteringModules"), "Registering feature module: {module} - Version: {version}", module.GetType().FullName, module.ModuleInfo?.Version);
            registeredFeatureModules.Add(module.GetType(), module);

            if (module is IWebFeatureModule webModule)
            {
                webModule.RegisterModule(builder);
            }
            else if (module is IFeatureModule featureModule)
            {
                featureModule?.RegisterModule(new()
                {
                    Configuration = builder.Configuration,
                    Environment = builder.Environment,
                    Services = builder.Services
                });
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
