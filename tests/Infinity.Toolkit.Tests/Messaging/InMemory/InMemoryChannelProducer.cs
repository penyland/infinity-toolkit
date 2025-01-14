using Infinity.Toolkit.Messaging.Diagnostics;
using Infinity.Toolkit.Messaging.InMemory;
using Microsoft.Extensions.Options;

namespace Infinity.Toolkit.Tests.Messaging.InMemory;

public class InMemoryChannelProducerTests : TestBase
{
    public class SendAsync(ITestOutputHelper testOutputHelper)
    {
        [Fact]
        public async Task Should_SucceedAsync()
        {
            // Arrange
            var serviceProvider = ConfigureServiceProvider(services =>
            {
            });

            var sequenceNumberGenerator = new SequenceNumberGenerator();
            var clientFactory = new InMemoryChannelClientFactory(sequenceNumberGenerator, new XunitLoggerFactory(testOutputHelper));

            var channelProducerOptions = new TestOptionsMonitor<InMemoryChannelProducerOptions>(new InMemoryChannelProducerOptions()
            {
                ChannelName = "testChannel",
                EventTypeName = "testEvent",
                JsonSerializerOptions = new(),
                Source = new("http://localhost")
            });

            var messageBusOptions = Options.Create(new MessageBusOptions());
            var messageBusMetrics = new Metrics(new MockMeterFactory());
            var producer = new InMemoryChannelProducer<InMemoryMessage>(clientFactory, channelProducerOptions, messageBusOptions, messageBusMetrics);

            var testMessage = new InMemoryMessage("testMessage");

            // Act
            var task = () => producer.SendAsync(testMessage, "Internal", CancellationToken.None);

            // Assert
            await task.Should().NotThrowAsync();
        }
    }
}
