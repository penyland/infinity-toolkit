using Microsoft.Extensions.DependencyModel;

namespace Infinity.Toolkit.FeatureModules;

internal static class ModuleUtilities
{

    /// <summary>
    /// Discover all modules that references IFeatureModule.
    /// </summary>
    /// <returns>A list of all feature modules in the solution.</returns>
    public static IEnumerable<TypeInfo> DiscoverModules(FeatureModuleOptions options, ILogger? logger)
    {
        var assemblies = new HashSet<Assembly>
        {
            typeof(Assembly).Assembly,
        };

        var entryAssembly = Assembly.GetEntryAssembly();
        var context = DependencyContext.Load(entryAssembly!)!;

        foreach (var assembly in context.RuntimeLibraries)
        {
            if (IsReferencingCurrentAssembly(assembly, typeof(WebApplicationBuilderExtensions).Assembly.GetName().Name))
            {
                foreach (var assemblyName in assembly.GetDefaultAssemblyNames(context))
                {
                    assemblies.Add(Assembly.Load(assemblyName));
                }
            }
        }

        var typesAssignableTo = assemblies
            .SelectMany(x =>
                x.DefinedTypes
                .Where(type => type is { IsAbstract: false, IsInterface: false } &&
                                      type.IsAssignableTo(typeof(IFeatureModule)) &&
                                      !options.ExcludedModules.Any(t => t == type.FullName)));

        logger?.LogInformation(new EventId(1001, "ModulesFound"), "Found {moduleCount} feature modules.", typesAssignableTo.Count());
        return typesAssignableTo;
    }

    private static bool IsReferencingCurrentAssembly(Library library, string? currentAssemblyName)
    {
        return library.Dependencies.Any(dependency => dependency.Name.Equals(currentAssemblyName));
    }
}
