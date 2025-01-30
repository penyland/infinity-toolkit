# Infinity.Toolkit.Messaging
Infinity Toolkit Messaging is a no frills messaging lightweight library with a super simple API. It can be used to send messages between different parts of the application.
The library is built with simplicity in mind and is designed to be easy to use and easy to understand.
The nomenclature is following Async API and the library is built with the idea of being able to easily integrate with any messaging system such as Azure Service Bus, RabbitMQ, Kafka etc.

## Features
- An Async API inspired API.
- An In-Memory message bus
- Azure Service Bus integration

### Quick Start
To get started with Infinity.Toolkit.Messaging follow these steps:
1. Look at the sample project in the repository. The sample project found here [MessagingSample](samples/MessagingSample) is a simple web api that demonstrates how to use the library.
2. Create a new project and integrate the library.

Let's look create a new project and integrate the library.
1. Create a new web api project using the dotnet cli.
```bash
dotnet new webapi -n MyWebApi
```

2. Add the Infinity.Toolkit.Messaging package to the project.
```bash
dotnet add package Infinity.Toolkit.Messaging
```

3. Start by creating a message class that will be sent between different parts of the application. For this sample let's reuse the WeatherForecast class from the sample project.
```csharp
internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
```

4. Create a message handler that will handle the message. The message handler should implement the `IMessageHandler<TMessage>` interface.
```csharp
internal class WeatherForecastHandler : IMessageHandler<WeatherForecast>
{
    public Task HandleAsync(WeatherForecast message, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Received message: {message.Summary}");
        return Task.CompletedTask;
    }
}
```

4. Now it's time to add and configure the message bus. First add the message bus to the services collection in the `Program.cs` file.
```csharp
using Infinity.Toolkit.Messaging;

var builder = WebApplication.CreateBuilder(args);
builder.AddInfinityMessaging();

var app = builder.Build();
...
```

5. Good, now the message bus is added to the services collection. Next, we need to configure a broker that will be used to send and receive messages. In this sample, we will use the in-memory message broker.
```csharp
builder.AddInfinityMessaging().ConfigureInMemoryBus();
```

6. Build and run the application and make sure everything is working as expected.

7. Now we need to configure a ChannelProducer that will be used to send messages. In this sample, we will use the in-memory channel producer.
```csharp
builder.AddInfinityMessaging()
    .ConfigureInMemoryBus(builder =>
    {
        builder.AddChannelProducer<WeatherForecast>(options => { options.ChannelName = "weatherforecasts"; })
    });
```
This will add a channel producer that will send messages of type WeatherForecast to the channel named weatherforecasts.

8. Now we can send messages to the channel but we also need to configure a channel consumer that will consume the messages. In this sample, we will use the in-memory channel consumer that consumes messages from the weatherforecasts channel.
The channel is configured as a topic so we need to configure a subscription name as well.
```csharp
builder.AddInfinityMessaging()
    .ConfigureInMemoryBus(builder =>
    {
        builder
            .AddChannelProducer<WeatherForecast>(options => { options.ChannelName = "weatherforecasts"; })
            .AddChannelConsumer<WeatherForecast>(options =>
            {
                options.ChannelName = "weatherforecasts";
                options.SubscriptionName = "weathersubscription";
            });
    });
```

9. Now we can send messages to the channel and consume the messages. However we have no handler that will handle the messages with the type WeatherForecast. Let's add the handler to the services collection.
```csharp
builder.AddInfinityMessaging()
    .ConfigureInMemoryBus(builder =>
    {
        builder
            .AddChannelProducer<WeatherForecast>(options => { options.ChannelName = "weatherforecasts"; })
            .AddChannelConsumer<WeatherForecast>(options =>
            {
                options.ChannelName = "weatherforecasts";
                options.SubscriptionName = "weathersubscription";
            });
    })
    .MapMessageHandler<WeatherForecast, WeatherForecastMessageHandler>();
```

10. Let's add an endpoint so we can send messages to the channel.
```csharp
app.MapPost("/send", async (ChannelProducer<WeatherForecast> producer) =>
{
    await producer.SendAsync(new WeatherForecast(DateTime.Now, 20, "Sunny"));
    return Results.Ok();
});
```

10. Now we have a complete setup with a message bus, channel producer, channel consumer and a message handler. We can now send messages to the channel and the message handler will handle the messages.


# Contributing
If you have any ideas, suggestions or issues, please create an issue or a pull request. Or reach out to me on [BlueSky](https://bsky.app/profile/peternylander.bsky.social).

# License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
