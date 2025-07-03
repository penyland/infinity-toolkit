using Microsoft.Extensions.Logging;

namespace Infinity.Toolkit.Tests.Messaging;

public class MessageBusTests(ITestOutputHelper testOutputHelper) : TestBase
{
    private readonly ILogger<MessageBus> logger = XunitLoggerFactory.CreateLogger<MessageBus>(testOutputHelper);

    [Fact]
    public async Task InitAsync_ShouldCallInitAsyncOnBroker()
    {
        // Arrange
        var broker = new TestBroker();

        var messageBus = new MessageBus([broker], logger);

        // Act
        await messageBus.InitAsync();

        // Assert
        broker.InitAsyncCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task InitAsync_ShouldCallInitAsyncOnAllBrokers()
    {
        // Arrange
        var broker1 = new TestBroker();
        var broker2 = new TestBroker();

        var messageBus = new MessageBus([broker1, broker2], logger);

        // Act
        await messageBus.InitAsync();

        // Assert
        broker1.InitAsyncCallCount.ShouldBe(1);
        broker2.InitAsyncCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task StartAsync_ShouldCallStartAsyncOnAllBrokers_WhenForceStartIsFalse()
    {
        // Arrange
        var broker1 = new TestBroker() { AutoStartListening = true };
        var broker2 = new TestBroker() { AutoStartListening = false };
        var messageBus = new MessageBus([broker1, broker2], logger);

        // Act
        await messageBus.InitAsync();
        await messageBus.StartAsync(cancellationToken: CancellationToken.None);

        // Assert
        broker1.StartAsyncCallCount.ShouldBe(1);
        broker2.StartAsyncCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task StartAsync_ShouldCallStartAsyncOnAllBrokers_WhenForceStartIsTrue()
    {
        // Arrange
        var broker1 = new TestBroker() { AutoStartListening = true };
        var broker2 = new TestBroker() { AutoStartListening = false };
        var messageBus = new MessageBus([broker1, broker2], logger);

        // Act
        await messageBus.InitAsync();
        await messageBus.StartAsync(true, CancellationToken.None);

        // Assert
        broker1.StartAsyncCallCount.ShouldBe(1);
        broker2.StartAsyncCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task StartAsync_ShouldNotCallStartAsyncOnAnyBroker_WhenBrokerAutoStartListeningIsFalse()
    {
        // Arrange
        var broker1 = new TestBroker() { AutoStartListening = false };
        var broker2 = new TestBroker() { AutoStartListening = false };
        var messageBus = new MessageBus([broker1, broker2], logger);

        // Act
        await messageBus.InitAsync();
        await messageBus.StartAsync(cancellationToken: CancellationToken.None);

        // Assert
        broker1.StartAsyncCallCount.ShouldBe(0);
        broker2.StartAsyncCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task StartAsync_ShouldCallStartAsyncOnSpecifiedBroker()
    {
        // Arrange
        var broker1 = new TestBroker() { AutoStartListening = false, Name = "broker1" };
        var broker2 = new TestBroker() { AutoStartListening = false, Name = "broker2" };
        var messageBus = new MessageBus([broker1, broker2], logger);

        // Act
        await messageBus.InitAsync();
        await messageBus.StartAsync(broker1, CancellationToken.None);

        // Assert
        broker1.StartAsyncCallCount.ShouldBe(1);
        broker2.StartAsyncCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task StopAsync_ShouldCallStopAsyncOnAllBrokers()
    {
        // Arrange
        var broker1 = new TestBroker();
        var broker2 = new TestBroker();
        var messageBus = new MessageBus([broker1, broker2], logger);

        // Act
        await messageBus.InitAsync();
        await messageBus.StopAsync(cancellationToken: CancellationToken.None);

        // Assert
        broker1.StopAsyncCallCount.ShouldBe(1);
        broker2.StopAsyncCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task StopAsync_ShouldCallStopAsyncOnSpecifiedBroker()
    {
        // Arrange
        var broker1 = new TestBroker() { Name = "broker1" };
        var broker2 = new TestBroker() { Name = "broker2" };
        var messageBus = new MessageBus([broker1, broker2], logger);

        // Act
        await messageBus.InitAsync();
        await messageBus.StopAsync(broker1, CancellationToken.None);

        // Assert
        broker1.StopAsyncCallCount.ShouldBe(1);
        broker2.StopAsyncCallCount.ShouldBe(0);
    }
}
