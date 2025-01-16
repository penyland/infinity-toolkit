using Infinity.Toolkit.Messaging.InMemory;

namespace Infinity.Toolkit.Tests.Messaging.InMemory;

public class InMemoryChannelProcessor : TestBase
{
    public class StartProcessingAsync(ITestOutputHelper testOutputHelper)
    {
        [Fact]
        public async Task Should_SucceedAsync()
        {
            // Arrange
            var client = new InMemoryChannelClientFactory(new SequenceNumberGenerator(), new XunitLoggerFactory(testOutputHelper));

            var processor = client.GetChannelProcessor("testChannel");
            processor.ProcessMessageAsync = async (args) => { await Task.CompletedTask; };

            // Act
            await processor.StartProcessingAsync(CancellationToken.None);

            // Assert
            processor.IsProcessing.ShouldBeTrue();
        }

        [Fact]
        public async Task ProcessMessageAsync_Must_Be_Set()
        {
            // Arrange
            var client = new InMemoryChannelClientFactory(new SequenceNumberGenerator(), new XunitLoggerFactory(testOutputHelper));

            var processor = client.GetChannelProcessor("testChannel");

            // Act
            var action = () => processor.StartProcessingAsync(CancellationToken.None);

            // Assert
            await action.ShouldThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task MultipleCalls_Should_Fail_Async()
        {
            // Arrange
            var client = new InMemoryChannelClientFactory(new SequenceNumberGenerator(), new XunitLoggerFactory(testOutputHelper));

            var processor = client.GetChannelProcessor("testChannel");
            processor.ProcessMessageAsync = async (args) => { await Task.CompletedTask; };

            // Act
            var action1 = () => processor.StartProcessingAsync(CancellationToken.None);
            var action2 = () => processor.StartProcessingAsync(CancellationToken.None);

            // Assert
            await action1.ShouldNotThrowAsync();
            await action2.ShouldThrowAsync<InvalidOperationException>();
            processor.IsProcessing.ShouldBeTrue();
        }

        [Fact]
        public async Task When_Cancelled_Should_Not_Be_Processing()
        {
            // Arrange
            var client = new InMemoryChannelClientFactory(new SequenceNumberGenerator(), new XunitLoggerFactory(testOutputHelper));

            var processor = client.GetChannelProcessor("testChannel");
            processor.ProcessMessageAsync = async (args) => { await Task.CompletedTask; };

            // Act
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            var action = () => processor.StartProcessingAsync(cancellationTokenSource.Token);

            // Assert
            await action.ShouldThrowAsync<OperationCanceledException>();
            processor.IsProcessing.ShouldBeFalse();
        }
    }

    public class StopProcessingAsync(ITestOutputHelper testOutputHelper)
    {
        [Fact]
        public async Task Should_SucceedAsync()
        {
            // Arrange
            var client = new InMemoryChannelClientFactory(new SequenceNumberGenerator(), new XunitLoggerFactory(testOutputHelper));

            var processor = client.GetChannelProcessor("testChannel");
            processor.ProcessMessageAsync = async (args) => { await Task.CompletedTask; };

            // Act
            await processor.StartProcessingAsync(CancellationToken.None);
            await processor.StopProcessingAsync(CancellationToken.None);

            // Assert
            processor.IsProcessing.ShouldBeFalse();
        }

        [Fact]
        public async Task MultipleCalls_Should_Not_Fail()
        {
            // Arrange
            var client = new InMemoryChannelClientFactory(new SequenceNumberGenerator(), new XunitLoggerFactory(testOutputHelper));

            var processor = client.GetChannelProcessor("testChannel");
            processor.ProcessMessageAsync = async (args) => { await Task.CompletedTask; };

            // Act
            var cancellationTokenSource = new CancellationTokenSource();
            await processor.StartProcessingAsync(cancellationTokenSource.Token);
            await processor.StopProcessingAsync(cancellationTokenSource.Token);
            await processor.StopProcessingAsync(cancellationTokenSource.Token);

            // Assert
            processor.IsProcessing.ShouldBeFalse();
        }

        [Fact]
        public async Task Should_Throw_When_Cancelled()
        {
            // Arrange
            var client = new InMemoryChannelClientFactory(new SequenceNumberGenerator(), new XunitLoggerFactory(testOutputHelper));

            var processor = client.GetChannelProcessor("testChannel");
            processor.ProcessMessageAsync = async (args) => { await Task.CompletedTask; };

            // Act
            var cancellationTokenSource = new CancellationTokenSource();
            await processor.StartProcessingAsync(cancellationTokenSource.Token);
            cancellationTokenSource.Cancel();
            var action = () => processor.StopProcessingAsync(cancellationTokenSource.Token);

            // Assert
            await action.ShouldThrowAsync<OperationCanceledException>();
        }
    }
}
