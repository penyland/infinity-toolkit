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

services.AddMediatorHandler<CreateProduct, CreateProductHandler>()
        .Decorate<CreateProductDecorator>();

services.AddMediatorHandler<ProductCreatedQuery, string, ProductCreatedQueryHandler>();
services.DecorateMediatorHandler<ProductCreatedQuery, string, ProductCreatedQueryHandlerDecorator>();

var serviceProvider = services.BuildServiceProvider();
var mediator = serviceProvider.GetRequiredService<IMediator>();

await mediator.SendAsync(new CreateProduct(1, "Product 1"));
var result = await mediator.SendAsync<ProductCreatedQuery, string>(new ProductCreatedQuery());

Console.WriteLine("Done");

record ProductCreatedQuery : IQuery;

class ProductCreatedQueryHandler : IMediatorHandler<ProductCreatedQuery, string>
{
    public Task<Result<string>> HandleAsync(MediatorHandlerContext<ProductCreatedQuery> context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("ProductCreatedQueryHandler:HandleAsync");
        return Task.FromResult(Result.Success("Product 1"));
    }
}

class ProductCreatedQueryHandlerDecorator(IMediatorHandler<ProductCreatedQuery, string> inner) : IMediatorHandler<ProductCreatedQuery, string>
{
    public async Task<Result<string>> HandleAsync(MediatorHandlerContext<ProductCreatedQuery> context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("ProductCreatedQueryHandlerDecorator:HandleAsync");
        var result = await inner.HandleAsync(context, cancellationToken);
        Console.WriteLine("ProductCreatedQueryHandlerDecorator:HandleAsync:Done");
        return result;
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
