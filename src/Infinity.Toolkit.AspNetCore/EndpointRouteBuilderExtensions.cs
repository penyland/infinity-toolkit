using Infinity.Toolkit.Handlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Infinity.Toolkit.AspNetCore;

public static class EndpointRouteBuilderExtensions
{
    /// <summary>
     /// Maps a GET endpoint to an asynchronous handler that processes a request and returns a response of the specified
     /// type.
     /// </summary>
     /// <typeparam name="TResponse">The type of the response returned by the request handler. Must be a reference type.</typeparam>
     /// <param name="builder">The endpoint route builder used to configure the route.</param>
     /// <param name="path">The route pattern to map the GET endpoint to. Must be a non-empty string.</param>
     /// <returns>A RouteHandlerBuilder that can be used to further configure the mapped endpoint.</returns>
    public static RouteHandlerBuilder MapGetRequestHandler<TResponse>(this IEndpointRouteBuilder builder, string path)
        where TResponse : class
    {
        return builder.MapGet(path, async ([FromServices] IRequestHandler<TResponse> requestHandler) =>
        {
            return await requestHandler.HandleAsync();
        })
        .Produces<TResponse>(statusCode: StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    /// <summary>
    /// Maps a GET endpoint to an asynchronous handler that processes a request and returns a response of the specified
    /// type.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request processed by the request handler. Must be a reference type.</typeparam>
    /// <typeparam name="TResponse">The type of the response returned by the request handler. Must be a reference type.</typeparam>
    /// <param name="builder">The endpoint route builder used to configure the route.</param>
    /// <param name="path">The route pattern to map the GET endpoint to. Must be a non-empty string.</param>
    /// <returns>A RouteHandlerBuilder that can be used to further configure the mapped endpoint.</returns>
    public static RouteHandlerBuilder MapGetRequestHandler<TRequest, TResponse>(this IEndpointRouteBuilder builder, string path)
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

            return result;
        })
        .Produces<TResponse>(statusCode: StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);
    }

    /// <summary>
    /// Maps a GET endpoint that handles a request using the specified request handler and applies a mapping function to
    /// the response before returning the result.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response produced by the request handler. Must be a reference type.</typeparam>
    /// <typeparam name="TMappedType">The type produced by the mapping function applied to the response.</typeparam>
    /// <param name="builder">The endpoint route builder used to configure the route.</param>
    /// <param name="path">The route pattern that identifies the GET endpoint.</param>
    /// <param name="mapper">A function that maps the response from the request handler to the type returned by the endpoint. Cannot be null.</param>
    /// <returns>A RouteHandlerBuilder that can be used to further configure the endpoint.</returns>
    public static RouteHandlerBuilder MapGetRequestHandler<TResponse, TMappedType>(this IEndpointRouteBuilder builder, string path, Func<TResponse, TMappedType> mapper)
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(mapper);

        return builder.MapGet(path, async ([FromServices] IRequestHandler<TResponse> requestHandler) =>
        {
            return mapper(await requestHandler.HandleAsync());
        })
        .Produces<TMappedType>(statusCode: StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest);
    }


    /// <summary>
    /// Maps a GET endpoint that handles a request using the specified request handler and applies a mapping function to
    /// the response before returning the result.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request object received by the endpoint. Must be a reference type.</typeparam>
    /// <typeparam name="TResponse">The type of the response produced by the request handler. Must be a reference type.</typeparam>
    /// <typeparam name="TMappedType">The type produced by the mapping function applied to the response.</typeparam>
    /// <param name="builder">The endpoint route builder used to configure the route.</param>
    /// <param name="path">The route pattern that identifies the GET endpoint.</param>
    /// <param name="mapper">A function that maps the response from the request handler to the type returned by the endpoint. Cannot be null.</param>
    /// <returns>A RouteHandlerBuilder that can be used to further configure the endpoint.</returns>
    public static RouteHandlerBuilder MapGetRequestHandler<TRequest, TResponse, TMappedType>(this IEndpointRouteBuilder builder, string path, Func<TResponse, TMappedType> mapper)
        where TRequest : class
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(mapper);

        return builder.MapGet(path, async ([AsParameters] TRequest request, [FromServices] IRequestHandler<TRequest, TResponse> requestHandler) =>
        {
            var result = await requestHandler.HandleAsync(
                new HandlerContext<TRequest>
                {
                    Body = BinaryData.FromObjectAsJson(request),
                    Request = request
                });

            return mapper(result);
        })
        .Produces<TMappedType>(statusCode: StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    /// <summary>
    /// Maps a GET endpoint to an asynchronous handler that processes a request and returns a response of the specified
    /// type.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response returned by the request handler. Must be a reference type.</typeparam>
    /// <param name="builder">The endpoint route builder used to configure the route.</param>
    /// <param name="path">The route pattern to map the GET endpoint to. Must be a non-empty string.</param>
    /// <returns>A RouteHandlerBuilder that can be used to further configure the mapped endpoint.</returns>
    public static RouteHandlerBuilder MapGetRequestHandlerWithResult<TResponse>(this IEndpointRouteBuilder builder, string path)
        where TResponse : class
    {
        return builder.MapGet(path, async ([FromServices] IRequestHandler<Result<TResponse>> requestHandler) =>
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
        .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    /// <summary>
    /// Maps a GET endpoint to an asynchronous handler that processes a request of the specified type and returns a Result containing a response of the specified type.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request handled by the endpoint. Must be a reference type.</typeparam>
    /// <typeparam name="TResponse">The type of the response returned by the request handler. Must be a reference type.</typeparam>
    /// <param name="builder">The endpoint route builder used to configure the route.</param>
    /// <param name="path">The route pattern to map the GET endpoint to. Must be a non-empty string.</param>
    /// <returns>A RouteHandlerBuilder that can be used to further configure the mapped endpoint.</returns>
    public static RouteHandlerBuilder MapGetRequestHandlerWithResult<TRequest, TResponse>(this IEndpointRouteBuilder builder, string path)
        where TRequest : class
        where TResponse : class
    {
        return builder.MapGet(path, async ([AsParameters] TRequest request, [FromServices] IRequestHandler<TRequest, Result<TResponse>> requestHandler) =>
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

    /// <summary>
    /// Maps a GET endpoint that handles a request using the specified request handler and applies a mapping function to
    /// the response before returning the result.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response produced by the request handler. Must be a reference type.</typeparam>
    /// <typeparam name="TMappedType">The type produced by the mapping function applied to the response.</typeparam>
    /// <param name="builder">The endpoint route builder used to configure the route.</param>
    /// <param name="path">The route pattern that identifies the GET endpoint.</param>
    /// <param name="mapper">A function that maps the response from the request handler to the type returned by the endpoint. Cannot be null.</param>
    /// <returns>A RouteHandlerBuilder that can be used to further configure the endpoint.</returns>
    public static RouteHandlerBuilder MapGetRequestHandlerWithResult<TResponse, TMappedType>(this IEndpointRouteBuilder builder, string path, Func<TResponse, TMappedType> mapper)
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(mapper);

        return builder.MapGet(path, async ([FromServices] IRequestHandler<Result<TResponse>> requestHandler) =>
        {
            var result = await requestHandler.HandleAsync();

            IResult response = result switch
            {
                Success => TypedResults.Ok(mapper(result)),
                Failure => TypedResults.Problem(result.ToProblemDetails()),
                _ => TypedResults.BadRequest("Failed to process request.")
            };

            return response;
        })
        .Produces<TMappedType>(statusCode: StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    /// <summary>
    /// Maps a GET endpoint that handles a request using the specified request handler and applies a mapping function to
    /// the response before returning the result.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request object received by the endpoint. Must be a reference type.</typeparam>
    /// <typeparam name="TResponse">The type of the response produced by the request handler. Must be a reference type.</typeparam>
    /// <typeparam name="TMappedType">The type produced by the mapping function applied to the response.</typeparam>
    /// <param name="builder">The endpoint route builder used to configure the route.</param>
    /// <param name="path">The route pattern that identifies the GET endpoint.</param>
    /// <param name="mapper">A function that maps the response from the request handler to the type returned by the endpoint. Cannot be null.</param>
    /// <returns>A RouteHandlerBuilder that can be used to further configure the endpoint.</returns>
    public static RouteHandlerBuilder MapGetRequestHandlerWithResult<TRequest, TResponse, TMappedType>(this IEndpointRouteBuilder builder, string path, Func<TResponse, TMappedType> mapper)
        where TRequest : class
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(mapper);

        return builder.MapGet(path, async ([AsParameters] TRequest request, [FromServices] IRequestHandler<TRequest, Result<TResponse>> requestHandler) =>
        {
            var result = await requestHandler.HandleAsync(
                new HandlerContext<TRequest>
                {
                    Body = BinaryData.FromObjectAsJson(request),
                    Request = request
                });

            IResult response = result switch
            {
                Success => TypedResults.Ok(mapper(result)),
                Failure => TypedResults.Problem(result.ToProblemDetails()),
                _ => TypedResults.BadRequest("Failed to process request.")
            };

            return response;
        })
        .Produces<TMappedType>(statusCode: StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
