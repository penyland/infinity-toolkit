namespace Infinity.Toolkit.Messaging.Diagnostics;

internal static class LogMessages
{
    // Trace
    public const string ProcessingMessage = "Processing message with id {messageId} on {channel}.";
    public const string ProcessingMessageWithType = "Processing message with id: {messageId} on channel: {channel} with type: {eventType}";

    // Debug
    public const string InitializingMessageBus = "Initializing message bus";
    public const string StartingMessageBus = "Starting message bus";
    public const string StoppingMessageBus = "Stopping message bus";
    public const string MessageBusStarted = "Message bus started";
    public const string MessageBusStopped = "Message bus stopped";
    public const string NoEventTypeFoundForMessage = "No event type found for message with id: {messageId} on channel: {channelName}";
    public const string StartingMessageBroker = "Starting message broker {Name}";
    public const string StoppingMessageBroker = "Stopping message broker {Name}";
    public const string StartProcessingChannelStart = "Starting processing channel: {channel}";
    public const string StopProcessingChannelStart = "Stopping processing channel: {channel}";
    public const string EventTypeFound = "Event type {eventType} found.";
    public const string EventTypeNotFound = "Event type {eventType} not found.";

    // Information
    public const string InitializingBus = "Initializing bus: {brokerName}";
    public const string InitializingChannelConsumerWithEventType = "Initializing channel consumer: {channelName} with eventType: {eventType}";
    public const string InitializingChannelConsumer = "Initializing channel consumer: {channelName}.";
    public const string InitializingChannelProducerWithEventType = "Initializing channel producer: {channelName} with eventType: {eventType}";
    public const string InitializingChannelProducer = "Initializing channel producer: {channelName}.";
    public const string MessageBusDelayedStart = "Message bus delayed start for {delay}s";
    public const string MessageBusAutomaticStartDisabled = "Message bus automatic start disabled. Call StartAsync() to start listening for messages.";

    public const string StartProcessingChannelStarted = "Started processing channel: {channel}";
    public const string StopProcessingChannelStopped = "Stopped processing channel: {channel}";

    public const string CouldNotCreateMessageProcessorMethod = "Could not create a message processor for message.";

    public const string DeferredMessageConsumed = "Deferred message with sequence number {sequenceNumber} was consumed on channel: {channel}.";
    public const string DeferredMessagesConsumed = "Deferred messages was consumed on channel: {channel}.";
    public const string DeferredMessage = "Deferred message with sequence number {sequenceNumber} on channel: {channel}.";

    public const string MessageBusVersion = "Infinity.Toolkit.Messaging:{version}";
    public const string MessageBusApplicationName = "ApplicationName\t: {ApplicationName}";
    public const string MessageBusEnvironment = "Environment\t: {Environment}";
    public const string MessageBusCloudEventsSource = "CloudEvents Source: {Source}";
    public const string MessageBusAutoStartListening = "AutoStartListening: {AutoStartListening}";
    public const string MessageBusAutoStartDelay = "AutoStartDelay\t: {AutoStartDelay}";
    public const string MessageBusEventTypeIdentifierPrefix = "EventTypeIdentifierPrefix: {EventTypeIdentifierPrefix}";

    // Warning
    public const string StopProcessingCancellationWarning = "Stop processing was cancelled for channel: {channel} {exception}";

    // Error
    public const string CouldNotConsumeDeferredMessage = "Could not consume deferred message with sequenceNumber: {sequenceNumber} on channel: {channel}.";
    public const string CouldNotConsumeDeferredMessages = "Could not consume deferred messages on channel: {channel}.";
    public const string MessageWithSequenceNumberNotFound = "Message with sequence number {sequenceNumber} was not found.";
    public const string MessageWithSequenceNumberNotFoundWhenCancellingScheduledMessage = "Message with sequence number {sequenceNumber} was not found when cancelling scheduled message.";
    public const string ErrorSendingScheduledMessage = "Error sending scheduled message with sequence number {sequenceNumber}. {errorMessage}";
    public const string ChannelProcessorIsAlreadyRunning = "Channel processor is already running and needs to be stopped to perform this operation.";
    public const string ChannelProcessorIsNotRunning = "Channel processor is not running and needs to be started to perform this operation.";
    public const string ProcessMessageAsyncNotSet = "ProcessMessageAsync should be set prior to start processing.";
    public const string ProcessErrorAsyncNotSet = "ProcessErrorAsync should be set prior to start processing.";

    // Critical
    public const string CouldNotDeserializeJsonToType = "Could not deserialize message to type: {type}";
    public const string CouldNotProcessMessage = "Could not process message on channel {channel}. Message: {message}";
    public const string ChannelOptionsNotFoundMessage = "Channel options for channel {channelName} was not found.";
    public const string ChannelProcessorNotFoundMessage = "Channel processor for channel {channelName} was not found.";
    public const string ApplicationFailedToStart = "Application failed to start.";
    public const string EventTypeNotRegistered = "Event type {eventType} not registered.";
    public const string AnErrorOccurredWhileStartingTheProcessor = "An error occurred while starting the processor on broker {broker} for channel {channel}";
}
