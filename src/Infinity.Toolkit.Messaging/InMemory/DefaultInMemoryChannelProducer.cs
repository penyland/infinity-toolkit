using Infinity.Toolkit.Messaging.Diagnostics;

namespace Infinity.Toolkit.Messaging.InMemory;

public interface IChannelPublisher
{
    Task PublishAsync<T>(T message) where T : class;
}

public interface IDefaultChannelProducer
{
    Task SendAsync<T>(T message) where T : class;
}

internal class DefaultInMemoryChannelProducer : IDefaultChannelProducer
{
    private readonly InMemoryChannelProducerOptions channelProducerOptions;
    private readonly ClientDiagnostics clientDiagnostics;
    private readonly InMemoryChannelClientFactory clientFactory;
    private readonly JsonSerializerOptions jsonSerializerOptions = new();
    private readonly Metrics messageBusMetrics;

    public DefaultInMemoryChannelProducer(InMemoryChannelClientFactory clientFactory, IOptionsMonitor<InMemoryChannelProducerOptions> options, Metrics messageBusMetrics)
    {
        channelProducerOptions = options.Get("default") ?? throw new ArgumentNullException(nameof(channelProducerOptions));
        this.clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        this.messageBusMetrics = messageBusMetrics;
        clientDiagnostics = new ClientDiagnostics(InMemoryBusDefaults.System, InMemoryBusDefaults.Name, channelProducerOptions.ChannelName, InMemoryBusDefaults.System);
    }

    public Task SendAsync<T>(T payload)
        where T : class
    {
        var envelope = new EnvelopeBuilder()
                .WithBody(payload, jsonSerializerOptions)
                .WithMessageId(Guid.NewGuid().ToString())
                .WithContentType(MediaTypeNames.Application.Json)
                .WithEventType(typeof(T).AssemblyQualifiedName ?? typeof(T).Name)
                .Build();

        return SendEnvelopeAsync(envelope, CancellationToken.None);
    }

    internal Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken)
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
        }

        return Task.CompletedTask;
    }

    public Task SendEnvelopeAsync2(Envelope envelope, CancellationToken cancellationToken)
    {
        var sender = clientFactory.GetSender(channelProducerOptions.ChannelName);
        var inMemoryMessage = envelope.ToInMemoryMessage();
        return sender.SendAsync(inMemoryMessage, cancellationToken);
    }
}
