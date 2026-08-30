namespace Infinity.Toolkit.FeatureModules;

/// <summary>
/// Marks a class as a feature module for compile-time discovery by the source generator.
/// The attributed class must implement <see cref="IFeatureModule"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class FeatureModuleAttribute(string name, string version) : Attribute
{
    public string Name { get; } = name;

    public string Version { get; } = version;
}
