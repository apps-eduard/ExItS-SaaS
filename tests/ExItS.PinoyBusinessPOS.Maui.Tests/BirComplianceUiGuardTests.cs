using System.Xml.Linq;

namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class BirComplianceUiGuardTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    [Fact]
    public void Org_web_tax_compliance_page_exists_without_bir_certification_claims()
    {
        var page = File.ReadAllText(Path.Combine(
            RepoRoot,
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Web",
            "Components",
            "Pages",
            "Organization",
            "TaxCompliance.razor"));

        Assert.Contains("@page \"/organization/tax-compliance\"", page, StringComparison.Ordinal);
        Assert.Contains("TaxCompliance_Title", page, StringComparison.Ordinal);
        Assert.Contains("org-page", page, StringComparison.Ordinal);
        Assert.DoesNotContain("BIR Compliant", page, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("BIR Certified", page, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("BIR accredited", page, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Maui_tax_compliance_page_uses_compact_classes_without_bir_certification_claims()
    {
        var page = File.ReadAllText(Path.Combine(
            RepoRoot,
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui",
            "Components",
            "Pages",
            "Organization",
            "TaxCompliance.razor"));

        Assert.Contains("@page \"/organization/tax-compliance\"", page, StringComparison.Ordinal);
        Assert.Contains("pos-tax-compliance", page, StringComparison.Ordinal);
        Assert.Contains("pos-tax-compliance__card--compact", page, StringComparison.Ordinal);
        Assert.DoesNotContain("BIR Compliant", page, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("BIR Certified", page, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<table", page, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Tax_compliance_localization_keys_exist_in_en_and_fil()
    {
        AssertTaxComplianceKeys(Path.Combine(
            RepoRoot,
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui",
            "Localization",
            "PosResources.resx"));
        AssertTaxComplianceKeys(Path.Combine(
            RepoRoot,
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui",
            "Localization",
            "PosResources.fil-PH.resx"));
        AssertTaxComplianceKeys(Path.Combine(
            RepoRoot,
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Web",
            "Localization",
            "OrgWebResources.resx"));
        AssertTaxComplianceKeys(Path.Combine(
            RepoRoot,
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Web",
            "Localization",
            "OrgWebResources.fil-PH.resx"));
    }

    private static void AssertTaxComplianceKeys(string resxPath)
    {
        var doc = XDocument.Load(resxPath);
        var names = doc.Root!
            .Elements("data")
            .Select(e => (string?)e.Attribute("name") ?? string.Empty)
            .Where(n => n.StartsWith("TaxCompliance_", StringComparison.Ordinal))
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("TaxCompliance_Title", names);
        Assert.Contains("TaxCompliance_Subtitle", names);
        Assert.Contains("TaxCompliance_Progress", names);

        var values = doc.Root!
            .Elements("data")
            .Where(e => ((string?)e.Attribute("name"))?.StartsWith("TaxCompliance_", StringComparison.Ordinal) == true)
            .Select(e => (string?)e.Element("value") ?? string.Empty)
            .ToArray();

        Assert.DoesNotContain(values, v => v.Contains("BIR Compliant", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(values, v => v.Contains("BIR Certified", StringComparison.OrdinalIgnoreCase));
    }
}
