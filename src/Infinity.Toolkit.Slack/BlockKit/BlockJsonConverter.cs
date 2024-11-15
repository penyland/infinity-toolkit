using System.Text.Json.Serialization;

namespace Infinity.Toolkit.Slack.BlockKit;

public class BlockJsonConverter : JsonConverter<Block>
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsAssignableTo(typeof(Block)) && typeToConvert.IsAbstract;
    }

    public override Block? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (JsonDocument.TryParseValue(ref reader, out var document))
        {
            if (document.RootElement.TryGetProperty("type", out var typeElement))
            {
                var type = typeElement.GetString();
                var rootElement = document.RootElement.GetRawText();

                if (type != null && rootElement != null)
                {
                    return type switch
                    {
                        BlockTypes.Section => JsonSerializer.Deserialize<SectionBlock>(rootElement, options),
                        BlockTypes.Header => JsonSerializer.Deserialize<HeaderBlock>(rootElement, options),
                        _ => throw new JsonException()
                    };
                }
            }
        }

        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, Block value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
