using Infinity.Toolkit.Slack;
using Infinity.Toolkit.Slack.BlockKit;
using System.Text.Json;
using System.Text.Json.Serialization;

Console.WriteLine("Hello, World!");

var jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
    WriteIndented = true,
    Converters = { new BlockJsonConverter() }
};

var slackMessage1 = new SlackMessageBuilder().Build();

var slackMessage2 = new SlackMessageBuilder("channel").Build();

var slackMessage3 = new SlackMessageBuilder(slackMessage1).Build();

var slackMessage4 = new SlackMessageBuilder()
    .AddChannel("channel")
    .AddTimestamp("ts")
    .AddResponseType("responseType")
    .Build();

var slackMessage = new SlackMessageBuilder()
    .AddBlocks(blocks => blocks
        .AddHeaderBlock("Hello, World!")
        .AddDividerBlock()
        .AddSectionBlock("Hello, World!")
        .AddContextBlock(elements => elements
            .AddElement("Hello, World!")
            .AddElement("Peter Nylander")))
    .Build();
//.BuildAndSerialize(jsonSerializerOptions);

Console.WriteLine(slackMessage);
