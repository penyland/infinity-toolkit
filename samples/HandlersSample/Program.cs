using Infinity.Toolkit.Experimental;
using Infinity.Toolkit.Handlers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<InMemoryDatabase>();
//builder.Services.AddScoped<IRequestHandler<CreateProduct>, CreateProductHandler>();
//builder.Services.AddScoped<IRequestHandler<ProductCreatedQuery, Product>, ProductCreatedQueryHandler>();
builder.Services.AddRequestHandler<CreateProduct, CreateProductHandler>();
builder.Services.AddRequestHandler<ProductCreatedQuery, Product, ProductCreatedQueryHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.MapPost("/product", async (CreateProduct command, IRequestHandler<CreateProduct> requestHandler) =>
{
    Console.WriteLine($"Received command: {command}");
    var result = await requestHandler.HandleAsync(
        new HandlerContext<CreateProduct>
        {
            Body = new BinaryData(command),
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

record CreateProduct(int Id, string Name) : ICommand;

class CreateProductHandler(InMemoryDatabase inMemoryDatabase) : IRequestHandler<CreateProduct>
{
    public Task<Result> HandleAsync(IHandlerContext<CreateProduct> context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"CreateProductHandler:HandleAsync - Id: {context.Request.Id}, Name: {context.Request.Name}");

        inMemoryDatabase.Add(new Product(context.Request.Id, context.Request.Name));

        return Task.FromResult(Result.Success());
    }
}

record ProductCreatedQuery(int Id) : IQuery;

class ProductCreatedQueryHandler(InMemoryDatabase inMemoryDatabase) : IRequestHandler<ProductCreatedQuery, Product>
{
    public Task<Result<Product>> HandleAsync(IHandlerContext<ProductCreatedQuery> context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("ProductCreatedQueryHandler:HandleAsync");

        var product = inMemoryDatabase.Get(context.Request.Id);
        if (product == null)
        {
            return Task.FromResult(Result.Failure<Product>($"Product with ID {context.Request.Id} not found."));
        }

        return Task.FromResult(Result.Success(product));
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

    public Product? Get(int id) => products.TryGetValue(id, out var product) ? product: null;
}
