namespace Infinity.Toolkit.FeatureModules;

/// <summary>
/// Provides a base class for feature modules that can be registered and described within an
/// application.
/// </summary>
public abstract class FeatureModule : IFeatureModule
{
    protected FeatureModule()
    {
        ModuleInfo = new FeatureModuleInfo(GetType().Name, "1.0.0");
    }

    public virtual IModuleInfo ModuleInfo { get; }

    public virtual void RegisterModule(IHostApplicationBuilder builder) { }
}

/// <summary>
/// Provides an abstract base class for defining modular web features that can be registered and
/// mapped to endpoints within an ASP.NET Core application.
/// </summary>
public abstract class WebFeatureModule : IWebFeatureModule
{
    protected WebFeatureModule()
    {
        ModuleInfo = new FeatureModuleInfo(GetType().Name, "1.0.0");
    }

    public virtual IModuleInfo ModuleInfo { get; }

    public virtual void RegisterModule(IHostApplicationBuilder builder) { }

    public virtual void MapEndpoints(WebApplication app) { }
}
