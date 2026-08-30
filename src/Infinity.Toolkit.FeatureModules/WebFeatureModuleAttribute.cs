namespace Infinity.Toolkit.FeatureModules;

/// <summary>
/// Marks a class as a web feature module for compile-time discovery by the source generator.
/// The attributed class must implement <see cref="IWebFeatureModule"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class WebFeatureModuleAttribute : Attribute
{
}
