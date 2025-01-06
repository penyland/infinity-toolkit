using Infinity.Toolkit.Slack.BlockKit;

namespace Infinity.Toolkit.Slack;

public static class SlackMessageBuilderExtensions
{
    public static SlackMessageBuilder AddChannel(this SlackMessageBuilder builder, string channel)
    {
        builder.SlackMessage.Channel = channel;
        return builder;
    }

    public static SlackMessageBuilder AddTimestamp(this SlackMessageBuilder builder, string ts)
    {
        builder.SlackMessage.Ts = ts;
        return builder;
    }

    

    public static SlackMessageBuilder AddResponseType(this SlackMessageBuilder builder, string responseType)
    {
        builder.SlackMessage.ResponseType = responseType;
        return builder;
    }    

    public static string BuildToJson(this SlackMessageBuilder builder, JsonSerializerOptions jsonSerializerOptions)
    {
        return JsonSerializer.Serialize(builder.Build(), jsonSerializerOptions);
    }

    public static SlackMessageBuilder AddBlocks(this SlackMessageBuilder builder, Action<BlocksBuilder> action)
    {
        var blocksBuilder = new BlocksBuilder();
        action(blocksBuilder);
        builder.SlackMessage.Blocks ??= [];
        builder.SlackMessage.Blocks.AddRange(blocksBuilder.Blocks);
        return builder;
    }
}
