namespace Infinity.Toolkit.Slack.BlockKit;

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
