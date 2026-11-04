using Infinity.Toolkit.Handlers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddInMemoryMessageQueue();

builder.Services.AddScoped<IEventHandler<TestIntegrationEvent>, TestIntegrationEventHandler>();
builder.Services.AddScoped<IEventHandler<TestIntegrationEvent>, TestIntegrationEventHandler2>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.MapPost("/publish-event", async (TestIntegrationEvent integrationEvent, IEventBus eventBus) =>
{
    await eventBus.PublishAsync(integrationEvent);
    return Results.Ok();
});

app.Run();

public record TestIntegrationEvent : IEvent
{
    public string EventType => nameof(TestIntegrationEvent);
}

public class TestIntegrationEventHandler(ILogger<TestIntegrationEventHandler> logger) : IEventHandler<TestIntegrationEvent>
{
    public Task HandleAsync(IEventHandlerContext<TestIntegrationEvent> context, CancellationToken cancellationToken = default)
    {
        logger.LogInformation($"Handled event of type: {context.Event.EventType}");
        return Task.CompletedTask;
    }
}

public class TestIntegrationEventHandler2(ILogger<TestIntegrationEventHandler2> logger) : IEventHandler<TestIntegrationEvent>
{
    public Task HandleAsync(IEventHandlerContext<TestIntegrationEvent> context, CancellationToken cancellationToken = default)
    {
        logger.LogInformation($"Handled event of type: {context.Event.EventType}");
        return Task.CompletedTask;
    }
}
