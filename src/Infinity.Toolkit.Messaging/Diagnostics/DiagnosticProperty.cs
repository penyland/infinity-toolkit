namespace Infinity.Toolkit.Messaging.Diagnostics;

public static partial class DiagnosticProperty
{
    public const string ActivitySourceName = "Infinity.Toolkit.Messaging";

    public const string ActivityId = "Activity-Id";
    public const string DiagnosticId = "Diagnostic-Id";
    public const string TraceParent = "traceparent";
    public const string TraceState = "tracestate";

    // OTEL Semantic Conventions
    public const string MessagingDestinationName = "messaging.destination.name";
    public const string MessagingDestinationSubscriptionName = "messaging.destination.subscription_name";
    public const string MessagingMessageId = "messaging.message.id";
    public const string MessagingOperation = "messaging.operation"; // For compatibility with Aspire Dashboard
    public const string MessagingOperationName = "messaging.operation.name";
    public const string MessagingOperationType = "messaging.operation.type";
    public const string MessagingSystem = "messaging.system";
    public const string OperationConsume = "consume";
    public const string OperationDefer = "defer";
    public const string OperationProcess = "process";
    public const string OperationPublish = "publish";
    public const string OperationSend = "send";
    public const string OperationReceive = "receive";
    public const string OperationHandle = "handle";
    public const string OperationCancel = "cancel";
    public const string ServerAddress = "server.address";
    public const string ServerPort = "server.port";

    public const string ErrorType = "error.type";

    // MessageBus Diagnostic Properties
    public const string MessagingTransport = "messaging.transport";
    public const string MessagingConsumerInvokingHandler = "messaging.consumer.invoking_handler";
    public const string MessagingConsumerInvokedHandler = "messaging.consumer.invoked_handler";
    public const string MessagingConsumerMessageHandler = "messaging.consumer.handler";
    public const string MessagingConsumerReceiveMessage = "messaging.consumer.receive_message";

    public const string MessagingProducerSendingMessage = "messaging.producer.sending_message";
    public const string MessagingProducerSendMessage = "messaging.producer.send_message";
    public const string MessagingProducerDeferMessage = "messaging.producer.defer_message";

    public const string MessageBusSystem = "messagebus.system";
    public const string MessageBusTraceParent = "messagebus.traceparent";
    public const string MessageBusTraceState = "messagebus.tracestate";
    public const string MessageBusMessageHandler = "messagebus.messagehandler.name";
    public const string MessageBusMessageType = "messagebus.message.type";
    public const string MessageBusSequenceNumber = "messagebus.message.sequenceNumber";

    public const string MessageTypeUndefined = "undefined";
}
