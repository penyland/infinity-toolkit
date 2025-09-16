﻿using Infinity.Toolkit.FeatureModules;

namespace FeatureModulesSample;

public class FeatureModule : Infinity.Toolkit.FeatureModules.FeatureModule
{
    public override IModuleInfo? ModuleInfo { get; } = new FeatureModuleInfo("FeatureModule", "1.0.0");

    public override ModuleContext RegisterModule(ModuleContext moduleContext)
    {
        return base.RegisterModule(moduleContext);
    }
}
