using Infinity.Toolkit.FeatureModules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using System.Reflection;
using System.Runtime.InteropServices;

namespace FeatureModulesSample.Module1;

public class SampleModule1 : IWebFeatureModule
{
    public IModuleInfo? ModuleInfo { get; } = new FeatureModuleInfo("SampleModule1", "1.0.0");

    public void MapEndpoints(IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/info")
            .WithOpenApi()
            .WithTags("Info");

        group.MapGet("/systeminfo", GetSystemInfo)
            .WithName("SystemInfo")
            .WithDisplayName("Get system info");
    }

    public ModuleContext RegisterModule(ModuleContext moduleContext)
    {
        return moduleContext;
    }

    private static JsonHttpResult<Response> GetSystemInfo(IWebHostEnvironment webHostEnvironment)
    {
        var processorArchitecture = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "64-bit",
            Architecture.X86 => "32-bit",
            Architecture.Arm => "ARM",
            Architecture.Arm64 => "ARM64",
            _ => "Unknown"
        };

        return TypedResults.Json<Response>(new Response
        {
            Name = Assembly.GetEntryAssembly()?.GetName().Name ?? webHostEnvironment.ApplicationName ?? "Name",
            Version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "0.0.0",
            DateTime = DateTimeOffset.Now.UtcDateTime,
            Environment = webHostEnvironment.EnvironmentName,
            FrameworkVersion = Environment.Version.ToString(),
            OSVersion = Environment.OSVersion.ToString(),
            BuildDate = File.GetLastWriteTime(Assembly.GetEntryAssembly()!.Location).ToString("yyyy-MM-dd HH:mm:ss"),
            Host = Environment.MachineName,
            ProcessorArchitecture = processorArchitecture,
            FrameworkDescription = RuntimeInformation.FrameworkDescription,
            RuntimeIdentifier = RuntimeInformation.RuntimeIdentifier,
            OSArchitecture = RuntimeInformation.OSArchitecture.ToString(),
            OSDescription = RuntimeInformation.OSDescription
        });
    }

    private record Response()
    {
        public string? Name { get; init; }

        public string? Version { get; init; }

        public DateTimeOffset DateTime { get; init; }

        public string? Environment { get; init; }

        public string? FrameworkVersion { get; init; }

        public string? OSVersion { get; init; }

        public string? BuildDate { get; init; }

        public string? Host { get; init; }

        public string? ProcessorArchitecture { get; init; }

        public string? FrameworkDescription { get; init; }

        public string? RuntimeIdentifier { get; init; }

        public string? OSArchitecture { get; init; }

        public string? OSDescription { get; init; }
    }
}
