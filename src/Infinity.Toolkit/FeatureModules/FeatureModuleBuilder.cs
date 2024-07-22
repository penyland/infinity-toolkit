namespace Infinity.Toolkit.FeatureModules;

public sealed class FeatureModuleBuilder(WebApplication webApplication)
{
    public WebApplication WebApplication { get; } = webApplication;
}
