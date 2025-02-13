using Infinity.Toolkit.Messaging.Abstractions;
using Infinity.Toolkit.Messaging.Diagnostics;

namespace Infinity.Toolkit.Messaging.AzureServiceBus;

internal sealed class AzureServiceBusChannelProducer : IChannelProducer
{
    private readonly AzureServiceBusClientFactory clientFactory;
    private readonly MessageBusOptions messageBusOptions;
    private readonly Metrics messageBusMetrics;
    private readonly AzureServiceBusChannelProducerOptions channelProducerOptions;
    private readonly JsonSerializerOptions jsonSerializerOptions;
    private readonly ClientDiagnostics clientDiagnostics;

    public AzureServiceBusChannelProducer([ServiceKey] string serviceKey, AzureServiceBusClientFactory clientFactory, IOptionsMonitor<AzureServiceBusChannelProducerOptions> options, IOptions<MessageBusOptions> messageBusOptions, Metrics messageBusMetrics)
    {
        ServiceKey = serviceKey;
        this.clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        this.messageBusMetrics = messageBusMetrics;
        this.messageBusOptions = messageBusOptions.Value;
        channelProducerOptions = options.Get(ServiceKey) ?? throw new ArgumentNullException(nameof(options));
        jsonSerializerOptions = channelProducerOptions.JsonSerializerOptions ?? messageBusOptions.Value.JsonSerializerOptions ?? new();
        clientDiagnostics = new ClientDiagnostics(AzureServiceBusDefaults.System, AzureServiceBusDefaults.Name, channelProducerOptions.ChannelName, AzureServiceBusDefaults.System);
    }

    public string ServiceKey { get; }

    public Task SendAsync(object payload, CancellationToken cancellationToken, string contentType = MediaTypeNames.Application.Json, string? correlationId = null, string? id = null, Dictionary<string, object?>? headers = null)
    {
        if (channelProducerOptions is not null)
        {
            // Wrap payload in envelope
            var envelope = new EnvelopeBuilder()
                .WithBody(payload, jsonSerializerOptions)
                .WithMessageId(id ?? Guid.NewGuid().ToString())
                .WithContentType(contentType)
                .WithCorrelationId(correlationId)
                .WithEventType(messageBusOptions.EventTypeIdentifierPrefix, channelProducerOptions.EventTypeName ?? channelProducerOptions.ServiceKey)
                .WithSource(channelProducerOptions.Source)
                .WithHeaders(headers)
                .Build();

            return SendEnvelopeAsync(envelope, cancellationToken);
        }
        else
        {
            throw new InvalidOperationException("Could not send message.");
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
            var sender = clientFactory.CreateSender(channelProducerOptions.ChannelName, channelProducerOptions.ServiceBusSenderOptions);

            scope?.SetTag(DiagnosticProperty.MessagingDestinationName, channelProducerOptions.ChannelName);
            scope?.SetTag(DiagnosticProperty.MessagingMessageId, envelope.MessageId);
            scope?.SetTag(DiagnosticProperty.MessageBusMessageType, DiagnosticProperty.MessageTypeUndefined);
            messageBusMetrics?.RecordMessagePublished(AzureServiceBusDefaults.System, channelProducerOptions.ChannelName);

            return sender.SendMessageAsync(envelope.ToServiceBusMessage(), cancellationToken);
        }
        else
        {
            scope?.SetStatus(ActivityStatusCode.Error);
            throw new InvalidOperationException("Could not send message.");
        }
    }
}
