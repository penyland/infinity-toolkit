using Infinity.Toolkit.Messaging.Abstractions;
using Microsoft.Extensions.Hosting;

namespace Infinity.Toolkit.Messaging;

internal class MessageBusBackgroundService(IMessageBus messageBus, IOptions<MessageBusOptions> options, IHostApplicationLifetime hostApplicationLifetime, ILogger<MessageBusBackgroundService> logger) : BackgroundService
{
    private readonly IMessageBus messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
    private readonly IOptions<MessageBusOptions> options = options;
    private readonly IHostApplicationLifetime hostApplicationLifetime = hostApplicationLifetime;

    public ILogger<MessageBusBackgroundService> Logger { get; } = logger;

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        await messageBus.StopAsync(stoppingToken);
        Logger?.MessageBusStopped();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Logger.MessageBusVersion(Assembly.GetExecutingAssembly().GetName().Version);
        Logger.MessageBusApplicationName(options.Value.ApplicationName);
        Logger.MessageBusEnvironment(options.Value.Environment);
        Logger.MessageBusCloudEventsSource(options.Value.Source);
        Logger.MessageBusAutoStartListening(options.Value.AutoStartListening);
        Logger.MessageBusAutoStartDelay(options.Value.AutoStartDelay);
        Logger.MessageBusEventTypeIdentifierPrefix(options.Value.EventTypeIdentifierPrefix);

        await messageBus.InitAsync();

        if (!await WaitForAppStartupAsync(hostApplicationLifetime, stoppingToken))
        {
            Logger?.ApplicationFailedToStart();
            return;
        }

        // Call messageBus.StartAsync() when the application starts if AutoStartListening is true.
        // Otherwise, call messageBus.StartAsync() after the specified delay AutoStartDelay.
        if (options.Value.AutoStartListening)
        {
            if (options.Value.AutoStartDelay > TimeSpan.Zero)
            {
                Logger?.MessageBusDelayedStart(options.Value.AutoStartDelay.TotalSeconds);
                await Task.Delay(options.Value.AutoStartDelay, stoppingToken);
            }

            await messageBus.StartAsync(cancellationToken: stoppingToken);
            Logger?.MessageBusStarted();
        }
        else
        {
            Logger?.MessageBusAutomaticStartDisabled();
        }
    }

    private async Task<bool> WaitForAppStartupAsync(IHostApplicationLifetime hostApplicationLifetime, CancellationToken stoppingToken)
    {
        var startedSource = new TaskCompletionSource<bool>();
        var cancelledSource = new TaskCompletionSource<bool>();

        hostApplicationLifetime.ApplicationStarted.Register(() => startedSource.TrySetResult(true));
        stoppingToken.Register(() => cancelledSource.SetResult(false));

        return await Task.WhenAny(startedSource.Task, cancelledSource.Task) == startedSource.Task;
    }
}
