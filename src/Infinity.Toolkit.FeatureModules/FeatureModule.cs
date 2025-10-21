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
public abstract class WebFeatureModule : IWebFeatureModule
{
    public abstract IModuleInfo? ModuleInfo { get; }

    public virtual void RegisterModule(IHostApplicationBuilder builder) { }

    public virtual void MapEndpoints(WebApplication app) { }
}
