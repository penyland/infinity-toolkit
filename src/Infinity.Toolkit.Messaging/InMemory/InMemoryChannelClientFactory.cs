namespace Infinity.Toolkit.Messaging.InMemory;

internal sealed class InMemoryChannelClientFactory(SequenceNumberGenerator sequenceNumberGenerator, ILoggerFactory loggerFactory)
{
    private readonly ConcurrentDictionary<string, InMemorySender> senders = new();
    private readonly ConcurrentDictionary<string, InMemoryChannelReceiver> receivers = new();
    private readonly ConcurrentDictionary<string, Channel<InMemoryMessage>> channels = new();
    private readonly SequenceNumberGenerator sequenceNumberGenerator = sequenceNumberGenerator;
    private readonly ILoggerFactory loggerFactory = loggerFactory;

    /// <summary>
    /// Gets a sender for a given channel name.
    /// </summary>
    /// <param name="channelName">The name of the channel.</param>
    /// <returns>An instance of an InMemorySender bound to the channel.</returns>
    internal InMemorySender GetSender(string channelName) => senders.GetOrAdd(channelName, _ => CreateSender(channelName));

    /// <summary>
    /// Creates a channel processor for a given channel name.
    /// </summary>
    /// <param name="channelName">The name of the channel.</param>
    /// <returns>An instance of an InMemoryChannelProcessor bound to the channel.</returns>
    internal InMemoryChannelProcessor GetChannelProcessor(string channelName)
    {
        var channel = GetChannel(channelName);
        var receiver = GetOrAddChannelReceiver(channelName);
        return InMemoryChannelProcessor.Create(channelName, channel.Reader, receiver, loggerFactory);
    }

    /// <summary>
    /// Creates a channel processor for a topic subscription with a predicate.
    /// </summary>
    /// <param name="channelName">The channel to listen to messages on.</param>
    /// <param name="subscriptionName">The subscription.</param>
    /// <param name="predicate">An optional predicate to filter messages.</param>
    /// <returns>An instance of an InMemoryChannelProcessor bound to the channel and subscription.</returns>
    internal InMemoryChannelProcessor GetChannelProcessor(string channelName, string subscriptionName, Predicate<InMemoryMessage>? predicate = null)
    {
        var parent = GetChannel(channelName, true);
        var subscriptionPath = $"{channelName}/subscriptions/{subscriptionName}";
        var subscriptionChannel = GetChannel(subscriptionPath, false);

        var subscriptionWriterInfo = new SubscriptionWriterInfo<InMemoryMessage>()
        {
            Writer = subscriptionChannel.Writer,
            Predicate = predicate ?? (x => true)
        };

        ((InMemoryChannelSubscriptionWriter<InMemoryMessage>)parent.Writer).AddSubscription(subscriptionWriterInfo);

        var receiver = GetOrAddChannelReceiver(subscriptionPath);
        return InMemoryChannelProcessor.Create(subscriptionPath, subscriptionChannel.Reader, receiver, loggerFactory);
    }

    /// <summary>
    /// Creates a channel receiver for a given channel name.
    /// </summary>
    /// <param name="channelName">The name of the channel.</param>
    /// <returns>An instance of an InMemoryChannelReceiver bound to the channel.</returns>
    internal InMemoryChannelReceiver GetOrAddChannelReceiver(string channelName)
    {
        return receivers.GetOrAdd(channelName, _ => new InMemoryChannelReceiver(channelName, loggerFactory));
    }

    /// <summary>
    /// Gets or adds a channel receiver for a given deferred channel consumer options.
    /// </summary>
    /// <param name="options">The deferred channel consumer options. See <see cref="InMemoryDeferredChannelConsumerOptions"/>.</param>
    /// <returns>An instance of an InMemoryChannelReceiver bound to the channel.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the channel type is not supported.</exception>
    internal InMemoryChannelReceiver GetOrAddChannelReceiver(InMemoryDeferredChannelConsumerOptions options)
    {
        var channelPath = options.ChannelType switch
        {
            ChannelType.Queue => options.ChannelName,
            ChannelType.Topic => options.ChannelName + "/subscriptions/" + options.SubscriptionName,
            _ => throw new ArgumentOutOfRangeException(nameof(options), "Invalid channel type.")
        };

        return GetOrAddChannelReceiver(channelPath);
    }

    private InMemorySender CreateSender(string channelName)
    {
        var channel = GetChannel(channelName);
        return new InMemorySender(channelName, channel.Writer, sequenceNumberGenerator, loggerFactory);
    }

    private Channel<InMemoryMessage> GetChannel(string channelName, bool subscription = false)
    {
        return channels.GetOrAdd(channelName, _ =>
        {
            if (subscription)
            {
                return new SubscriptionChannel<InMemoryMessage>();
            }
            else
            {
                return Channel.CreateUnbounded<InMemoryMessage>();
            }
        });
    }
}

internal class SubscriptionChannel<T> : Channel<T>
{
    public SubscriptionChannel()
    {
        Writer = new InMemoryChannelSubscriptionWriter<T>();
    }
}
