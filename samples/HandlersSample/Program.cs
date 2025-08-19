using Infinity.Toolkit;
using Infinity.Toolkit.Handlers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<InMemoryDatabase>();

// Register a request handler for CreateProduct using AddRequestHandler and decorate it with a decorator.
builder.Services.AddRequestHandler<CreateProduct, CreateProductHandler>()
    .Decorate<CreateProductHandlerDecorator<CreateProduct>>();

// Or register a request handler for ProductCreatedQuery directly on the service collection
builder.Services.AddScoped<IRequestHandler<ProductCreatedQuery, Product>, ProductCreatedQueryHandler>();
builder.Services.Decorate<IRequestHandler<ProductCreatedQuery, Product>, ProductCreatedQueryDecorator>();

// Decorate the CreateProductRequestHandler with a logging decorator using .Decorate
builder.Services.Decorate<IRequestHandler<CreateProduct>, LoggingRequestHandler<CreateProduct>>();

// Alternatively, decorate all IRequestHandler<TIn> implementations with a logging handler.
// This will apply to all handlers that implement IRequestHandler<TIn> so in this example we will get double logging for CreateProductHandler.
builder.Services.Decorate(typeof(IRequestHandler<>), typeof(LoggingRequestHandler<>));

// Decorate all IRequestHandler<TIn, TResult> implementations with a logging handler.
builder.Services.Decorate(typeof(IRequestHandler<,>), typeof(LoggingRequestHandler<,>));

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.MapPost("/product", async (CreateProduct command, IRequestHandler<CreateProduct> requestHandler) =>
{
    Console.WriteLine($"Received command: {command}");
    var result = await requestHandler.HandleAsync(
        new HandlerContext<CreateProduct>
        {
            Body = BinaryData.FromObjectAsJson(command),
            Request = command
        });

    return result.Succeeded ? Results.Ok() : Results.Problem(result.Error);
})
.WithName("CreateProduct")
.WithSummary("Creates a new product.")
.WithDescription("This endpoint creates a new product with the specified ID and name.");

app.MapGetQuery<ProductCreatedQuery, Product>("/product/{id}")
    .WithName("GetProductCreated")
    .WithSummary("Gets the product created information.")
    .WithDescription("This endpoint retrieves the product created information.");

app.Run();

record Product(int Id, string Name);

record CreateProduct(int Id, string Name);

class CreateProductHandler(InMemoryDatabase inMemoryDatabase) : IRequestHandler<CreateProduct>
{
    public Task<Result> HandleAsync(IHandlerContext<CreateProduct> context, CancellationToken cancellationToken = default)
    {
        inMemoryDatabase.Add(new Product(context.Request.Id, context.Request.Name));
        return Task.FromResult(Result.Success());
    }
}

class CreateProductHandlerDecorator<CreateProduct>(IRequestHandler<CreateProduct> innerHandler, ILogger<CreateProductHandlerDecorator<CreateProduct>> logger) : IRequestHandler<CreateProduct>
    where CreateProduct : class
{
    public async Task<Result> HandleAsync(IHandlerContext<CreateProduct> context, CancellationToken cancellationToken = default)
    {
        logger.LogInformation($"Handling request of type {typeof(CreateProduct).Name} with data: {context.Request}");
        var result = await innerHandler.HandleAsync(context, cancellationToken);
        if (result.Succeeded)
        {
            logger.LogInformation($"Successfully handled request of type {typeof(CreateProduct).Name}");
        }
        else
        {
            logger.LogInformation($"Failed to handle request of type {typeof(CreateProduct).Name}: {result.Error}");
        }
        return result;
    }
}

record ProductCreatedQuery(int Id);

class ProductCreatedQueryHandler(InMemoryDatabase inMemoryDatabase, ILogger<ProductCreatedQueryHandler> logger) : IRequestHandler<ProductCreatedQuery, Product>
{
    public Task<Result<Product>> HandleAsync(IHandlerContext<ProductCreatedQuery> context, CancellationToken cancellationToken = default)
    {
        logger.LogInformation($"Handling ProductCreatedQuery for ID: {context.Request.Id}");

        var product = inMemoryDatabase.Get(context.Request.Id);
        if (product == null)
        {
            return Task.FromResult(Result.Failure<Product>($"Product with ID {context.Request.Id} not found."));
        }

        return Task.FromResult(Result.Success(product));
    }
}

class ProductCreatedQueryDecorator(IRequestHandler<ProductCreatedQuery, Product> innerHandler, ILogger<ProductCreatedQueryDecorator> logger) : IRequestHandler<ProductCreatedQuery, Product>
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
    public async Task<Result> HandleAsync(IHandlerContext<TIn> context, CancellationToken cancellationToken = default)
    {
        logger.LogInformation($"Handling request of type {typeof(TIn).Name} with data: {context.Request}");
        var result = await innerHandler.HandleAsync(context, cancellationToken);
        if (result.Succeeded)
        {
            logger.LogInformation($"Successfully handled request of type {typeof(TIn).Name}");
        }
        else
        {
            logger.LogInformation($"Failed to handle request of type {typeof(TIn).Name}: {result.Error}");
        }
        return result;
    }
}

class LoggingRequestHandler<TIn, TResult>(IRequestHandler<TIn, TResult> innerHandler, ILogger<LoggingRequestHandler<TIn, TResult>> logger) : IRequestHandler<TIn, TResult>
    where TIn : class
{
    public async Task<Result<TResult>> HandleAsync(IHandlerContext<TIn> context, CancellationToken cancellationToken = default)
    {
        logger.LogInformation($"Handling request of type {typeof(TIn).Name} with data: {context.Request}");
        var result = await innerHandler.HandleAsync(context, cancellationToken);
        if (result.Succeeded)
        {
            logger.LogInformation($"Successfully handled request of type {typeof(TIn).Name}");
        }
        else
        {
            logger.LogInformation($"Failed to handle request of type {typeof(TIn).Name}: {result.Error}");
        }
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
