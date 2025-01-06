namespace Infinity.Toolkit.Slack.BlockKit;

public class ContextBlockBuilder(ContextBlock block)
{
    private readonly ContextBlock contextBlock = block;

    public ContextBlockBuilder AddElement(string text, string type = TextTypes.Markdown)
    {
        contextBlock.Elements ??= [];
        contextBlock.Elements.Add(new TextBlock
        {
            Text = text,
            Emoji = type == TextTypes.PlainText,
            Type = type
        });

        return this;
    }

    public ContextBlock Build()
    {
        return contextBlock;
    }
}
