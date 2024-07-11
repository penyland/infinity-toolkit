using Infinity.Toolkit.Mediator;

namespace MediatorSample;

internal record ProductCreated(int Id, string Name) : ICommand;

internal class ProductCreatedHandler : ICommandHandler<ProductCreated>
{
    public ValueTask HandleAsync(ProductCreated command, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Product created: {command.Id} - {command.Name}");
        return ValueTask.CompletedTask;
    }
}
