namespace Infinity.Toolkit.Messaging;

public abstract class ChannelProducerOptions : ChannelOptions
{
    /// <summary>
    /// The key of the producer.
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// The name of the producer.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Uri that identifies the context in which an event happened.
    /// Default value is "{broker-abbreviation}://{application-name}/{environment}/{channel-producer-name}".
    /// </summary>
    public Uri Source { get; set; }
}
