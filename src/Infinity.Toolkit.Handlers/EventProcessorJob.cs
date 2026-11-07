using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infinity.Toolkit.Handlers;

internal sealed class EventProcessorJob(InMemoryMessageQueue messageQueue, IServiceScopeFactory serviceScopeFactory, ILogger<EventProcessorJob> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("EventProcessorJob started.");

        await foreach (var envelope in messageQueue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                logger.LogTrace("Processing event of type: {EventType}", envelope.EventType);
                await using var scope = serviceScopeFactory.CreateAsyncScope();
                var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

                await EventDispatcher.For(envelope.Event.GetType())
                    .DispatchAsync(envelope, eventPublisher, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process event {EventType}", envelope.EventType);
            }
        }
    }
}
