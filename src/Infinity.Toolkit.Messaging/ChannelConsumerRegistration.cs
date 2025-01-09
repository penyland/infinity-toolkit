namespace Infinity.Toolkit.Messaging;

[DebuggerDisplay("BrokerName = {BrokerName}, EventType = {EventType.FullName,nq}")]
internal class ChannelConsumerRegistration
{
    public string BrokerName { get; set; } = string.Empty;

    public Type EventType { get; set; }

    public string Key { get; set; }
}

internal class ChannelRegistration
{
    public string BrokerName { get; set; } = string.Empty;

    public bool IsConsumer { get; init; }

    public string Key { get; set; }
}
