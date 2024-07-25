using Infinity.Toolkit.FeatureModules;
using Infinity.Toolkit.LogFormatter;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddCodeThemeConsoleFormatter(_ => { });
builder.AddFeatureModules();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("openapi", new Microsoft.OpenApi.Models.OpenApiInfo());
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
