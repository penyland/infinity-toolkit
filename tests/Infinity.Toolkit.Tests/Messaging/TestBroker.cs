using Infinity.Toolkit.Messaging.Abstractions;

namespace Infinity.Toolkit.Tests.Messaging;

internal class TestBroker : IMessagingBroker
{
    public int InitAsyncCallCount { get; private set; }

    public int StartAsyncCallCount { get; private set; }

    public int StopAsyncCallCount { get; private set; }

    public string Name { get; set; } = "TestBroker";

    public bool IsProcessing { get; private set; } = false;

    public bool AutoStartListening { get; init; } = true;

    public Task InitAsync()
    {
        InitAsyncCallCount++;
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        IsProcessing = true;
        StartAsyncCallCount++;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        IsProcessing = false;
        StopAsyncCallCount++;
        return Task.CompletedTask;
    }
}
