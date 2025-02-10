using Infinity.Toolkit.Messaging.Diagnostics;

namespace Infinity.Toolkit.Messaging.InMemory;

internal sealed class InMemoryChannelProcessor : IAsyncDisposable
{
    private readonly InMemoryChannelReceiver inMemoryChannelReceiver;
    private readonly ChannelReader<InMemoryMessage> reader;

    private readonly SemaphoreSlim processingStartStopSemaphore = new(1, 1);

    private Task ActiveProcessorTask { get; set; }

    private CancellationTokenSource RunningTaskTokenSource { get; set; }

    private ILogger<InMemoryChannelProcessor> Logger { get; }

    private InMemoryChannelProcessor(string channelName, ChannelReader<InMemoryMessage> reader, InMemoryChannelReceiver receiver, ILogger<InMemoryChannelProcessor> logger)
    {
        ChannelName = channelName;
        this.reader = reader;
        Logger = logger;
        inMemoryChannelReceiver = receiver;
    }

    public string ChannelName { get; private set; }

    public bool IsProcessing => ActiveProcessorTask is not null && !ActiveProcessorTask.IsCompleted;

    public Func<ProcessMessageEventArgs, Task> ProcessMessageAsync { get; set; } = default!;

    public Func<ProcessErrorEventArgs, Task> ProcessErrorAsync { get; set; } = default!;

    public static InMemoryChannelProcessor Create(string channelName, ChannelReader<InMemoryMessage> reader, InMemoryChannelReceiver receiver, ILoggerFactory loggerFactory)
    {
        if (string.IsNullOrEmpty(channelName))
        {
            throw new ArgumentException($"'{nameof(channelName)}' cannot be null or empty.", nameof(channelName));
        }

        return new InMemoryChannelProcessor(channelName, reader, receiver, loggerFactory.CreateLogger<InMemoryChannelProcessor>());
    }

    public async Task StartProcessingAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var releaseSemaphore = true;

        try
        {
            await processingStartStopSemaphore.WaitAsync(cancellationToken);
            releaseSemaphore = true;
            if (ActiveProcessorTask is null)
            {
                if (ProcessMessageAsync is null)
                {
                    throw new InvalidOperationException(LogMessages.ProcessMessageAsyncNotSet);
                }

                try
                {
                    RunningTaskTokenSource?.Cancel();
                    RunningTaskTokenSource?.Dispose();
                    RunningTaskTokenSource = new CancellationTokenSource();

                    ActiveProcessorTask = RunAsync(RunningTaskTokenSource.Token);
                }
                catch (Exception)
                {
                    Logger.AnErrorOccurredWhileStartingTheProcessor(InMemoryBusDefaults.Name, ChannelName);
                    throw;
                }

                Logger?.StartProcessingChannelStarted(ChannelName);
            }
            else
            {
                throw new InvalidOperationException(LogMessages.ChannelProcessorIsAlreadyRunning);
            }
        }
        finally
        {
            if (releaseSemaphore)
            {
                processingStartStopSemaphore.Release();
            }
        }
    }

    public async Task StopProcessingAsync(CancellationToken cancellationToken = default)
    {
        var releaseSemaphore = false;
        try
        {
            await processingStartStopSemaphore.WaitAsync(cancellationToken);
            releaseSemaphore = true;

            if (ActiveProcessorTask != null)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    RunningTaskTokenSource.Cancel();
                }
                catch (Exception exception)
                {
                    Logger.StopProcessingCancellationWarning(ChannelName, exception.ToString());
                }

                RunningTaskTokenSource.Dispose();
                RunningTaskTokenSource = null!;

                try
                {
                    await ActiveProcessorTask;
                }
                catch (OperationCanceledException)
                {
                }
                finally
                {
                    ActiveProcessorTask.Dispose();
                    ActiveProcessorTask = null!;
                }
            }
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            if (releaseSemaphore)
            {
                processingStartStopSemaphore.Release();
            }
        }
    }

    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        if (IsProcessing)
        {
            await StopProcessingAsync(cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await CloseAsync();
        processingStartStopSemaphore.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var messages = reader.ReadAllAsync(cancellationToken);
            await foreach (var message in messages)
            {
                try
                {
                    var receivedMessage = new InMemoryReceivedMessage(message);
                    await ProcessMessageAsync!.Invoke(new ProcessMessageEventArgs(receivedMessage, ChannelName, inMemoryChannelReceiver, cancellationToken: cancellationToken));
                }
                catch (Exception ex)
                {
                    var errorArgs = new ProcessErrorEventArgs(ex, ChannelName);
                    ProcessErrorAsync?.Invoke(errorArgs);
                }
            }
        }
    }
}
