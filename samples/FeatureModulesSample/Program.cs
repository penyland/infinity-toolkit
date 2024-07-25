using Infinity.Toolkit.FeatureModules;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddFeatureModules();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("openapi", new Microsoft.OpenApi.Models.OpenApiInfo ());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options => options.RouteTemplate = "/{documentName}/v1.{json|yaml}");
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "v1"));
}

app.UseHttpsRedirection();

app.MapFeatureModules();
app.MapScalarApiReference();
app.Run();
