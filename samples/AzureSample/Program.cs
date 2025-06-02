using Azure.Identity;
using Infinity.Toolkit.Azure.Configuration;
using Infinity.Toolkit.OpenApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureAzureAppConfiguration(configureSettings: options =>
{
    options.TokenCredential = new AzureCliCredential();
}, refreshOptions: refreshOptions =>
{
    refreshOptions.SetRefreshInterval(TimeSpan.FromSeconds(15));
    refreshOptions.Register("Sentinel", false);
});

builder.Services.AddOAuth2OpenApiSecuritySchemeDefinition();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        var settings = app.Services.GetRequiredService<IOptions<OpenApiOAuth2Settings>>();
        options.WithDefaultHttpClient(ScalarTarget.Shell, ScalarClient.Curl)
               .AddPreferredSecuritySchemes(OpenApiOAuth2Settings.AuthenticationScheme)
               .AddAuthorizationCodeFlow(OpenApiOAuth2Settings.AuthenticationScheme, flow =>
               {
                   flow.ClientId = settings?.Value.ClientId;
                   flow.Pkce = Pkce.Sha256;
                   flow.SelectedScopes = settings?.Value.ScopesArray;
               });
    });
}

app.UseHttpsRedirection();

app.MapGet("/config", ([FromServices] IConfiguration configuration) =>
{
    return (configuration as IConfigurationRoot)?.GetDebugView();
});

app.Run();
