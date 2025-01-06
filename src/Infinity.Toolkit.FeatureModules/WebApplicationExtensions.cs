namespace Infinity.Toolkit.FeatureModules;

public static class WebApplicationExtensions
{
    /// <summary>
    /// Maps all endpoints provided by the feature modules.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if no modules are found in the assembly.</exception>
    public static FeatureModuleBuilder MapFeatureModules(this WebApplication app)
    {
        var builder = new FeatureModuleBuilder(app);
        var featureModules = app.Services.GetRequiredService<IEnumerable<IFeatureModule>>();
        if (featureModules == null || !featureModules.Any())
        {
            app.Logger.LogWarning(new EventId(4000, "NoModulesRegistered"), "No feature modules registered.");
            return builder;
        }

        // Get all elements that implements IWebFeatureModule in featureModules
        var webFeatureModules = featureModules.Where(x => x is IWebFeatureModule).Cast<IWebFeatureModule>();

        // Map all endpoints provided by the feature modules, if any.
        foreach (var module in webFeatureModules)
        {
            app.Logger.LogDebug(new EventId(1004, "MappingEndpoints"), "Mapping endpoints for {module}", module.GetType().FullName ?? nameof(module));
            module.MapEndpoints(app);
        }

        return builder;
    }

    /// <summary>
    /// Maps the page components defined in the specified <typeparamref name="TRootComponent"/> to the given assembly
    /// and renders the component specified by <typeparamref name="TRootComponent"/> when the route matches.
    /// As well as adding the additional assemblies where feature modules are located.
    /// </summary>
    /// <param name="builder">The <see cref="FeatureModuleBuilder"/>.</param>
    /// <returns>An <see cref="RazorComponentsEndpointConventionBuilder"/> that can be used to further configure the API.</returns>
    public static RazorComponentsEndpointConventionBuilder MapRazorComponents<TRootComponent>(this FeatureModuleBuilder builder)
    {
        var options = builder.WebApplication.Services.GetService<IOptions<FeatureModuleOptions>>();

        // Make sure the assembly of the root component is not included in the additional assemblies.
        options?.Value.AdditionalAssemblies?.RemoveAll(m => m.FullName == typeof(TRootComponent).Assembly.FullName);

        var razorComponentsEndpointConventionBuilder = builder.WebApplication.MapRazorComponents<TRootComponent>().AddAdditionalAssemblies([.. options?.Value.AdditionalAssemblies!]);

        return razorComponentsEndpointConventionBuilder;
    }
}
