using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.UnitTests.Inventory;

public sealed class OpeningStockUnitCostTests
{
    [Fact]
    public void Opening_stock_movement_stores_unit_cost()
    {
        var org = PosOrganizationId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var productId = CatalogProductId.New();
        var accountId = InventoryAccountId.New();
        var actor = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var utc = DateTimeOffset.UtcNow;

        var movement = StockMovement.OpeningStock(
            org,
            productId,
            accountId,
            24m,
            UnitOfMeasure.Piece,
            actor,
            utc,
            unitCost: 18m);

        Assert.Equal(18m, movement.UnitCost);
        Assert.Equal(24m, movement.QuantityEffect);
    }

    [Fact]
    public void NormalizeOpeningUnitCost_rejects_non_positive_values()
    {
        var ex = Assert.Throws<DomainException>(() => StockMovement.NormalizeOpeningUnitCost(0m));
        Assert.Equal(DomainErrorCodes.InvalidInventoryOpeningUnitCost, ex.ErrorCode);
    }
}
