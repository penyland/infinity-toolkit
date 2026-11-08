using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;

namespace Infinity.Toolkit.Handlers;

public static class InMemoryMessageQueueExtensions
{
    public static IServiceCollection AddEventBus(this IServiceCollection services)
    {
        services.TryAddSingleton<InMemoryMessageQueue>();
        services.TryAddSingleton<IEventBus, EventBus>();
        services.AddHostedService<EventProcessorJob>();
        services.TryAddScoped<IEventPublisher, EventPublisher>();
        return services;
    }

    public static IServiceCollection AddEventHandler<TEvent, THandler>(this IServiceCollection services)
        where TEvent : class, IEvent
        where THandler : class, IEventHandler<TEvent>
    {
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IEventHandler<TEvent>, THandler>());
        return services;
    }
}

// Separate reader and writer so we can exchange them for other implementations such as writing to a database or message bus. This allows us to swap out the in-memory implementation for a more robust one in production.
internal class InMemoryMessageQueue
{
    private readonly Channel<Envelope> channel = Channel.CreateUnbounded<Envelope>(new UnboundedChannelOptions { SingleReader = true });

    public ChannelReader<Envelope> Reader => channel.Reader;

    public ChannelWriter<Envelope> Writer => channel.Writer;
}

public interface IEvent;

public interface IEventBus
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
        where T : IEvent;
}

internal sealed class EventBus(InMemoryMessageQueue messageQueue, TimeProvider timeProvider) : IEventBus
{
    public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
        where T : IEvent
    {
        ArgumentNullException.ThrowIfNull(@event);
        var envelope = new Envelope
        {
            Payload = new BinaryData(@event),
            Type = @event.GetType(),
            EnqueuedTimeUtc = timeProvider.GetUtcNow(),
            MessageId = Guid.CreateVersion7().ToString(),
            ParentContext = Activity.Current?.Context ?? default,
        };

        return messageQueue.Writer.WriteAsync(envelope, cancellationToken).AsTask();
    }
}

internal interface IEventPublisher
{
    Task PublishAsync<TEvent>(Envelope envelope, CancellationToken cancellationToken = default)
        where TEvent : IEvent;
}

internal sealed class EventPublisher(IServiceProvider serviceProvider, ILogger<EventPublisher> logger) : IEventPublisher
{
    public async Task PublishAsync<TEvent>(Envelope envelope, CancellationToken cancellationToken = default)
        where TEvent : IEvent
    {
        var handlers = serviceProvider.GetServices<IEventHandler<TEvent>>().ToArray();
        if (handlers.Length == 0)
        {
            logger.LogWarning("No handlers found for event type {EventType}", envelope.Type);
            return;
        }

        var context = new EventHandlerContext<TEvent>()
        {
            Data = envelope.Payload.ToObjectFromJson<TEvent>()!,
            Payload = envelope.Payload,
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
                    handler.GetType().Name, envelope.Type);
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

    public abstract Task DispatchAsync(Envelope envelope, IEventPublisher publisher, CancellationToken cancellationToken);
}

internal sealed class EventDispatcher<TEvent> : EventDispatcher
    where TEvent : IEvent
{
    public override Task DispatchAsync(Envelope envelope, IEventPublisher publisher, CancellationToken cancellationToken) =>
        publisher.PublishAsync<TEvent>(envelope, cancellationToken);
}

public interface IEventHandlerContext<TEvent> : IHandlerContext
{
    TEvent Data { get; }

    IDictionary<string, object?> ApplicationProperties { get; init; }
}

public class EventHandlerContext<TEvent> : IEventHandlerContext<TEvent>
{
    public required TEvent Data { get; init; }

    public required BinaryData Payload { get; init; }

    public IDictionary<string, object?> ApplicationProperties { get; init; } = new Dictionary<string, object?>(20);
}
