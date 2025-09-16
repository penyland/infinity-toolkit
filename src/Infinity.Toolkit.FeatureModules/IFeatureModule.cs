namespace Infinity.Toolkit.FeatureModules;


public interface IFeatureModuleBase
{
    /// <summary>
    /// Gets the meta data that describes the module such as name and version.
    /// </summary>
    IModuleInfo? ModuleInfo { get; }
}

/// <summary>
/// Defines the contract for a feature module that provides metadata and registers its dependencies within an
/// application.
/// </summary>
/// <remarks>Implementations of this interface are intended to encapsulate distinct features or components that
/// can be integrated into an application. Each module exposes its metadata and is responsible for registering its
/// required services and dependencies in the dependency injection container. This interface is typically used in
/// modular application architectures to enable dynamic discovery and composition of features.</remarks>
public interface IFeatureModule : IFeatureModuleBase
{
    /// <summary>
    /// Register all dependencies needed by a module in the DI-container.
    /// </summary>
    ModuleContext RegisterModule(ModuleContext moduleContext);
}

/// <summary>
/// Defines the contract for a web feature module that can register its dependencies and map its endpoints within an
/// ASP.NET Core application.
/// </summary>
/// <remarks>Implementations of this interface enable modular configuration of web features by allowing each
/// module to independently register services and endpoints. This facilitates separation of concerns and improves
/// maintainability in large applications. Modules should ensure that all required services are registered before
/// mapping endpoints.</remarks>
public interface IWebFeatureModule : IFeatureModuleBase
{
    /// <summary>
    /// Maps all endpoints provided by the module in the DI-container.
    /// </summary>
    void MapEndpoints(WebApplication app);

    /// <summary>
    /// Register all dependencies needed by a web module in the DI-container.
    /// </summary>
    void RegisterModule(WebApplicationBuilder builder);
}
