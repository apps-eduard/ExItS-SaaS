using System.Xml.Linq;

namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class SalesDocumentUiGuardTests
{
    [Fact]
    public void Sales_document_disclaimer_denies_authorization_without_compliance_claims()
    {
        var resources = XDocument.Load(Path.Combine(MauiProject(), "Localization", "PosResources.resx"));
        var value = resources.Root!
            .Elements("data")
            .Single(element => (string?)element.Attribute("name") == "SalesDocument_DisclaimerBody")
            .Element("value")!
            .Value;

        Assert.DoesNotContain("BIR compliant", value, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Official Receipt", value, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not represented", value, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Online_and_offline_sale_documents_use_transaction_summary_disclaimer()
    {
        var sales = Path.Combine(MauiProject(), "Components", "Pages", "Sales");
        foreach (var name in new[] { "SaleReceipt.razor", "LocalSaleReceipt.razor" })
        {
            var page = File.ReadAllText(Path.Combine(sales, name));
            Assert.Contains("SalesDocument_TransactionSummaryTitle", page, StringComparison.Ordinal);
            Assert.Contains("SalesDocument_DisclaimerBody", page, StringComparison.Ordinal);
        }

        var detail = File.ReadAllText(Path.Combine(sales, "SaleDetail.razor"));
        Assert.Contains("SalesDocument_Open", detail, StringComparison.Ordinal);
    }

    [Fact]
    public void Education_page_is_owner_acknowledgment_not_a_compliance_claim()
    {
        var page = File.ReadAllText(Path.Combine(
            MauiProject(),
            "Components",
            "Pages",
            "Organization",
            "SalesDocumentEducation.razor"));

        Assert.Contains("SalesDocument_EducationAckCheckbox", page, StringComparison.Ordinal);
        Assert.Contains("IsCurrentOwner", page, StringComparison.Ordinal);
        Assert.Contains("SalesDocument_EducationOwnerRequired", page, StringComparison.Ordinal);
        Assert.DoesNotContain("BIR compliant", page, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EnableTax", page, StringComparison.OrdinalIgnoreCase);

        var gate = File.ReadAllText(Path.Combine(MauiProject(), "Services", "NavigationGate.cs"));
        var more = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "MoreHub.razor"));
        Assert.Contains("ResolveOperationalSetupRouteAsync", gate, StringComparison.Ordinal);
        Assert.Contains("RequiresSalesDocumentEducationAsync", gate, StringComparison.Ordinal);
        Assert.Contains("Gate.ResolveOperationalSetupRouteAsync()", more, StringComparison.Ordinal);
    }

    [Fact]
    public void Education_copy_keeps_transaction_summary_and_bir_boundary_explicit()
    {
        var resources = XDocument.Load(Path.Combine(MauiProject(), "Localization", "PosResources.resx"));
        var values = resources.Root!
            .Elements("data")
            .Where(element => ((string?)element.Attribute("name"))?.StartsWith(
                "SalesDocument_Education",
                StringComparison.Ordinal) == true)
            .Select(element => element.Element("value")!.Value)
            .ToArray();

        Assert.Contains(values, value => value.Contains("Transaction Summary", StringComparison.Ordinal));
        Assert.DoesNotContain(values, value => value.Contains("BIR compliant", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(values, value => value.Contains("does not enable BIR invoicing", StringComparison.OrdinalIgnoreCase));
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
