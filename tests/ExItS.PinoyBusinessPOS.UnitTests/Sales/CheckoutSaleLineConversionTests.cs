using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.Sales;

public sealed class CheckoutSaleLineConversionTests
{
    [Fact]
    public void Online_draft_recomputes_base_quantity_from_sell_unit()
    {
        var org = PosOrganizationId.From(Guid.NewGuid());
        var product = CatalogProduct.Create(org, "Soda", UnitOfMeasure.Piece, 10m, DateTimeOffset.UtcNow);
        var pack = CatalogProductUnit.Create(
            org,
            product.Id,
            ProductUnitKind.Sell,
            "6-pack",
            "6pk",
            6m,
            DateTimeOffset.UtcNow,
            sellingPrice: 55m);

        var line = new CheckoutSaleLineRequest(product.Id.Value, Quantity: 2m, SellingUnitId: pack.Id.Value, EnteredQuantity: 2m);
        var draft = CheckoutSaleLineSnapshots.TryCreateOnlineDraft(line, product, pack);

        Assert.True(draft.IsSuccess);
        Assert.Equal(12m, draft.Value!.Quantity);
        Assert.Equal(2m, draft.Value.EnteredQuantity);
        Assert.Equal(6m, draft.Value.MultiplierToBaseSnapshot);
        Assert.Equal(55m, draft.Value.UnitPrice);
        Assert.Equal(pack.Id, draft.Value.SellingUnitId);
    }

    [Fact]
    public void Offline_snapshot_ignores_client_base_quantity_when_unit_present()
    {
        var org = PosOrganizationId.From(Guid.NewGuid());
        var product = CatalogProduct.Create(org, "Soda", UnitOfMeasure.Piece, 10m, DateTimeOffset.UtcNow);
        var pack = CatalogProductUnit.Create(
            org,
            product.Id,
            ProductUnitKind.Sell,
            "6-pack",
            "6pk",
            6m,
            DateTimeOffset.UtcNow,
            sellingPrice: 55m);

        var line = new CheckoutSaleLineRequest(
            product.Id.Value,
            Quantity: 999m, // malicious/stale client base — must be ignored
            UnitPriceSnapshot: 55m,
            UnitOfMeasure: "Piece",
            SellingMode: "PerItem",
            LineTotal: 110m,
            NameSnapshot: "Soda",
            SellingUnitId: pack.Id.Value,
            EnteredQuantity: 2m);

        var draft = CheckoutSaleLineSnapshots.TryCreateDraftFromSnapshot(line, product, pack);
        Assert.True(draft.IsSuccess);
        Assert.Equal(12m, draft.Value!.Quantity);
        Assert.Equal(2m, draft.Value.EnteredQuantity);
    }
}
