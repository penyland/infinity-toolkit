using Infinity.Toolkit.FeatureModules;

namespace FeatureModulesSample;

internal class WeatherModule : IWebFeatureModule
{
    public IModuleInfo? ModuleInfo { get; } = new FeatureModuleInfo("WeatherModule", "1.0.0");

    public void MapEndpoints(IEndpointRouteBuilder builder)
    {

        var summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        builder.MapGet("/weatherforecast", () =>
        {
            var forecast =  Enumerable.Range(1, 5).Select(index =>
            new WeatherForecast
            (
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                summaries[Random.Shared.Next(summaries.Length)]
            )).ToArray();

            return forecast;
        })
        .WithName("GetWeatherForecast")
        .WithOpenApi();
    }

    public ModuleContext RegisterModule(ModuleContext moduleContext)
    {
        return moduleContext;
    }
}

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
