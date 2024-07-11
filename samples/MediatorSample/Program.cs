// See https://aka.ms/new-console-template for more information
using Infinity.Toolkit.Mediator;
using MediatorSample;
using Microsoft.Extensions.DependencyInjection;

Console.WriteLine("Hello, World!");

var services = new ServiceCollection();
services.AddLogging();
services.AddMediator();

services.AddCommandHandler<ProductCreated, ProductCreatedHandler>();

var serviceProvider = services.BuildServiceProvider();

var dispatcher = serviceProvider.GetRequiredService<ICommandDispatcher>();

await dispatcher.DispatchAsync(new ProductCreated(1, "Product 1"));
