using System.Xml.Linq;

namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class PrivacyDataProtectionUiGuardTests
{
    [Fact]
    public void Maui_and_org_web_privacy_pages_share_semantics_without_compliance_claims()
    {
        var maui = File.ReadAllText(Path.Combine(
            MauiProject(), "Components", "Pages", "Organization", "PrivacyDataProtection.razor"));
        var web = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Web",
            "Components", "Pages", "Settings", "PrivacyDataProtection.razor"));

        foreach (var page in new[] { maui, web })
        {
            Assert.Contains("OrganizationPrivacyReadinessDto", page, StringComparison.Ordinal);
            Assert.Contains("LegalVerificationStatus", page, StringComparison.Ordinal);
            Assert.Contains("Privacy_NoCertificationClaim", page, StringComparison.Ordinal);
            Assert.DoesNotContain("NPC compliant", page, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Privacy compliant", page, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("@page \"/org/privacy\"", maui, StringComparison.Ordinal);
        Assert.Contains("@page \"/settings/privacy\"", web, StringComparison.Ordinal);
    }

    [Fact]
    public void Localization_defaults_legal_verification_to_not_verified_language()
    {
        foreach (var relative in new[]
                 {
                     Path.Combine("src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Maui", "Localization", "PosResources.resx"),
                     Path.Combine("src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Web", "Localization", "OrgWebResources.resx")
                 })
        {
            var resources = XDocument.Load(Path.Combine(FindRepoRoot(), relative));
            var values = resources.Root!
                .Elements("data")
                .Where(e => ((string?)e.Attribute("name"))?.StartsWith("Privacy_", StringComparison.Ordinal) == true)
                .Select(e => e.Element("value")!.Value)
                .ToArray();

            Assert.Contains(values, v => v.Contains("Not verified", StringComparison.OrdinalIgnoreCase)
                                         || v.Contains("not a legal or NPC certification", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(values, v => v.Equals("Privacy compliant", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(values, v => v.Equals("NPC compliant", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void More_hub_exposes_privacy_entry_for_owners_and_setup_viewers()
    {
        var more = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "MoreHub.razor"));
        Assert.Contains("GoPrivacy", more, StringComparison.Ordinal);
        Assert.Contains("/org/privacy", more, StringComparison.Ordinal);
        Assert.Contains("ViewOperationalSetup", more, StringComparison.Ordinal);
    }

    private static string MauiProject() => Path.Combine(
        FindRepoRoot(),
        "src",
        "Products",
        "PinoyBusinessPOS",
        "ExItS.PinoyBusinessPOS.Maui");

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
