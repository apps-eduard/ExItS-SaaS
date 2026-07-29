using System.Reflection;
using NetArchTest.Rules;

namespace ExItS.ArchitectureTests;

public sealed class LayerDependencyTests
{
    private static readonly Assembly Domain = typeof(ExItS.Platform.Domain.AssemblyMarker).Assembly;
    private static readonly Assembly Application = typeof(ExItS.Platform.Application.AssemblyMarker).Assembly;
    private static readonly Assembly Infrastructure = typeof(ExItS.Platform.Infrastructure.AssemblyMarker).Assembly;
    private static readonly Assembly Api = typeof(Program).Assembly;

    [Fact]
    public void Domain_does_not_depend_on_Application_Infrastructure_or_Api()
    {
        var result = Types.InAssembly(Domain)
            .ShouldNot()
            .HaveDependencyOnAny(
                "ExItS.Platform.Application",
                "ExItS.Platform.Infrastructure",
                "ExItS.Platform.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, Format(result));
    }

    [Fact]
    public void Application_does_not_depend_on_Infrastructure_or_Api()
    {
        var result = Types.InAssembly(Application)
            .ShouldNot()
            .HaveDependencyOnAny(
                "ExItS.Platform.Infrastructure",
                "ExItS.Platform.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, Format(result));
    }

    [Fact]
    public void Infrastructure_does_not_depend_on_Api()
    {
        var result = Types.InAssembly(Infrastructure)
            .ShouldNot()
            .HaveDependencyOn("ExItS.Platform.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, Format(result));
    }

    [Fact]
    public void Platform_assemblies_do_not_reference_HealthCare_or_AntDesign()
    {
        foreach (var assembly in new[] { Domain, Application, Infrastructure, Api })
        {
            var referenced = assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToArray();
            Assert.DoesNotContain(referenced, name =>
                name.Contains("HealthCare", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(referenced, name =>
                name.Contains("AntDesign", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(referenced, name =>
                name.Contains("AntDesign.Components", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void Platform_projects_do_not_reference_Tailwind_packages()
    {
        foreach (var assembly in new[] { Domain, Application, Infrastructure, Api })
        {
            var referenced = assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToArray();
            Assert.DoesNotContain(referenced, name =>
                name.Contains("Tailwind", StringComparison.OrdinalIgnoreCase));
        }
    }

    private static string Format(TestResult result) =>
        result.IsSuccessful
            ? string.Empty
            : "Failing types: " + string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>());
}
