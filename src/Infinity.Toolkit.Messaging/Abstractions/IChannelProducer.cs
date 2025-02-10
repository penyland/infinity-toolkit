namespace Infinity.Toolkit.Messaging.Abstractions;

public interface IChannelProducer
{
    /// <summary>
    /// Sends a message to a channel on a broker.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <param name="contentType">The content type of the payload. Default application/json.</param>
    /// <param name="correlationId">An optional correlationId to add to the message.</param>
    /// <param name="id">The id of the message. If not provided a new Guid will be generated.</param>
    /// <param name="headers">Message headers.</param>
    /// <returns>An awaitable task.</returns>
    Task SendAsync(object message, CancellationToken cancellationToken, string contentType = MediaTypeNames.Application.Json, string? correlationId = null, string? id = null, Dictionary<string, object?>? headers = null);

    /// <summary>
    /// Sends a message wrapped in an envelope to a channel on a broker.
    /// </summary>
    /// <param name="envelope">The envelope containing the message to send.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>An awaitable task.</returns>
    Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken);
}

/// <summary>
/// Represents a channel producer that can send a message to a channel on a broker.
/// </summary>
/// <typeparam name="TMessage">The type of the message that the producer can send.</typeparam>
public interface IChannelProducer<TMessage>
{
    /// <summary>
    /// Sends a message to a channel on a broker.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <param name="contentType">The content type of the payload. Default application/json.</param>
    /// <param name="correlationId">An optional correlationId to add to the message.</param>
    /// <param name="id">The id of the message. If not provided a new Guid will be generated.</param>
    /// <param name="headers">Message headers.</param>
    /// <returns>An awaitable task.</returns>
    Task SendAsync(TMessage message, CancellationToken cancellationToken, string contentType = MediaTypeNames.Application.Json, string? correlationId = null, string? id = null, Dictionary<string, object?>? headers = null);

    /// <summary>
    /// Sends a message to a channel on a broker.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="eventType">The event type of the message.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <param name="contentType">The content type of the payload. Default application/json.</param>
    /// <param name="correlationId">An optional correlationId to add to the message.</param>
    /// <param name="id">The id of the message. If not provided a new Guid will be generated.</param>
    /// <param name="headers">Message headers.</param>
    /// <returns>An awaitable task.</returns>
    Task SendAsync(TMessage message, string eventType, CancellationToken cancellationToken, string contentType = MediaTypeNames.Application.Json, string? correlationId = null, string? id = null, Dictionary<string, object?>? headers = null);

    /// <summary>
    /// Schedule a message to be sent to a channel on a broker.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="scheduledEnqueueTimeUtc">The UTC time at which the message should be available for processing.</param>
    /// <param name="contentType">The content type of the payload. Default application/json.</param>
    /// <param name="correlationId">An optional correlationId to add to the message.</param>
    /// <param name="id">The id of the message. If not provided a new Guid will be generated.</param>
    /// <param name="headers">Message headers.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>An awaitable task.</returns>
    Task<long> ScheduleSendAsync(TMessage message, DateTimeOffset scheduledEnqueueTimeUtc, string contentType = MediaTypeNames.Application.Json, string? correlationId = null, string? id = null, Dictionary<string, object?>? headers = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedule a message to be sent to a channel on a broker.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="eventType">The event type of the message.</param>
    /// <param name="scheduledEnqueueTimeUtc">The UTC time at which the message should be available for processing.</param>
    /// <param name="contentType">The content type of the payload. Default application/json.</param>
    /// <param name="correlationId">An optional correlationId to add to the message.</param>
    /// <param name="id">The id of the message. If not provided a new Guid will be generated.</param>
    /// <param name="headers">Message headers.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>An awaitable task.</returns>
    Task<long> ScheduleSendAsync(TMessage message, string eventType, DateTimeOffset scheduledEnqueueTimeUtc, string contentType = MediaTypeNames.Application.Json, string? correlationId = null, string? id = null, Dictionary<string, object?>? headers = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel a scheduled message.
    /// </summary>
    /// <param name="sequenceNumber">The sequence number of the message to cancel.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>An awaitable task.</returns>
    Task CancelScheduledMessageAsync(long sequenceNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message wrapped in an envelope to a channel on a broker.
    /// </summary>
    /// <param name="envelope">The envelope containing the message to send.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>An awaitable task.</returns>
    Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken);
}
