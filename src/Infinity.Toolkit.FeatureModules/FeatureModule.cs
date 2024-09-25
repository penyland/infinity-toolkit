using Microsoft.AspNetCore.Routing;

namespace Infinity.Toolkit.FeatureModules;

public abstract class FeatureModule : IFeatureModule
{
    public abstract IModuleInfo? ModuleInfo { get; }

    public virtual ModuleContext RegisterModule(ModuleContext moduleContext) => moduleContext;
}

public abstract class WebFeatureModule : FeatureModule, IWebFeatureModule
{
    public virtual void MapEndpoints(IEndpointRouteBuilder builder) { }
}
