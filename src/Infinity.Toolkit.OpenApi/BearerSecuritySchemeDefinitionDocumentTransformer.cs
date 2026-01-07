using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

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
            Name = "bearer",
            Scheme = "bearer",
        };

        document.Components ??= new();
        document.Components?.SecuritySchemes?.Add("bearer", securityScheme);
        return Task.CompletedTask;
    }
}
