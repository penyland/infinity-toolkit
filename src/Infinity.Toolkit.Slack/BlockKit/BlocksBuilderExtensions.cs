namespace Infinity.Toolkit.Slack.BlockKit;

public sealed class BlocksBuilder
{
    public BlocksBuilder()
    {
    }

    public List<Block> Blocks { get; init; } = [];

    public BlocksBuilder AddDividerBlock()
    {
        Blocks.Add(new DividerBlock());
        return this;
    }

    public List<Block> Build()
    {
        return Blocks;
    }
}

public class SectionBlockBuilder(SectionBlock block)
{
    private readonly SectionBlock sectionBlock = block;

    public SectionBlockBuilder AddField(string text, string type = TextTypes.Markdown)
    {
        sectionBlock.Fields ??= [];
        sectionBlock.Fields.Add(new TextBlock
        {
            Text = text,
            Emoji = type == TextTypes.PlainText,
            Type = type
        });

        return this;
    }

    public SectionBlock Build()
    {
        return sectionBlock;
    }
}


public static class BlocksBuilderExtensions
{
    public static BlocksBuilder AddHeaderBlock(this BlocksBuilder builder, string text)
    {
        builder.Blocks.Add(new HeaderBlock
        {
            Text = new TextBlock
            {
                Type = TextTypes.PlainText,
                Text = text,
                Emoji = true
            }
        });
        return builder;
    }

    public static BlocksBuilder AddSectionBlock(this BlocksBuilder builder, string text, Action<SectionBlockBuilder> action)
    {
        var sectionBlock = new SectionBlock
        {
            Text = new TextBlock
            {
                Type = TextTypes.Markdown,
                Text = text
            }
        };

        var sectionBlockBuilder = new SectionBlockBuilder(sectionBlock);
        action(sectionBlockBuilder);

        builder.Blocks.Add(sectionBlockBuilder.Build());

        return builder;
    }

    public static SlackMessageBuilder AddDividerBlock(this SlackMessageBuilder builder)
    {
        builder.SlackMessage.Blocks.Add(new DividerBlock());

        return builder;
    }

    public static SlackMessageBuilder AddHeaderBlock(this SlackMessageBuilder builder, string text)
    {
        builder.SlackMessage.Blocks.Add(new HeaderBlock
        {
            Text = new TextBlock
            {
                Type = TextTypes.PlainText,
                Text = text,
                Emoji = true
            }
        });

        return builder;
    }
    public static SlackMessageBuilder AddSectionBlock(this SlackMessageBuilder builder, string text) => builder.AddSectionBlock(text, _ => { });

    public static SlackMessageBuilder AddSectionBlock(this SlackMessageBuilder builder, string text, Action<SectionBlockBuilder> action)
    {
        var sectionBlock = new SectionBlock
        {
            Text = new TextBlock
            {
                Type = TextTypes.Markdown,
                Text = text
            }
        };

        var sectionBlockBuilder = new SectionBlockBuilder(sectionBlock);
        action(sectionBlockBuilder);

        builder.SlackMessage.Blocks.Add(sectionBlockBuilder.Build());

        return builder;
    }
}
