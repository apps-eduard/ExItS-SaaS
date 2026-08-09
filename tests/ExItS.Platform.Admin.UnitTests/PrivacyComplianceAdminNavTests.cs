namespace ExItS.Platform.Admin.UnitTests;

public sealed class PrivacyComplianceAdminNavTests
{
    [Fact]
    public void Admin_nav_exposes_privacy_compliance_platform_routes_only()
    {
        var root = FindRepoRoot();
        var nav = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Layout", "AdminNav.razor"));
        var overview = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "PrivacyComplianceOverview.razor"));

        Assert.Contains("privacy-compliance", nav, StringComparison.Ordinal);
        Assert.Contains("ViewPrivacyCompliance", nav, StringComparison.Ordinal);
        Assert.Contains("/admin/privacy-compliance/documents", nav, StringComparison.Ordinal);
        Assert.Contains("/admin/privacy-compliance/systems", nav, StringComparison.Ordinal);
        Assert.Contains("/admin/privacy-compliance/evidence", nav, StringComparison.Ordinal);
        Assert.Contains("@page \"/admin/privacy-compliance\"", overview, StringComparison.Ordinal);
        Assert.Contains("ViewPrivacyCompliance", overview, StringComparison.Ordinal);
        Assert.Contains("UnauthorizedPanel", overview, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ExItS.slnx"))
                || File.Exists(Path.Combine(dir.FullName, "ExItS.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
