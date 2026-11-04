using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Infinity.Toolkit.Handlers;

public static class InMemoryMessageQueueExtensions
{
    public static IServiceCollection AddInMemoryMessageQueue(this IServiceCollection services)
    {
        services.TryAddSingleton<InMemoryMessageQueue>();
        services.TryAddSingleton<IEventBus, EventBus>();
        services.AddHostedService<EventProcessorJob>();
        services.TryAddScoped<IEventPublisher, EventPublisher>();
        return services;
    }
}

internal class InMemoryMessageQueue
{
    private readonly Channel<IEvent> channel = Channel.CreateUnbounded<IEvent>(new UnboundedChannelOptions { SingleReader = true });

    public ChannelReader<IEvent> Reader => channel.Reader;

    public ChannelWriter<IEvent> Writer => channel.Writer;
}

public interface IEvent
{
    string EventType { get; }
}

public interface IEventBus
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
        where T : IEvent;
}

internal sealed class EventBus(InMemoryMessageQueue messageQueue) : IEventBus
{
    public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
        where T : IEvent
    {
        ArgumentNullException.ThrowIfNull(@event);
        return messageQueue.Writer.WriteAsync(@event, cancellationToken).AsTask();
    }
}

internal sealed class EventProcessorJob(InMemoryMessageQueue messageQueue, IServiceScopeFactory serviceScopeFactory, ILogger<EventProcessorJob> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("EventProcessorJob started.");

        await foreach (var @event in messageQueue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                logger.LogTrace("Processing event of type: {EventType}", @event.EventType);
                await using var scope = serviceScopeFactory.CreateAsyncScope();
                var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

                await EventDispatcher.For(@event.GetType())
                    .DispatchAsync(@event, eventPublisher, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process event {EventType}", @event.EventType);
            }
        }
    }
}

internal interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent;
}

internal sealed class EventPublisher(IServiceProvider serviceProvider, ILogger<EventPublisher> logger) : IEventPublisher
{
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IEvent
    {
        var handlers = serviceProvider.GetServices<IEventHandler<TEvent>>().ToArray();
        if (handlers.Length == 0)
        {
            logger.LogWarning("No handlers found for event type {EventType}", typeof(TEvent).Name);
            return;
        }

        var context = new EventHandlerContext<TEvent>()
        {
            Event = @event,
            Body = BinaryData.FromObjectAsJson(@event)
        };

        foreach (var handler in handlers)
        {
            try
            {
                await handler.HandleAsync(context, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Handler {Handler} failed for event {EventType}",
                    handler.GetType().Name, @event.EventType);
            }
        }
    }
}

public interface IEventHandler<T>
    where T : IEvent
{
    Task HandleAsync(IEventHandlerContext<T> context, CancellationToken cancellationToken = default);
}


internal abstract class EventDispatcher
{
    private static readonly ConcurrentDictionary<Type, EventDispatcher> Cache = new();

    public static EventDispatcher For(Type eventType) =>
        Cache.GetOrAdd(eventType, static t =>
            (EventDispatcher)Activator.CreateInstance(typeof(EventDispatcher<>).MakeGenericType(t))!);

    public abstract Task DispatchAsync(IEvent @event, IEventPublisher publisher, CancellationToken ct);
}

internal sealed class EventDispatcher<TEvent> : EventDispatcher
    where TEvent : IEvent
{
    public override Task DispatchAsync(IEvent @event, IEventPublisher publisher, CancellationToken ct) =>
        publisher.PublishAsync((TEvent)@event, ct);
}

public interface IEventHandlerContext<TEvent> : IHandlerContext
{
    TEvent Event { get; }
}

public class EventHandlerContext<TEvent> : IEventHandlerContext<TEvent>
{
    public required TEvent Event { get; init; }
    public required BinaryData Body { get; init; }
}
