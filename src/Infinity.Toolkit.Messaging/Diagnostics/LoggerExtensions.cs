using Infinity.Toolkit.Messaging.Diagnostics;

namespace Infinity.Toolkit.Messaging;

internal static partial class LoggerExtensions
{
    // LogLevel.Trace
    [LoggerMessage(EventId = 0, Level = LogLevel.Trace, Message = LogMessages.ProcessingMessage)]
    public static partial void ProcessingMessage(this ILogger logger, string messageId, string channel);

    [LoggerMessage(EventId = 1, Level = LogLevel.Trace, Message = LogMessages.ProcessingMessageWithType)]
    public static partial void ProcessingMessageWithType(this ILogger logger, string messageId, string channel, string eventType);

    // LogLevel.Debug
    [LoggerMessage(EventId = 100, Level = LogLevel.Debug, Message = LogMessages.InitializingMessageBus)]
    public static partial void InitializingMessageBus(this ILogger logger);

    [LoggerMessage(EventId = 101, Level = LogLevel.Debug, Message = LogMessages.StartingMessageBus)]
    public static partial void StartingMessageBus(this ILogger logger);

    [LoggerMessage(EventId = 102, Level = LogLevel.Debug, Message = LogMessages.StoppingMessageBus)]
    public static partial void StoppingMessageBus(this ILogger logger);

    [LoggerMessage(EventId = 103, Level = LogLevel.Debug, Message = LogMessages.MessageBusStarted)]
    public static partial void MessageBusStarted(this ILogger logger);

    [LoggerMessage(EventId = 104, Level = LogLevel.Debug, Message = LogMessages.MessageBusStopped)]
    public static partial void MessageBusStopped(this ILogger logger);

    [LoggerMessage(EventId = 105, Level = LogLevel.Debug, Message = LogMessages.NoEventTypeFoundForMessage)]
    public static partial void NoEventTypeFoundForMessage(this ILogger logger, string messageId, string channelName);

    [LoggerMessage(EventId = 106, Level = LogLevel.Debug, Message = LogMessages.StartingMessageBroker)]
    public static partial void StartingMessageBroker(this ILogger logger, string name);

    [LoggerMessage(EventId = 107, Level = LogLevel.Debug, Message = LogMessages.StoppingMessageBroker)]
    public static partial void StoppingMessageBroker(this ILogger logger, string name);

    [LoggerMessage(EventId = 108, Level = LogLevel.Debug, Message = LogMessages.StartProcessingChannelStart)]
    public static partial void StartProcessingChannelStart(this ILogger logger, string channel);

    [LoggerMessage(EventId = 109, Level = LogLevel.Debug, Message = LogMessages.StopProcessingChannelStart)]
    public static partial void StopProcessingChannelStart(this ILogger logger, string channel);

    [LoggerMessage(EventId = 110, Level = LogLevel.Debug, Message = LogMessages.EventTypeFound)]
    public static partial void EventTypeFound(this ILogger logger, string eventType);

    [LoggerMessage(EventId = 111, Level = LogLevel.Debug, Message = LogMessages.EventTypeNotFound)]
    public static partial void EventTypeNotFound(this ILogger logger, string eventType);

    // LogLevel.Information
    [LoggerMessage(EventId = 200, Level = LogLevel.Information, Message = LogMessages.MessageBusDelayedStart)]
    public static partial void MessageBusDelayedStart(this ILogger logger, double delay);

    [LoggerMessage(EventId = 201, Level = LogLevel.Information, Message = LogMessages.MessageBusAutomaticStartDisabled)]
    public static partial void MessageBusAutomaticStartDisabled(this ILogger logger);

    [LoggerMessage(EventId = 202, Level = LogLevel.Information, Message = LogMessages.MessageBusVersion)]
    public static partial void MessageBusVersion(this ILogger logger, Version? version);

    [LoggerMessage(EventId = 203, Level = LogLevel.Information, Message = LogMessages.MessageBusApplicationName)]
    public static partial void MessageBusApplicationName(this ILogger logger, string applicationName);

    [LoggerMessage(EventId = 204, Level = LogLevel.Information, Message = LogMessages.MessageBusEnvironment)]
    public static partial void MessageBusEnvironment(this ILogger logger, string environment);

    [LoggerMessage(EventId = 205, Level = LogLevel.Information, Message = LogMessages.MessageBusCloudEventsSource)]
    public static partial void MessageBusCloudEventsSource(this ILogger logger, Uri source);

    [LoggerMessage(EventId = 206, Level = LogLevel.Information, Message = LogMessages.MessageBusAutoStartListening)]
    public static partial void MessageBusAutoStartListening(this ILogger logger, bool autoStartListening);

    [LoggerMessage(EventId = 207, Level = LogLevel.Information, Message = LogMessages.MessageBusAutoStartDelay)]
    public static partial void MessageBusAutoStartDelay(this ILogger logger, TimeSpan autoStartDelay);

    [LoggerMessage(EventId = 208, Level = LogLevel.Information, Message = LogMessages.MessageBusEventTypeIdentifierPrefix)]
    public static partial void MessageBusEventTypeIdentifierPrefix(this ILogger logger, string eventTypeIdentifierPrefix);

    [LoggerMessage(EventId = 209, Level = LogLevel.Information, Message = LogMessages.InitializingBus)]
    public static partial void InitializingBus(this ILogger logger, string brokerName);

    [LoggerMessage(EventId = 210, Level = LogLevel.Information, Message = LogMessages.InitializingChannelConsumerWithEventType)]
    public static partial void InitializingChannelConsumerWithEventType(this ILogger logger, string channelName, string eventType);

    [LoggerMessage(EventId = 211, Level = LogLevel.Information, Message = LogMessages.InitializingChannelConsumer)]
    public static partial void InitializingChannelConsumer(this ILogger logger, string channelName);

    [LoggerMessage(EventId = 212, Level = LogLevel.Information, Message = LogMessages.StartProcessingChannelStarted)]
    public static partial void StartProcessingChannelStarted(this ILogger logger, string channel);

    [LoggerMessage(EventId = 213, Level = LogLevel.Information, Message = LogMessages.StopProcessingChannelStopped)]
    public static partial void StopProcessingChannelStopped(this ILogger logger, string channel);

    [LoggerMessage(EventId = 214, Level = LogLevel.Information, Message = LogMessages.DeferredMessageConsumed)]
    public static partial void DeferredMessageConsumed(this ILogger logger, long sequenceNumber, string channel);

    [LoggerMessage(EventId = 215, Level = LogLevel.Information, Message = LogMessages.DeferredMessagesConsumed)]
    public static partial void DeferredMessagesConsumed(this ILogger logger, string channel);

    [LoggerMessage(EventId = 216, Level = LogLevel.Information, Message = LogMessages.DeferredMessage)]
    public static partial void DeferredMessage(this ILogger logger, long sequenceNumber, string channel);

    [LoggerMessage(EventId = 217, Level = LogLevel.Information, Message = LogMessages.InitializingChannelProducerWithEventType)]
    public static partial void InitializingChannelProducerWithEventType(this ILogger logger, string channelName, string eventType);

    [LoggerMessage(EventId = 218, Level = LogLevel.Information, Message = LogMessages.InitializingChannelProducer)]
    public static partial void InitializingChannelProducer(this ILogger logger, string channelName);

    // LogLevel.Warning
    [LoggerMessage(EventId = 300, Level = LogLevel.Warning, Message = LogMessages.StopProcessingCancellationWarning)]
    public static partial void StopProcessingCancellationWarning(this ILogger logger, string channel, string exception);

    // LogLevel.Error
    [LoggerMessage(EventId = 400, Level = LogLevel.Error, Message = LogMessages.CouldNotConsumeDeferredMessage)]
    public static partial void CouldNotConsumeDeferredMessage(this ILogger logger, long sequenceNumber, string channel);

    [LoggerMessage(EventId = 401, Level = LogLevel.Error, Message = LogMessages.CouldNotConsumeDeferredMessages)]
    public static partial void CouldNotConsumeDeferredMessages(this ILogger logger, string channel);

    [LoggerMessage(EventId = 402, Level = LogLevel.Error, Message = LogMessages.MessageWithSequenceNumberNotFound)]
    public static partial void MessageWithSequenceNumberNotFound(this ILogger logger, long sequenceNumber);

    [LoggerMessage(EventId = 403, Level = LogLevel.Error, Message = LogMessages.MessageWithSequenceNumberNotFoundWhenCancellingScheduledMessage)]
    public static partial void MessageWithSequenceNumberNotFoundWhenCancellingScheduledMessage(this ILogger logger, long sequenceNumber);

    [LoggerMessage(EventId = 404, Level = LogLevel.Error, Message = LogMessages.ErrorSendingScheduledMessage)]
    public static partial void ErrorSendingScheduledMessage(this ILogger logger, long sequenceNumber, string errorMessage);

    // LogLevel.Critical
    [LoggerMessage(EventId = 500, Level = LogLevel.Critical, Message = LogMessages.CouldNotProcessMessage)]
    public static partial void CouldNotProcessMessage(this ILogger logger, string channel, string message);

    [LoggerMessage(EventId = 501, Level = LogLevel.Critical, Message = LogMessages.ChannelOptionsNotFoundMessage)]
    public static partial void ChannelOptionsNotFound(this ILogger logger, string channelName);

    [LoggerMessage(EventId = 502, Level = LogLevel.Critical, Message = LogMessages.ChannelProcessorNotFoundMessage)]
    public static partial void ChannelProcessorNotFound(this ILogger logger, string channelName);

    [LoggerMessage(EventId = 503, Level = LogLevel.Critical, Message = LogMessages.CouldNotDeserializeJsonToType)]
    public static partial void CouldNotDeserializeToType(this ILogger logger, string type);

    [LoggerMessage(EventId = 504, Level = LogLevel.Critical, Message = LogMessages.ApplicationFailedToStart)]
    public static partial void ApplicationFailedToStart(this ILogger logger);

    [LoggerMessage(EventId = 505, Level = LogLevel.Critical, Message = LogMessages.EventTypeNotRegistered)]
    public static partial void EventTypeNotRegistered(this ILogger logger, string eventType);

    [LoggerMessage(EventId = 506, Level = LogLevel.Critical, Message = LogMessages.AnErrorOccurredWhileStartingTheProcessor)]
    public static partial void AnErrorOccurredWhileStartingTheProcessor(this ILogger logger, string broker, string channel);    

    [LoggerMessage(EventId = 507, Level = LogLevel.Critical, Message = LogMessages.CouldNotCreateMessageProcessorMethod)]
    public static partial void CouldNotCreateMessageProcessorMethod(this ILogger logger);
}
