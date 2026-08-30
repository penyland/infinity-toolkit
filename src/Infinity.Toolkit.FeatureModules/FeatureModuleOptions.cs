namespace Infinity.Toolkit.FeatureModules;

/// <summary>
/// Enables the possibility to exclude feature modules from loading at startup.
///
/// Exclusions can be configured in code using <see cref="ExcludedModules"/> with
/// concrete <see cref="Type"/> values, or in appsettings.json using
/// <see cref="ExcludedModuleNames"/>.
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
    public List<Type> ExcludedModules { get; } = [];

    [ConfigurationKeyName("ExcludedModules")]
    public List<string> ExcludedModuleNames { get; set; } = [];

    public List<Assembly> AdditionalAssemblies { get; } = [];
}
