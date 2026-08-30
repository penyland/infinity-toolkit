using Infinity.Toolkit.FeatureModules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Infinity.Toolkit.Tests.FeatureModules;

public class FeatureModuleDiscoveryTests
{
    [Test]
    public void FeatureModuleAttribute_Should_Be_Defined()
    {
        var attribute = typeof(FeatureModuleAttribute);
        attribute.ShouldNotBeNull();
        attribute.IsClass.ShouldBeTrue();
        attribute.BaseType.ShouldBe(typeof(Attribute));
    }

    [Test]
    public void WebFeatureModuleAttribute_Should_Be_Defined()
    {
        var attribute = typeof(WebFeatureModuleAttribute);
        attribute.ShouldNotBeNull();
        attribute.IsClass.ShouldBeTrue();
        attribute.BaseType.ShouldBe(typeof(Attribute));
    }

    [Test]
    public void FeatureModuleAttribute_Should_Have_Correct_Usage()
    {
        var attributeUsage = typeof(FeatureModuleAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .FirstOrDefault();

        attributeUsage.ShouldNotBeNull();
        attributeUsage.ValidOn.ShouldBe(AttributeTargets.Class);
        attributeUsage.AllowMultiple.ShouldBeFalse();
        attributeUsage.Inherited.ShouldBeFalse();
    }

    [Test]
    public void WebFeatureModuleAttribute_Should_Have_Correct_Usage()
    {
        var attributeUsage = typeof(WebFeatureModuleAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .FirstOrDefault();

        attributeUsage.ShouldNotBeNull();
        attributeUsage.ValidOn.ShouldBe(AttributeTargets.Class);
        attributeUsage.AllowMultiple.ShouldBeFalse();
        attributeUsage.Inherited.ShouldBeFalse();
    }

    [Test]
    public void FeatureModuleAttribute_Should_Expose_Name_And_Version()
    {
        var attribute = new FeatureModuleAttribute("ModuleA", "2.0.0");

        attribute.Name.ShouldBe("ModuleA");
        attribute.Version.ShouldBe("2.0.0");
    }

    [Test]
    public void WebFeatureModuleAttribute_Should_Expose_Name_And_Version()
    {
        var attribute = new WebFeatureModuleAttribute("ModuleB", "3.0.0");

        attribute.Name.ShouldBe("ModuleB");
        attribute.Version.ShouldBe("3.0.0");
    }

    [Test]
    public void AddFeatureModules_Should_Discover_Decorated_And_Reflection_Modules()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.AddFeatureModules();

        var provider = builder.Services.BuildServiceProvider();
        var modules = provider.GetServices<IFeatureModuleBase>()
            .Select(x => x.GetType())
            .ToArray();

        modules.ShouldContain(typeof(TestFeatureModule));
        modules.ShouldContain(typeof(TestWebFeatureModule));
        modules.ShouldContain(typeof(TestDirectFeatureModule));
        modules.ShouldContain(typeof(ReflectionOnlyFeatureModule));
    }

    [Test]
    public void AddFeatureModules_Should_Exclude_Module_By_Type()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.AddFeatureModules(options => options.ExcludedModules.Add(typeof(TestFeatureModule)));

        var provider = builder.Services.BuildServiceProvider();
        var modules = provider.GetServices<IFeatureModuleBase>()
            .Select(x => x.GetType())
            .ToArray();

        modules.ShouldNotContain(typeof(TestFeatureModule));
        modules.ShouldContain(typeof(TestWebFeatureModule));
    }

    [Test]
    public void AddFeatureModules_Should_Exclude_Module_By_AppSettings_Module_Name()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["FeatureModules:ExcludedModules:0"] = nameof(TestFeatureModule)
        });

        builder.AddFeatureModules();

        var provider = builder.Services.BuildServiceProvider();
        var modules = provider.GetServices<IFeatureModuleBase>()
            .Select(x => x.GetType())
            .ToArray();

        modules.ShouldNotContain(typeof(TestFeatureModule));
        modules.ShouldContain(typeof(TestWebFeatureModule));
    }

    [Test]
    public void Generated_Metadata_Registry_Should_Contain_Attribute_Name_And_Version()
    {
        var registryType = typeof(TestFeatureModule).Assembly
            .GetType("Infinity.Toolkit.FeatureModules.GeneratedFeatureModuleMetadataRegistry");

        registryType.ShouldNotBeNull();

        var method = registryType.GetMethod(
            "TryGetModuleInfo",
            System.Reflection.BindingFlags.Static |
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Public);

        method.ShouldNotBeNull();

        AssertMetadata(method, typeof(TestFeatureModule), "TestModule", "1.1.0");
        AssertMetadata(method, typeof(TestWebFeatureModule), "TestWebModule", "1.2.0");
        AssertMetadata(method, typeof(TestDirectFeatureModule), "DirectModule", "1.3.0");
    }

    private static void AssertMetadata(
        System.Reflection.MethodInfo method,
        Type moduleType,
        string expectedName,
        string expectedVersion)
    {
        var args = new object?[] { moduleType, null };
        var result = method.Invoke(null, args);

        result.ShouldBeOfType<bool>().ShouldBeTrue();
        args[1].ShouldNotBeNull();

        var moduleInfo = args[1].ShouldBeAssignableTo<IModuleInfo>()!;
        moduleInfo.Name.ShouldBe(expectedName);
        moduleInfo.Version.ShouldBe(expectedVersion);
    }
}

[FeatureModule("TestModule", "1.1.0")]
public class TestFeatureModule : FeatureModule;

[WebFeatureModule("TestWebModule", "1.2.0")]
public class TestWebFeatureModule : WebFeatureModule;

[FeatureModule("DirectModule", "1.3.0")]
public class TestDirectFeatureModule : IFeatureModule
{
    public IModuleInfo ModuleInfo { get; } = new FeatureModuleInfo("DirectModule", "1.3.0");

    public void RegisterModule(IHostApplicationBuilder builder)
    {
    }
}

public class ReflectionOnlyFeatureModule : FeatureModule
{
    public override IModuleInfo ModuleInfo =>
        new FeatureModuleInfo("ReflectionOnlyFeatureModule", "1.0.0");
}
