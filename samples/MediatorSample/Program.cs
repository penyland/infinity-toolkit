// See https://aka.ms/new-console-template for more information

using Infinity.Toolkit.Experimental;
using Infinity.Toolkit.Experimental.Mediator;
using MediatorSample;
using Microsoft.Extensions.DependencyInjection;

Console.WriteLine("Hello Pipeline World!");

var services = new ServiceCollection();
services.AddLogging();
services
    .AddMediator()
    .Decorate<IMediator, LoggingMediator>();

services.AddMediatorHandler<ProductCreated, ProductCreatedHandler>()
        .Decorate<ProductCreatedDecorator>();

services.AddMediatorHandler<ProductCreateQuery, string, ProductCreateQueryHandler>();

var serviceProvider = services.BuildServiceProvider();
var mediator = serviceProvider.GetRequiredService<IMediator>();

await mediator.SendAsync(new ProductCreated(1, "Product 1"));
var result = await mediator.SendAsync<ProductCreateQuery, string>(new ProductCreateQuery());

Console.WriteLine("Done");

record ProductCreateQuery : IQuery;

class ProductCreateQueryHandler : IMediatorHandler<ProductCreateQuery, string>
{
    public Task<Result<string>> HandleAsync(MediatorHandlerContext<ProductCreateQuery> context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success("Product 1"));
    }
}

public class LoggingMediator(IMediator inner) : IMediator
{
    public async Task<Result> SendAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : class, ICommand
    {
        Console.WriteLine($"Sending command: {request}");
        var result = await inner.SendAsync(request, cancellationToken);
        Console.WriteLine($"Command result: IsSuccess={result.Succeeded}");
        return result;
    }
    public async Task<Result<TResponse>> SendAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default) where TRequest : class, IQuery
    {
        Console.WriteLine($"Sending query: {request}");
        var result = await inner.SendAsync<TRequest, TResponse>(request, cancellationToken);
        Console.WriteLine($"Query result: {result.Value}");
        return result;
    }
}
