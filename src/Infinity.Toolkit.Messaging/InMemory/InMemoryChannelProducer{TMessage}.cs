using Infinity.Toolkit.Messaging.Abstractions;
using Infinity.Toolkit.Messaging.Diagnostics;
using static Infinity.Toolkit.Messaging.Diagnostics.Errors;

namespace Infinity.Toolkit.Messaging.InMemory;

/// <summary>
/// The in-memory channel producer can send messages on a channel.
/// </summary>
/// <typeparam name="TMessage">The type of the message.</typeparam>
internal sealed class InMemoryChannelProducer<TMessage> : IChannelProducer<TMessage>
    where TMessage : class
{
    private readonly InMemoryChannelClientFactory clientFactory;
    private readonly MessageBusOptions messageBusOptions;
    private readonly Metrics messageBusMetrics;
    private readonly InMemoryChannelProducerOptions channelProducerOptions;
    private readonly JsonSerializerOptions jsonSerializerOptions;
    private readonly ClientDiagnostics clientDiagnostics;

    public InMemoryChannelProducer(InMemoryChannelClientFactory clientFactory, IOptionsMonitor<InMemoryChannelProducerOptions> options, IOptions<MessageBusOptions> messageBusOptions, Metrics messageBusMetrics)
    {
        this.clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        this.messageBusOptions = messageBusOptions.Value;
        this.messageBusMetrics = messageBusMetrics;
        channelProducerOptions = options.Get(typeof(TMessage).AssemblyQualifiedName) ?? throw new ArgumentNullException(nameof(options));

        jsonSerializerOptions = channelProducerOptions.JsonSerializerOptions ?? messageBusOptions.Value.JsonSerializerOptions ?? new();
        clientDiagnostics = new ClientDiagnostics(InMemoryBusDefaults.System, InMemoryBusDefaults.Name, channelProducerOptions.ChannelName, InMemoryBusDefaults.System);
    }

    /// <inheritdoc/>
    public Task CancelScheduledMessageAsync(long sequenceNumber, CancellationToken cancellationToken = default)
    {
        if (channelProducerOptions is not null)
        {
            var sender = clientFactory.GetSender(channelProducerOptions.ChannelName);
            return sender.CancelScheduledMessageAsync(sequenceNumber, cancellationToken);
        }
        else
        {
            throw new InvalidOperationException($"{EventTypeWasNotRegistered} {typeof(TMessage).Name}");
        }
    }

    /// <inheritdoc/>
    public Task<long> ScheduleSendAsync(TMessage message, DateTimeOffset scheduledEnqueueTimeUtc, string contentType = MediaTypeNames.Application.Json, string? correlationId = null, string? id = null, Dictionary<string, object?>? headers = null, CancellationToken cancellationToken = default) =>
        ScheduleSendAsync(message, channelProducerOptions.EventTypeName ?? typeof(TMessage).Name, scheduledEnqueueTimeUtc, contentType, correlationId, id, headers, cancellationToken);

    /// <inheritdoc/>
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

            var sender = clientFactory.GetSender(channelProducerOptions.ChannelName);
            return sender.ScheduleSendAsync(envelope.ToInMemoryMessage(), scheduledEnqueueTimeUtc, cancellationToken);
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
                //.WithEventType(messageBusOptions.EventTypeIdentifierPrefix, eventType ?? channelProducerOptions.EventTypeName ?? typeof(TMessage).Name)
                .WithEventType(eventType ?? channelProducerOptions.EventTypeName ?? typeof(TMessage).Name)
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
            var sender = clientFactory.GetSender(channelProducerOptions.ChannelName);

            scope?.SetTag(DiagnosticProperty.MessagingDestinationName, channelProducerOptions.ChannelName);
            scope?.SetTag(DiagnosticProperty.MessagingMessageId, envelope.MessageId);
            scope?.SetTag(DiagnosticProperty.MessageBusMessageType, typeof(TMessage).FullName ?? typeof(TMessage).Name);
            messageBusMetrics?.RecordMessagePublished<TMessage>(InMemoryBusDefaults.System, channelProducerOptions.ChannelName);

            return sender.SendAsync(envelope.ToInMemoryMessage(), cancellationToken);
        }
        else
        {
            scope?.SetStatus(ActivityStatusCode.Error);
            throw new InvalidOperationException($"{EventTypeWasNotRegistered} {typeof(TMessage).Name}");
        }
    }
}
