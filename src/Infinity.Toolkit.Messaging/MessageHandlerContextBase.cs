using Infinity.Toolkit.Messaging.Abstractions;

namespace Infinity.Toolkit.Messaging;

/// <summary>
/// Base class for context used by message handlers.
/// </summary>
public abstract class MessageHandlerContextBase : IMessageHandlerContext
{
    /// <inheritdoc />
    public BinaryData Body { get; init; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> Headers { get; init; }

    /// <inheritdoc />
    public long SequenceNumber { get; init; }

    /// <inheritdoc />
    public DateTimeOffset EnqueuedTimeUtc { get; init; }

    /// <inheritdoc />
    public DateTimeOffset ScheduledEnqueueTime { get; init; }

    /// <inheritdoc />
    public string ChannelName { get; init; }

    /// <inheritdoc />
    public virtual Task CompleteMessageAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();

    /// <inheritdoc />
    public virtual Task<long> DeferMessageAsync(IDictionary<string, object?>? propertiesToModify = default, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
