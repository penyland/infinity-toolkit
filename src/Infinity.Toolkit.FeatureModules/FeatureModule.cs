namespace Infinity.Toolkit.FeatureModules;

/// <summary>
/// Provides a base class for feature modules that can be registered and described within an application.
/// </summary>
public abstract class FeatureModule : IFeatureModule
{
    public abstract IModuleInfo? ModuleInfo { get; }

    public virtual ModuleContext RegisterModule(ModuleContext moduleContext) => moduleContext;
}

/// <summary>
/// Provides an abstract base class for defining modular web features that can be registered and mapped to endpoints
/// within an ASP.NET Core application.
/// </summary>
public abstract class WebFeatureModule : FeatureModule, IWebFeatureModule
{
    public virtual void RegisterModule(WebApplicationBuilder builder) { }

    public abstract void MapEndpoints(WebApplication app);
}
