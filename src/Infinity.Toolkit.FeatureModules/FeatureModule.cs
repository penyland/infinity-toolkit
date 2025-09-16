namespace Infinity.Toolkit.FeatureModules;

/// <summary>
/// Provides a base class for feature modules that can be registered and described within an application.
/// </summary>
public abstract class FeatureModule : IFeatureModule
{
    public virtual IModuleInfo? ModuleInfo => new FeatureModuleInfo(nameof(FeatureModule), Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString() ?? "1.0.0");

    public virtual ModuleContext RegisterModule(ModuleContext moduleContext) => moduleContext;
}

/// <summary>
/// Provides an abstract base class for defining modular web features that can be registered and mapped to endpoints
/// within an ASP.NET Core application.
/// </summary>
public abstract class WebFeatureModule : IWebFeatureModule
{
    public virtual IModuleInfo? ModuleInfo => new FeatureModuleInfo(nameof(WebFeatureModule), Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString() ?? "1.0.0");

    public virtual void RegisterModule(WebApplicationBuilder builder) { }

    public virtual void MapEndpoints(WebApplication app) { }
}
