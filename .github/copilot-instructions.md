# Copilot Instructions — Infinity Toolkit

A collection of NuGet packages for building modular and vertical-slice architecture applications on .NET.

## Build & Test

```powershell
# Build
dotnet build
dotnet build --configuration Release

# Test
dotnet test                                         # all tests
dotnet test tests/Infinity.Toolkit.Tests/Infinity.Toolkit.Tests.csproj  # single project

# Run a single test class or method
dotnet test --filter "FullyQualifiedName~ResultTests"
dotnet test --filter "DisplayName~Result_Success_Should_Be_Successful"

# Pack NuGet packages (output: ./artifacts)
dotnet pack --configuration Release -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg
```

The solution file is `Infinity.Toolkit.slnx` (`.slnx` format).

## Architecture

The repository is structured as independent, composable packages under `src/`:

| Package | Purpose |
|---|---|
| `Infinity.Toolkit` | Core: Result type, handlers, functional utilities |
| `Infinity.Toolkit.FeatureModules` | Auto-discovery and registration of feature modules |
| `Infinity.Toolkit.AspNetCore` | ASP.NET Core extensions (Result ↔ HTTP, JWT helpers) |
| `Infinity.Toolkit.Messaging` | In-memory message bus, OpenTelemetry, diagnostics |
| `Infinity.Toolkit.Messaging.AzureServiceBus` | Azure Service Bus implementation of the messaging abstraction |
| `Infinity.Toolkit.LogFormatter` | VS Code-themed console log formatter |
| `Infinity.Toolkit.OpenApi` | OpenAPI security scheme document transformers |
| `Infinity.Toolkit.Slack` | Slack BlockKit message builders, signature validation, OAuth |
| `Infinity.Toolkit.Azure` | `TokenCredentialHelper`, Azure App Configuration integration |
| `Infinity.Toolkit.EntityFramework` | EF Core extensions |
| `Infinity.Toolkit.Experimental` | Pipeline / Mediator patterns (unstable) |
| `Infinity.Toolkit.TestUtils` | `XunitLogger`, test base classes for consuming projects |
| `Infinity.Toolkit.IntegrationTests` | SQL Server TestContainers, `WebApplicationFactory` helpers |

`tests/Infinity.Toolkit.Tests` is the single main test project. `samples/` contains standalone runnable examples for each feature area.

## Key Conventions

### Feature Modules

Feature modules are the primary extension point. They auto-register services and endpoints via assembly scanning:

```csharp
// src/YourProject/YourFeature/YourFeatureModule.cs
public class YourFeatureModule : WebFeatureModule
{
    public override ModuleInfo ModuleInfo => new("YourFeature", "1.0.0");

    public override void RegisterModule(IServiceCollection services)
    {
        // Register dependencies
    }

    public override void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/your-feature", Handler);
    }
}

// In Program.cs
builder.AddFeatureModules();
app.MapFeatureModules();
```

Use `FeatureModule` (no endpoints) or `WebFeatureModule` (with endpoints).

### Result Type

All operations that can fail return `Result` or `Result<T>` — never throw exceptions for expected failures:

```csharp
// Return success
return Result.Success(value);

// Return failure
return Result.Failure<T>(new Error("Code", "Description"));

// Consume
if (result.Succeeded) { /* result.Value */ }
if (result.Failed)    { /* result.Errors */ }
```

### Handlers

Request/response operations use the handler pattern from `Infinity.Toolkit`:

```csharp
public class GetItemHandler : RequestHandlerBase<GetItemRequest, Result<Item>>
{
    public override Task<Result<Item>> HandleAsync(GetItemRequest request, CancellationToken ct) { ... }
}
```

### Tests

- **Framework**: xUnit v3 — use `[Fact]` and `[Theory]`
- **Assertions**: Shouldly — `result.Succeeded.ShouldBeTrue()`, `value.ShouldBe(expected)`
- **Mocking**: NSubstitute
- **Naming**: `ClassName_Scenario_Should_ExpectedBehavior`
- Global usings are declared in `Usings.cs` (`global using Xunit;`)

### Code Style (enforced via `.editorconfig`)

- **File-scoped namespaces are required** (`namespace Foo.Bar;`) — nesting is an error
- **`TreatWarningsAsErrors=true`** globally; XML doc warnings (1591) and NU1505 are suppressed
- `Nullable=enable` everywhere — no nullable-suppression without justification
- Async methods must end with `Async`
- `ImplicitUsings=enable` — no need to add `using System;` etc. manually
- Max line length: 100 characters
- Expression-bodied members: properties and accessors only (not methods)

### NuGet Packaging

`Directory.Build.props` auto-sets `IsPackable=false` for any project whose name contains "Sample" or "Test". Packable projects must have their own `<VersionPrefix>` set in the `.csproj`. Packages are output to `./artifacts/`.

When adding a new packable project, include in the `.csproj`:
```xml
<PropertyGroup>
  <VersionPrefix>0.1.0</VersionPrefix>
  <Description>One-line description for NuGet.org</Description>
</PropertyGroup>
```

## Commit messages
- Follow [Conventional Commits](https://www.conventionalcommits.org) standard.
- The commit message should be structured as follows:
  ```
  <type>[optional scope]: <description>

  [optional body]

  [optional footer(s)]
  ```
- Valid types include: `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `chore`.
- Do not end subject line with a period. Use the imperative mood ("Add feature" not "Added feature").