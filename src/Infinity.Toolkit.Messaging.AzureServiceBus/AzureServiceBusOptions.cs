using Azure.Core;
using Azure.Identity;

namespace Infinity.Toolkit.Messaging.AzureServiceBus;

/// <summary>
/// Represents the options for the Azure Service Bus.
/// </summary>
public sealed class AzureServiceBusOptions : MessageBusBrokerOptions
{
    /// <summary>
    /// Gets the connection string to use for connecting to the Service Bus namespace.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets the fully qualified Service Bus namespace to connect to.
    /// </summary>
    public string FullyQualifiedNamespace { get; set; }

    /// <summary>
    /// Gets the Azure managed identity credential to use for authorization.
    /// </summary>
    [JsonIgnore]
    public TokenCredential? TokenCredential { get; set; }

    /// <summary>
    /// The set of <see cref="ServiceBusClientOptions"/> to use for configuring this <see cref="AzureServiceBusBroker"/>.
    /// </summary>
    public ServiceBusClientOptions? ServiceBusClientOptions { get; set; } = new();
}

internal class ConfigureAzureServiceBusOptions : IValidateOptions<AzureServiceBusOptions>
{
    public ValidateOptionsResult Validate(string? name, AzureServiceBusOptions options)
    {
        if (options.FullyQualifiedNamespace is null && string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return ValidateOptionsResult.Fail("The Azure Service Bus broker credentials must have either a connection string or a fully qualified namespace configured.");
        }

        if (options.FullyQualifiedNamespace is not null && options.TokenCredential is null)
        {
            options.TokenCredential = new DefaultAzureCredential();
        }

        // Validate that the channel consumers are configured correctly and BrokerName is set
        foreach (var (messageTypeKey, registration) in options.ChannelConsumerRegistry)
        {
            if (string.IsNullOrWhiteSpace(registration.BrokerName))
            {
                return ValidateOptionsResult.Fail($"The Azure Service Bus broker channel consumer '{messageTypeKey}' must have a broker name configured.");
            }
        }

        return ValidateOptionsResult.Success;
    }
}
