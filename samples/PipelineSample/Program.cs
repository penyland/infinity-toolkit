using Infinity.Toolkit.Experimental.Mediator;
using Infinity.Toolkit.Experimental.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using PipelineSample;

Console.WriteLine("Hello Pipeline World!");

var services = new ServiceCollection();
services.AddMediator();
services.AddMediatorHandler<ProductCreated, ProductCreatedHandler>();
services.AddPipeline<ProductCreated, ProductCreatedResult>(services => ProductCreatedPipeline.CreatePipeline(services));

var serviceProvider = services.BuildServiceProvider();
var mediator = serviceProvider.GetRequiredService<IMediator>();
await mediator.SendAsync(new ProductCreated(1, "Product 1"));

Console.WriteLine("Done");
