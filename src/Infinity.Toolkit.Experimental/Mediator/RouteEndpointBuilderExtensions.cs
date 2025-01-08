using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Infinity.Toolkit.Experimental.Mediator;

public static class RouteEndpointBuilderExtensions
{
    public static RouteHandlerBuilder MapGetQuery<TRequest, TResponse>(this IEndpointRouteBuilder builder, string path)
        where TRequest : class, IQuery
    {
        return builder.MapGet(path, async ([AsParameters] TRequest request, [FromServices] IMediator mediator) =>
        {
            var result = await mediator.SendAsync<TRequest, TResponse>(request);

            IResult response = result switch
            {
                Success => TypedResults.Ok<TResponse>(result.Value),
                Failure => TypedResults.BadRequest(),
                _ => TypedResults.BadRequest("Failed to process request.")
            };

            return response;
        })
        .Produces<TResponse>(statusCode: StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);
    }
}
