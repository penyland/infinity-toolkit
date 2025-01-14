using System.Text.Json.Serialization;

namespace Infinity.Toolkit.Tests.Messaging;

public record TestMessage(string Content);

public record TestMessage2(string Content);

public record TestMessageWithDecoratedPropertyName
{
    [JsonPropertyName("content")]
    public string Content { get; init; }
}

public record TestMessageHandlerData()
{
    public int NumberOfTimesInvoked { get; set; } = 0;

    public bool MessageHandlerInvoked { get; set; } = false;

    public Dictionary<string, string> MessageProperties { get; set; } = new();
}
