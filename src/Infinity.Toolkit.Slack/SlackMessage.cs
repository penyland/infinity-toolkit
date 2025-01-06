using Infinity.Toolkit.Slack.BlockKit;
using System.Text.Json.Serialization;

namespace Infinity.Toolkit.Slack;

public record SlackMessage
{
    public string? Channel { get; set; }

    public string? Ts { get; set; }

    [JsonPropertyName("response_type")]
    public string? ResponseType { get; set; }

    public List<Block>? Blocks { get; internal set; }
}
