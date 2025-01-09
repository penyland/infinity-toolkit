namespace Infinity.Toolkit.Messaging.InMemory;

internal class InMemoryChannelSubscriptionWriter<T> : ChannelWriter<T>
{
    public List<SubscriptionWriterInfo<T>> Subscriptions { get; } = new();

    public override bool TryWrite(T item) => Subscriptions.Where(s => s.Predicate(item))
                                .Select(t => t.Writer)
                                .All(w => w.TryWrite(item));

    public override ValueTask<bool> WaitToWriteAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public void AddSubscription(SubscriptionWriterInfo<T> subscriptionWriterInfo)
    {
        ArgumentNullException.ThrowIfNull(subscriptionWriterInfo);

        if (subscriptionWriterInfo.Writer is null)
        {
            throw new ArgumentNullException(nameof(subscriptionWriterInfo), "Writer cannot be null.");
        }

        Subscriptions.Add(subscriptionWriterInfo);
    }

    public void ClearSubscriptions() => Subscriptions.Clear();
}

internal record SubscriptionWriterInfo<T>
{
    public Predicate<T> Predicate { get; init; } = _ => true;

    public ChannelWriter<T> Writer { get; init; }
}
