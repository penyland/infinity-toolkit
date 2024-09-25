// See https://aka.ms/new-console-template for more information
using Infinity.Toolkit.Mediator;
using MediatorSample;
using Microsoft.Extensions.DependencyInjection;

Console.WriteLine("Hello Pipeline World!");

var services = new ServiceCollection();
services.AddLogging();
services.AddMediator();

services.AddCommandHandler<ProductCreated, ProductCreatedHandler>();

services.AddPipeline<ProductCreated, ProductCreatedResult>(services => ProductCreatedPipeline.CreatePipeline(services));

var serviceProvider = services.BuildServiceProvider();

var commandDispatcher = serviceProvider.GetRequiredService<ICommandDispatcher>();

await commandDispatcher.DispatchAsync(new ProductCreated(1, "Product 1"));

Console.WriteLine("Done");
