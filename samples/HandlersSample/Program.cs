using Infinity.Toolkit;
using Infinity.Toolkit.AspNetCore;
using Infinity.Toolkit.Handlers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<InMemoryDatabase>();

// Register a request handler for CreateProduct using AddRequestHandler and decorate it with a decorator.
builder.Services.AddRequestHandler<CreateProduct, Result<CreateProductResponse>, CreateProductHandler>()
    .Decorate<CreateProductHandlerDecorator<CreateProduct>>();

// Decorate the CreateProductHandler request handler with a logging decorator using .Decorate
builder.Services.Decorate<IRequestHandler<CreateProduct, Result<CreateProductResponse>>, CreateProductHandlerLoggingDecorator>();

// Or register a request handler for ProductCreatedQuery directly on the service collection
builder.Services.AddScoped<IRequestHandler<ProductCreatedQuery, Result<Product>>, ProductCreatedQueryHandler>();
builder.Services.Decorate<IRequestHandler<ProductCreatedQuery, Result<Product>>, ProductCreatedQueryDecorator>();

// Alternatively, decorate all IRequestHandler<TIn> implementations with a logging handler.
// This will apply to all handlers that implement IRequestHandler<TIn> so in this example we will get double logging for CreateProductHandler.
//builder.Services.Decorate(typeof(IRequestHandler<>), typeof(LoggingRequestHandler<>));

// Decorate all IRequestHandler<TIn, TResult> implementations with a logging handler.
builder.Services.Decorate(typeof(IRequestHandler<,>), typeof(LoggingRequestHandler2<,>));

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.MapPost("/product", async (CreateProduct command, IRequestHandler<CreateProduct, Result<CreateProductResponse>> requestHandler) =>
{
    Console.WriteLine($"Received command: {command}");
    var result = await requestHandler.HandleAsync(
        new HandlerContext<CreateProduct>
        {
            Body = BinaryData.FromObjectAsJson(command),
            Request = command
        });

    return result.Succeeded ? Results.Ok() : Results.Problem(result.ToProblemDetails());
    //return Results.Ok();
})
.WithName("CreateProduct")
.WithSummary("Creates a new product.")
.WithDescription("This endpoint creates a new product with the specified ID and name.");

app.MapGetQueryWithResult<ProductCreatedQuery, Product>("/product/{id}")
    .WithName("GetProductCreated")
    .WithSummary("Gets the product created information.")
    .WithDescription("This endpoint retrieves the product created information.");

app.Run();

record Product(int Id, string Name);

record CreateProduct(int Id, string Name);
record CreateProductResponse();

class CreateProductHandler(InMemoryDatabase inMemoryDatabase) : IRequestHandler<CreateProduct, Result<CreateProductResponse>>
{
    public Task<Result<CreateProductResponse>> HandleAsync(IHandlerContext<CreateProduct>? context, CancellationToken cancellationToken = default)
    {

        try
        {
            inMemoryDatabase.Add(new Product(context!.Request.Id, context.Request.Name));
            return Task.FromResult(Result.Success(new CreateProductResponse()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure<CreateProductResponse>(ex));
        }
    }
}

class CreateProductHandlerDecorator<CreateProduct>(IRequestHandler<CreateProduct, Result<CreateProductResponse>> innerHandler, ILogger<CreateProductHandlerDecorator<CreateProduct>> logger) : IRequestHandler<CreateProduct, Result<CreateProductResponse>>
    where CreateProduct : class
{
    public async Task<Result<CreateProductResponse>> HandleAsync(IHandlerContext<CreateProduct>? context, CancellationToken cancellationToken = default)
    {
        logger.LogInformation($"Handling request of type {typeof(CreateProduct).Name} with data: {context!.Request}");
        var result = await innerHandler.HandleAsync(context, cancellationToken);
        if (result.Succeeded)
        {
            logger.LogInformation($"Successfully handled request of type {typeof(CreateProduct).Name}");
        }
        else
        {
            logger.LogInformation($"FAILED to handle request of type {typeof(CreateProduct).Name} with {result.Errors.Count} errors. {string.Join(", ", result.Errors)}");
        }

        return result;
    }
}

class CreateProductHandlerLoggingDecorator(IRequestHandler<CreateProduct, Result<CreateProductResponse>> innerHandler, ILogger<CreateProductHandlerLoggingDecorator> logger) : IRequestHandler<CreateProduct, Result<CreateProductResponse>>
{
    public async Task<Result<CreateProductResponse>> HandleAsync(IHandlerContext<CreateProduct>? context, CancellationToken cancellationToken = default)
    {
        logger.LogInformation($"Starting handling CreateProduct command for ID: {context!.Request.Id}, Name: {context.Request.Name}");
        var result = await innerHandler.HandleAsync(context, cancellationToken);
        logger.LogInformation($"Finished handling CreateProduct command for ID: {context.Request.Id}");
        return result;
    }
}

record ProductCreatedQuery(int Id);

class ProductCreatedQueryHandler(InMemoryDatabase inMemoryDatabase, ILogger<ProductCreatedQueryHandler> logger) : IRequestHandler<ProductCreatedQuery, Result<Product>>
{
    public Task<Result<Product>> HandleAsync(IHandlerContext<ProductCreatedQuery>? context, CancellationToken cancellationToken = default)
    {
        logger.LogInformation($"Handling ProductCreatedQuery for ID: {context!.Request.Id}");

        var product = inMemoryDatabase.Get(context.Request.Id);
        if (product == null)
        {
            return Task.FromResult(Result.Failure<Product>($"Product with ID {context.Request.Id} not found."));
        }

        return Task.FromResult(Result.Success(product));
    }
}

class ProductCreatedQueryDecorator(IRequestHandler<ProductCreatedQuery, Result<Product>> innerHandler, ILogger<ProductCreatedQueryDecorator> logger) : IRequestHandler<ProductCreatedQuery, Result<Product>>
{
    public Task<Result<Product>> HandleAsync(IHandlerContext<ProductCreatedQuery> context, CancellationToken cancellationToken = default)
    {
        logger.LogInformation($"Doing stuff...");
        var result = innerHandler.HandleAsync(context, cancellationToken);
        logger.LogInformation($"Did stuff...");
        return result;
    }
}

class LoggingRequestHandler<TIn>(IRequestHandler<TIn> innerHandler, ILogger<LoggingRequestHandler<TIn>> logger) : IRequestHandler<TIn>
    where TIn : class
{
    public async Task<TIn> HandleAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation($"LoggingRequestHandler: Handling request of type {typeof(TIn).Name}");
        var result = await innerHandler.HandleAsync(cancellationToken);
        logger.LogInformation($"LoggingRequestHandler: Finished handling request of type {typeof(TIn).Name}");

        //if (result.Succeeded)
        //{
        //    logger.LogInformation($"Successfully handled request of type {typeof(TIn).Name}");
        //}
        //else
        //{
        //    logger.LogInformation($"Failed to handle request of type {typeof(TIn).Name}: {result.Errors}");
        //}
        return result;
    }
}

class LoggingRequestHandler2<TIn, TResult>(IRequestHandler<TIn, TResult> innerHandler, ILogger<LoggingRequestHandler2<TIn, TResult>> logger) : IRequestHandler<TIn, TResult>
    where TIn : class
    where TResult : class
{
    public async Task<TResult> HandleAsync(IHandlerContext<TIn> context, CancellationToken cancellationToken = default)
    {
        logger.LogInformation($"LoggingRequestHandler2: Handling request of type {typeof(TIn).Name} with data: {context.Request}");
        var result = await innerHandler.HandleAsync(context, cancellationToken);
        logger.LogInformation($"LoggingRequestHandler2: Finished handling request of type {typeof(TIn).Name}");
        return result;
    }
}

class InMemoryDatabase
{
    private readonly Dictionary<int, Product> products = [];

    public void Add(Product product)
    {
        if (products.ContainsKey(product.Id))
        {
            throw new InvalidOperationException($"Product with name {product.Id} already exists.");
        }
        products[product.Id] = product;
    }

    public Product? Get(string name)
    {
        var result = products
            .Where(t => t.Value.Name == name)
            .Select(t => t.Value)
            .FirstOrDefault();

        return result;
    }

    public Product? Get(int id) => products.TryGetValue(id, out var product) ? product : null;
}
