using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Infinity.Toolkit.FeatureModules.SourceGenerator;

[Generator]
public class FeatureModulesSourceGenerator : IIncrementalGenerator
{
    private const string FeatureModuleAttributeFullName =
        "Infinity.Toolkit.FeatureModules.FeatureModuleAttribute";
    private const string WebFeatureModuleAttributeFullName =
        "Infinity.Toolkit.FeatureModules.WebFeatureModuleAttribute";
    private const string IFeatureModuleBaseFullName =
        "Infinity.Toolkit.FeatureModules.IFeatureModuleBase";
    private const string IFeatureModuleFullName =
        "Infinity.Toolkit.FeatureModules.IFeatureModule";
    private const string IWebFeatureModuleFullName =
        "Infinity.Toolkit.FeatureModules.IWebFeatureModule";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var featureModuleProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                FeatureModuleAttributeFullName,
                static (node, _) => node is ClassDeclarationSyntax,
                static (ctx, _) => GetAttributedModuleInfo(ctx, IFeatureModuleFullName))
            .Where(static result => result.Module != null);

        var webFeatureModuleProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                WebFeatureModuleAttributeFullName,
                static (node, _) => node is ClassDeclarationSyntax,
                static (ctx, _) => GetAttributedModuleInfo(ctx, IWebFeatureModuleFullName))
            .Where(static result => result.Module != null);

        var attributedModulesAndDiagnostics = featureModuleProvider
            .Collect()
            .Combine(webFeatureModuleProvider.Collect())
            .Select(static (pair, _) =>
            {
                var combined = pair.Left.AddRange(pair.Right);
                var modules = combined
                    .Where(item => item.Module != null)
                    .Select(item => item.Module!)
                    .ToImmutableArray();
                var diagnostics = combined.SelectMany(item => item.Diagnostics).ToImmutableArray();
                return (Modules: modules, Diagnostics: diagnostics);
            });

        var assignableModules = context.CompilationProvider
            .Select(static (compilation, _) => GetAssignableModules(compilation));

        var allModules = attributedModulesAndDiagnostics.Combine(assignableModules)
            .Select(static (data, _) =>
            {
                var modulesByFullName = data.Left.Modules.ToDictionary(
                    module => module.FullTypeName,
                    module => module,
                    StringComparer.Ordinal);

                foreach (var module in data.Right)
                {
                    if (!modulesByFullName.ContainsKey(module.FullTypeName))
                    {
                        modulesByFullName[module.FullTypeName] = module;
                    }
                }

                var orderedModules = modulesByFullName.Values
                    .OrderBy(module => module.FullTypeName, StringComparer.Ordinal)
                    .ToImmutableArray();

                return (Modules: orderedModules, Diagnostics: data.Left.Diagnostics);
            });

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

            spc.AddSource("GeneratedFeatureModuleRegistry.g.cs", GenerateRegistry(data.Modules));

            var modulesWithMetadata = data.Modules
                .Where(module => module.Name != null && module.Version != null)
                .ToImmutableArray();

            if (!modulesWithMetadata.IsEmpty)
            {
                spc.AddSource(
                    "GeneratedFeatureModuleMetadataRegistry.g.cs",
                    GenerateMetadataRegistry(modulesWithMetadata));
            }
        });
    }

    private static (ModuleInfo? Module, ImmutableArray<Diagnostic> Diagnostics)
        GetAttributedModuleInfo(
            GeneratorAttributeSyntaxContext context,
            string expectedInterface)
    {
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        var classSymbol = context.TargetSymbol as INamedTypeSymbol;

        if (classSymbol == null)
        {
            return (null, diagnostics.ToImmutable());
        }

        if (!ImplementsInterface(classSymbol, expectedInterface))
        {
            diagnostics.Add(Diagnostic.Create(
                new DiagnosticDescriptor(
                    id: "IFTK001",
                    title: "Invalid FeatureModule attribute usage",
                    messageFormat:
                        "Class '{0}' is marked with [{1}] but does not implement {2}",
                    category: "Infinity.Toolkit.FeatureModules",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                classSymbol.Locations.FirstOrDefault(),
                classSymbol.Name,
                context.Attributes[0].AttributeClass?.Name ?? "FeatureModuleAttribute",
                expectedInterface.Split('.').Last()));

            return (null, diagnostics.ToImmutable());
        }

        if (classSymbol.IsAbstract)
        {
            diagnostics.Add(Diagnostic.Create(
                new DiagnosticDescriptor(
                    id: "IFTK002",
                    title: "Abstract class cannot be a feature module",
                    messageFormat:
                        "Class '{0}' is marked with a FeatureModule attribute but is abstract",
                    category: "Infinity.Toolkit.FeatureModules",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                classSymbol.Locations.FirstOrDefault(),
                classSymbol.Name));

            return (null, diagnostics.ToImmutable());
        }

        var hasParameterlessConstructor = classSymbol.Constructors
            .Any(c => c.DeclaredAccessibility == Accessibility.Public &&
                      c.Parameters.Length == 0);

        if (!hasParameterlessConstructor)
        {
            diagnostics.Add(Diagnostic.Create(
                new DiagnosticDescriptor(
                    id: "IFTK003",
                    title: "Feature module must have a public parameterless constructor",
                    messageFormat:
                        "Class '{0}' is marked with a FeatureModule attribute but " +
                        "does not have a public parameterless constructor",
                    category: "Infinity.Toolkit.FeatureModules",
                    DiagnosticSeverity.Warning,
                    isEnabledByDefault: true),
                classSymbol.Locations.FirstOrDefault(),
                classSymbol.Name));
        }

        if (!TryReadAttributeArguments(context.Attributes[0], out var name, out var version))
        {
            diagnostics.Add(Diagnostic.Create(
                new DiagnosticDescriptor(
                    id: "IFTK004",
                    title: "Invalid FeatureModule attribute arguments",
                    messageFormat:
                        "Class '{0}' has invalid FeatureModule attribute arguments. " +
                        "Name and Version must be non-empty strings",
                    category: "Infinity.Toolkit.FeatureModules",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                classSymbol.Locations.FirstOrDefault(),
                classSymbol.Name));

            return (null, diagnostics.ToImmutable());
        }

        return (BuildModuleInfo(classSymbol, name, version), diagnostics.ToImmutable());
    }

    private static bool TryReadAttributeArguments(
        AttributeData attributeData,
        out string? name,
        out string? version)
    {
        name = null;
        version = null;

        if (attributeData.ConstructorArguments.Length < 2)
        {
            return false;
        }

        name = attributeData.ConstructorArguments[0].Value as string;
        version = attributeData.ConstructorArguments[1].Value as string;

        return !string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(version);
    }

    private static ModuleInfo BuildModuleInfo(
        INamedTypeSymbol classSymbol,
        string? name,
        string? version)
    {
        return new ModuleInfo(
            classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
                .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)),
            classSymbol.ContainingAssembly.Name,
            name,
            version);
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

            if (!ImplementsSymbol(type, featureModuleBaseSymbol))
            {
                continue;
            }

            modules.Add(BuildModuleInfo(type, null, null));
        }

        return modules
            .GroupBy(module => module.FullTypeName, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToImmutableArray();
    }

    private static bool ImplementsInterface(INamedTypeSymbol classSymbol, string interfaceFullName)
    {
        return classSymbol.AllInterfaces.Any(i =>
            i.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
                .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)) ==
            interfaceFullName);
    }

    private static bool ImplementsSymbol(
        INamedTypeSymbol classSymbol,
        INamedTypeSymbol interfaceSymbol)
    {
        return classSymbol.AllInterfaces.Any(i =>
            SymbolEqualityComparer.Default.Equals(i, interfaceSymbol));
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
        sb.AppendLine("internal static class GeneratedFeatureModuleRegistry");
        sb.AppendLine("{");
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

    private static string GenerateMetadataRegistry(ImmutableArray<ModuleInfo> modules)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("namespace Infinity.Toolkit.FeatureModules;");
        sb.AppendLine();
        sb.AppendLine("internal static class GeneratedFeatureModuleMetadataRegistry");
        sb.AppendLine("{");
        sb.AppendLine("    internal static bool TryGetModuleInfo(");
        sb.AppendLine("        System.Type moduleType,");
        sb.AppendLine("        out IModuleInfo? moduleInfo)");
        sb.AppendLine("    {");

        foreach (var module in modules)
        {
            sb.AppendLine($"        if (moduleType == typeof({module.FullTypeName}))");
            sb.AppendLine("        {");
            sb.Append("            moduleInfo = new FeatureModuleInfo(")
                .Append($"\"{EscapeString(module.Name!)}\", ")
                .Append($"\"{EscapeString(module.Version!)}\");")
                .AppendLine();
            sb.AppendLine("            return true;");
            sb.AppendLine("        }");
        }

        sb.AppendLine("        moduleInfo = null;");
        sb.AppendLine("        return false;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string EscapeString(string value)
    {
        return value.Replace("\\", "\\\\")
            .Replace("\"", "\\\"");
    }

    private sealed class ModuleInfo
    {
        public ModuleInfo(
            string fullTypeName,
            string assemblyName,
            string? name,
            string? version)
        {
            FullTypeName = fullTypeName;
            AssemblyName = assemblyName;
            Name = name;
            Version = version;
        }

        public string FullTypeName { get; }

        public string AssemblyName { get; }

        public string? Name { get; }

        public string? Version { get; }
    }
}
