using Infinity.Toolkit;
using Infinity.Toolkit.Experimental;
using Infinity.Toolkit.Experimental.Mediator;
using Infinity.Toolkit.Experimental.Pipeline;

namespace PipelineSample;

internal record ProductCreated(int Id, string Name) : ICommand;

internal record ProductCreatedResult(string NewName);

internal class ProductCreatedHandler : IMediatorHandler<ProductCreated>
{
    private readonly IPipeline<ProductCreated, ProductCreatedResult> pipeline;

    public ProductCreatedHandler(IPipeline<ProductCreated, ProductCreatedResult> pipeline)
    {
        this.pipeline = pipeline;
    }

    public Task<Result> HandleAsync(MediatorHandlerContext<ProductCreated> context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("ProductCreatedHandler:HandleAsync");
        Console.WriteLine($"Product created: {context.Request.Id} - {context.Request.Name}");

        Console.WriteLine("Executing pipeline");
        //var result = await pipeline.ExecuteAsync(context.Request, cancellationToken);
        //Console.WriteLine($"Pipeline executed: {result.NewName}");

        return Task.FromResult(Result.Success());
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
