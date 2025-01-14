using Infinity.Toolkit.Messaging.Abstractions;

namespace Infinity.Toolkit.Messaging.InMemory;

internal class InMemoryBusMessageHandlerContext : MessageHandlerContextBase, IMessageHandlerContext
{
    [JsonIgnore]
    internal ProcessMessageEventArgs ProcessMessageEventArgs { get; set; }

    public override async Task<long> DeferMessageAsync(IDictionary<string, object?>? propertiesToModify = null, CancellationToken cancellationToken = default)
    {
        var message = new InMemoryMessage(ProcessMessageEventArgs.Message);
        return await ProcessMessageEventArgs.Receiver.DeferMessageAsync(message, propertiesToModify, cancellationToken);
    }
}

internal sealed class InMemoryBrokerMessageHandlerContext<TMessage> : InMemoryBusMessageHandlerContext, IMessageHandlerContext, IMessageHandlerContext<TMessage>
    where TMessage : class
{
    /// <inheritdoc />
    public Type MessageType { get; } = typeof(TMessage);

    /// <inheritdoc />
    public TMessage? Message { get; init; }
}
