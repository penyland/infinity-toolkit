namespace Infinity.Toolkit.Slack;

public class SlackMessageBuilder
{
    public SlackMessageBuilder()
    {
        SlackMessage = new();
    }

    public SlackMessageBuilder(string channel)
    {
        SlackMessage = new SlackMessage
        {
            Channel = channel
        };
    }

    public SlackMessageBuilder(SlackMessage slackMessage)
    {
        SlackMessage = slackMessage with { Blocks = [.. slackMessage.Blocks ?? []] };
    }

    internal SlackMessage SlackMessage { get; }

    public SlackMessage Build()
    {
        return SlackMessage;
    }
}
