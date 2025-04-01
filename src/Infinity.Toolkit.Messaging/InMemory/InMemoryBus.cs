using Infinity.Toolkit.Messaging.Abstractions;
using Infinity.Toolkit.Messaging.Diagnostics;
using static Infinity.Toolkit.Messaging.Diagnostics.Errors;

namespace Infinity.Toolkit.Messaging.InMemory;

internal class InMemoryBus : IBroker
{
    private readonly InMemoryChannelClientFactory inMemoryChannelClient;
    private readonly InMemoryBusOptions inMemoryBusOptions;
    private readonly IOptionsMonitor<InMemoryChannelConsumerOptions> channelConsumerOptions;
    private readonly IOptionsMonitor<InMemoryChannelProducerOptions> channelProducerOptions;
    private readonly IServiceProvider serviceProvider;
    private readonly Metrics metrics;
    private readonly MessageBusOptions messageBusOptions;
    private readonly ConcurrentDictionary<int, InMemoryChannelProcessor> processorCache = new();
    private readonly ClientDiagnostics clientDiagnostics;

    public InMemoryBus(
        InMemoryChannelClientFactory inMemoryChannelClient,
        IOptions<MessageBusOptions> messageBusOptions,
        IOptions<InMemoryBusOptions> inMemoryBusOptions,
        IOptionsMonitor<InMemoryChannelConsumerOptions> channelConsumerOptions,
        IOptionsMonitor<InMemoryChannelProducerOptions> channelProducerOptions,
        IServiceProvider serviceProvider,
        Metrics metrics,
        ILogger<InMemoryBus> logger)
    {
        this.inMemoryChannelClient = inMemoryChannelClient;
        this.messageBusOptions = messageBusOptions.Value;
        this.inMemoryBusOptions = inMemoryBusOptions.Value;
        this.channelConsumerOptions = channelConsumerOptions;
        this.channelProducerOptions = channelProducerOptions;
        this.serviceProvider = serviceProvider;
        this.metrics = metrics;
        Logger = logger;
        Name = inMemoryBusOptions.Value.DisplayName;
        clientDiagnostics = new ClientDiagnostics(InMemoryBusDefaults.System, Name, InMemoryBusDefaults.System);
    }

    public string Name { get; }

    public bool IsProcessing => processorCache.Values.Any(x => x.IsProcessing);

    public bool AutoStartListening => inMemoryBusOptions.AutoStartListening;

    private ILogger<InMemoryBus> Logger { get; }

    public Task InitAsync()
    {
        Logger?.InitializingBus(Name);
        var channelConsumerRegistry = inMemoryBusOptions.ChannelConsumerRegistry.Where(t => t.Value.BrokerName == Name);
        foreach (var (eventType, registration) in channelConsumerRegistry)
        {
            var options = channelConsumerOptions.Get((string?)registration.Key);
            if (options is not null)
            {
                if (!string.IsNullOrEmpty(options.EventTypeName))
                {
                    Logger?.InitializingChannelConsumerWithEventType(options.ChannelName, options.EventTypeName!);
                }
                else
                {
                    Logger?.InitializingChannelConsumer(options.ChannelName);
                }

                var processor = GetChannelProcessor(options);
                if (processor is null)
                {
                    Logger?.ChannelProcessorNotFound(options.ChannelName);
                    throw new InvalidOperationException(LogMessages.ChannelProcessorNotFoundMessage);
                }

                processor.ProcessErrorAsync += ProcessErrorAsync;
                processor.ProcessMessageAsync += async (args) => await ProcessMessageAsync(args, options);
            }
            else
            {
                Logger?.ChannelOptionsNotFound(eventType);
                throw new InvalidOperationException($"{ChannelOptionsNotFound} {eventType}");
            }
        }

        // Initialize any channel producers if needed.
        foreach (var channelProducerRegistration in inMemoryBusOptions.ChannelProducerRegistry)
        {
            var options = channelProducerOptions.Get(channelProducerRegistration.Key);
            if (options is not null)
            {
                if (!string.IsNullOrEmpty(options.EventTypeName))
                {
                    Logger?.InitializingChannelProducerWithEventType(options.ChannelName, options.EventTypeName!);
                }
                else
                {
                    Logger?.InitializingChannelProducer(options.ChannelName);
                }
            }
            else
            {
                Logger?.ChannelOptionsNotFound(channelProducerRegistration.Key);
                throw new InvalidOperationException($"{ChannelOptionsNotFound} {channelProducerRegistration.Key}");
            }
        }

        return Task.CompletedTask;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var processor in processorCache.Values)
        {
            if (!processor.IsProcessing)
            {
                Logger?.StartProcessingChannelStart(processor.ChannelName);
                await processor.StartProcessingAsync(cancellationToken);
                Logger?.StartProcessingChannelStarted(processor.ChannelName);
            }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var processor in processorCache.Values)
        {
            if (processor.IsProcessing)
            {
                Logger?.StopProcessingChannelStart(processor.ChannelName);
                await processor.StopProcessingAsync(cancellationToken);
                Logger?.StopProcessingChannelStopped(processor.ChannelName);
            }
        }
    }

    internal Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        var exception = new MessageBusException(args.Broker, args.ChannelName, args.Exception.Message, args.Exception);
        var exceptionHandler = serviceProvider.GetRequiredService<MessagingExceptionHandler>();
        return exceptionHandler.HandleExceptionAsync(exception);
    }

    internal Task ProcessMessageAsync(ProcessMessageEventArgs args, InMemoryChannelConsumerOptions options)
    {
        Logger?.ProcessingMessage(args.Message.MessageId, args.ChannelName);
        MethodInfo? onProcessMessageAsync;

        // Messages sent by Infinity.Toolkit.Messaging will always have the Constants.EventTypeName property set which is the type of the message.
        if (args.Message.ApplicationProperties.TryGetValue(Constants.EventTypeName, out var property) && property is string eventType)
        {
            // The message has the Constants.EventTypeName property set, this means that the message is a message sent by Infinity.Toolkit.Messaging.
            try
            {
                // Get the type of the message from the registry
                if (inMemoryBusOptions.ChannelConsumerRegistry.TryGetValue(eventType, out var messageTypeRegistration))
                {
                    Logger?.EventTypeFound(eventType);
                    onProcessMessageAsync = CreateOnProcessMessageAsync(messageTypeRegistration.EventType);
                }
                else
                {
                    // Could not find the type of the message in the registry
                    // This means that the message type was not registered
                    // That's not a problem then we'll just handle the message as a raw message
                    Logger?.EventTypeNotFound(eventType);
                    onProcessMessageAsync = CreateOnProcessMessageAsync();
                }

            }
            catch (Exception ex)
            {
                throw new MessageBusException(Name, args.ChannelName, Reasons.EventTypeWasNotRegistered, ex);
            }
        }
        else
        {
            // The message does not have the Constants.EventTypeName property set, this means that the message is not a message sent by Infinity.Toolkit.Messaging.
            // TODO: Try resolve the message type from the CloudEvents.Type property
            onProcessMessageAsync = CreateOnProcessMessageAsync();
            //metrics.RecordMessageConsumed(Name, args.ChannelName, errortype: Reasons.EventTypeWasNotRegistered);
            Logger?.NoEventTypeFoundForMessage(args.Message.MessageId, args.ChannelName);
        }

        if (onProcessMessageAsync is not null)
        {
            using var scope = clientDiagnostics.CreateDiagnosticActivityScope(ActivityKind.Consumer, $"{DiagnosticProperty.OperationReceive} {args.ChannelName}", DiagnosticProperty.OperationProcess, args.Message.ApplicationProperties.ToDictionary());
            scope?.AddTag(DiagnosticProperty.MessagingMessageId, args.Message.MessageId);
            scope?.AddTag(DiagnosticProperty.MessagingDestinationName, args.ChannelName);

            return (Task)onProcessMessageAsync?.Invoke(this, [args, options])!;

        }
        else
        {
            metrics.RecordMessageConsumed(Name, args.ChannelName, errortype: Reasons.EventTypeWasNotRegistered);
            Logger?.CouldNotCreateMessageProcessorMethod();
            return Task.FromException(new InvalidOperationException(LogMessages.CouldNotCreateMessageProcessorMethod));
        }
    }

    internal async Task OnProcessRawMessageAsync(ProcessMessageEventArgs args, InMemoryChannelConsumerOptions channelConsumerOptions)
    {
        var startTime = ValueStopwatch.StartNew();

        var messageHandlerContext = new InMemoryBusMessageHandlerContext
        {
            Body = args.Message.Body,
            ChannelName = args.ChannelName,
            Headers = args.Message.ApplicationProperties,
            SequenceNumber = args.Message.SequenceNumber,
            EnqueuedTimeUtc = args.Message.EnqueuedTimeUtc,
            ScheduledEnqueueTime = args.Message.ScheduledEnqueueTime,
            ProcessMessageEventArgs = args
        };

        var messageHandlers = serviceProvider.GetServices<IMessageHandler>();
        var processDuration = ValueStopwatch.StartNew();
        // Parallell.ForEachAsync(messageHandlers, stoppingToken, async (messageHandler, token) => { });
        foreach (var messageHandler in messageHandlers)
        {
            using var activity = clientDiagnostics.CreateDiagnosticActivityScopeForMessageHandler(args.ChannelName, messageHandler.GetType(), args.Message.ApplicationProperties.ToDictionary());

            activity?.SetTag(DiagnosticProperty.MessagingMessageId, args.Message.MessageId);
            activity?.SetTag(DiagnosticProperty.MessageBusMessageType, DiagnosticProperty.MessageTypeUndefined);
            activity?.SetTag(DiagnosticProperty.MessageBusMessageHandler, messageHandler.GetType().FullName);

            activity?.AddEvent(new ActivityEvent(DiagnosticProperty.MessagingConsumerInvokingHandler));
            var messageHandlerExecutionTime = ValueStopwatch.StartNew();
            await messageHandler.Handle(messageHandlerContext, args.CancellationToken);
            metrics.RecordMessageHandlerElapsedTime(messageHandlerExecutionTime.GetElapsedTime().TotalMilliseconds, Name, args.ChannelName, messageHandler.GetType().Name);

            activity?.AddEvent(new ActivityEvent(DiagnosticProperty.MessagingConsumerInvokedHandler));
        }

        metrics.RecordMessagingProcessDuration(processDuration.GetElapsedTime().TotalMilliseconds, Name, args.ChannelName);
        metrics.RecordMessagingClientOperationDuration(startTime.GetElapsedTime().TotalMilliseconds, Name, args.ChannelName);
        metrics.RecordMessageConsumed(Name, args.ChannelName);
    }

    internal async Task OnProcessMessageAsync<TMessage>(ProcessMessageEventArgs args, InMemoryChannelConsumerOptions channelConsumerOptions)
        where TMessage : class
    {
        var startTime = ValueStopwatch.StartNew();
        var messageHandlers = serviceProvider.GetServices<IMessageHandler<TMessage>>();
        if (!messageHandlers.Any())
        {
            throw new InvalidOperationException($"No message handlers found for message type {typeof(TMessage).AssemblyQualifiedName ?? typeof(TMessage).Name}");
        }

        var jsonSerializerOptions =
            channelConsumerOptions.JsonSerializerOptions
            ?? inMemoryBusOptions.JsonSerializerOptions
            ?? messageBusOptions.JsonSerializerOptions
            ?? new JsonSerializerOptions();

        TMessage? message = default;
        if (channelConsumerOptions.AutoDeserializeMessages && args.Message.Body is not null)
        {
            try
            {
                message = args.Message.Body.ToObjectFromJson<TMessage>(jsonSerializerOptions) ?? throw new JsonException($"{CouldNotDeserializeJsonToType} {typeof(TMessage)}");
            }
            catch (JsonException)
            {
                Logger?.CouldNotDeserializeToType(typeof(TMessage).AssemblyQualifiedName ?? typeof(TMessage).Name);
            }
        }

        var messageHandlerContext = new InMemoryBrokerMessageHandlerContext<TMessage>
        {
            Message = message,
            Body = args.Message.Body!,
            ChannelName = args.ChannelName,
            Headers = args.Message.ApplicationProperties,
            SequenceNumber = args.Message.SequenceNumber,
            EnqueuedTimeUtc = args.Message.EnqueuedTimeUtc,
            ScheduledEnqueueTime = args.Message.ScheduledEnqueueTime,
            ProcessMessageEventArgs = args
        };

        var processDuration = ValueStopwatch.StartNew();
        foreach (var messageHandler in messageHandlers)
        {
            using var activity = clientDiagnostics.CreateDiagnosticActivityScopeForMessageHandler(args.ChannelName, messageHandler.GetType(), args.Message.ApplicationProperties.ToDictionary());

            activity?.SetTag(DiagnosticProperty.MessagingMessageId, args.Message.MessageId);
            activity?.SetTag(DiagnosticProperty.MessageBusMessageType, typeof(TMessage).FullName ?? string.Empty);
            activity?.SetTag(DiagnosticProperty.MessageBusMessageHandler, messageHandler.GetType().FullName);

            activity?.AddEvent(new ActivityEvent(DiagnosticProperty.MessagingConsumerInvokingHandler));
            var messageHandlerExecutionTime = ValueStopwatch.StartNew();
            await messageHandler.Handle(messageHandlerContext, args.CancellationToken);
            metrics.RecordMessageHandlerElapsedTime<TMessage>(messageHandlerExecutionTime.GetElapsedTime().TotalMilliseconds, Name, args.ChannelName, messageHandler.GetType().Name);

            activity?.AddEvent(new ActivityEvent(DiagnosticProperty.MessagingConsumerInvokedHandler));
        }

        metrics.RecordMessagingProcessDuration<TMessage>(processDuration.GetElapsedTime().TotalMilliseconds, Name, args.ChannelName);
        metrics.RecordMessagingClientOperationDuration<TMessage>(startTime.GetElapsedTime().TotalMilliseconds, Name, args.ChannelName);
        metrics.RecordMessageConsumed(Name, args.ChannelName, messageType: typeof(TMessage).Name);
    }

    private MethodInfo CreateOnProcessMessageAsync(Type? messageType = default)
    {
        var onProcessMessageAsync = messageType != default
            ? GetType().GetMethod(nameof(OnProcessMessageAsync), BindingFlags.Instance | BindingFlags.NonPublic)?.MakeGenericMethod(messageType)
            : GetType().GetMethod(nameof(OnProcessRawMessageAsync), BindingFlags.Instance | BindingFlags.NonPublic);

        return onProcessMessageAsync ?? throw new InvalidOperationException($"{CouldNotCreateMethodForMessageType} {messageType?.Name}");
    }

    private InMemoryChannelProcessor GetChannelProcessor(InMemoryChannelConsumerOptions options)
    {
        var processor = options.ChannelType switch
        {
            ChannelType.Topic => inMemoryChannelClient.GetChannelProcessor(options.ChannelName, options.SubscriptionName, options.Predicate),
            ChannelType.Queue => inMemoryChannelClient.GetChannelProcessor(options.ChannelName),
            _ => throw new NotSupportedException($"{ChannelTypeIsNotSupported} {options.ChannelType}")
        };

        processorCache.TryAdd(options.GetHashCode(), processor);

        return processor;
    }
}
