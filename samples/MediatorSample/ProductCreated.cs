using Infinity.Toolkit.Mediator;

namespace MediatorSample;

internal record ProductCreated(int Id, string Name) : ICommand;

internal record ProductCreatedResult(string NewName);

internal class ProductCreatedHandler : ICommandHandler<ProductCreated>
{
    private readonly IPipeline<ProductCreated, ProductCreatedResult> pipeline;

    public ProductCreatedHandler(IPipeline<ProductCreated, ProductCreatedResult> pipeline)
    {
        this.pipeline = pipeline;
    }

    public async ValueTask HandleAsync(ProductCreated command, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("ProductCreatedHandler:HandleAsync");
        Console.WriteLine($"Product created: {command.Id} - {command.Name}");

        Console.WriteLine("Executing pipeline");
        var result = await pipeline.ExecuteAsync(command, cancellationToken);
        Console.WriteLine($"Pipeline executed: {result.NewName}");
    }
}
