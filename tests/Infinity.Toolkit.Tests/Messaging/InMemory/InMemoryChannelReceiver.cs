using Infinity.Toolkit.Messaging.InMemory;

namespace Infinity.Toolkit.Tests.Messaging.InMemory;

public class InMemoryChannelReceiverTests : TestBase
{
    public class DeferMessageAsync(ITestOutputHelper testOutputHelper)
    {
        [Fact]
        public async Task Should_SucceedAsync()
        {
            // Arrange
            var receiver = new InMemoryChannelReceiver("testChannel", new XunitLoggerFactory(testOutputHelper));
            var testMessage = new InMemoryMessage("testMessage");
            testMessage.SequenceNumber = 1;

            // Act
            var sequenceNumber = await receiver.DeferMessageAsync(testMessage, cancellationToken: CancellationToken.None);

            // Assert
            sequenceNumber.Should().Be(1);
        }

        [Fact]
        public async Task Then_ReceiveDeferred_Should_Succeed()
        {
            // Arrange
            var receiver = new InMemoryChannelReceiver("testChannel", new XunitLoggerFactory(testOutputHelper));
            var testMessage = new InMemoryMessage("testMessage");
            testMessage.SequenceNumber = 1;

            // Act
            var sequenceNumber = await receiver.DeferMessageAsync(testMessage, cancellationToken: CancellationToken.None);
            var message = await receiver.ReceiveDeferredMessageAsync(sequenceNumber);

            // Assert
            sequenceNumber.Should().Be(1);
            message.Should().NotBeNull();
            message.SequenceNumber.Should().Be(1);
            message.Body.ToString().Should().Be("testMessage");
        }

        [Fact]
        public async Task Same_Message_MultipleTimes_Should_Fail()
        {
            // Arrange
            var receiver = new InMemoryChannelReceiver("testChannel", new XunitLoggerFactory(testOutputHelper));
            var testMessage = new InMemoryMessage("testMessage");
            testMessage.SequenceNumber = 1;

            // Act
            await receiver.DeferMessageAsync(testMessage, cancellationToken: CancellationToken.None);
            var action2 = () => receiver.DeferMessageAsync(testMessage, cancellationToken: CancellationToken.None);

            // Assert
            await action2.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task Multiple_Messages_Should_Succeed()
        {
            // Arrange
            var receiver = new InMemoryChannelReceiver("testChannel", new XunitLoggerFactory(testOutputHelper));

            // Act
            var sequenceNumber1 = await receiver.DeferMessageAsync(new InMemoryMessage("testMessage1") { SequenceNumber = 1 }, cancellationToken: CancellationToken.None);
            var sequenceNumber2 = await receiver.DeferMessageAsync(new InMemoryMessage("testMessage2") { SequenceNumber = 2 }, cancellationToken: CancellationToken.None);

            // Assert
            sequenceNumber1.Should().Be(1);
            sequenceNumber2.Should().Be(2);
        }

        [Fact]
        public async Task Multiple_Messages_Multiple_Times_Should_Fail()
        {
            // Arrange
            var receiver = new InMemoryChannelReceiver("testChannel", new XunitLoggerFactory(testOutputHelper));

            // Act
            var sequenceNumber1 = await receiver.DeferMessageAsync(new InMemoryMessage("testMessage1") { SequenceNumber = 1 }, cancellationToken: CancellationToken.None);
            var sequenceNumber2 = await receiver.DeferMessageAsync(new InMemoryMessage("testMessage1") { SequenceNumber = 2 }, cancellationToken: CancellationToken.None);
            var action = () => receiver.DeferMessageAsync(new InMemoryMessage("testMessage2") { SequenceNumber = 2 }, cancellationToken: CancellationToken.None);

            // Assert
            sequenceNumber1.Should().Be(1);
            sequenceNumber2.Should().Be(2);
            await action.Should().ThrowAsync<InvalidOperationException>();
        }
    }

    public class ReceiveDeferredMessageAsync(ITestOutputHelper testOutputHelper)
    {
        [Fact]
        public async Task Should_Succeed()
        {
            // Arrange
            var receiver = new InMemoryChannelReceiver("testChannel", new XunitLoggerFactory(testOutputHelper));
            var testMessage = new InMemoryMessage("testMessage");
            testMessage.SequenceNumber = 1;

            // Act
            var sequenceNumber = await receiver.DeferMessageAsync(testMessage, cancellationToken: CancellationToken.None);
            var message = await receiver.ReceiveDeferredMessageAsync(sequenceNumber);

            // Assert
            message.Should().NotBeNull();
            message.SequenceNumber.Should().Be(1);
            message.Body.ToString().Should().Be("testMessage");
        }

        [Fact]
        public async Task MultipleTimes_Should_Fail()
        {
            // Arrange
            var receiver = new InMemoryChannelReceiver("testChannel", new XunitLoggerFactory(testOutputHelper));
            var testMessage = new InMemoryMessage("testMessage");
            testMessage.SequenceNumber = 1;

            // Act
            var sequenceNumber = await receiver.DeferMessageAsync(testMessage, cancellationToken: CancellationToken.None);
            await receiver.ReceiveDeferredMessageAsync(sequenceNumber);
            var action = () => receiver.ReceiveDeferredMessageAsync(sequenceNumber);

            // Assert
            await action.Should().ThrowAsync<InvalidOperationException>();
        }
    }
}
