using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class ConnectedBuyersUiGuardTests
{
    [Fact]
    public void MauiSuppliersShowsConnectedBuyersEntry()
    {
        var list = File.ReadAllText(Path.Combine(MauiProject(),
            "Components", "Pages", "Suppliers", "SuppliersList.razor"));
        Assert.Contains("GoConnectedBuyers", list, StringComparison.Ordinal);
        Assert.Contains("/suppliers/connected/buyers", list, StringComparison.Ordinal);
        Assert.Contains("ConnectedBuyers_Title", list, StringComparison.Ordinal);

        var more = File.ReadAllText(Path.Combine(MauiProject(),
            "Components", "Pages", "MoreHub.razor"));
        Assert.Contains("GoConnectedBuyers", more, StringComparison.Ordinal);
        Assert.Contains("/suppliers/connected/buyers", more, StringComparison.Ordinal);
    }

    [Fact]
    public void MauiConnectedBuyersShowsActiveRelationships_and_empty_state()
    {
        var page = File.ReadAllText(Path.Combine(MauiProject(),
            "Components", "Pages", "Suppliers", "ConnectedBuyers.razor"));
        Assert.Contains("@page \"/suppliers/connected/buyers\"", page, StringComparison.Ordinal);
        Assert.Contains("@page \"/suppliers/connected/buyers/{RelationshipId:guid}\"", page, StringComparison.Ordinal);
        Assert.Contains("ListRelationshipsAsync(\"supplier\")", page, StringComparison.Ordinal);
        Assert.Contains("string.Equals(x.Status, \"Active\"", page, StringComparison.Ordinal);
        Assert.Contains("ConnectedBuyers_EmptyTitle", page, StringComparison.Ordinal);
        Assert.Contains("ConnectedBuyers_EmptyMessage", page, StringComparison.Ordinal);
        Assert.Contains("ConnectedBuyers_ReviewRequests", page, StringComparison.Ordinal);
        Assert.Contains("ConnectedBuyers_Direction", page, StringComparison.Ordinal);
        Assert.Contains("ConnectedBuyers_NotCustomerNote", page, StringComparison.Ordinal);
        Assert.Contains("ConnectedBuyers_ViewConnection", page, StringComparison.Ordinal);
        Assert.Contains("/suppliers/connected/requests", page, StringComparison.Ordinal);
        Assert.DoesNotContain("ICustomerRepository", page, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateCustomer", page, StringComparison.Ordinal);
        Assert.DoesNotContain("/customers", page, StringComparison.Ordinal);
    }

    [Fact]
    public void MauiConnectedBuyerDetailShowsDirection()
    {
        var page = File.ReadAllText(Path.Combine(MauiProject(),
            "Components", "Pages", "Suppliers", "ConnectedBuyers.razor"));
        Assert.Contains("ConnectedBuyers_DetailTitle", page, StringComparison.Ordinal);
        Assert.Contains("ConnectedBuyers_Direction", page, StringComparison.Ordinal);
        Assert.Contains("ConnectedBuyers_ConnectedSince", page, StringComparison.Ordinal);
        Assert.Contains("ConnectedBuyers_NotCustomerNote", page, StringComparison.Ordinal);
        Assert.Contains("ConnectedBuyers_ManageSharedProducts", page, StringComparison.Ordinal);
        Assert.Contains("/shared-products", page, StringComparison.Ordinal);
    }

    [Fact]
    public void MauiAcceptNavigatesToProductSharingPrompt()
    {
        var requests = File.ReadAllText(Path.Combine(MauiProject(),
            "Components", "Pages", "Suppliers", "ConnectedSupplierIncomingRequests.razor"));
        Assert.Contains("ApproveAsync", requests, StringComparison.Ordinal);
        Assert.Contains("/{relationshipId:D}/share-products", requests, StringComparison.Ordinal);

        var notifications = File.ReadAllText(Path.Combine(MauiProject(),
            "Components", "Pages", "Organization", "OrganizationNotifications.razor"));
        Assert.Contains("ApproveAsync", notifications, StringComparison.Ordinal);
        Assert.Contains("/{relationshipId:D}/share-products", notifications, StringComparison.Ordinal);
        Assert.Contains("AcceptedConfirmation", notifications, StringComparison.Ordinal);
    }

    [Fact]
    public void MauiBuyerProductSharingPagesWireClientOperations()
    {
        var suppliers = Path.Combine(MauiProject(), "Components", "Pages", "Suppliers");
        var prompt = File.ReadAllText(Path.Combine(suppliers, "ConnectedBuyerSharePrompt.razor"));
        Assert.Contains("@page \"/suppliers/connected/buyers/{RelationshipId:guid}/share-products\"", prompt, StringComparison.Ordinal);
        Assert.Contains("ListEligibleProductsForSharingAsync", prompt, StringComparison.Ordinal);
        Assert.Contains("ConfirmBuyerProductSharingAsync", prompt, StringComparison.Ordinal);
        Assert.Contains("GoBuyer", prompt, StringComparison.Ordinal);

        var manage = File.ReadAllText(Path.Combine(suppliers, "ConnectedBuyerSharedProducts.razor"));
        Assert.Contains("@page \"/suppliers/connected/buyers/{RelationshipId:guid}/shared-products\"", manage, StringComparison.Ordinal);
        Assert.Contains("ListBuyerProductSharesAsync", manage, StringComparison.Ordinal);
        Assert.Contains("SetBuyerProductSharesAsync", manage, StringComparison.Ordinal);
        Assert.Contains("ConnectedBuyers_FilterNotShared", manage, StringComparison.Ordinal);
        Assert.Contains("ConnectedBuyers_FilterCategory", manage, StringComparison.Ordinal);
        Assert.Contains("ConnectedBuyers_FilterAllCategories", manage, StringComparison.Ordinal);
        Assert.Contains("pos-share-products__row", manage, StringComparison.Ordinal);
        Assert.Contains("BuyerSpecificPoPrice", manage, StringComparison.Ordinal);

        Assert.Contains("ConnectedBuyers_FilterCategory", prompt, StringComparison.Ordinal);
        Assert.Contains("pos-share-prompt__row", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void Localization_includes_connected_buyers_keys()
    {
        var en = File.ReadAllText(Path.Combine(MauiProject(), "Localization", "PosResources.resx"));
        var fil = File.ReadAllText(Path.Combine(MauiProject(), "Localization", "PosResources.fil-PH.resx"));
        foreach (var key in new[]
                 {
                     "ConnectedBuyers_Title",
                     "ConnectedBuyers_Help",
                     "ConnectedBuyers_EmptyMessage",
                     "ConnectedBuyers_NotCustomerNote",
                     "ConnectedBuyers_ReviewRequests",
                     "ConnectedBuyers_ViewConnection",
                     "ConnectedBuyers_Direction",
                     "ConnectedBuyers_SharePromptTitle",
                     "ConnectedBuyers_ManageSharedProducts",
                     "ConnectedBuyers_SharedProductsTitle",
                     "ConnectedBuyers_BuyerSpecificPoPrice",
                     "ConnectedBuyers_FilterCategory",
                     "ConnectedBuyers_FilterAllCategories",
                     "ConnectedBuyers_FilterUncategorized",
                     "ConnectedBuyers_ShareShort",
                     "ConnectedBuyers_RetailShort",
                     "ConnectedBuyers_DefaultPoShort"
                 })
        {
            Assert.Contains($"name=\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"name=\"{key}\"", fil, StringComparison.Ordinal);
        }

        Assert.Contains("Businesses that buy from you through ExItS supplier connections.", en, StringComparison.Ordinal);
        Assert.Contains("These are business connections, not customer records.", en, StringComparison.Ordinal);
        Assert.Contains("Mga negosyong bumibili sa iyong negosyo gamit ang supplier connection.", fil, StringComparison.Ordinal);
    }

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
