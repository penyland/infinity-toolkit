using Infinity.Toolkit.Messaging.Abstractions;
using Infinity.Toolkit.Messaging.Diagnostics;
using static Infinity.Toolkit.Messaging.Diagnostics.Errors;

namespace Infinity.Toolkit.Messaging.InMemory;

internal class InMemoryBus : IBroker
{
    private readonly InMemoryChannelClientFactory inMemoryChannelClient;
    private readonly InMemoryBusOptions inMemoryBusOptions;
    private readonly IOptionsMonitor<InMemoryChannelConsumerOptions> channelConsumerOptions;
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
        IServiceProvider serviceProvider,
        Metrics metrics,
        ILogger<InMemoryBus> logger)
    {
        this.inMemoryChannelClient = inMemoryChannelClient;
        this.messageBusOptions = messageBusOptions.Value;
        this.inMemoryBusOptions = inMemoryBusOptions.Value;
        this.channelConsumerOptions = channelConsumerOptions;
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
                    Logger?.InitializingChannelWithEventType(options.ChannelName, options.EventTypeName!);
                }
                else
                {
                    Logger?.InitializingChannel(options.ChannelName);
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
        MethodInfo? onProcessMessageAsync = default;

        if (options.IsDefault)
        {
            // Try to figure out the type of the message
            // The type of the message is always found in the ApplicationProperties in the CloudEvents.Type property if any.
            if (args.Message.ApplicationProperties.TryGetValue(CloudEvents.Type, out var property) && property is string cloudEventsType)
            {
                try
                {
                    if (inMemoryBusOptions.ChannelConsumerRegistry.TryGetValue(cloudEventsType, out var messageTypeRegistration))
                    {
                        onProcessMessageAsync = CreateOnProcessMessageAsync(messageTypeRegistration.EventType);
                    }
                    else
                    {
                        // Could not find the type of the message in the registry
                        // This means that the message type was not registered
                        // That's not a problem then we'll just handle the message as a raw message
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
                metrics.RecordMessageConsumed(Name, args.ChannelName, errortype: Reasons.EventTypeWasNotRegistered);
                Logger?.MissingCloudEventsTypeProperty(args.Message.MessageId, args.ChannelName);
            }
        }
        else
        {
            if (options.RequireCloudEventsTypeProperty)
            {
                if (args.Message.ApplicationProperties.TryGetValue(CloudEvents.Type, out var property) && property is string cloudEventsType)
                {
                    try
                    {
                        Logger?.ProcessingMessage(args.Message.MessageId, args.ChannelName, cloudEventsType);
                        var eventType = cloudEventsType?[(cloudEventsType.LastIndexOf('.') + 1)..] ?? string.Empty;

                        if (!eventType.Equals(options.EventTypeName))
                        {
                            Logger?.CloudEventsTypeMismatch(options.EventTypeName ?? string.Empty, eventType);
                            throw new InvalidOperationException(CloudEventsTypeNotFound);
                        }

                        if (!inMemoryBusOptions.ChannelConsumerRegistry.TryGetValue(options.EventType.AssemblyQualifiedName ?? string.Empty, out var messageTypeRegistration))
                        {
                            Logger?.EventTypeNotRegistered(eventType);
                            throw new InvalidOperationException($"{EventTypeWasNotRegistered} {CloudEvents.Type}");
                        }

                        onProcessMessageAsync = CreateOnProcessMessageAsync(messageTypeRegistration.EventType);
                    }
                    catch (Exception ex)
                    {
                        throw new MessageBusException(Name, args.ChannelName, Reasons.EventTypeWasNotRegistered, ex);
                    }
                }
                else
                {
                    metrics.RecordMessageConsumed(Name, args.ChannelName, errortype: Reasons.EventTypeWasNotRegistered);
                    Logger?.MissingCloudEventsTypeProperty(args.Message.MessageId, args.ChannelName);
                }
            }
            else
            {
                onProcessMessageAsync = CreateOnProcessMessageAsync(options.EventType);
            }
        }

        if (onProcessMessageAsync is not null)
        {
            using var scope = clientDiagnostics.CreateDiagnosticActivityScope(ActivityKind.Consumer, $"{DiagnosticProperty.OperationReceive} {args.ChannelName}", DiagnosticProperty.OperationProcess, args.Message.ApplicationProperties.ToDictionary());
            scope?.AddTag(DiagnosticProperty.MessagingMessageId, args.Message.MessageId);
            scope?.AddTag(DiagnosticProperty.MessagingDestinationName, args.ChannelName);

            var task = (Task)onProcessMessageAsync?.Invoke(this, [args, options])!;
            return task;

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
        foreach (var messageHandler in messageHandlers)
        {
            using var activity = clientDiagnostics.CreateDiagnosticActivityScopeForMessageHandler(args.ChannelName, messageHandler.GetType(), args.Message.ApplicationProperties.ToDictionary());

            activity?.SetTag(DiagnosticProperty.MessagingMessageId, args.Message.MessageId);
            activity?.SetTag(DiagnosticProperty.MessageBusMessageType, DiagnosticProperty.MessageTypeRaw);
            activity?.SetTag(DiagnosticProperty.MessageBusMessageHandler, messageHandler.GetType().FullName);

            activity?.AddEvent(new ActivityEvent(DiagnosticProperty.MessagingConsumerInvokingHandler));
            var messageHandlerExecutionTime = ValueStopwatch.StartNew();
            await messageHandler.Handle(messageHandlerContext, args.CancellationToken);
            metrics.RecordMessageHandlerElapsedTime<Envelope>(messageHandlerExecutionTime.GetElapsedTime().TotalMilliseconds, Name, args.ChannelName, messageHandler.GetType().Name);

            activity?.AddEvent(new ActivityEvent(DiagnosticProperty.MessagingConsumerInvokedHandler));
        }

        metrics.RecordMessagingProcessDuration<Envelope>(processDuration.GetElapsedTime().TotalMilliseconds, Name, args.ChannelName);
        metrics.RecordMessagingClientOperationDuration<Envelope>(startTime.GetElapsedTime().TotalMilliseconds, Name, args.ChannelName);
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

            activity?.AddEvent(new ActivityEvent(DiagnosticProperty.MessagingConsumerInvokingHandler));
            var messageHandlerExecutionTime = ValueStopwatch.StartNew();
            await messageHandler.Handle(messageHandlerContext, args.CancellationToken);
            metrics.RecordMessageHandlerElapsedTime<TMessage>(messageHandlerExecutionTime.GetElapsedTime().TotalMilliseconds, Name, args.ChannelName, messageHandler.GetType().Name);

            activity?.AddEvent(new ActivityEvent(DiagnosticProperty.MessagingConsumerInvokedHandler));
        }

        metrics.RecordMessagingProcessDuration<TMessage>(processDuration.GetElapsedTime().TotalMilliseconds, Name, args.ChannelName);
        metrics.RecordMessagingClientOperationDuration<TMessage>(startTime.GetElapsedTime().TotalMilliseconds, Name, args.ChannelName);
        metrics.RecordMessageConsumed<TMessage>(Name, args.ChannelName);
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
