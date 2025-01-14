namespace Infinity.Toolkit.Messaging.Abstractions;

/// <summary>
/// Represents a message handler context which contains the message body and the message headers.
/// The consumer can use this context to complete the message if necessary.
/// </summary>
public interface IMessageHandlerContext
{
    /// <summary>
    /// The message body.
    /// </summary>
    [JsonIgnore]
    BinaryData Body { get; }

    /// <summary>
    /// The channel name.
    /// </summary>
    string ChannelName { get; }

    /// <summary>
    /// The message headers.
    /// </summary>
    IReadOnlyDictionary<string, object?> Headers { get; init; }

    /// <summary>
    /// The sequence number of the message which uniquely identifies the message within the channel.
    /// </summary>
    long SequenceNumber { get; }

    /// <summary>
    /// The time the message was enqueued.
    /// </summary>
    DateTimeOffset EnqueuedTimeUtc { get; }

    /// <summary>
    /// Gets the date and time, in UTC, at which the message should be made available to consumers.
    /// </summary>
    DateTimeOffset ScheduledEnqueueTime { get; }

    /// <summary>
    /// Completes the message.
    /// This will delete the message from the service.
    /// </summary>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>An awaitable task.</returns>
    Task CompleteMessageAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Defers the message to be processed later.
    /// </summary>
    /// <param name="propertiesToModify">An optional dictionary of properties to modify.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    Task<long> DeferMessageAsync(IDictionary<string, object?>? propertiesToModify = default, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a typed message handler context which contains the deserialized message and the message headers.
/// </summary>
/// <typeparam name="TMessage">The type of the message.</typeparam>
public interface IMessageHandlerContext<out TMessage> : IMessageHandlerContext
    where TMessage : class
{
    /// <summary>
    /// The deserialized message.
    /// </summary>
    TMessage? Message { get; }

    /// <summary>
    /// The type of the message.
    /// </summary>
    [JsonIgnore]
    Type MessageType { get; }
}
