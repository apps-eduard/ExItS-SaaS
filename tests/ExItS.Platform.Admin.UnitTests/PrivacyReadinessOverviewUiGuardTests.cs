using System.Xml.Linq;

namespace ExItS.Platform.Admin.UnitTests;

public sealed class PrivacyReadinessOverviewUiGuardTests
{
    [Fact]
    public void Overview_shows_derived_readiness_and_does_not_claim_compliance()
    {
        var page = File.ReadAllText(Path.Combine(AdminProject(), "Components", "Pages", "PrivacyComplianceOverview.razor"));
        Assert.Contains("OverallReadiness", page, StringComparison.Ordinal);
        Assert.Contains("PrivacyCompliance_NoCertificationClaim", page, StringComparison.Ordinal);
        Assert.Contains("CategorySummaries", page, StringComparison.Ordinal);
        Assert.Contains("PrivacyImpactFollowUps", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Privacy compliant", page, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("NPC compliant", page, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Fully compliant", page, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Status_tag_uses_verified_internally_not_compliant_green_success()
    {
        var tag = File.ReadAllText(Path.Combine(AdminProject(), "Components", "Shared", "PrivacyComplianceStatusTag.razor"));
        Assert.Contains("PrivacyCompliance_Status_VerifiedInternally", tag, StringComparison.Ordinal);
        Assert.Contains("\"Approved\" or \"VerifiedInternally\" or \"ReadyForReview\" => \"processing\"", tag, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Approved\" => \"success\"", tag, StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_resources_avoid_certified_npc_claims_on_overview_keys()
    {
        var resources = XDocument.Load(Path.Combine(AdminProject(), "Localization", "AdminResources.resx"));
        var values = resources.Root!
            .Elements("data")
            .Where(e => ((string?)e.Attribute("name"))?.StartsWith("PrivacyCompliance_", StringComparison.Ordinal) == true)
            .Select(e => e.Element("value")!.Value)
            .ToArray();

        Assert.DoesNotContain(values, v => v.Contains("Privacy compliant", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(values, v => v.Contains("NPC compliant", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(values, v => v.Contains("Certified by NPC", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(values, v => v.Contains("not represent legal or NPC certification", StringComparison.OrdinalIgnoreCase));
    }

    private static string AdminProject() => Path.Combine(
        FindRepoRoot(),
        "src",
        "Platform",
        "ExItS.Platform.Admin");

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ExItS.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
