using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Infinity.Toolkit.FeatureModules;

public static class ServiceCollectionExtensions
{
    private static readonly string? CurrentAssemblyName;

    static ServiceCollectionExtensions()
    {
        CurrentAssemblyName = typeof(ServiceCollectionExtensions).Assembly.GetName().Name;
    }

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
        loggerFactory ??= LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger("Infinity.Toolkit.FeatureModules");

        try
        {
            logger?.LogInformation("Scanning assemblies for feature modules...");

            var modules = DiscoverModules(options, logger);

            var featureModulesRegistry = new FeatureModules();
            featureModulesRegistry.RegisterModules(services, configuration, hostEnvironment, modules, logger);

            services.TryAddSingleton<FeatureModules>(featureModulesRegistry);

            logger?.LogInformation("Registering feature modules completed.");
            return services;
        }
        catch (Exception ex)
        {
            logger.LogError("Failed to register feature modules. {ex}", ex.Message);
            return services;
        }
    }

    /// <summary>
    /// Discover all modules that references IFeatureModule.
    /// </summary>
    /// <returns>A list of all feature modules in the solution.</returns>
    private static IEnumerable<IFeatureModule> DiscoverModules(FeatureModuleOptions options, ILogger? logger)
    {
        var results = new HashSet<Assembly>
        {
            typeof(Assembly).Assembly,
        };

        var entryAssembly = Assembly.GetEntryAssembly();
        var context = DependencyContext.Load(entryAssembly!)!;

        foreach (var assembly in context.RuntimeLibraries)
        {
            if (IsReferencingCurrentAssembly(assembly))
            {
                foreach (var assemblyName in assembly.GetDefaultAssemblyNames(context))
                {
                    results.Add(Assembly.Load(assemblyName));
                }
            }
        }

        var modules = results
            .SelectMany(x =>
                x.GetTypes()
                 .Where(p => p.IsClass && p.IsAssignableTo(typeof(IFeatureModule)) && !options.ExcludedModules.Any(t => t == p.Name))
                 .Select(Activator.CreateInstance)
                 .Cast<IFeatureModule>());

        logger?.LogInformation("Found {moduleCount} feature modules.", modules.Count());
        return modules;
    }

    private static bool IsReferencingCurrentAssembly(Library library)
    {
        return library.Dependencies.Any(dependency => dependency.Name.Equals(CurrentAssemblyName));
    }
}

internal class FeatureModules
{
    private readonly Dictionary<Type, IFeatureModule> registeredFeatureModules = [];

    /// <summary>
    /// Register all classes implementing IFeatureModule while scanning the project to IServiceCollection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/>.</param>
    /// <param name="hostEnvironment">The <see cref="IHostEnvironment"/>.</param>
    /// <param name="modules">List of found feature modules.</param>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    /// <exception cref="InvalidOperationException">Thrown if no modules are found while scanning.</exception>
    public void RegisterModules(IServiceCollection services, IConfiguration configuration, IHostEnvironment hostEnvironment, IEnumerable<IFeatureModule> modules, ILogger? logger)
    {
        ArgumentNullException.ThrowIfNull(modules, nameof(modules));
        ArgumentNullException.ThrowIfNull(services, nameof(services));

        foreach (var module in modules)
        {
            logger?.LogInformation("Registering feature module: {module} - v{version}", module.GetType().FullName, module.ModuleInfo?.Version);
            if (registeredFeatureModules.ContainsKey(module.GetType()))
            {
                continue;
            }

            registeredFeatureModules.Add(module.GetType(), module);
            module.RegisterModule(new()
            {
                Configuration = configuration,
                Environment = hostEnvironment,
                Services = services,
            });

            services.AddSingleton<IFeatureModule>(module);
        }

        services.Configure<FeatureModuleOptions>(options =>
        {
            options.AdditionalAssemblies.AddRange(registeredFeatureModules.Keys.Select(x => x.Assembly).ToArray());
        });
    }
}
