using Infinity.Toolkit.Messaging.Abstractions;
using Infinity.Toolkit.Messaging.Diagnostics;

namespace Infinity.Toolkit.Messaging;

internal class MessagingExceptionHandler(IServiceProvider serviceProvider, Metrics messageBusMetrics, ILoggerFactory loggerFactory)
{
    private readonly IServiceProvider serviceProvider = serviceProvider;
    private readonly Metrics messageBusMetrics = messageBusMetrics;
    private readonly ILogger<MessagingExceptionHandler> logger = loggerFactory.CreateLogger<MessagingExceptionHandler>();

    public async Task HandleExceptionAsync(MessageBusException exception)
    {
        var errorHandlers = serviceProvider.GetServices<IMessagingExceptionHandler>();
        var handled = false;
        foreach (var errorHandler in errorHandlers)
        {
            handled = await errorHandler.TryHandleAsync(exception.ChannelName, exception.InnerException ?? exception);
            if (handled)
            {
                break;
            }
        }

        messageBusMetrics.RecordMessageConsumed(exception.Broker, exception.ChannelName, errortype: exception.Message);

        if (!handled)
        {
            logger?.CouldNotProcessMessage(exception.ChannelName, exception.Message);
            throw exception;
        }
    }
}
