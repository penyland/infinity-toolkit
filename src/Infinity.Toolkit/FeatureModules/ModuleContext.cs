namespace Infinity.Toolkit.FeatureModules;

public sealed record ModuleContext
{
    /// <summary>
    /// Gets the host environment.
    /// </summary>
    public required IHostEnvironment Environment { get; init; }

    /// <summary>
    /// Gets the service collection.
    /// </summary>
    public required IServiceCollection Services { get; init; }

    /// <summary>
    /// Gets the configuration.
    /// </summary>
    public required IConfiguration Configuration { get; init; }

    /// <summary>
    /// Optional Logger that can be used when registering module. 
    /// </summary>
    public required ILogger? Logger { get; init; }
}
