using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Inventory;

namespace ExItS.PinoyBusinessPOS.UnitTests.Catalog;

public sealed class CatalogBranchStockEnrichmentTests
{
    [Fact]
    public void STOCKVIS_04_stamps_branch_available_not_organization_total()
    {
        var branchRead = new BranchInventoryProductRead(
            ProductId: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            BranchOnHand: 0m,
            OrganizationOnHand: 42m,
            BranchReserved: 0m,
            BranchAvailable: 0m,
            ReorderLevel: null,
            ReorderQuantity: null,
            IsLowStock: false,
            IsReorderSuggested: false,
            SuggestedOrderQuantity: null);

        var snapshot = CatalogBranchStockEnrichment.BuildSnapshot(isTracked: true, branchRead, sellableQuantity: null);
        var product = new PosCatalogProductDto(
            branchRead.ProductId,
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "Coke",
            null,
            null,
            null,
            null,
            "Piece",
            "PerItem",
            100m,
            "Active",
            DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            IsTracked: true,
            OnHandQuantity: 42m,
            StockStatus: "InStock");

        var enriched = CatalogBranchStockEnrichment.Apply(product, snapshot);

        Assert.Equal(0m, enriched.OnHandQuantity);
        Assert.Equal(42m, enriched.OrganizationOnHandQuantity);
        Assert.Equal(0m, enriched.BranchOnHandQuantity);
        Assert.Equal(0m, enriched.BranchAvailableQuantity);
        Assert.Equal("OutOfStock", enriched.StockStatus);
    }

    [Fact]
    public void STOCKVIS_08_respects_reservation_backed_branch_available()
    {
        var branchRead = new BranchInventoryProductRead(
            ProductId: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            BranchOnHand: 10m,
            OrganizationOnHand: 10m,
            BranchReserved: 3m,
            BranchAvailable: 7m,
            ReorderLevel: null,
            ReorderQuantity: null,
            IsLowStock: false,
            IsReorderSuggested: false,
            SuggestedOrderQuantity: null);

        var snapshot = CatalogBranchStockEnrichment.BuildSnapshot(isTracked: true, branchRead, sellableQuantity: null);
        Assert.Equal(7m, snapshot.SaleEligibleQuantity);
    }

    [Fact]
    public void STOCKVIS_09_caps_sale_eligible_by_sellable_quantity()
    {
        var branchRead = new BranchInventoryProductRead(
            ProductId: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            BranchOnHand: 10m,
            OrganizationOnHand: 10m,
            BranchReserved: 0m,
            BranchAvailable: 10m,
            ReorderLevel: null,
            ReorderQuantity: null,
            IsLowStock: false,
            IsReorderSuggested: false,
            SuggestedOrderQuantity: null);

        var snapshot = CatalogBranchStockEnrichment.BuildSnapshot(isTracked: true, branchRead, sellableQuantity: 6m);
        Assert.Equal(6m, snapshot.SaleEligibleQuantity);
        Assert.Equal(6m, snapshot.SellableQuantity);
    }
}
