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
