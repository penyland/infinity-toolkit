namespace Infinity.Toolkit.Tests.Messaging;

public class MessageBusBuilderTests : TestBase
{
    [Fact]
    public void MapMessageHandler_Should_Succeed()
    {
        // Arrange
        var services = Substitute.For<IServiceCollection>();
        var messageBusBuilder = new MessageBusBuilder(services);

        // Act
        var result = messageBusBuilder.MapMessageHandler<TestMessage, TestMessageHandler>();

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void AddBroker_Should_Succeed()
    {
        // Arrange
        var services = Substitute.For<IServiceCollection>();
        var messageBusBuilder = new MessageBusBuilder(services);

        // Act
        var result = messageBusBuilder.AddBroker<TestBroker, DefaultMessageBrokerOptions>("brokerType", options => { });

        // Assert
        result.Should().NotBeNull();
    }
}

internal class DefaultMessageBrokerOptions : MessageBusBrokerOptions
{
}
