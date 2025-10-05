using Infinity.Toolkit.Handlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Infinity.Toolkit.AspNetCore;

public static class EndpointRouteBuilderExtensions
{
    public static RouteHandlerBuilder MapGetQuery<TResponse>(this IEndpointRouteBuilder builder, string path)
    where TResponse : class
    {
        return builder.MapGet(path, async ([FromServices] IRequestHandler<TResponse> requestHandler) =>
        {
            var result = await requestHandler.HandleAsync();

            IResult response = result switch
            {
                Success => TypedResults.Ok(result.Value),
                Failure => TypedResults.Problem(result.ToProblemDetails()),
                _ => TypedResults.BadRequest("Failed to process request.")
            };

            return response;
        })
        .Produces<TResponse>(statusCode: StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);
    }

    public static RouteHandlerBuilder MapGetQuery<TRequest, TResponse>(this IEndpointRouteBuilder builder, string path)
        where TRequest : class
        where TResponse : class
    {
        return builder.MapGet(path, async ([AsParameters] TRequest request, [FromServices] IRequestHandler<TRequest, TResponse> requestHandler) =>
        {
            var result = await requestHandler.HandleAsync(
                new HandlerContext<TRequest>
                {
                    Body = BinaryData.FromObjectAsJson(request),
                    Request = request
                });

            IResult response = result switch
            {
                Success => TypedResults.Ok(result.Value),
                Failure => TypedResults.Problem(result.ToProblemDetails()),
                _ => TypedResults.BadRequest("Failed to process request.")
            };

            return response;
        })
        .Produces<TResponse>(statusCode: StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);
    }


    public static RouteHandlerBuilder MapGetQuery2<TRequest, TResponse, T>(this IEndpointRouteBuilder builder, string path, Func<TResponse, T>? responseTransformer)
        where TRequest : class
        where TResponse : class
    {
        return builder.MapGet(path, async ([AsParameters] TRequest request, [FromServices] IRequestHandler<TRequest, TResponse> requestHandler) =>
        {
            var result = await requestHandler.HandleAsync(
                new HandlerContext<TRequest>
                {
                    Body = BinaryData.FromObjectAsJson(request),
                    Request = request
                });

            var value = responseTransformer != null ? responseTransformer.Invoke(result.Value) : default;

            IResult response = result switch
            {
                Success => TypedResults.Ok(value),
                Failure => TypedResults.Problem(result.ToProblemDetails()),
                _ => TypedResults.BadRequest("Failed to process request.")
            };

            return response;
        })
        .Produces<TResponse>(statusCode: StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);
    }
}
