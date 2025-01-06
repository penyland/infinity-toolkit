namespace Infinity.Toolkit.Slack.BlockKit;

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
    public static BlocksBuilder AddSectionBlock(this BlocksBuilder builder, string text) => builder.AddSectionBlock(text, _ => { });

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

    public static BlocksBuilder AddDividerBlock(this BlocksBuilder builder)
    {
        builder.Blocks.Add(new DividerBlock());
        return builder;
    }

    public static BlocksBuilder AddContextBlock(this BlocksBuilder builder, Action<ContextBlockBuilder> action)
    {
        var contextBlock = new ContextBlock();
        var contextBlockBuilder = new ContextBlockBuilder(contextBlock);
        action(contextBlockBuilder);
        builder.Blocks.Add(contextBlockBuilder.Build());
        return builder;
    }
}
