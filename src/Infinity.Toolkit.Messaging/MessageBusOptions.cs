using Microsoft.Extensions.Configuration;

namespace Infinity.Toolkit.Messaging;

public sealed class MessageBusOptions
{
    /// <summary>
    /// Gets or sets the name of the application.
    /// </summary>
    public string ApplicationName { get; set; }

    /// <summary>
    /// Gets or sets the environment of the application.
    /// </summary>
    public string Environment { get; set; }

    /// <summary>
    /// Gets or sets the prefix to use for event types.
    /// </summary>
    public string EventTypeIdentifierPrefix { get; set; }

    /// <summary>
    /// An URI that identifies the context in which an event happened.
    /// Default value is "{broker-abbreviation}://{application-name}/{environment}/{channel-producer-name}".
    /// </summary>
    public Uri Source { get; set; }

    /// <summary>
    /// Gets or sets whether the message bus should start messaging brokers automatically. Brokers are only started if their property AutoStartListening is set to true or if force is set to true.
    /// </summary>
    public bool AutoStartListening { get; set; } = true;

    /// <summary>
    /// Gets or sets the amount of time to wait before starting to listen for messages.
    ///
    /// 0 = start listening immediately (default)
    /// > 0 = start after x seconds. Max value is 60 minutes.
    /// </summary>
    [Range(typeof(TimeSpan), "00:00", "01:00")]
    public TimeSpan AutoStartDelay { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Gets or sets the <see cref="JsonSerializerOptions"/> to use when deserializing the message body. This setting is global and will be used for all consumers and producers if not overridden on the consumer or producer level.
    /// </summary>
    [JsonIgnore]
    public JsonSerializerOptions? JsonSerializerOptions { get; set; }
}

internal class ConfigureMessageBusOptions(IConfiguration configuration) : IPostConfigureOptions<MessageBusOptions>
{
    private readonly IConfiguration configuration = configuration;

    public void PostConfigure(string? name, MessageBusOptions options)
    {
        options.ApplicationName ??= Environment.GetEnvironmentVariable("ASPNETCORE_APPLICATIONNAME")
            ?? Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);
        options.Environment ??= Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                            ?? "Production";

        options.EventTypeIdentifierPrefix ??= options.EventTypeIdentifierPrefix ?? configuration["MessageBus:EventTypePrefix"] ?? string.Empty;

        options.Source ??= new UriBuilder
        {
            Scheme = "mb", // Message Bus
            Host = options.ApplicationName,
            Path = options.Environment
        }.Uri;

        options.JsonSerializerOptions ??= new JsonSerializerOptions(JsonSerializerDefaults.Web);
    }
}
