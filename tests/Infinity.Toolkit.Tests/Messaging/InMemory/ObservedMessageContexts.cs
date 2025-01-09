using Infinity.Toolkit.Messaging.Abstractions;

namespace Infinity.Toolkit.Tests.Messaging.InMemory;

public class ObservedMessageContexts(IReadOnlyList<IMessageHandlerContext> incommingMessageContexts)
{
    public IReadOnlyList<IMessageHandlerContext> IncomingMessageContexts { get; } = incommingMessageContexts;

    public IEnumerable<object> ReceivedMessages => IncomingMessageContexts.Select(c => c);
}
