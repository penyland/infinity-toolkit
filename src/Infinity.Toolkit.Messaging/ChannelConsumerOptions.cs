namespace Infinity.Toolkit.Messaging;

public class ChannelConsumerOptions : ChannelOptions
{
    /// <summary>
    /// The name of the subscription.
    /// Required if <see cref="ChannelType"/> is <see cref="ChannelType.Topic"/>.
    /// </summary>
    [RequiredIf(nameof(ChannelType), ChannelType.Topic)]
    public string SubscriptionName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether automatically dead-letter messages if an exception occurs while processing.
    /// </summary>
    public bool AutoDeadLetterMessagesOnException { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether automatically dead-letter messages if the message is missing required properties.
    /// </summary>
    public bool AutoDeadLetterMessagesMissingRequiredProperties { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether automatically deserialize messages.
    /// </summary>
    public bool AutoDeserializeMessages { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the <see cref="CloudEvents.Type"/> property is required.
    /// </summary>
    public bool RequireCloudEventsTypeProperty { get; set; } = true;
}
