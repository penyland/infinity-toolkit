using Infinity.Toolkit.FeatureModules;
using Infinity.Toolkit.LogFormatter;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddCodeThemeConsoleFormatter(_ => { });
builder.AddFeatureModules();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options => options.RouteTemplate = "/openapi/{documentName}.{json|yaml}");
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "v1"));
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapFeatureModules();
app.Run();
