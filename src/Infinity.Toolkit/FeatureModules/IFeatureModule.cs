using Microsoft.AspNetCore.Routing;

namespace Infinity.Toolkit.FeatureModules;

/// <summary>
/// Represents a Feature Module.
/// </summary>
public interface IFeatureModule
{
    /// <summary>
    /// Gets the meta data that describes the module such as name and version.
    /// </summary>
    IModuleInfo? ModuleInfo { get; }

    /// <summary>
    /// Register all dependencies needed by a module in the DI-container.
    /// </summary>
    ModuleContext RegisterModule(ModuleContext moduleContext);
}

public interface IWebFeatureModule : IFeatureModule
{
    /// <summary>
    /// Maps all endpoints provided by the module in the DI-container.
    /// </summary>
    void MapEndpoints(IEndpointRouteBuilder builder);
}
