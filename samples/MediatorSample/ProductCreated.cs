using Infinity.Toolkit.Experimental;
using Infinity.Toolkit.Experimental.Mediator;

namespace MediatorSample;

internal record ProductCreated(int Id, string Name) : ICommand;

internal class ProductCreatedHandler : IMediatorHandler<ProductCreated>
{
    public Task<Result> HandleAsync(MediatorHandlerContext<ProductCreated> context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("ProductCreatedHandler:HandleAsync");
        Console.WriteLine($"Product created: {context.Request.Id} - {context.Request.Name}");
        Console.WriteLine("ProductCreatedHandler:HandleAsync:Done");
        return Task.FromResult(Result.Success());
    }
}

internal class ProductCreatedDecorator(IMediatorHandler<ProductCreated> inner) : IMediatorHandler<ProductCreated>
{
    public async Task<Result> HandleAsync(MediatorHandlerContext<ProductCreated> context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("ProductCreatedDecorator:HandleAsync");
        var result = await inner.HandleAsync(context, cancellationToken);
        Console.WriteLine("ProductCreatedDecorator:HandleAsync:Done");
        return result;
    }
}
