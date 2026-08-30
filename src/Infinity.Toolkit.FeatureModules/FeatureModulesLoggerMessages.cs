namespace Infinity.Toolkit.FeatureModules;

internal static partial class FeatureModulesLoggerMessages
{
    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Debug,
        Message = "Scanning assemblies for feature modules...",
        EventName = "Scanning")]
    internal static partial void ScanningAssembliesForFeatureModules(this ILogger logger);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Discovered total {moduleCount} feature modules. " +
                  "{compileTimeModule}/{runtimeModule} (compile time/runtime)",
        EventName = "ModulesFound")]
    internal static partial void DiscoveredTotalModules(
        this ILogger logger,
        int moduleCount,
        int compileTimeModule,
        int runtimeModule);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Information,
        Message = "Registering feature module: {module} - v{version}",
        EventName = "RegisteringModules")]
    internal static partial void RegisteringFeatureModule(
        this ILogger logger,
        string? module,
        string? version);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Error,
        Message = "Module {module} does not implement IFeatureModule or " +
                  "IWebFeatureModule.",
        EventName = "InvalidModuleType")]
    internal static partial void ModuleDoesNotImplementFeatureModule(
        this ILogger logger,
        string? module);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Debug,
        Message = "Registering feature modules completed.",
        EventName = "ScanningComplete")]
    internal static partial void RegisteringFeatureModulesCompleted(this ILogger logger);

    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Debug,
        Message = "Discovered {count} modules at compile-time.",
        EventName = "UsingGeneratedModules")]
    internal static partial void DiscoveredCompileTimeModules(this ILogger logger, int count);

    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Debug,
        Message = "Mapping endpoints for {module}",
        EventName = "MappingEndpoints")]
    internal static partial void MappingEndpointsForModule(
        this ILogger logger,
        string module);

    [LoggerMessage(
        EventId = 1006,
        Level = LogLevel.Debug,
        Message = "Failed to load generated module registry: {message}",
        EventName = "GeneratedRegistryError")]
    internal static partial void FailedToLoadGeneratedModuleRegistry(
        this ILogger logger,
        string? message);

    [LoggerMessage(
        EventId = 1007,
        Level = LogLevel.Debug,
        Message = "Discovered {count} modules by reflection.",
        EventName = "ReflectionModulesFound")]
    internal static partial void DiscoveredReflectionModules(this ILogger logger, int count);

    [LoggerMessage(
        EventId = 1008,
        Level = LogLevel.Debug,
        Message = "Failed to load generated module metadata registry: {message}",
        EventName = "GeneratedMetadataRegistryError")]
    internal static partial void FailedToLoadGeneratedModuleMetadataRegistry(
        this ILogger logger,
        string? message);

    [LoggerMessage(
        EventId = 1009,
        Level = LogLevel.Warning,
        Message = "Configured excluded module '{moduleName}' was not found.",
        EventName = "ExcludedModuleNotFound")]
    internal static partial void ConfiguredExcludedModuleNotFound(
        this ILogger logger,
        string moduleName);

    [LoggerMessage(
        EventId = 1010,
        Level = LogLevel.Warning,
        Message = "Configured excluded module '{moduleName}' matched " +
                  "{matchCount} modules.",
        EventName = "ExcludedModuleAmbiguous")]
    internal static partial void ConfiguredExcludedModuleAmbiguous(
        this ILogger logger,
        string moduleName,
        int matchCount);

    [LoggerMessage(
        EventId = 4000,
        Level = LogLevel.Warning,
        Message = "No feature modules registered.",
        EventName = "NoModulesRegistered")]
    internal static partial void NoFeatureModulesRegistered(this ILogger logger);

    [LoggerMessage(
        EventId = 5000,
        Level = LogLevel.Error,
        Message = "Failed to register feature modules.",
        EventName = "ScanningFailed")]
    internal static partial void FailedToRegisterFeatureModules(
        this ILogger logger,
        Exception exception);
}
