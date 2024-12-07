namespace Infinity.Toolkit.Slack;

public record SlackSlashCommandPayload
{
    public string TeamId { get; set; }

    public string TeamDomain { get; set; }

    public string ChannelId { get; set; }

    public string ChannelName { get; set; }

    public string UserId { get; set; }

    public string UserName { get; set; }

    public string Command { get; set; }

    public string Text { get; set; }

    public string ResponseUrl { get; set; }

    public string TriggerId { get; set; }

    public string? ApiAppId { get; set; }

    public string Token { get; set; }
}

public static class SlackActionPayloadExtensions
{
    public static SlackSlashCommandPayload ToSlackActionPayload(this IFormCollection form, JsonSerializerOptions jsonSerializerOptions)
    {
        var dictionary = form.ToDictionary(kvp => kvp.Key, kvp => Uri.UnescapeDataString(kvp.Value.ToString()));
        var jsonString = JsonSerializer.Serialize(dictionary);
        var payload = JsonSerializer.Deserialize<SlackSlashCommandPayload>(jsonString, jsonSerializerOptions);

        return payload is null ? throw new InvalidOperationException("Failed to deserialize SlackActionPayload.") : payload;
    }
}
