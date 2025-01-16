using Infinity.Toolkit.Messaging;
using Infinity.Toolkit.Messaging.Abstractions;
using Infinity.Toolkit.Messaging.InMemory;

var builder = WebApplication.CreateBuilder(args);

builder.AddInfinityMessaging()
    .ConfigureInMemoryBus(builder =>
    {
        builder
            .AddChannelProducer<WeatherForecast>(options => { options.ChannelName = "weatherforecasts"; })
            .AddChannelConsumer<WeatherForecast>(options =>
            {
                options.ChannelName = "weatherforecasts";
                options.SubscriptionName = "weathersubscription";
            });

        builder
            .AddChannelProducer<WeatherForecast2>()
            .AddChannelConsumer<WeatherForecast2>();
    })
    .MapMessageHandler<WeatherForecast2, WeatherForecastMessageHandler>();


// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapPost("/weatherforecast", async (IChannelProducer<WeatherForecast2> channelProducer) =>
{
    await channelProducer.SendAsync(new WeatherForecast2(DateOnly.FromDateTime(DateTime.Now), 20, "Sunny"), CancellationToken.None);
    return Results.Accepted();
});

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

internal record WeatherForecastMessageHandler : IMessageHandler<WeatherForecast2>
{
    public Task Handle(IMessageHandlerContext<WeatherForecast2> context, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Received message: {context.Message?.Summary} on Channel ${context.ChannelName}");
        return Task.CompletedTask;
    }
}

internal record WeatherForecast2(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
