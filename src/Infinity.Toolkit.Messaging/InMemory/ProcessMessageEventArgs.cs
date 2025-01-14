namespace Infinity.Toolkit.Messaging.InMemory;

internal class ProcessMessageEventArgs(InMemoryReceivedMessage message, string channelName, InMemoryChannelReceiver receiver, CancellationToken cancellationToken)
{
    public InMemoryReceivedMessage Message { get; } = message ?? throw new ArgumentNullException(nameof(message));

    public string ChannelName { get; } = channelName ?? throw new ArgumentNullException(nameof(channelName));

    public CancellationToken CancellationToken { get; } = cancellationToken;

    internal InMemoryChannelReceiver Receiver { get; } = receiver;
}

internal sealed class ProcessErrorEventArgs(Exception exception, string channelName)
{
    public string ChannelName { get; } = channelName;

    public Exception Exception { get; } = exception;

    public string Broker => InMemoryBusDefaults.System;
}
