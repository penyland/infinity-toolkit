using Infinity.Toolkit.FeatureModules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Infinity.Toolkit.FeatureModules.Tests;

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
        modules.ShouldContain(typeof(ReflectionOnlyFeatureModule));
    }
}

[FeatureModule]
public class TestFeatureModule : FeatureModule
{
    public override IModuleInfo ModuleInfo => new FeatureModuleInfo("TestModule", "1.0.0");
}

[WebFeatureModule]
public class TestWebFeatureModule : WebFeatureModule
{
    public override IModuleInfo ModuleInfo => new FeatureModuleInfo("TestWebModule", "1.0.0");
}

public class ReflectionOnlyFeatureModule : FeatureModule
{
    public override IModuleInfo ModuleInfo =>
        new FeatureModuleInfo("ReflectionOnlyFeatureModule", "1.0.0");
}
