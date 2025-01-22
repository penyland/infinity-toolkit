namespace Infinity.Toolkit.Messaging;

[DebuggerDisplay("BrokerName = {BrokerName}, EventType = {EventType.FullName,nq}")]
internal class ChannelConsumerRegistration
{
    public string BrokerName { get; set; } = string.Empty;

    public Type EventType { get; set; }

    public object? Key { get; set; }
}
