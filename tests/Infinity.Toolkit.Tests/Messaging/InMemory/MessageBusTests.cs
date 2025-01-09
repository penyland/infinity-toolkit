using Infinity.Toolkit.Messaging.Abstractions;
using Infinity.Toolkit.Messaging.InMemory;

namespace Infinity.Toolkit.Tests.Messaging.InMemory;

public class MessageBusTests : TestBase
{
    public class IsProcessing(ITestOutputHelper testOutputHelper)
    {
        [Fact]
        public async Task Is_True_When_Processing()
        {
            // Arrange
            var serviceProvider = ConfigureServiceProvider(
                services =>
                {
                    services.AddInfinityMessaging(bus =>
                    {
                        bus.ConfigureInMemoryBus(builder =>
                           {
                               builder.AddChannelConsumer<TestMessage>(options =>
                                {
                                    options.ChannelName = "test";
                                    options.SubscriptionName = "test";
                                });
                           });
                    });
                },
                testOutputHelper);

            // Act
            var messageBus = serviceProvider.GetRequiredService<IMessageBus>();
            await messageBus.InitAsync();
            await messageBus.StartAsync(cancellationToken: CancellationToken.None);

            // Assert
            messageBus.IsProcessing.Should().BeTrue();
        }

        [Fact]
        public async Task Is_False_When_Not_Processing()
        {
            // Arrange
            var serviceProvider = ConfigureServiceProvider(
                services =>
                {
                    services.AddInfinityMessaging(bus =>
                    {
                        bus.AddInMemoryBus(builder =>
                        {
                            builder.AddChannelConsumer<TestMessage>(options =>
                            {
                                options.ChannelName = "test";
                                options.SubscriptionName = "test";
                            });
                        });
                    });
                },
                testOutputHelper);

            // Act
            var messageBus = serviceProvider.GetRequiredService<IMessageBus>();
            await messageBus.InitAsync();
            await messageBus.StartAsync(cancellationToken: CancellationToken.None);
            await messageBus.StopAsync(CancellationToken.None);

            // Assert
            messageBus.IsProcessing.Should().BeFalse();
        }
    }

    public class StartAsync(ITestOutputHelper testOutputHelper)
    {
        [Fact]
        public async Task Should_Not_Throw_When_Already_Started()
        {
            // Arrange
            var serviceProvider = ConfigureServiceProvider(
                services =>
                {
                    services.AddInfinityMessaging(bus =>
                    {
                        bus.AddInMemoryBus(builder =>
                        {
                            builder.AddChannelConsumer<TestMessage>(options =>
                            {
                                options.ChannelName = "test";
                                options.SubscriptionName = "test";
                            });
                        });
                    });
                },
                testOutputHelper);

            // Act
            var messageBus = serviceProvider.GetRequiredService<IMessageBus>();
            await messageBus.InitAsync();
            await messageBus.StartAsync(cancellationToken: CancellationToken.None);
            var act = () => messageBus.StartAsync(cancellationToken: CancellationToken.None);

            // Assert
            await act.Should().NotThrowAsync<InvalidOperationException>();
        }
    }
}
