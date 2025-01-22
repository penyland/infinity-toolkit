using Infinity.Toolkit.Messaging.Abstractions;
using Infinity.Toolkit.Messaging.Diagnostics;
using Infinity.Toolkit.Messaging.AzureServiceBus;
using static Infinity.Toolkit.Messaging.Diagnostics.Errors;

namespace Infinity.Toolkit.Messaging.AzureServiceBus;

/// <summary>
/// Azure Service Bus channel producer is channel producer that can send messages on a channel on Azure Service Bus.
/// </summary>
/// <typeparam name="TMessage">The type of the message.</typeparam>
internal sealed class AzureServiceBusChannelProducer<TMessage> : IChannelProducer<TMessage>
    where TMessage : class
{
    private readonly AzureServiceBusClientFactory clientFactory;
    private readonly Metrics messageBusMetrics;
    private readonly MessageBusOptions messageBusOptions;
    private readonly AzureServiceBusChannelProducerOptions channelProducerOptions;
    private readonly JsonSerializerOptions jsonSerializerOptions;
    private readonly ClientDiagnostics clientDiagnostics;

    public AzureServiceBusChannelProducer(AzureServiceBusClientFactory clientFactory, IOptionsMonitor<AzureServiceBusChannelProducerOptions> options, IOptions<MessageBusOptions> messageBusOptions, Metrics messageBusMetrics)
    {
        this.clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        this.messageBusMetrics = messageBusMetrics;
        this.messageBusOptions = messageBusOptions.Value;
        channelProducerOptions = options.Get(typeof(TMessage).AssemblyQualifiedName) ?? throw new ArgumentNullException(nameof(options));
        jsonSerializerOptions = channelProducerOptions.JsonSerializerOptions ?? messageBusOptions.Value.JsonSerializerOptions ?? new();
        clientDiagnostics = new ClientDiagnostics(AzureServiceBusDefaults.System, AzureServiceBusDefaults.Name, channelProducerOptions.ChannelName, AzureServiceBusDefaults.System);
    }

    /// <inheritdoc/>
    public async Task CancelScheduledMessageAsync(long sequenceNumber, CancellationToken cancellationToken = default)
    {
        if (channelProducerOptions is not null)
        {
            var sender = clientFactory.CreateSender(channelProducerOptions.ChannelName, channelProducerOptions.ServiceBusSenderOptions);
            await sender.CancelScheduledMessageAsync(sequenceNumber, cancellationToken);
        }
        else
        {
            throw new InvalidOperationException($"{EventTypeWasNotRegistered} {typeof(TMessage).Name}");
        }
    }

    /// <inheritdoc/>
    public Task<long> ScheduleSendAsync(TMessage message, DateTimeOffset scheduledEnqueueTimeUtc, string contentType = MediaTypeNames.Application.Json, string? correlationId = null, string? id = null, Dictionary<string, object?>? headers = null, CancellationToken cancellationToken = default) =>
        ScheduleSendAsync(message, channelProducerOptions.EventTypeName ?? typeof(TMessage).Name, scheduledEnqueueTimeUtc, contentType, correlationId, id, headers, cancellationToken);

    public Task<long> ScheduleSendAsync(TMessage payload, string eventType, DateTimeOffset scheduledEnqueueTimeUtc, string contentType = MediaTypeNames.Application.Json, string? correlationId = null, string? id = null, Dictionary<string, object?>? headers = null, CancellationToken cancellationToken = default)
    {
        if (channelProducerOptions is not null)
        {
            var envelope = new EnvelopeBuilder()
                .WithBody(payload, jsonSerializerOptions)
                .WithMessageId(id ?? Guid.NewGuid().ToString())
                .WithContentType(contentType)
                .WithCorrelationId(correlationId)
                .WithEventType(messageBusOptions.EventTypeIdentifierPrefix, eventType ?? channelProducerOptions.EventTypeName ?? typeof(TMessage).Name)
                .WithSource(channelProducerOptions.Source)
                .WithHeaders(headers)
                .Build();

            var sender = clientFactory.CreateSender(channelProducerOptions.ChannelName, channelProducerOptions.ServiceBusSenderOptions);
            return sender.ScheduleMessageAsync(envelope.ToServiceBusMessage(), scheduledEnqueueTimeUtc, cancellationToken);
        }
        else
        {
            throw new InvalidOperationException($"{EventTypeWasNotRegistered} {typeof(TMessage).Name}");
        }
    }

    /// <inheritdoc/>
    public Task SendAsync(TMessage message, CancellationToken cancellationToken, string contentType = MediaTypeNames.Application.Json, string? correlationId = null, string? id = null, Dictionary<string, object?>? headers = null) =>
        SendAsync(message, channelProducerOptions.EventTypeName ?? typeof(TMessage).Name, cancellationToken, contentType, correlationId, id, headers);

    /// <inheritdoc/>
    public Task SendAsync(TMessage payload, string eventType, CancellationToken cancellationToken = default, string contentType = MediaTypeNames.Application.Json, string? correlationId = null, string? id = null, Dictionary<string, object?>? headers = null)
    {
        if (channelProducerOptions is not null)
        {
            // Wrap payload in envelope
            var envelope = new EnvelopeBuilder()
                .WithBody(payload, jsonSerializerOptions)
                .WithMessageId(id ?? Guid.NewGuid().ToString())
                .WithContentType(contentType)
                .WithCorrelationId(correlationId)
                .WithEventType(messageBusOptions.EventTypeIdentifierPrefix, eventType ?? channelProducerOptions.EventTypeName ?? typeof(TMessage).Name)
                .WithSource(channelProducerOptions.Source)
                .WithHeaders(headers)
                .Build();

            return SendEnvelopeAsync(envelope, cancellationToken);
        }
        else
        {
            throw new InvalidOperationException($"{EventTypeWasNotRegistered} {typeof(TMessage).Name}");
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
            scope?.SetTag(DiagnosticProperty.MessageBusMessageType, typeof(TMessage).FullName ?? typeof(TMessage).Name);
            messageBusMetrics?.RecordMessagePublished<TMessage>(AzureServiceBusDefaults.System, channelProducerOptions.ChannelName);

            return sender.SendMessageAsync(envelope.ToServiceBusMessage(), cancellationToken);
        }
        else
        {
            scope?.SetStatus(ActivityStatusCode.Error);
            throw new InvalidOperationException($"{EventTypeWasNotRegistered} {typeof(TMessage).Name}");
        }
    }
}
