using System.Diagnostics.Metrics;

namespace Infinity.Toolkit.Tests.TestUtils;

public sealed class MockMeterFactory : IMeterFactory
{
    public Meter Create(MeterOptions options)
    {
        return new Meter(options);
    }

    public void Dispose()
    {
    }
}
