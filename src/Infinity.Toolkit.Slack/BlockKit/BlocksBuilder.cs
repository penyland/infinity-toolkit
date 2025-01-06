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
