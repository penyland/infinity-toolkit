# infinity-toolkit
Infinity Toolkit is a collection of useful nuget-packages simplifying development of modular monoliths and applications using vertical slice architecture.

## Features
- [Feature modules](#feature-modules) - Let's you automatically register dependencies and endpoints in modules which simplifies development when you are working in feature slices.
- [Result type](#result-type) - A simple Result type for handling success and failure in a functional way.
- [Handlers](#handlers) - A simple way to create handlers for commands and queries.
- [Logging formatter](#logging-formatter) - logging formatter with a Visual Studio Code inspired theme and Serilog like formatting
- OpenApi document transformers - Simplifying setting up security schemes.
- Messaging framework - A simple messaging framework for sending and listening to messages. Supports in-memory and Azure Service Bus.
- [Infinity.Toolkit.Azure](#Infinity.Toolkit.Azure) - Utilities for working with Azure. 
  - TokenCredentialHelper - Helps creating a ChainedTokenCredential used to authenticate with Azure services.
  - Simplifying setup and configuring Azure App Configuration.
- And more to come...

# Feature modules
Infinity.Toolkit.FeatureModules is a library that simplifies development applications where you want to split functionality in different modules. It is especially useful when you are working with vertical slices in a modular monolith or application.
However though, the library can be used in any type of application. It let's you automatically register dependencies and endpoints in modules which simplifies development when you are working in feature slices.

## Quick Start
To get started with Feature Modules there are two options:
1. Look at the sample project in the repository. The sample project found here [FeatureModulesSample](samples/FeatureModulesSample) is a simple web api with two feature modules.
2. Create a new project and integrate the library.

Let's look create a new project and integrate the library.
1. Create a new web api project using the dotnet cli.
```bash
dotnet new webapi -n MyWebApi
```

2. Add the Infinity.Toolkit.FeatureModules package to the project.
```bash
dotnet add package Infinity.Toolkit
```

3. In Program.cs, add the following code:
```csharp
using Infinity.Toolkit.FeatureModules;

var builder = WebApplication.CreateBuilder(args);
builder.AddFeatureModules();

var app = builder.Build();

app.MapFeatureModules();
app.Run();
```

This will add all feature modules added to the project and add the modules services to the application. The `MapFeatureModules` method will map all endpoints to the application.

4. Run the application and make sure that it is working. The application should return a 404 error since there are no endpoints mapped to the application.

### Create a feature module
1. Create a new class called WeatherModule.cs in the root of the project. Make sure to remove the endpoint that was created by the template. Add the following code to the WeatherModule.cs file.

```csharp
internal class WeatherModule : WebFeatureModule
{
    public override IModuleInfo? ModuleInfo { get; } = new FeatureModuleInfo("WeatherModule", "1.0.0");

    public override void MapEndpoints(WebApplication app)
    {

        var summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        app.MapGet("/weatherforecast", () =>
        {
            var forecast =  Enumerable.Range(1, 5).Select(index =>
            new WeatherForecast
            (
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                summaries[Random.Shared.Next(summaries.Length)]
            )).ToArray();

            return forecast;
        })
        .WithName("GetWeatherForecast")
        .WithOpenApi();
    }
}

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
```

2. Run the application and navigate to the /weatherforecast endpoint. You should see the weather forecast.

### Feature modules in a class library
The sample project also refers to a class library with another [feature module](samples/FeatureModulesSample.Module1). When you add the class library to the project, the feature module will be automatically registered and the endpoints will be mapped to the application.

### Types of feature modules

There are two types of feature modules:
1. FeatureModule
2. WebFeatureModule

The difference is that the WebFeatureModule has access to `WebApplication` which allows you to map endpoints to the application.
To create a web feature module, you need to create a class that inherits from `WebFeatureModule` or implements `IWebFeatureModule`. 

# Logging formatter
A logging formatter that formats log messages with a Visual Studio Code inspired theme and Serilog like formatting.

# Infinity.Toolkit.Azure
## TokenCredentialHelper
Helps creating a ChainedTokenCredential used to authenticate with Azure services.
By default, the following credential types are included:
EnvironmentCredential
ManagedIdentityCredential

To include additional credential types, set the corresponding environment variable to "true".
INCLUDE_VISUAL_STUDIO_CREDENTIAL
INCLUDE_VISUAL_STUDIO_CODE_CREDENTIAL
INCLUDE_INTERACTIVE_BROWSER_CREDENTIAL
INCLUDE_AZURE_DEVELOPER_CLI_CREDENTIAL
INCLUDE_AZURE_POWER_SHELL_CREDENTIAL
INCLUDE_AZURE_CLI_CREDENTIAL
INCLUDE_WORKLOAD_IDENTITY_CREDENTIAL

# Contributing
If you have any ideas, suggestions or issues, please create an issue or a pull request. Or reach out to me on [BlueSky](https://bsky.app/profile/peternylander.bsky.social).

# License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

# Pushing to NuGet
 dotnet nuget push .\artifacts\Infinity.Toolkit.FeatureModules.1.1.0.nupkg -k API-KEY -s https://api.nuget.org/v3/index.json