namespace Infinity.Toolkit.Messaging.InMemory;

public sealed class InMemoryBusOptions : MessageBusBrokerOptions
{
}

internal class ConfigureInMemoryBusOptions : IValidateOptions<InMemoryBusOptions>
{
    public ValidateOptionsResult Validate(string? name, InMemoryBusOptions options)
    {
        // Validate that the channel consumers are configured correctly and BrokerName is set
        foreach (var (messageTypeKey, registration) in options.ChannelConsumerRegistry)
        {
            if (string.IsNullOrWhiteSpace(registration.BrokerName))
            {
                return ValidateOptionsResult.Fail($"The InMemoryBus channel consumer '{messageTypeKey}' must have a broker name configured.");
            }
        }

        return ValidateOptionsResult.Success;
    }
}
