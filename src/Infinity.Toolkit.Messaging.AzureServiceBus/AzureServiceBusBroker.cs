using System.Collections.Concurrent;
using System.Reflection;
using Infinity.Toolkit.Messaging.Abstractions;
using Infinity.Toolkit.Messaging.AzureServiceBus;
using Infinity.Toolkit.Messaging.Diagnostics;
using static Infinity.Toolkit.Messaging.Diagnostics.Errors;

namespace Infinity.Toolkit.Messaging.AzureServiceBus;

internal class AzureServiceBusBroker : IBroker
{
    private readonly AzureServiceBusClientFactory azureServiceBusClientFactory;
    private readonly AzureServiceBusOptions brokerOptions;
    private readonly IOptionsMonitor<AzureServiceBusChannelConsumerOptions> channelOptions;
    private readonly IServiceProvider serviceProvider;
    private readonly Metrics messageBusMetrics;
    private readonly MessageBusOptions messageBusOptions;
    private readonly ConcurrentDictionary<int, ServiceBusProcessor> processorCache = new();
    private readonly ClientDiagnostics clientDiagnostics;

    public AzureServiceBusBroker(
        AzureServiceBusClientFactory azureServiceBusClientFactory,
        IOptions<MessageBusOptions> messageBusOptions,
        IOptions<AzureServiceBusOptions> brokerOptions,
        IOptionsMonitor<AzureServiceBusChannelConsumerOptions> channelOptions,
        IServiceProvider serviceProvider,
        Metrics messageBusMetrics,
        ILogger<AzureServiceBusBroker>? logger)
    {
        this.azureServiceBusClientFactory = azureServiceBusClientFactory;
        this.messageBusOptions = messageBusOptions.Value;
        this.brokerOptions = brokerOptions.Value;
        this.channelOptions = channelOptions;
        this.serviceProvider = serviceProvider;
        this.messageBusMetrics = messageBusMetrics;
        Logger = logger;
        Name = brokerOptions.Value.DisplayName;
        clientDiagnostics = new ClientDiagnostics(AzureServiceBusDefaults.System, Name, AzureServiceBusDefaults.System);
    }

    public string Name { get; }

    public bool IsProcessing => processorCache.Values.Any(x => x.IsProcessing);

    public bool AutoStartListening => brokerOptions.AutoStartListening;

    private ILogger<AzureServiceBusBroker>? Logger { get; }

    public Task InitAsync()
    {
        Logger?.InitializingBus(Name);
        var channelConsumerRegistry = brokerOptions.ChannelConsumerRegistry.Where(r => r.Value.BrokerName == Name);
        foreach (var (eventType, registration) in channelConsumerRegistry)
        {
            var options = channelOptions.Get((string?)registration.Key);

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

                processor.ProcessMessageAsync += (args) => ProcessMessageAsync(args, options);

                processor.ProcessErrorAsync += ProcessErrorAsync;
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
                Logger?.StartProcessingChannelStart(processor.EntityPath);
                await processor.StartProcessingAsync(cancellationToken);
                Logger?.StartProcessingChannelStarted(processor.EntityPath);
            }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var processor in processorCache.Values)
        {
            if (processor.IsProcessing)
            {
                Logger?.StopProcessingChannelStart(processor.EntityPath);
                await processor.StopProcessingAsync(cancellationToken);
                Logger?.StopProcessingChannelStopped(processor.EntityPath);
            }
        }
    }

    internal Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        var exception = new MessageBusException(Name, args.EntityPath, args.Exception.Message, args.Exception);
        var exceptionHandler = serviceProvider.GetRequiredService<MessagingExceptionHandler>();
        return exceptionHandler.HandleExceptionAsync(exception);
    }

    internal Task ProcessMessageAsync(ProcessMessageEventArgs args, AzureServiceBusChannelConsumerOptions options)
    {
        MethodInfo? onProcessMessageAsync = default;

        if (!options.RequireCloudEventsTypeProperty)
        {
            // Check if message type is registered in the registry
            // If the RequireCloudEventsTypeProperty is false, we can't use the CloudEventsType property to determine the message type.
            // The CloudEventsType property is only available if the message has been produced by the Infinity.Toolkit.Messaging library.
            // If the message has been produced by another library, the CloudEventsType property will not be available and we have to use the EventType property defined in the channel options.

            // But what do we do if the EventType property is not defined in the channel options and we still want to process the message?
            // Then we can't use the EventType property to resolve any message handlers.
            // We have to try to process the message without a message type using an untyped message handler.
            // If we can't find any untyped message handlers, we have to log an error and skip the message.
            // The user must explicitly add an untyped message handler to the message bus to be able to process messages without a message type.
            // This means that the user must add a message handler that implements IMessageHandler<IMessageHandlerContext> to the message bus and handle deserialization and processing of the message manually.

            // If the EventType property is defined in the channel options, we can use it to resolve the message type and process the message.
            onProcessMessageAsync = CreateOnProcessMessageAsync(options.EventType);
        }
        else
        {
            if (options.RequireCloudEventsTypeProperty && args.Message.ApplicationProperties.TryGetValue(CloudEvents.Type, out var property) && property is string cloudEventsType)
            {
                try
                {
                    Logger?.ProcessingMessage(args.Message.MessageId, args.EntityPath, cloudEventsType);
                    var eventType = cloudEventsType?[(cloudEventsType.LastIndexOf('.') + 1)..] ?? string.Empty;

                    if (!eventType.Equals(options.EventTypeName))
                    {
                        Logger?.CloudEventsTypeMismatch(options.EventTypeName ?? string.Empty, eventType);
                        throw new InvalidOperationException(CloudEventsTypeNotFound);
                    }

                    // Check if message type is registered in the registry
                    if (!brokerOptions.ChannelConsumerRegistry.TryGetValue(options.EventType.AssemblyQualifiedName ?? string.Empty, out var messageTypeRegistration))
                    {
                        Logger?.EventTypeNotRegistered(eventType);
                        throw new InvalidOperationException($"{EventTypeWasNotRegistered} {CloudEvents.Type}");
                    }

                    // Create generic method for processing message.
                    onProcessMessageAsync = CreateOnProcessMessageAsync(messageTypeRegistration.EventType);
                }
                catch (Exception ex)
                {
                    throw new MessageBusException(Name, args.EntityPath, Reasons.EventTypeWasNotRegistered, ex);
                }
            }
            else
            {
                messageBusMetrics.RecordMessageConsumed(Name, args.EntityPath, errortype: Reasons.EventTypeWasNotRegistered);
                Logger?.MissingCloudEventsTypeProperty(args.Message.MessageId, args.EntityPath);
            }
        }

        if (onProcessMessageAsync is not null)
        {
            using var scope = clientDiagnostics.CreateDiagnosticActivityScope(ActivityKind.Consumer, $"{DiagnosticProperty.OperationReceive} {args.EntityPath}", DiagnosticProperty.OperationProcess, args.Message.ApplicationProperties.ToDictionary());
            scope?.AddTag(DiagnosticProperty.MessagingMessageId, args.Message.MessageId);
            scope?.AddTag(DiagnosticProperty.MessagingDestinationName, args.EntityPath);

            return (Task)onProcessMessageAsync?.Invoke(this, [args, options])!;
        }
        else
        {
            messageBusMetrics.RecordMessageConsumed(Name, args.EntityPath, errortype: Reasons.EventTypeWasNotRegistered);
            Logger?.CouldNotCreateMessageProcessorMethod();
            return Task.FromException(new InvalidOperationException(LogMessages.CouldNotCreateMessageProcessorMethod));
        }
    }

    internal async Task OnProcessRawMessageAsync(ProcessMessageEventArgs args, AzureServiceBusChannelConsumerOptions options)
    {
        var startTime = ValueStopwatch.StartNew();

        var messageHandlerContext = new AzureServiceBusMessageHandlerContext
        {
            Body = args.Message.Body,
            ChannelName = args.EntityPath,
            Headers = args.Message.ApplicationProperties,
            SequenceNumber = args.Message.SequenceNumber,
            EnqueuedTimeUtc = args.Message.EnqueuedTime,
            ScheduledEnqueueTime = args.Message.ScheduledEnqueueTime,
            ProcessMessageEventArgs = args
        };

        var messageHandlers = serviceProvider.GetServices<IMessageHandler>();
        // Use execution strategy
        var processDuration = ValueStopwatch.StartNew();
        foreach (var messageHandler in messageHandlers)
        {
            using var activity = clientDiagnostics.CreateDiagnosticActivityScopeForMessageHandler(args.EntityPath, messageHandler.GetType(), args.Message.ApplicationProperties.ToDictionary());

            activity?.SetTag(DiagnosticProperty.MessagingMessageId, args.Message.MessageId);
            activity?.SetTag(DiagnosticProperty.MessageBusMessageType, DiagnosticProperty.MessageTypeRaw);
            activity?.SetTag(DiagnosticProperty.MessageBusMessageHandler, messageHandler.GetType().FullName);

            activity?.AddEvent(new ActivityEvent(DiagnosticProperty.MessagingConsumerInvokingHandler));
            var messageHandlerExecutionTime = ValueStopwatch.StartNew();
            await messageHandler.Handle(messageHandlerContext, args.CancellationToken);
            messageBusMetrics.RecordMessageHandlerElapsedTime<Envelope>(messageHandlerExecutionTime.GetElapsedTime().TotalMilliseconds, Name, args.EntityPath, messageHandler.GetType().Name);

            activity?.AddEvent(new ActivityEvent(DiagnosticProperty.MessagingConsumerInvokedHandler));
        }

        messageBusMetrics.RecordMessagingProcessDuration<Envelope>(processDuration.GetElapsedTime().TotalMilliseconds, Name, args.EntityPath);
        messageBusMetrics.RecordMessagingClientOperationDuration<Envelope>(startTime.GetElapsedTime().TotalMilliseconds, Name, args.EntityPath);
        messageBusMetrics.RecordMessageConsumed(Name, args.EntityPath);
    }

    internal async Task OnProcessMessageAsync<TMessage>(ProcessMessageEventArgs args, AzureServiceBusChannelConsumerOptions options)
        where TMessage : class
    {
        var startTime = ValueStopwatch.StartNew();

        var messageHandlers = serviceProvider.GetServices<IMessageHandler<TMessage>>();
        if (!messageHandlers.Any())
        {
            throw new InvalidOperationException($"No message handlers found for message type {typeof(TMessage).AssemblyQualifiedName ?? typeof(TMessage).Name}");
        }

        var jsonSerializerOptions =
            options.JsonSerializerOptions
            ?? brokerOptions.JsonSerializerOptions
            ?? messageBusOptions.JsonSerializerOptions
            ?? new JsonSerializerOptions();

        // If we can't deserialize the message just skip deserialization and log an error. Then invoke the message handlers with a null message and just pass the raw message body.
        TMessage? message = default;
        if (options.AutoDeserializeMessages && args.Message.Body is not null)
        {
            try
            {
                message = args.Message.Body.ToObjectFromJson<TMessage>(jsonSerializerOptions) ?? throw new JsonException($"{CouldNotDeserializeJsonToType} {typeof(TMessage)}");
            }
            catch (JsonException)
            {
                // Log error and continue processing the message
                Logger?.CouldNotDeserializeToType(typeof(TMessage).AssemblyQualifiedName ?? typeof(TMessage).Name);
            }
        }

        var messageHandlerContext = new AzureServiceBusBrokerMessageHandlerContext<TMessage>
        {
            Message = message,
            Body = args.Message.Body!,
            ChannelName = args.EntityPath,
            Headers = args.Message.ApplicationProperties,
            SequenceNumber = args.Message.SequenceNumber,
            EnqueuedTimeUtc = args.Message.EnqueuedTime,
            ScheduledEnqueueTime = args.Message.ScheduledEnqueueTime,
            ProcessMessageEventArgs = args
        };

        // Use execution strategy
        var processDuration = ValueStopwatch.StartNew();
        foreach (var messageHandler in messageHandlers)
        {
            using var activity = clientDiagnostics.CreateDiagnosticActivityScopeForMessageHandler(args.EntityPath, messageHandler.GetType(), args.Message.ApplicationProperties.ToDictionary());

            activity?.SetTag(DiagnosticProperty.MessagingMessageId, args.Message.MessageId);
            activity?.SetTag(DiagnosticProperty.MessageBusMessageType, typeof(TMessage).FullName ?? string.Empty);

            activity?.AddEvent(new ActivityEvent(DiagnosticProperty.MessagingConsumerInvokingHandler));
            var messageHandlerExecutionTime = ValueStopwatch.StartNew();
            await messageHandler.Handle(messageHandlerContext, args.CancellationToken);
            messageBusMetrics.RecordMessageHandlerElapsedTime<TMessage>(messageHandlerExecutionTime.GetElapsedTime().TotalMilliseconds, Name, args.EntityPath, messageHandler.GetType().Name);

            activity?.AddEvent(new ActivityEvent(DiagnosticProperty.MessagingConsumerInvokedHandler));
        }

        messageBusMetrics.RecordMessagingProcessDuration<TMessage>(processDuration.GetElapsedTime().TotalMilliseconds, Name, args.EntityPath);
        messageBusMetrics.RecordMessagingClientOperationDuration<TMessage>(startTime.GetElapsedTime().TotalMilliseconds, Name, args.EntityPath);
        messageBusMetrics.RecordMessageConsumed<TMessage>(Name, args.EntityPath);
    }

    private MethodInfo CreateOnProcessMessageAsync(Type messageType)
    {
        var onProcessMessageAsync = messageType != default
            ? GetType().GetMethod(nameof(OnProcessMessageAsync), BindingFlags.Instance | BindingFlags.NonPublic)?.MakeGenericMethod(messageType)
            : GetType().GetMethod(nameof(OnProcessRawMessageAsync), BindingFlags.Instance | BindingFlags.NonPublic);

        return onProcessMessageAsync ?? throw new InvalidOperationException($"{CouldNotCreateMethodForMessageType} {messageType?.Name}");
    }

    private ServiceBusProcessor GetChannelProcessor(AzureServiceBusChannelConsumerOptions options)
    {
        var processor = options.ChannelType switch
        {
            ChannelType.Topic => azureServiceBusClientFactory.CreateProcessor(options.ChannelName, options.SubscriptionName, options.ServiceBusProcessorOptions),
            ChannelType.Queue => azureServiceBusClientFactory.CreateProcessor(options.ChannelName, options.ServiceBusProcessorOptions),
            _ => throw new NotSupportedException($"{ChannelTypeIsNotSupported} {options.ChannelType}")
        };

        processorCache.TryAdd(options.GetHashCode(), processor);

        return processor;
    }
}
