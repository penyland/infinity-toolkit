using Infinity.Toolkit.Messaging.Diagnostics;

namespace Infinity.Toolkit.Messaging.InMemory;


public interface IDefaultChannelProducer
{
    Task SendAsync(object message);

    Task SendAsync<T>(T message);

    Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken);
}

internal class DefaultInMemoryChannelProducer : IDefaultChannelProducer
{
    private readonly InMemoryChannelProducerOptions channelProducerOptions;
    private readonly ClientDiagnostics clientDiagnostics;
    private readonly InMemoryChannelClientFactory clientFactory;
    private readonly JsonSerializerOptions jsonSerializerOptions = new();
    private readonly Metrics messageBusMetrics;

    public DefaultInMemoryChannelProducer([ServiceKey] string serviceKey, InMemoryChannelClientFactory clientFactory, IOptionsMonitor<InMemoryChannelProducerOptions> channelProducerOptions)
    {
        this.channelProducerOptions = channelProducerOptions.Get(serviceKey) ?? throw new ArgumentNullException(nameof(channelProducerOptions));

        clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        clientDiagnostics = new ClientDiagnostics(InMemoryBusDefaults.System, InMemoryBusDefaults.Name, channelProducerOptions.ChannelName, InMemoryBusDefaults.System);
    }

    public Task SendAsync(object message)
    {
        var envelope = new EnvelopeBuilder()
                .WithBody(message, jsonSerializerOptions)
                .WithMessageId(Guid.NewGuid().ToString())
                //.WithContentType(contentType)
                //.WithCorrelationId(correlationId)
                //.WithEventType(typeof(T).Name)
                //.WithSource(channelProducerOptions.Source)
                //.WithHeaders(headers)
                .Build();

        return SendEnvelopeAsync(envelope, CancellationToken.None);
    }

    public Task SendAsync<T>(T payload)
    {
        var envelope = new EnvelopeBuilder()
                .WithBody(payload, jsonSerializerOptions)
                .WithMessageId(Guid.NewGuid().ToString())
                //.WithContentType(contentType)
                //.WithCorrelationId(correlationId)
                .WithEventType(typeof(T).AssemblyQualifiedName ?? typeof(T).Name)
                //.WithSource(channelProducerOptions.Source)
                //.WithHeaders(headers)
                .Build();

        return SendEnvelopeAsync(envelope, CancellationToken.None);
    }

    public Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        using var scope = clientDiagnostics.CreateDiagnosticActivityScope(
                    ActivityKind.Producer,
                    $"{DiagnosticProperty.OperationPublish} {channelProducerOptions.ChannelName}",
                    DiagnosticProperty.OperationPublish,
                    envelope.ApplicationProperties);

        if (channelProducerOptions is not null)
        {
            var sender = clientFactory.GetSender(channelProducerOptions.ChannelName);

            scope?.SetTag(DiagnosticProperty.MessagingDestinationName, channelProducerOptions.ChannelName);
            scope?.SetTag(DiagnosticProperty.MessagingMessageId, envelope.MessageId);
            scope?.SetTag(DiagnosticProperty.MessageBusMessageType, DiagnosticProperty.MessageTypeUndefined);
            messageBusMetrics?.RecordMessagePublished(InMemoryBusDefaults.System, channelProducerOptions.ChannelName);

            return sender.SendAsync(envelope.ToInMemoryMessage(), cancellationToken);
        }
        else
        {
            scope?.SetStatus(ActivityStatusCode.Error);
            //throw new InvalidOperationException($"{EventTypeWasNotRegistered} {typeof(Envelope).Name}");
        }

        return Task.CompletedTask;
    }

    public Task SendEnvelopeAsync2(Envelope envelope, CancellationToken cancellationToken)
    {
        var options = channelProducerOptions.Get("default");
        var sender = clientFactory.GetSender(options.ChannelName);
        var inMemoryMessage = envelope.ToInMemoryMessage();
        return sender.SendAsync(inMemoryMessage, cancellationToken);
    }
}
