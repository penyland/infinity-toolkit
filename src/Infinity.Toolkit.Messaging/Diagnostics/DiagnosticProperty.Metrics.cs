namespace Infinity.Toolkit.Messaging.Diagnostics;

public static partial class DiagnosticProperty
{
    public static class Metrics
    {
        // OTEL Semantic Conventions
        public const string ClientConsumedMessages = "messaging.client.consumed.messages";
        public const string ClientOperationDuration = "messaging.client.operation.duration";
        public const string ClientPublishedMessages = "messaging.client.published.messages";
        public const string MessagingProcessDuration = "messaging.process.duration";

        public const string MessagesDeadlettered = "messaging.messages.deadletter.count";
        public const string MessagesDeferred = "messaging.messages.deferred.count";
        public const string MessagesFailed = "messaging.messages.failed.count";

        // MessagBus Metrics
        public const string MessageHandlerElapsedTime = "messaging.client.operation.message_handler.duration";
    }
}
