namespace Infinity.Toolkit.FeatureModules;

public interface IModuleInfo
{
    string? Name { get; init; }

    string? Version { get; init; }
}

public class FeatureModuleInfo(string? name, string? version) : IModuleInfo
{
    public string? Name { get; init; } = name;

    public string? Version { get; init; } = version;
}
