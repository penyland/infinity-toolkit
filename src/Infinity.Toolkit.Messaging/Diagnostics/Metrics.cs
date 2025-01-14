using System.Diagnostics.Metrics;

namespace Infinity.Toolkit.Messaging.Diagnostics;

internal sealed class Metrics
{
    private readonly Meter meter;

    public Metrics(IMeterFactory meterFactory)
    {
        var options = new MeterOptions(DiagnosticProperty.ActivitySourceName)
        {
            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty
        };

        meter = meterFactory.Create(options);

        // OTEL Semantic Conventions
        MessagingClientConsumedMessages = meter.CreateCounter<int>(DiagnosticProperty.Metrics.ClientConsumedMessages, "messages", "Number of messages that were delivered to the application.");
        MessagingClientOperationDuration = meter.CreateHistogram<double>(DiagnosticProperty.Metrics.ClientOperationDuration, "ms", "Total message processing duration.");
        MessagingProcessDuration = meter.CreateHistogram<double>(DiagnosticProperty.Metrics.MessagingProcessDuration, "ms", "Duration of processing operation.");
        MessagingClientPublishedMessages = meter.CreateCounter<int>(DiagnosticProperty.Metrics.ClientPublishedMessages, "messages", "Number of messages producer attempted to publish to the broker.");

        // MessageBus Metrics
        MessageHandlerElapsedTime = meter.CreateHistogram<double>(DiagnosticProperty.Metrics.MessageHandlerElapsedTime, "ms", "Message handler elapsed time.");
    }

    // Number of total messages processed
    private Counter<int> MessagingClientConsumedMessages { get; }

    // Number of total messages sent
    private Counter<int> MessagingClientPublishedMessages { get; }

    // Duration of messaging operation initiated by a producer or consumer client.
    private Histogram<double> MessagingClientOperationDuration { get; }

    // Elapsed time for execution time of message handlers.
    private Histogram<double> MessageHandlerElapsedTime { get; }

    // Duration of processing operation.
    private Histogram<double> MessagingProcessDuration { get; }

    public void RecordMessageConsumed<TMessage>(string system, string channel, int delta = 1, string operationName = DiagnosticProperty.OperationConsume, string? errortype = default) =>
        RecordMessageConsumed(system, channel, delta, operationName, typeof(TMessage).Name, errortype);

    public void RecordMessageConsumed(string system, string channel, int delta = 1, string operationName = DiagnosticProperty.OperationConsume, string? messageType = default, string? errortype = default)
    {
        var tagList = new TagList()
        {
            { DiagnosticProperty.MessagingTransport, system },
            { DiagnosticProperty.MessagingSystem, system },
            { DiagnosticProperty.MessagingOperationName, operationName },
            { DiagnosticProperty.MessagingOperationType, DiagnosticProperty.OperationConsume },
            { DiagnosticProperty.MessagingDestinationName, channel }
        };

        if (messageType is not null)
        {
            tagList.Add(DiagnosticProperty.MessageBusMessageType, messageType);
        }

        if (errortype is not null)
        {
            tagList.Add(DiagnosticProperty.ErrorType, errortype);
        }

        MessagingClientConsumedMessages.Add(delta, tagList);
    }

    public void RecordMessagingClientOperationDuration<TMessage>(double value, string system, string channel) =>
        MessagingClientOperationDuration.Record(
            value,
            new TagList()
            {
                { DiagnosticProperty.MessagingTransport, system },
                { DiagnosticProperty.MessagingSystem, system },
                { DiagnosticProperty.MessagingDestinationName, channel },
                { DiagnosticProperty.MessagingOperationName, DiagnosticProperty.OperationConsume },
                { DiagnosticProperty.MessagingOperationType, DiagnosticProperty.OperationProcess },
                { DiagnosticProperty.MessageBusMessageType, typeof(TMessage).Name }
            });

    public void RecordMessageHandlerElapsedTime<TMessage>(double value, string system, string channel, string handler) =>
        MessageHandlerElapsedTime.Record(
            value,
            new TagList()
            {
                { DiagnosticProperty.MessagingTransport, system },
                { DiagnosticProperty.MessagingSystem, system },
                { DiagnosticProperty.MessagingDestinationName, channel },
                { DiagnosticProperty.MessagingOperationName, DiagnosticProperty.OperationConsume },
                { DiagnosticProperty.MessagingOperationType, DiagnosticProperty.OperationProcess },
                { DiagnosticProperty.MessageBusMessageType, typeof(TMessage).Name },
                { DiagnosticProperty.MessageBusMessageHandler, handler }
            });

    public void RecordMessagingProcessDuration<TMessage>(double value, string system, string channel) =>
        MessagingProcessDuration.Record(
            value,
            new TagList()
            {
                { DiagnosticProperty.MessagingTransport, system },
                { DiagnosticProperty.MessagingSystem, system },
                { DiagnosticProperty.MessagingDestinationName, channel },
                { DiagnosticProperty.MessagingOperationName, DiagnosticProperty.OperationConsume },
                { DiagnosticProperty.MessagingOperationType, DiagnosticProperty.OperationProcess },
                { DiagnosticProperty.MessageBusMessageType, typeof(TMessage).Name }
            });

    public void RecordMessagePublished<TMessage>(string system, string channel, string? errortype = default) =>
        RecordMessagePublished(system, channel, typeof(TMessage).Name, errortype);

    public void RecordMessagePublished(string system, string channel, string? messageType = default, string? errortype = default)
    {
        var tagList = new TagList()
        {
            { DiagnosticProperty.MessagingTransport, system },
            { DiagnosticProperty.MessagingSystem, system },
            { DiagnosticProperty.MessagingDestinationName, channel },
            { DiagnosticProperty.MessagingOperationName, DiagnosticProperty.OperationPublish },
            { DiagnosticProperty.MessagingOperationType, DiagnosticProperty.OperationPublish },
        };

        if (messageType is not null)
        {
            tagList.Add(DiagnosticProperty.MessageBusMessageType, messageType);
        }

        if (errortype is not null)
        {
            tagList.Add(DiagnosticProperty.ErrorType, errortype);
        }

        MessagingClientPublishedMessages.Add(1, tagList);
    }
}
