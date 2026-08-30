namespace Infinity.Toolkit.FeatureModules;

/// <summary>
/// Marks a class as a feature module for compile-time discovery by the source generator.
/// The attributed class must implement <see cref="IFeatureModule"/> or <see cref="IWebFeatureModule"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class FeatureModuleAttribute : Attribute
{
}
