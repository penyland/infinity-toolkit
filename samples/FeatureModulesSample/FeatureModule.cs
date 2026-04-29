using Infinity.Toolkit.FeatureModules;

namespace FeatureModulesSample;

public class FeatureModule : Infinity.Toolkit.FeatureModules.FeatureModule
{
    public override IModuleInfo ModuleInfo { get; } = new FeatureModuleInfo("CustomFeatureModuleInfo", "1.0.0");
}
