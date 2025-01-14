using Infinity.Toolkit.Messaging.Abstractions;
using Infinity.Toolkit.Messaging.Diagnostics;
using static Infinity.Toolkit.Messaging.Diagnostics.Errors;

namespace Infinity.Toolkit.Messaging.InMemory;

/// <summary>
/// The in-memory channel producer can send messages on a channel.
/// </summary>
internal sealed class InMemoryChannelProducer : IChannelProducer
{
    private readonly InMemoryChannelClientFactory clientFactory;
    private readonly MessageBusOptions messageBusOptions;
    private readonly Metrics messageBusMetrics;
    private readonly InMemoryChannelProducerOptions channelProducerOptions;
    private readonly JsonSerializerOptions jsonSerializerOptions;
    private readonly ClientDiagnostics clientDiagnostics;

    public InMemoryChannelProducer([ServiceKey] string serviceKey, InMemoryChannelClientFactory clientFactory, IOptionsMonitor<InMemoryChannelProducerOptions> options, IOptions<MessageBusOptions> messageBusOptions, Metrics messageBusMetrics)
    {
        ServiceKey = serviceKey;
        this.clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        this.messageBusOptions = messageBusOptions.Value;
        this.messageBusMetrics = messageBusMetrics;
        channelProducerOptions = options.Get(ServiceKey) ?? throw new ArgumentNullException(nameof(options));
        jsonSerializerOptions = channelProducerOptions.JsonSerializerOptions ?? messageBusOptions.Value.JsonSerializerOptions ?? new();
        clientDiagnostics = new ClientDiagnostics(InMemoryBusDefaults.System, InMemoryBusDefaults.Name, channelProducerOptions.ChannelName, InMemoryBusDefaults.System);
    }

    internal string ServiceKey { get; init; }

    public Task SendAsync(object payload, CancellationToken cancellationToken, string contentType = MediaTypeNames.Application.Json, string? correlationId = null, string? id = null, Dictionary<string, object?>? headers = null)
    {
        if (channelProducerOptions is not null)
        {
            // Wrap payload in envelope
            var envelope = new EnvelopeBuilder()
                .WithBody(payload, jsonSerializerOptions)
                .WithMessageId(id ?? Guid.NewGuid().ToString())
                .WithContentType(contentType)
                .WithEventType(channelProducerOptions.EventTypeName ?? channelProducerOptions.EventTypeName ?? channelProducerOptions.Key)
                .WithCorrelationId(correlationId)
                .WithHeaders(headers)
                .WithSource(channelProducerOptions.Source)
                .Build();

            return SendEnvelopeAsync(envelope, cancellationToken);
        }
        else
        {
            throw new InvalidOperationException("Could not send envelope.");
        }
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
            scope?.SetTag(DiagnosticProperty.MessageBusMessageType, DiagnosticProperty.MessageTypeRaw);
            messageBusMetrics?.RecordMessagePublished(InMemoryBusDefaults.System, channelProducerOptions.ChannelName);

            return sender.SendAsync(envelope.ToInMemoryMessage(), cancellationToken);
        }
        else
        {
            scope?.SetStatus(ActivityStatusCode.Error);
            throw new InvalidOperationException($"{EventTypeWasNotRegistered} {typeof(Envelope).Name}");
        }
    }
}
