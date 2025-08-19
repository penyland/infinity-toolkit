using Infinity.Toolkit;
using Infinity.Toolkit.Experimental;
using Infinity.Toolkit.Experimental.Mediator;

namespace MediatorSample;

internal record CreateProduct(int Id, string Name) : ICommand;

internal class CreateProductHandler : IMediatorHandler<CreateProduct>
{
    public Task<Result> HandleAsync(MediatorHandlerContext<CreateProduct> context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("CreateProductHandler:HandleAsync");
        Console.WriteLine($"Product created: {context.Request.Id} - {context.Request.Name}");
        Console.WriteLine("CreateProductHandler:HandleAsync:Done");
        return Task.FromResult(Result.Success());
    }
}

internal class CreateProductDecorator(IMediatorHandler<CreateProduct> inner) : IMediatorHandler<CreateProduct>
{
    public async Task<Result> HandleAsync(MediatorHandlerContext<CreateProduct> context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("CreateProductDecorator:HandleAsync");
        var result = await inner.HandleAsync(context, cancellationToken);
        Console.WriteLine("CreateProductDecorator:HandleAsync:Done");
        return result;
    }
}
