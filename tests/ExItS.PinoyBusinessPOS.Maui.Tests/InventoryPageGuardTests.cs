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
        Assert.Contains("pos-action-grid--three", list, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"warning\")", list, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"check\")", list, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"refresh\")", list, StringComparison.Ordinal);
        Assert.Contains("pos-inventory__row", list, StringComparison.Ordinal);

        var detail = File.ReadAllText(Path.Combine(pages, "InventoryDetail.razor"));
        Assert.Contains("@page \"/inventory/{ProductId:guid}\"", detail, StringComparison.Ordinal);
        Assert.Contains("ListMovementsAsync", detail, StringComparison.Ordinal);

        var adjust = File.ReadAllText(Path.Combine(pages, "InventoryAdjust.razor"));
        Assert.Contains("@page \"/inventory/{ProductId:guid}/adjust\"", adjust, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ManageInventory", adjust, StringComparison.Ordinal);

        var low = File.ReadAllText(Path.Combine(pages, "InventoryLowStock.razor"));
        Assert.Contains("@page \"/inventory/low-stock\"", low, StringComparison.Ordinal);
        Assert.Contains("pos-action-grid--two", low, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"products\")", low, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"refresh\")", low, StringComparison.Ordinal);
        Assert.Contains("pos-inventory__row--low", low, StringComparison.Ordinal);
        Assert.Contains("Inventory_LowStockEmptyTitle", low, StringComparison.Ordinal);
        Assert.DoesNotContain("ResponsiveDataList", low, StringComparison.Ordinal);

        var create = File.ReadAllText(Path.Combine(pages, "StockCountCreate.razor"));
        Assert.Contains("@page \"/inventory/counts/new\"", create, StringComparison.Ordinal);
        Assert.Contains("CreateStockCountAsync", create, StringComparison.Ordinal);
        Assert.Contains("Inventory_CountsReset", create, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"products\")", create, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"check\")", create, StringComparison.Ordinal);
        Assert.Contains("pos-stock-count-create__line", create, StringComparison.Ordinal);
        Assert.Contains("ConfirmDialog", create, StringComparison.Ordinal);

        var counts = File.ReadAllText(Path.Combine(pages, "StockCountsList.razor"));
        Assert.Contains("@page \"/inventory/counts\"", counts, StringComparison.Ordinal);
        Assert.Contains("pos-stock-counts__row", counts, StringComparison.Ordinal);
        Assert.Contains("pos-action-grid--three", counts, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"plus\")", counts, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"products\")", counts, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"refresh\")", counts, StringComparison.Ordinal);
        Assert.DoesNotContain("ResponsiveDataList", counts, StringComparison.Ordinal);
    }

    [Fact]
    public void Inventory_pages_guard_entry_and_gate_view_capability()
    {
        foreach (var name in new[] { "InventoryList.razor", "InventoryDetail.razor", "InventoryLowStock.razor", "StockCountsList.razor" })
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
