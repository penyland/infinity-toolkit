using Infinity.Toolkit.Experimental;
using Infinity.Toolkit.Experimental.Mediator;
using Infinity.Toolkit.Experimental.Pipeline;

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

    public async Task<Result> HandleAsync(MediatorCommandHandlerContext<ProductCreated> context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("ProductCreatedHandler:HandleAsync");
        Console.WriteLine($"Product created: {context.Command.Id} - {context.Command.Name}");

        Console.WriteLine("Executing pipeline");
        var result = await pipeline.ExecuteAsync(context.Command, cancellationToken);
        Console.WriteLine($"Pipeline executed: {result.NewName}");

        return Result.Success();
    }
}

public static class ProductCreatedPipeline
{
    internal static IPipeline<ProductCreated, ProductCreatedResult> CreatePipeline(IServiceProvider services) => new Pipeline<ProductCreated, ProductCreatedResult>()
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
}
