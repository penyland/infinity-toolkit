using Microsoft.Extensions.DependencyInjection;

namespace Infinity.Toolkit.Experimental.Mediator;

public class QueryMediator(IServiceProvider serviceProvider) : IQueryMediator
{
    public Task<Result<TResult>> SendAsync<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : class, IQuery
    {
        using var scope = serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetService<IQueryHandler<TQuery, TResult>>();
        var context = new MediatorQueryHandlerContext<TQuery>
        {
            Body = new BinaryData(query),
            Query = query
        };
        return handler switch
        {
            null => throw new InvalidOperationException($"No handler found for query of type {typeof(TQuery).Name}."),
            _ => handler.HandleAsync(context, cancellationToken)
        };
    }
}
