using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
}
