namespace Infinity.Toolkit.Messaging.AzureServiceBus;

public static class ServiceBusMessageExtensions
{
    public static ServiceBusMessage ToServiceBusMessage(this Envelope message)
    {
        var serviceBusMessage = new ServiceBusMessage
        {
            Body = new BinaryData(message.Body),
            MessageId = message.MessageId,
            ContentType = message.ContentType,
            CorrelationId = message.CorrelationId,
        };

        // Add any user defined properties
        message.ApplicationProperties?.ForEach(x => serviceBusMessage.ApplicationProperties.Add(x));

        return serviceBusMessage;
    }
}
