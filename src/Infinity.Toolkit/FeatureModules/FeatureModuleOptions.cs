using System.Reflection;

namespace Infinity.Toolkit.FeatureModules;

/// <summary>
/// Enables the possibility to exclude feature modules from loading at startup.
///
/// Can be configured by adding the following to appsettings.json. Or by using on of the overloads for AddFeatureModules.
///
/// Example:
///
/// "FeatureModules": {
///    "ExcludedModules": [
///      "ErrorModule"
///    ]
/// }
///
/// </summary>
public record FeatureModuleOptions
{
    public List<string> ExcludedModules { get; set; } = [];

    public List<Assembly> AdditionalAssemblies { get; } = [];
}
