using Infinity.Toolkit.FeatureModules;
using Infinity.Toolkit.LogFormatter;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddCodeThemeConsoleFormatter();
builder.AddFeatureModules();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();

app.UseHttpsRedirection();

app.MapFeatureModules();
app.Run();
