namespace Infinity.Toolkit.Messaging;

public sealed class MessageBusException : Exception
{
    public MessageBusException(string broker, string channel, string message)
        : base(message)
    {
        Broker = broker;
        ChannelName = channel;
    }

    public MessageBusException(string broker, string channel, string message, Exception innerException)
        : base(message, innerException)
    {
        Broker = broker;
        ChannelName = channel;
    }

    public string Broker { get; }

    public string ChannelName { get; }

    public string ErrorType { get; }
}
