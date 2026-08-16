using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class ConnectedBuyersBulkShareUiGuardTests
{
    [Fact]
    public void PostAcceptPrompt_is_lightweight_confirmation_not_per_product_editor()
    {
        var prompt = File.ReadAllText(Path.Combine(Suppliers(), "ConnectedBuyerSharePrompt.razor"));
        Assert.Contains("ConnectedBuyers_ConnectionAcceptedTitle", prompt, StringComparison.Ordinal);
        Assert.Contains("ConnectedBuyers_ShareAllAvailable", prompt, StringComparison.Ordinal);
        Assert.Contains("ConnectedBuyers_ReviewProducts", prompt, StringComparison.Ordinal);
        Assert.Contains("ConnectedBuyers_ConfirmAndShare", prompt, StringComparison.Ordinal);
        Assert.Contains("ConnectedBuyers_NotNow", prompt, StringComparison.Ordinal);
        Assert.Contains("BulkMutateBuyerProductSharesAsync", prompt, StringComparison.Ordinal);
        Assert.Contains("SelectAllMatching: true", prompt, StringComparison.Ordinal);
        Assert.Contains("BuyerShareDraftState", prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("table", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ManageProducts_is_mobile_bulk_list_with_search_filters_and_actions()
    {
        var manage = File.ReadAllText(Path.Combine(Suppliers(), "ConnectedBuyerSharedProducts.razor"));
        Assert.Contains("QueryBuyerProductSharesAsync", manage, StringComparison.Ordinal);
        Assert.Contains("ConnectedBuyers_FilterCustomPrice", manage, StringComparison.Ordinal);
        Assert.Contains("ConnectedBuyers_SearchProducts", manage, StringComparison.Ordinal);
        Assert.Contains("SelectAllMatching", manage, StringComparison.Ordinal);
        Assert.Contains("BulkMutateBuyerProductSharesAsync", manage, StringComparison.Ordinal);
        Assert.Contains("PreviewBuyerProductPricingAsync", manage, StringComparison.Ordinal);
        Assert.Contains("ApplyBuyerProductPricingAsync", manage, StringComparison.Ordinal);
        Assert.Contains("pos-manage-share__sticky", manage, StringComparison.Ordinal);
        Assert.Contains("pos-manage-share__clear", manage, StringComparison.Ordinal);
        Assert.Contains("pos-manage-share__action--primary", manage, StringComparison.Ordinal);
        Assert.Contains("pos-denom-sheet", manage, StringComparison.Ordinal);
        Assert.Contains("pos-manage-share__sheet-body", manage, StringComparison.Ordinal);
        Assert.Contains("pos-manage-share__line3", manage, StringComparison.Ordinal);
        Assert.Contains("pos-denom-sheet__panel--fit", manage, StringComparison.Ordinal);
        Assert.DoesNotContain("Emphasized=\"true\"", manage, StringComparison.Ordinal);
        Assert.Contains("ConnectedBuyers_SetBuyerPriceTitle", manage, StringComparison.Ordinal);
        Assert.Contains("ConnectedBuyers_ReviewPricesTitle", manage, StringComparison.Ordinal);
        Assert.Contains("ConnectedBuyers_UseDefaultPoHelp", manage, StringComparison.Ordinal);
        Assert.Contains("ConnectedBuyers_EmptySearch", manage, StringComparison.Ordinal);
        Assert.Contains("EffectiveBuyerPrice", manage, StringComparison.Ordinal);
        Assert.DoesNotContain("ConnectedBuyers_UsesDefaultPrice", manage, StringComparison.Ordinal);
        Assert.DoesNotContain("SellingPrice", manage, StringComparison.Ordinal);
        Assert.DoesNotContain("<table", manage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DataGrid", manage, StringComparison.Ordinal);
    }

    [Fact]
    public void Localization_includes_bulk_manage_keys()
    {
        var en = File.ReadAllText(Path.Combine(MauiProject(), "Localization", "PosResources.resx"));
        var fil = File.ReadAllText(Path.Combine(MauiProject(), "Localization", "PosResources.fil-PH.resx"));
        foreach (var key in new[]
                 {
                     "ConnectedBuyers_ManageProductsTitle",
                     "ConnectedBuyers_ManageProductsSubtitle",
                     "ConnectedBuyers_SearchProducts",
                     "ConnectedBuyers_FilterCustomPrice",
                     "ConnectedBuyers_SelectAllMatching",
                     "ConnectedBuyers_BulkShare",
                     "ConnectedBuyers_BulkUnshare",
                     "ConnectedBuyers_BulkPrice",
                     "ConnectedBuyers_PricePreview",
                     "ConnectedBuyers_SetBuyerPriceTitle",
                     "ConnectedBuyers_ReviewPricesTitle",
                     "ConnectedBuyers_BuyerPrice",
                     "ConnectedBuyers_UsesDefaultPo",
                     "ConnectedBuyers_UseDefaultPoPrice",
                     "ConnectedBuyers_UseDefaultPoHelp",
                     "ConnectedBuyers_ConnectionAcceptedTitle",
                     "ConnectedBuyers_ConfirmAndShare",
                     "ConnectedBuyers_ManageProductsCtaHelp",
                     "ConnectedBuyers_EmptyNoEligible"
                 })
        {
            Assert.Contains($"name=\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"name=\"{key}\"", fil, StringComparison.Ordinal);
        }

        Assert.Contains("<value>Review prices</value>", en, StringComparison.Ordinal);
        Assert.Contains("<value>Manage products</value>", en, StringComparison.Ordinal);
        Assert.Contains("available to share", en, StringComparison.Ordinal);

        // Guard against re-corruption of special-character strings.
        var unitStock = Extract(en, "Sales_Checkout_UnitStock");
        Assert.True(unitStock.Length < 40, unitStock);
        Assert.Contains("{0}", unitStock, StringComparison.Ordinal);
        Assert.Contains("{1}", unitStock, StringComparison.Ordinal);
    }

    private static string Extract(string resx, string key)
    {
        var marker = $"name=\"{key}\"";
        var start = resx.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(start > 0);
        var valueStart = resx.IndexOf("<value>", start, StringComparison.Ordinal) + 7;
        var valueEnd = resx.IndexOf("</value>", valueStart, StringComparison.Ordinal);
        return resx[valueStart..valueEnd];
    }

    private static string Suppliers() =>
        Path.Combine(MauiProject(), "Components", "Pages", "Suppliers");

    private static string MauiProject() =>
        Path.Combine(FindRepoRoot(), "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Maui");

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ExItS.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
