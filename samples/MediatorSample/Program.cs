// See https://aka.ms/new-console-template for more information
using Infinity.Toolkit.Mediator;
using MediatorSample;
using Microsoft.Extensions.DependencyInjection;

Console.WriteLine("Hello Pipeline World!");

var services = new ServiceCollection();
services.AddLogging();
services.AddMediator();

services.AddCommandHandler<ProductCreated, ProductCreatedHandler>();

services.AddPipeline<ProductCreated, ProductCreatedResult>(services => CreatePipeline(services));

var serviceProvider = services.BuildServiceProvider();

var commandDispatcher = serviceProvider.GetRequiredService<ICommandDispatcher>();

await commandDispatcher.DispatchAsync(new ProductCreated(1, "Product 1"));

Console.WriteLine("Done");

static IPipeline<ProductCreated, ProductCreatedResult> CreatePipeline(IServiceProvider services) => new Pipeline<ProductCreated, ProductCreatedResult>()
        .AddStep<ProductCreated, string>(input =>
        {
            Console.WriteLine("Step 1");
            Console.WriteLine($"Product created: {input.Id} - {input.Name}");
            var result = input.Name + " - Modified";
            return result;
        })
        .AddStep<string, ProductCreatedResult>(input =>
        {
            Console.WriteLine("Step 2");
            Console.WriteLine($"Product modified: {input}");
            return new ProductCreatedResult(input);
        })
        .Build();
