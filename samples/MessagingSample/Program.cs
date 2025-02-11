using Infinity.Toolkit.Messaging;
using Infinity.Toolkit.Messaging.Abstractions;
using Infinity.Toolkit.Messaging.InMemory;
using Microsoft.AspNetCore.Mvc;

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

        builder.AddChannelProducer("generic", options => { options.ChannelName = "generic"; });
        builder.AddChannelConsumer("generic", options =>
        {
            options.ChannelName = "generic";
            options.SubscriptionName = "genericsubscription";
        });

        builder.AddDefaultChannelProducer();
        builder.AddDefaultChannelConsumer();
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

app.MapPost("/weatherforecast", async ([FromServices]IChannelProducer<WeatherForecast> channelProducer) =>
{
    await channelProducer.SendAsync(new WeatherForecast(DateOnly.FromDateTime(DateTime.Now), 20, "Sunny"), CancellationToken.None);
    return Results.Accepted();
});

app.MapPost("/generic", async ([FromKeyedServices("generic")] IChannelProducer channelProducer) =>
{
    await channelProducer.SendAsync(new { Message = "Hello, World!" }, CancellationToken.None);
    return Results.Accepted();
});

app.MapPost("/default", async (IChannelProducer2 channelProducer) =>
{
    await channelProducer.SendAsync<WeatherForecast>(new WeatherForecast(DateOnly.FromDateTime(DateTime.Now), 20, "Cloudy"));
    return Results.Accepted();
});

app.MapPost("/default2", async (IChannelProducer2 channelProducer) =>
{
    await channelProducer.SendAsync((object)(new { Message = "Hello, World!" }));
    //await channelProducer.SendAsync(new WeatherForecast(DateOnly.FromDateTime(DateTime.Now), 20, "Rainy"));
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
