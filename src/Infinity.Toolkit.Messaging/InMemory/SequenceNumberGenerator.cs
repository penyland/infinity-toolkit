namespace Infinity.Toolkit.Messaging.InMemory;

internal class SequenceNumberGenerator
{
    private long currentValue = 0;

    public SequenceNumberGenerator()
    {
        var random = new Random(DateTimeOffset.UtcNow.Millisecond);
        currentValue = random.Next();
    }

    public long Generate()
    {
        return Interlocked.Increment(ref currentValue);
    }
}
