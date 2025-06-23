using Infinity.Toolkit.Messaging.Abstractions;

namespace Infinity.Toolkit.Messaging.InMemory;

public interface IChannelProducerProvider
{
    IDefaultChannelProducer GetDefaultProducer();

    IChannelProducer<T> GetProducer<T>();
    IChannelProducer GetProducer(string serviceKey);
}

internal class ChannelProducerProvider : IChannelProducerProvider
{
    private readonly IServiceProvider serviceProvider;

    public ChannelProducerProvider(IServiceProvider serviceProvider) => this.serviceProvider = serviceProvider;

    public IDefaultChannelProducer GetDefaultProducer()
    {
        var defaultChannelProducer = serviceProvider.GetService<IDefaultChannelProducer>();

        return defaultChannelProducer!;
    }

    public IChannelProducer<T> GetProducer<T>()
    {
        var channelProducer = serviceProvider.GetService<IChannelProducer<T>>();

        return channelProducer ?? throw new InvalidOperationException($"No channel producer found for type {typeof(T).Name}");
    }

    public IChannelProducer GetProducer(string serviceKey)
    {
        var channelProducer = serviceProvider.GetKeyedService<IChannelProducer>(serviceKey);
        return channelProducer ?? throw new InvalidOperationException($"No channel producer found for service key {serviceKey}");
    }
}
