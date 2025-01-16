using Infinity.Toolkit.Messaging.Abstractions;
using Infinity.Toolkit.Messaging.InMemory;
using NSubstitute.ReceivedExtensions;
using System.Diagnostics;

namespace Infinity.Toolkit.Tests.Messaging.InMemory;

public class InMemoryBus(ITestOutputHelper testOutputHelper) : TestBase
{
    private readonly ITestOutputHelper testOutputHelper = testOutputHelper;
    private Activity activity = new Activity("unitTest").Start();

    [Fact]
    public async Task Should_Process_Message_Successfully()
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
                            })
                            .AddChannelProducer<TestMessage>(options => { options.ChannelName = "test"; });
                       });

                    bus.MapMessageHandler<TestMessage, TestMessageHandler>();
                });
            },
            testOutputHelper);

        ObservedMessageContexts results;

        var messageBus = serviceProvider.GetRequiredService<IMessageBus>();
        await messageBus.InitAsync();
        await messageBus.StartAsync(cancellationToken: CancellationToken.None);
        var channelProducer = serviceProvider.GetRequiredService<IChannelProducer<TestMessage>>();

        // Act
        results = await MessagingTestHelpers.SendAndWaitForEvent<TestMessage>(
            () => channelProducer.SendAsync(new TestMessage("testContent"), CancellationToken.None));

        // Assert
        results.ShouldNotBeNull();
        results.IncomingMessageContexts.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Should_Invoke_ExceptionHandler()
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
                        })
                            .AddChannelProducer<TestMessage>(options => { options.ChannelName = "test"; });
                    });

                    bus.MapMessageHandler<TestMessage, TestMessageHandler2>();
                })
                .AddExceptionHandler<TestExceptionHandler2>();

                services.AddScoped<TestExceptionHandlerData>();
            },
            testOutputHelper);

        // Act
        var messageBus = serviceProvider.GetRequiredService<IMessageBus>();
        await messageBus.InitAsync();
        await messageBus.StartAsync(cancellationToken: CancellationToken.None);
        var channelProducer = serviceProvider.GetRequiredService<IChannelProducer<TestMessage>>();
        await channelProducer.SendAsync(new TestMessage("testContent"), cancellationToken: CancellationToken.None);
        await Task.Delay(1000, CancellationToken.None);

        // Assert
        var testExceptionHandlerData = serviceProvider.GetRequiredService<TestExceptionHandlerData>();
        testExceptionHandlerData.Handled.ShouldBeTrue();
    }

    public class ProcessErrorAsync(ITestOutputHelper testOutputHelper)
    {
        private readonly ITestOutputHelper testOutputHelper = testOutputHelper;

        [Fact]
        public async Task Should_Use_DefaultExceptionHandlerAsync()
        {
            // Arrange
            var serviceProvider = ConfigureServiceProvider(
                services =>
                {
                    services.AddInfinityMessaging().ConfigureInMemoryBus();
                },
                testOutputHelper);

            // Act
            var bus = serviceProvider.GetRequiredService<IBroker>() as Infinity.Toolkit.Messaging.InMemory.InMemoryBus;
            var processErrorArgs = new ProcessErrorEventArgs(new Exception("Test"), "Test");
            var act = () => bus!.ProcessErrorAsync(processErrorArgs);

            // Assert
            var exception = await act.ShouldThrowAsync<Exception>();
            exception.Message.ShouldBe("Test");
        }

        [Fact]
        public async Task Should_Call_Registered_ErrorHandlerAsync()
        {
            // Arrange
            var testExceptionHandler = Substitute.For<TestExceptionHandler>(false);

            var serviceProvider = ConfigureServiceProvider(services =>
            {
                services
                    .AddInfinityMessaging()
                        .ConfigureInMemoryBus()
                        .AddExceptionHandler(testExceptionHandler);
            },
            testOutputHelper);

            // Act
            var inMemoryBroker = serviceProvider.GetRequiredService<IBroker>() as Infinity.Toolkit.Messaging.InMemory.InMemoryBus;
            var processErrorArgs = new ProcessErrorEventArgs(new Exception("Test"), "Test");
            var act = () => inMemoryBroker!.ProcessErrorAsync(processErrorArgs);

            // Assert
            var exception = await act.ShouldThrowAsync<Exception>();
            exception.Message.ShouldBe("Test");

            await testExceptionHandler.Received().TryHandleAsync("Test", Arg.Any<Exception>());
        }

        [Fact]
        public async Task With_Multiple_ExceptionHandler_Should_Be_Called_In_OrderAsync()
        {
            // Arrange
            var testExceptionHandler1 = Substitute.For<TestExceptionHandler>(false);
            var testExceptionHandler2 = Substitute.For<TestExceptionHandler>(true);

            var serviceProvider = ConfigureServiceProvider(
                services =>
                {
                    services
                        .AddInfinityMessaging()
                            .ConfigureInMemoryBus()
                            .AddExceptionHandler(testExceptionHandler1)
                            .AddExceptionHandler(testExceptionHandler2);
                },
                testOutputHelper);

            // Act
            var inMemoryBroker = serviceProvider.GetRequiredService<IBroker>() as Infinity.Toolkit.Messaging.InMemory.InMemoryBus;
            var processErrorArgs = new ProcessErrorEventArgs(new Exception("Test"), "Test");
            var task = () => inMemoryBroker!.ProcessErrorAsync(processErrorArgs);
            await task.Invoke();

            // Assert
            await testExceptionHandler1.Received(1).TryHandleAsync("Test", Arg.Any<Exception>());
            await testExceptionHandler2.Received(1).TryHandleAsync("Test", Arg.Any<Exception>());
        }
    }
}

public class TestMessageHandler : IMessageHandler<TestMessage>
{
    public Task Handle(IMessageHandlerContext<TestMessage> context, CancellationToken cancellationToken)
    {
        Activity.Current?.AddTag("testing.incoming.message.context", context);
        return Task.CompletedTask;
    }
}

public class TestMessageHandler2(IChannelProducer<TestMessage2> channelProducer) : IMessageHandler<TestMessage>
{
    private readonly IChannelProducer<TestMessage2> channelProducer = channelProducer;

    public Task Handle(IMessageHandlerContext<TestMessage> context, CancellationToken cancellationToken)
    {
        Activity.Current?.AddTag("testing.incoming.message.context", context);
        return Task.CompletedTask;
    }
}

public class TestExceptionHandler(bool result = false) : IMessagingExceptionHandler
{
    private readonly bool result = result;

    public ValueTask<bool> TryHandleAsync(string channelName, Exception exception)
    {
        return ValueTask.FromResult(result);
    }
}

public class TestExceptionHandler2(TestExceptionHandlerData testExceptionHandlerData) : IMessagingExceptionHandler
{
    public ValueTask<bool> TryHandleAsync(string channelName, Exception exception)
    {
        testExceptionHandlerData.Handled = true;
        return ValueTask.FromResult(true);
    }
}

public record TestExceptionHandlerData
{
    public bool Handled { get; set; }
}
