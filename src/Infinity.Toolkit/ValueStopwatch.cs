// Slightly refactored version of https://github.com/dotnet/aspnetcore/blob/main/src/Shared/ValueStopwatch/ValueStopwatch.cs

using System.Diagnostics;

namespace Infinity.Toolkit;

/// <summary>
/// A value type representing a high-resolution stopwatch, suitable for use in performance-sensitive code.
/// </summary>
public readonly struct ValueStopwatch
{
    private readonly long startTimestamp;

    private ValueStopwatch(long startTimestamp)
    {
        this.startTimestamp = startTimestamp;
    }

    public bool IsActive => startTimestamp != 0;

    public static ValueStopwatch StartNew() => new(Stopwatch.GetTimestamp());

    /// <summary>
    /// Gets the total elapsed time measured by the current instance, in milliseconds.
    /// </summary>
    /// <returns>A timespan with the elapsed time for the ValueStopWatch.</returns>
    /// <exception cref="InvalidOperationException">Thrown if uninitalized.</exception>
    public TimeSpan GetElapsedTime()
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("An uninitialized, or 'default', ValueStopwatch cannot be used to get elapsed time.");
        }

        var end = Stopwatch.GetTimestamp();

        return Stopwatch.GetElapsedTime(startTimestamp, end);
    }
}
