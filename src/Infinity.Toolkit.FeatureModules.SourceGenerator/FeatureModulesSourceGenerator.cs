using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Infinity.Toolkit.FeatureModules.SourceGenerator;

[Generator]
public class FeatureModulesSourceGenerator : IIncrementalGenerator
{
    private const string FeatureModuleAttributeFullName = "Infinity.Toolkit.FeatureModules.FeatureModuleAttribute";
    private const string WebFeatureModuleAttributeFullName = "Infinity.Toolkit.FeatureModules.WebFeatureModuleAttribute";
    private const string IFeatureModuleBaseFullName = "Infinity.Toolkit.FeatureModules.IFeatureModuleBase";
    private const string IFeatureModuleFullName = "Infinity.Toolkit.FeatureModules.IFeatureModule";
    private const string IWebFeatureModuleFullName = "Infinity.Toolkit.FeatureModules.IWebFeatureModule";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Register both attribute types for discovery
        var featureModuleProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                FeatureModuleAttributeFullName,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) => GetModuleInfo(ctx, IFeatureModuleFullName))
            .Where(static m => m.Module != null);

        var webFeatureModuleProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                WebFeatureModuleAttributeFullName,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) => GetModuleInfo(ctx, IWebFeatureModuleFullName))
            .Where(static m => m.Module != null);

        var attributedModulesAndDiagnostics = featureModuleProvider
            .Collect()
            .Combine(webFeatureModuleProvider.Collect())
            .Select(static (pair, _) =>
            {
                var combined = pair.Left.AddRange(pair.Right);
                var modules = combined
                    .Where(m => m.Module != null)
                    .Select(m => m.Module!)
                    .ToImmutableArray();
                var diagnostics = combined.SelectMany(m => m.Diagnostics).ToImmutableArray();
                return (Modules: modules, Diagnostics: diagnostics);
            });

        var assignableModules = context.CompilationProvider
            .Select(static (compilation, _) => GetAssignableModules(compilation));

        var allModules = attributedModulesAndDiagnostics.Combine(assignableModules)
            .Select(static (data, _) =>
            {
                var combinedModules = data.Left.Modules
                    .Concat(data.Right)
                    .GroupBy(module => module.FullTypeName, StringComparer.Ordinal)
                    .Select(group => group.First())
                    .OrderBy(module => module.FullTypeName, StringComparer.Ordinal)
                    .ToImmutableArray();

                return (Modules: combinedModules, Diagnostics: data.Left.Diagnostics);
            });

        // Generate the registry and emit diagnostics
        context.RegisterSourceOutput(allModules, static (spc, data) =>
        {
            foreach (var diagnostic in data.Diagnostics)
            {
                spc.ReportDiagnostic(diagnostic);
            }

            if (data.Modules.IsEmpty)
            {
                return;
            }

            var registrySource = GenerateRegistry(data.Modules);
            spc.AddSource("GeneratedFeatureModuleRegistry.g.cs", registrySource);
        });
    }

    private static (ModuleInfo? Module, ImmutableArray<Diagnostic> Diagnostics) GetModuleInfo(
        GeneratorAttributeSyntaxContext context, 
        string expectedInterface)
    {
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        var classSymbol = context.TargetSymbol as INamedTypeSymbol;

        if (classSymbol == null)
        {
            return (null, diagnostics.ToImmutable());
        }

        // Validate the class implements the expected interface
        var implementsInterface = ImplementsInterface(classSymbol, expectedInterface);

        if (!implementsInterface)
        {
            // Add diagnostic: Attribute applied to class that doesn't implement required interface
            var diagnostic = Diagnostic.Create(
                new DiagnosticDescriptor(
                    id: "IFTK001",
                    title: "Invalid FeatureModule attribute usage",
                    messageFormat: "Class '{0}' is marked with [{1}] but does not implement {2}",
                    category: "Infinity.Toolkit.FeatureModules",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                classSymbol.Locations.FirstOrDefault(),
                classSymbol.Name,
                context.Attributes[0].AttributeClass?.Name ?? "FeatureModuleAttribute",
                expectedInterface.Split('.').Last());

            diagnostics.Add(diagnostic);
            return (null, diagnostics.ToImmutable());
        }

        // Validate class is not abstract
        if (classSymbol.IsAbstract)
        {
            var diagnostic = Diagnostic.Create(
                new DiagnosticDescriptor(
                    id: "IFTK002",
                    title: "Abstract class cannot be a feature module",
                    messageFormat: "Class '{0}' is marked with a FeatureModule attribute but is abstract",
                    category: "Infinity.Toolkit.FeatureModules",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                classSymbol.Locations.FirstOrDefault(),
                classSymbol.Name);

            diagnostics.Add(diagnostic);
            return (null, diagnostics.ToImmutable());
        }

        // Validate class has parameterless constructor
        var hasParameterlessConstructor = classSymbol.Constructors
            .Any(c => c.DeclaredAccessibility == Accessibility.Public && c.Parameters.Length == 0);

        if (!hasParameterlessConstructor)
        {
            var diagnostic = Diagnostic.Create(
                new DiagnosticDescriptor(
                    id: "IFTK003",
                    title: "Feature module must have a public parameterless constructor",
                    messageFormat: "Class '{0}' is marked with a FeatureModule attribute but does not have a public parameterless constructor",
                    category: "Infinity.Toolkit.FeatureModules",
                    DiagnosticSeverity.Warning,
                    isEnabledByDefault: true),
                classSymbol.Locations.FirstOrDefault(),
                classSymbol.Name);

            diagnostics.Add(diagnostic);
        }

        // Return the module info
        var moduleInfo = new ModuleInfo(
            classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
                .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)),
            classSymbol.ContainingAssembly.Name);

        return (moduleInfo, diagnostics.ToImmutable());
    }

    private static bool ImplementsInterface(INamedTypeSymbol classSymbol, string interfaceFullName)
    {
        return classSymbol.AllInterfaces.Any(i =>
            i.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
                .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)) == interfaceFullName);
    }

    private static ImmutableArray<ModuleInfo> GetAssignableModules(Compilation compilation)
    {
        var featureModuleBaseSymbol = compilation.GetTypeByMetadataName(IFeatureModuleBaseFullName);
        if (featureModuleBaseSymbol == null)
        {
            return ImmutableArray<ModuleInfo>.Empty;
        }

        var modules = ImmutableArray.CreateBuilder<ModuleInfo>();

        foreach (var type in GetAllTypes(compilation.Assembly.GlobalNamespace))
        {
            if (type is not { TypeKind: TypeKind.Class, IsAbstract: false } ||
                type.IsImplicitlyDeclared)
            {
                continue;
            }

            if (!SymbolEqualityComparer.Default.Equals(type, featureModuleBaseSymbol) &&
                type.AllInterfaces.Any(i =>
                    SymbolEqualityComparer.Default.Equals(i, featureModuleBaseSymbol)))
            {
                modules.Add(new ModuleInfo(
                    type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
                        .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)),
                    type.ContainingAssembly.Name));
            }
        }

        return modules
            .GroupBy(module => module.FullTypeName, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToImmutableArray();
    }

    private static IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol @namespace)
    {
        foreach (var type in @namespace.GetTypeMembers())
        {
            yield return type;

            foreach (var nestedType in GetNestedTypes(type))
            {
                yield return nestedType;
            }
        }

        foreach (var nestedNamespace in @namespace.GetNamespaceMembers())
        {
            foreach (var type in GetAllTypes(nestedNamespace))
            {
                yield return type;
            }
        }
    }

    private static IEnumerable<INamedTypeSymbol> GetNestedTypes(INamedTypeSymbol type)
    {
        foreach (var nestedType in type.GetTypeMembers())
        {
            yield return nestedType;

            foreach (var deeperNestedType in GetNestedTypes(nestedType))
            {
                yield return deeperNestedType;
            }
        }
    }

    private static string GenerateRegistry(ImmutableArray<ModuleInfo> modules)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("namespace Infinity.Toolkit.FeatureModules;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Generated registry of feature modules discovered at compile-time.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("internal static class GeneratedFeatureModuleRegistry");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Gets all feature module types discovered at compile-time.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    internal static System.Type[] GetModuleTypes()");
        sb.AppendLine("    {");
        sb.AppendLine("        return new System.Type[]");
        sb.AppendLine("        {");

        foreach (var module in modules)
        {
            sb.AppendLine($"            typeof({module.FullTypeName}),");
        }

        sb.AppendLine("        };");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private sealed class ModuleInfo
    {
        public ModuleInfo(string fullTypeName, string assemblyName)
        {
            FullTypeName = fullTypeName;
            AssemblyName = assemblyName;
        }

        public string FullTypeName { get; }
        public string AssemblyName { get; }
    }
}
