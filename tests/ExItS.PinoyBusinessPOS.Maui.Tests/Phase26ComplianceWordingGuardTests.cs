using System.Xml.Linq;

namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class Phase26ComplianceWordingGuardTests
{
    [Fact]
    public void Org_web_sales_documents_page_avoids_compliance_claims()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var page = File.ReadAllText(Path.Combine(
            root,
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Web",
            "Components",
            "Pages",
            "Organization",
            "SalesDocuments.razor"));

        Assert.Contains("SalesDocument_EducationTitle", page, StringComparison.Ordinal);
        Assert.Contains("SalesDocument_BirInvoicingNotEnabled", page, StringComparison.Ordinal);
        Assert.Contains("/organization/tax-compliance", page, StringComparison.Ordinal);
        Assert.DoesNotContain("BIR compliant", page, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("BIR accredited", page, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("legally exempt", page, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Org_web_resources_keep_denial_wording_without_accreditation_claims()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var resx = XDocument.Load(Path.Combine(
            root,
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Web",
            "Localization",
            "OrgWebResources.resx"));
        var values = resx.Root!
            .Elements("data")
            .Where(e => ((string?)e.Attribute("name"))?.StartsWith("SalesDocument_", StringComparison.Ordinal) == true)
            .Select(e => (string?)e.Element("value") ?? string.Empty)
            .ToArray();

        Assert.Contains(values, v => v.Contains("Transaction Summary", StringComparison.Ordinal));
        Assert.DoesNotContain(values, v => v.Contains("BIR compliant", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(values, v => v.Contains("BIR accredited", StringComparison.OrdinalIgnoreCase));
    }
}
