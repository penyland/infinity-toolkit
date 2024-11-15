using Infinity.Toolkit.Slack.BlockKit;

namespace Infinity.Toolkit.Slack;

public class SlackMessageBuilder
{
    public SlackMessageBuilder()
    {
        SlackMessage = new SlackMessage
        {
            Blocks = []
        };
    }

    public SlackMessageBuilder(string channel)
    {
        SlackMessage = new SlackMessage
        {
            Channel = channel,
            Blocks = []
        };
    }

    public SlackMessageBuilder(SlackMessage slackMessage)
    {
        SlackMessage = slackMessage with { Blocks = [.. slackMessage.Blocks] };
    }

    public SlackMessage SlackMessage { get; }

    public SlackMessage Build()
    {
        return SlackMessage;
    }
}
