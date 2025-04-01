using Infinity.Toolkit.LogFormatter;
using Infinity.Toolkit.Messaging;
using Infinity.Toolkit.Messaging.Abstractions;
using Infinity.Toolkit.Messaging.InMemory;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddCodeThemeConsoleFormatter();

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

        builder.AddKeyedChannelProducer("generic", options => { options.ChannelName = "generic"; });
        builder.AddKeyedChannelConsumer("generic", options =>
        {
            options.ChannelName = "generic";
            options.SubscriptionName = "genericsubscription";
        });

        builder.AddKeyedChannelProducer<WeatherForecast>("key1");
        builder.AddDefaultChannelProducer();
    })
    .MapMessageHandler<WeatherForecast, WeatherForecastMessageHandler>()
    .MapMessageHandler<DefaultMessageHandler>();


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

app.MapPost("/1", async ([FromServices] IChannelProducer<WeatherForecast> channelProducer) =>
{
    await channelProducer.SendAsync(new WeatherForecast(DateOnly.FromDateTime(DateTime.Now), 20, "Sunny"), CancellationToken.None);
    return Results.Accepted();
});

// Message received by DefaultMessageHandler
app.MapPost("/2", async ([FromKeyedServices("generic")] IChannelProducer channelProducer) =>
{
    await channelProducer.SendAsync(new { Message = "Hello, World!" }, CancellationToken.None);
    return Results.Accepted();
});

app.MapPost("/3", async ([FromKeyedServices("key1")] IChannelProducer<WeatherForecast> channelProducer) =>
{
    await channelProducer.SendAsync(new WeatherForecast(DateOnly.FromDateTime(DateTime.Now), 20, "Cloudy"), CancellationToken.None);
    return Results.Accepted();
});

app.MapPost("/4", async ([FromServices] IChannelProducer channelProducer) =>
{
    await channelProducer.SendAsync(new WeatherForecast(DateOnly.FromDateTime(DateTime.Now), 20, "Cloudy"), CancellationToken.None);
    return Results.Accepted();
});

app.MapPost("/5", async (IDefaultChannelProducer channelProducer) =>
{
    await channelProducer.SendAsync<WeatherForecast>(new WeatherForecast(DateOnly.FromDateTime(DateTime.Now), 20, "Cloudy"));
    return Results.Accepted();
});

app.MapPost("/6", async ([FromServices] IDefaultChannelProducer channelProducer) =>
{
    //await channelProducer.SendAsync((object)(new { Message = "Hello, World!" }));
    await channelProducer.SendAsync(new WeatherForecast(DateOnly.FromDateTime(DateTime.Now), 20, "Rainy"));
    return Results.Accepted();
});

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

internal record WeatherForecastMessageHandler : IMessageHandler<WeatherForecast>
{
    public Task Handle(IMessageHandlerContext<WeatherForecast> context, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Received message: {context.Message?.Summary} on Channel {context.ChannelName}");
        return Task.CompletedTask;
    }
}

public class DefaultMessageHandler(ILogger<DefaultMessageHandler> logger) : IMessageHandler
{
    private readonly ILogger<DefaultMessageHandler> logger = logger;

    public Task Handle(IMessageHandlerContext context, CancellationToken cancellationToken)
    {
        logger.LogInformation("DefaultMessageHandler: Raw message received with sequence number {Message}", context.SequenceNumber);
        return Task.CompletedTask;
    }
}
