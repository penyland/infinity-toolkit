using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;

namespace Infinity.Toolkit.OpenApi;

/// <summary>
/// A document transformer that adds the bearer security scheme definition to the OpenAPI document.
/// </summary>
public sealed class BearerSecuritySchemeDefinitionDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var securityScheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Name = JwtBearerDefaults.AuthenticationScheme,
            Scheme = JwtBearerDefaults.AuthenticationScheme,
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = JwtBearerDefaults.AuthenticationScheme }
        };

        document.Components ??= new();
        document.Components.SecuritySchemes.Add(JwtBearerDefaults.AuthenticationScheme, securityScheme);
        return Task.CompletedTask;
    }
}
