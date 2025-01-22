using Azure.Identity;
using Infinity.Toolkit.Messaging;
using Infinity.Toolkit.Messaging.Abstractions;
using Infinity.Toolkit.Messaging.AzureServiceBus;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.AddInfinityMessaging()
    .ConfigureAzureServiceBus(builder =>
    {
        builder
            .AddChannelProducer<WeatherForecast>(options => { options.ChannelName = "toolkit-development"; })
            .AddChannelConsumer<WeatherForecast>(options =>
            {
                options.ChannelName = "toolkit-development";
                options.SubscriptionName = "subscription1";
            });
    },
    options =>
    {
        options.TokenCredential = new AzureCliCredential();
    })
    .MapMessageHandler<WeatherForecast, WeatherForecastMessageHandler>();

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

app.MapGet("/config", ([FromServices] IConfiguration configuration) =>
{
    return (configuration as IConfigurationRoot)?.GetDebugView();
});

app.MapPost("/weatherforecast", async (IChannelProducer<WeatherForecast> channelProducer) =>
{
    await channelProducer.SendAsync(new WeatherForecast(DateOnly.FromDateTime(DateTime.Now), 20, "Sunny"), CancellationToken.None);
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
