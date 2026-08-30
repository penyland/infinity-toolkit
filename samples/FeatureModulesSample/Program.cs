using Infinity.Toolkit.FeatureModules;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddFeatureModules();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();

app.UseHttpsRedirection();

app.MapFeatureModules();
app.Run();
