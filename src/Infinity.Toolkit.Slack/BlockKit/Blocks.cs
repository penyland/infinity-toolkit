using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Infinity.Toolkit.Slack.BlockKit;

public interface IBlock
{
    string Type { get; }
}

[JsonDerivedType(typeof(TextBlock))]
[JsonDerivedType(typeof(HeaderBlock))]
[JsonDerivedType(typeof(SectionBlock))]
public abstract record Block : IBlock
{
    public virtual string Type { get; init; } = string.Empty;
}

public record HeaderBlock : Block
{
    public override string Type { get; init; } = BlockTypes.Header;

    public required TextBlock Text { get; set; }
}

public record TextBlock : Block
{
    public TextBlock() { }

    [SetsRequiredMembers]
    public TextBlock(string text, string type = TextTypes.Markdown)
    {
        Text = text;
        Emoji = string.Equals(type, TextTypes.PlainText);
    }

    public override string Type { get; init; } = TextTypes.Markdown;

    public required string Text { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Emoji { get; set; } = false;
}

public record SectionBlock : Block
{
    public override string Type { get; init; } = BlockTypes.Section;

    public required TextBlock Text { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IList<TextBlock>? Fields { get; set; }
}

public record DividerBlock : Block
{
    public override string Type { get; init; } = BlockTypes.Divider;
}

public static class BlockTypes
{
    public const string Section = "section";
    public const string Divider = "divider";
    public const string Actions = "actions";
    public const string Context = "context";
    public const string Image = "image";
    public const string Header = "header";
}

public static class TextTypes
{
    public const string Markdown = "mrkdwn";
    public const string PlainText = "plain_text";
}
