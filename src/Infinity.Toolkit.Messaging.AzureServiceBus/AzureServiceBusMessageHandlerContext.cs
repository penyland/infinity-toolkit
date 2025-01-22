using Infinity.Toolkit.Messaging.Abstractions;

namespace Infinity.Toolkit.Messaging.AzureServiceBus;

internal class AzureServiceBusMessageHandlerContext : MessageHandlerContextBase, IMessageHandlerContext
{
    [JsonIgnore]
    internal ProcessMessageEventArgs ProcessMessageEventArgs { get; set; }

    public override async Task CompleteMessageAsync(CancellationToken cancellationToken = default)
    {
        await ProcessMessageEventArgs.CompleteMessageAsync(ProcessMessageEventArgs.Message, cancellationToken);
    }

    public override async Task<long> DeferMessageAsync(IDictionary<string, object?>? propertiesToModify = default, CancellationToken cancellationToken = default)
    {
        await ProcessMessageEventArgs.DeferMessageAsync(ProcessMessageEventArgs.Message, propertiesToModify, cancellationToken);
        return ProcessMessageEventArgs.Message.SequenceNumber;
    }
}

internal sealed class AzureServiceBusBrokerMessageHandlerContext<TMessage> : AzureServiceBusMessageHandlerContext, IMessageHandlerContext, IMessageHandlerContext<TMessage>
    where TMessage : class
{
    /// <inheritdoc />
    public Type MessageType { get; } = typeof(TMessage);

    /// <inheritdoc />
    public TMessage? Message { get; init; }
}
