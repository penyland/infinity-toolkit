namespace Infinity.Toolkit.Messaging.Abstractions;

/// <summary>
/// Consume a deferred message from the channel.
/// </summary>
/// <typeparam name="T">The type of message that the deferred channel consumer can consume.</typeparam>
public interface IDeferredChannelConsumer<T>
    where T : class
{
    /// <summary>
    /// Consume a deferred message from a channel defined by the type parameter T.
    /// </summary>
    /// <param name="sequenceNumber">The sequence number of the deferred message.</param>
    /// <param name="autoPurgeMessage">Specifies whether the message should be purged from the service bus after it has been consumed. Default true.</param>
    Task<IMessageHandlerContext<T>> ConsumeDeferredMessageAsync(long sequenceNumber, bool autoPurgeMessage = true);

    /// <summary>
    /// Consume deferred messages from the channel defined by the type parameter T.
    /// </summary>
    /// <param name="sequenceNumbers">An array of sequence numbers of the deferred messages.</param>
    /// <param name="autoPurgeMessage">Specifies whether the message should be purged from the service bus after it has been consumed. Default true.</param>
    Task<IEnumerable<IMessageHandlerContext<T>>> ConsumeDeferredMessagesAsync(IReadOnlyCollection<long> sequenceNumbers, bool autoPurgeMessage = true);

    /// <summary>
    /// Consume deferred messages from the channel as an async enumerable defined by the type parameter T.
    /// </summary>
    /// <param name="sequenceNumbers">An array of sequence numbers of the deferred messages.</param>
    /// <param name="autoPurgeMessage">Specifies whether the message should be purged from the service bus after it has been consumed. Default true.</param>
    /// <returns>An IAsyncEnumerable&lt;IMessageHandler&lt;<typeparamref name="T"/>&gt;&gt; that represents the deserialized deferred messages.</returns>
    IAsyncEnumerable<IMessageHandlerContext<T>> ConsumeDeferredMessagesAsAsyncEnumerable(IReadOnlyCollection<long> sequenceNumbers, bool autoPurgeMessage = true);
}
