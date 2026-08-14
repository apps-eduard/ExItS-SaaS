namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class InventoryPageGuardTests
{
    [Fact]
    public void Inventory_routes_cover_list_detail_adjust_and_low_stock()
    {
        var pages = InventoryPagesDirectory();

        var list = File.ReadAllText(Path.Combine(pages, "InventoryList.razor"));
        Assert.Contains("@page \"/inventory\"", list, StringComparison.Ordinal);
        Assert.Contains("IPosInventoryClient", list, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ViewInventory", list, StringComparison.Ordinal);
        Assert.Contains("!_allowed", list, StringComparison.Ordinal);
        Assert.Contains("Access_RestrictedTitle", list, StringComparison.Ordinal);
        Assert.Contains("Access_RestrictedMessage", list, StringComparison.Ordinal);
        Assert.Contains("pos-action-grid--four", list, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"warning\")", list, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"check\")", list, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"lent\")", list, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"refresh\")", list, StringComparison.Ordinal);
        Assert.Contains("/inventory/transfers", list, StringComparison.Ordinal);
        Assert.Contains("pos-inventory__row", list, StringComparison.Ordinal);
        Assert.Contains("pos-inventory__header", list, StringComparison.Ordinal);
        Assert.DoesNotContain("PageHeader", list, StringComparison.Ordinal);

        var detail = File.ReadAllText(Path.Combine(pages, "InventoryDetail.razor"));
        Assert.Contains("@page \"/inventory/{ProductId:guid}\"", detail, StringComparison.Ordinal);
        Assert.Contains("StoreHeaderBack Href=\"/inventory\"", detail, StringComparison.Ordinal);
        Assert.Contains("ListMovementsAsync", detail, StringComparison.Ordinal);
        Assert.Contains("pos-inventory-detail__header", detail, StringComparison.Ordinal);
        Assert.Contains("pos-inventory-detail__facts", detail, StringComparison.Ordinal);
        Assert.Contains("ListLotsAsync", detail, StringComparison.Ordinal);
        Assert.Contains("Inventory_Lots", detail, StringComparison.Ordinal);
        Assert.Contains("InventoryUiOptions.MovementTypeLabel", detail, StringComparison.Ordinal);
        Assert.Contains("pos-inventory-detail__entry-remarks", detail, StringComparison.Ordinal);
        Assert.DoesNotContain("PageHeader", detail, StringComparison.Ordinal);
        Assert.DoesNotContain("OnClick=\"GoBack\"", detail, StringComparison.Ordinal);
        Assert.DoesNotContain("<span class=\"pos-inventory-detail__entry-name\">@m.MovementType</span>", detail, StringComparison.Ordinal);

        var adjust = File.ReadAllText(Path.Combine(pages, "InventoryAdjust.razor"));
        Assert.Contains("@page \"/inventory/{ProductId:guid}/adjust\"", adjust, StringComparison.Ordinal);
        Assert.Contains("StoreHeaderBack Href=", adjust, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ManageInventory", adjust, StringComparison.Ordinal);
        Assert.Contains("pos-inventory-adjust__header", adjust, StringComparison.Ordinal);
        Assert.Contains("Inventory_DirectionIn", adjust, StringComparison.Ordinal);
        Assert.Contains("Inventory_DirectionOut", adjust, StringComparison.Ordinal);
        Assert.Contains("Inventory_Remarks", adjust, StringComparison.Ordinal);
        Assert.DoesNotContain("PageHeader", adjust, StringComparison.Ordinal);
        Assert.DoesNotContain("OnClick=\"GoBack\"", adjust, StringComparison.Ordinal);
        Assert.DoesNotContain("Label=\"@L[\"Inventory_Reason\"]\"", adjust, StringComparison.Ordinal);

        var low = File.ReadAllText(Path.Combine(pages, "InventoryLowStock.razor"));
        Assert.Contains("@page \"/inventory/low-stock\"", low, StringComparison.Ordinal);
        Assert.Contains("pos-action-grid--two", low, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"products\")", low, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"refresh\")", low, StringComparison.Ordinal);
        Assert.Contains("pos-inventory__row--low", low, StringComparison.Ordinal);
        Assert.Contains("pos-low-stock__header", low, StringComparison.Ordinal);
        Assert.Contains("Inventory_LowStockEmptyTitle", low, StringComparison.Ordinal);
        Assert.DoesNotContain("PageHeader", low, StringComparison.Ordinal);
        Assert.DoesNotContain("ResponsiveDataList", low, StringComparison.Ordinal);

        var create = File.ReadAllText(Path.Combine(pages, "StockCountCreate.razor"));
        Assert.Contains("@page \"/inventory/counts/new\"", create, StringComparison.Ordinal);
        Assert.Contains("StoreHeaderBack Href=\"/inventory/counts\"", create, StringComparison.Ordinal);
        Assert.Contains("CreateStockCountAsync", create, StringComparison.Ordinal);
        Assert.Contains("Inventory_CountsReset", create, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"products\")", create, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"check\")", create, StringComparison.Ordinal);
        Assert.Contains("pos-stock-count-create__line", create, StringComparison.Ordinal);
        Assert.Contains("pos-stock-count-create__header", create, StringComparison.Ordinal);
        Assert.Contains("ConfirmDialog", create, StringComparison.Ordinal);
        Assert.DoesNotContain("PageHeader", create, StringComparison.Ordinal);
        Assert.DoesNotContain("OnClick=\"GoBack\"", create, StringComparison.Ordinal);

        var reorder = File.ReadAllText(Path.Combine(pages, "InventoryReorder.razor"));
        Assert.Contains("@page \"/inventory/{ProductId:guid}/reorder\"", reorder, StringComparison.Ordinal);
        Assert.Contains("StoreHeaderBack Href=", reorder, StringComparison.Ordinal);
        Assert.Contains("pos-inventory-reorder__header", reorder, StringComparison.Ordinal);
        Assert.Contains("Inventory_ReorderIntro", reorder, StringComparison.Ordinal);
        Assert.Contains("Inventory_ReorderDetails", reorder, StringComparison.Ordinal);
        Assert.Contains("Inventory_ReorderGoAdjust", reorder, StringComparison.Ordinal);
        Assert.Contains("IsNullOrWhiteSpace(_reason)", reorder, StringComparison.Ordinal);
        Assert.Contains("inputmode", reorder, StringComparison.Ordinal);
        Assert.DoesNotContain("PageHeader", reorder, StringComparison.Ordinal);
        Assert.DoesNotContain("OnClick=\"GoBack\"", reorder, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"inventory-reorder-heading\">@L[\"Inventory_ReorderTitle\"]", reorder, StringComparison.Ordinal);

        var counts = File.ReadAllText(Path.Combine(pages, "StockCountsList.razor"));
        Assert.Contains("@page \"/inventory/counts\"", counts, StringComparison.Ordinal);
        Assert.Contains("pos-stock-counts__row", counts, StringComparison.Ordinal);
        Assert.Contains("pos-stock-counts__header", counts, StringComparison.Ordinal);
        Assert.Contains("pos-action-grid--three", counts, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"plus\")", counts, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"products\")", counts, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"refresh\")", counts, StringComparison.Ordinal);
        Assert.DoesNotContain("PageHeader", counts, StringComparison.Ordinal);
        Assert.DoesNotContain("ResponsiveDataList", counts, StringComparison.Ordinal);

        var countDetail = File.ReadAllText(Path.Combine(pages, "StockCountDetail.razor"));
        Assert.Contains("@page \"/inventory/counts/{StockCountId:guid}\"", countDetail, StringComparison.Ordinal);
        Assert.Contains("StoreHeaderBack Href=\"/inventory/counts\"", countDetail, StringComparison.Ordinal);
        Assert.Contains("pos-stock-count-detail__header", countDetail, StringComparison.Ordinal);
        Assert.DoesNotContain("PageHeader", countDetail, StringComparison.Ordinal);
        Assert.DoesNotContain("OnClick=\"GoBack\"", countDetail, StringComparison.Ordinal);

        var transfers = File.ReadAllText(Path.Combine(pages, "InventoryTransfers.razor"));
        Assert.Contains("@page \"/inventory/transfers\"", transfers, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ViewInventory", transfers, StringComparison.Ordinal);
        Assert.Contains("outgoing", transfers, StringComparison.Ordinal);
        Assert.Contains("incoming", transfers, StringComparison.Ordinal);
        Assert.Contains("history", transfers, StringComparison.Ordinal);
        Assert.DoesNotContain("PageHeader", transfers, StringComparison.Ordinal);

        var transferCreate = File.ReadAllText(Path.Combine(pages, "InventoryTransferCreate.razor"));
        Assert.Contains("@page \"/inventory/transfers/new\"", transferCreate, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ManageInventory", transferCreate, StringComparison.Ordinal);
        Assert.Contains("CreateTransferAsync", transferCreate, StringComparison.Ordinal);
        Assert.DoesNotContain("PageHeader", transferCreate, StringComparison.Ordinal);

        var transferDetail = File.ReadAllText(Path.Combine(pages, "InventoryTransferDetail.razor"));
        Assert.Contains("@page \"/inventory/transfers/{TransferId:guid}\"", transferDetail, StringComparison.Ordinal);
        Assert.Contains("DispatchTransferAsync", transferDetail, StringComparison.Ordinal);
        Assert.DoesNotContain("PageHeader", transferDetail, StringComparison.Ordinal);

        var transferReceive = File.ReadAllText(Path.Combine(pages, "InventoryTransferReceive.razor"));
        Assert.Contains("@page \"/inventory/transfers/{TransferId:guid}/receive\"", transferReceive, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ManageInventory", transferReceive, StringComparison.Ordinal);
        Assert.Contains("ReceiveTransferAsync", transferReceive, StringComparison.Ordinal);
        Assert.Contains("ConfirmDialog", transferReceive, StringComparison.Ordinal);
        Assert.DoesNotContain("PageHeader", transferReceive, StringComparison.Ordinal);
    }

    [Fact]
    public void Inventory_pages_guard_entry_and_gate_view_capability()
    {
        foreach (var name in new[] { "InventoryList.razor", "InventoryDetail.razor", "InventoryLowStock.razor", "StockCountsList.razor", "InventoryTransfers.razor", "InventoryTransferDetail.razor" })
        {
            var text = File.ReadAllText(Path.Combine(InventoryPagesDirectory(), name));
            Assert.Contains("Gate.CanEnterProtectedShell", text, StringComparison.Ordinal);
            Assert.Contains("UtangCapability.ViewInventory", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Inventory_adjust_requires_manage_capability_and_validates_input()
    {
        var adjust = File.ReadAllText(Path.Combine(InventoryPagesDirectory(), "InventoryAdjust.razor"));
        Assert.Contains("UtangCapability.ManageInventory", adjust, StringComparison.Ordinal);
        Assert.Contains("qty <= 0", adjust, StringComparison.Ordinal);
        Assert.Contains("IsNullOrWhiteSpace(_reason)", adjust, StringComparison.Ordinal);
        Assert.Contains("Inventory_RemarksRequired", adjust, StringComparison.Ordinal);
        Assert.Contains("_trackExpiration", adjust, StringComparison.Ordinal);
        Assert.Contains("Inventory_ExpirationDate", adjust, StringComparison.Ordinal);
        Assert.Contains("UpdateProductAsync", adjust, StringComparison.Ordinal);
        Assert.Contains("TracksExpiration: true", adjust, StringComparison.Ordinal);
        Assert.Contains("Inventory_DirectionIn", adjust, StringComparison.Ordinal);
        Assert.Contains("Inventory_DirectionOut", adjust, StringComparison.Ordinal);
    }

    [Fact]
    public void MoreHub_gates_inventory_with_view_capability()
    {
        var more = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui",
            "Components",
            "Pages",
            "MoreHub.razor"));
        Assert.Contains("UtangCapability.ViewInventory", more, StringComparison.Ordinal);
        Assert.Contains("GoInventory", more, StringComparison.Ordinal);
    }

    [Fact]
    public void Inventory_movement_history_maps_manual_types_to_friendly_labels()
    {
        var options = File.ReadAllText(Path.Combine(InventoryPagesDirectory(), "InventoryUiOptions.cs"));
        Assert.Contains("Inventory_Movement_{code}", options, StringComparison.Ordinal);
        Assert.Contains("StockMovementPresentation.ToFriendlyLabel", options, StringComparison.Ordinal);

        var resources = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui",
            "Localization",
            "PosResources.resx"));
        Assert.Contains("<data name=\"Inventory_Movement_ManualIncrease\"", resources, StringComparison.Ordinal);
        Assert.Contains("<value>Stock added</value>", resources, StringComparison.Ordinal);
        Assert.Contains("<data name=\"Inventory_Movement_ManualDecrease\"", resources, StringComparison.Ordinal);
        Assert.Contains("<value>Stock removed</value>", resources, StringComparison.Ordinal);
        Assert.Contains("<data name=\"Inventory_Remarks\"", resources, StringComparison.Ordinal);
        Assert.Contains("<value>Remarks</value>", resources, StringComparison.Ordinal);
    }

    private static string InventoryPagesDirectory() => Path.Combine(
        FindRepoRoot(),
        "src",
        "Products",
        "PinoyBusinessPOS",
        "ExItS.PinoyBusinessPOS.Maui",
        "Components",
        "Pages",
        "Inventory");

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
