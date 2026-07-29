using ExItS.Platform.Domain;

namespace ExItS.Platform.UnitTests;

public sealed class FoundationTests
{
    [Fact]
    public void Domain_assembly_marker_identifies_platform_domain()
    {
        Assert.Equal("ExItS.Platform.Domain", AssemblyMarker.Name);
        Assert.NotNull(typeof(AssemblyMarker).Assembly.GetName().Name);
    }

    [Fact]
    public void Domain_assembly_name_matches_approved_namespace_casing()
    {
        var name = typeof(AssemblyMarker).Assembly.GetName().Name;
        Assert.Equal("ExItS.Platform.Domain", name);
        Assert.DoesNotContain("ExitS", name, StringComparison.Ordinal);
        Assert.DoesNotContain("EXITS", name, StringComparison.Ordinal);
    }
}
