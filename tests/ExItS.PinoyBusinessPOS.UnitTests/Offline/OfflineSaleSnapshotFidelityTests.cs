using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.Offline;

/// <summary>WP08: offline sync must preserve immutable price/qty/mode snapshots (no live re-price).</summary>
public sealed class OfflineSaleSnapshotFidelityTests
{
    private static readonly PosOrganizationId Org = PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly DateTimeOffset Now = new(2026, 8, 11, 20, 0, 0, TimeSpan.Zero);

    private static CatalogProduct Tomato(decimal livePrice = 150m, string name = "Tomato") =>
        CatalogProduct.Create(
            Org,
            name,
            UnitOfMeasure.Kilogram,
            livePrice,
            Now,
            sku: "TOM-1",
            barcode: "4800001000001",
            sellingMode: SellingMode.ByWeight);

    private static CatalogProduct Coke(decimal livePrice = 30m) =>
        CatalogProduct.Create(
            Org,
            "Coke",
            UnitOfMeasure.Bottle,
            livePrice,
            Now,
            sku: "COKE-1",
            sellingMode: SellingMode.PerItem);

    [Fact]
    public void ByWeight_snapshot_preserves_historical_price_when_live_catalog_changed()
    {
        var live = Tomato(livePrice: 150m);
        var line = new CheckoutSaleLineRequest(
            live.Id.Value,
            1.200m,
            UnitPriceSnapshot: 120m,
            UnitOfMeasure: nameof(UnitOfMeasure.Kilogram),
            SellingMode: nameof(SellingMode.ByWeight),
            LineTotal: 144.00m,
            NameSnapshot: "Tomato",
            SkuSnapshot: "TOM-1");

        var draft = CheckoutSaleLineSnapshots.TryCreateDraftFromSnapshot(line, live);
        Assert.True(draft.IsSuccess);
        Assert.Equal(120m, draft.Value!.UnitPrice);
        Assert.Equal(1.200m, draft.Value.Quantity);
        Assert.Equal(SellingMode.ByWeight, draft.Value.SellingModeSnapshot);
        Assert.Equal(UnitOfMeasure.Kilogram, draft.Value.UnitOfMeasureSnapshot);
        Assert.Equal(144.00m, SaleMoney.RoundMoney(draft.Value.UnitPrice * draft.Value.Quantity));
        Assert.NotEqual(live.SellingPrice, draft.Value.UnitPrice);
    }

    [Fact]
    public void Partial_weight_0_350_kg_at_120_equals_42()
    {
        var live = Tomato(150m);
        var line = new CheckoutSaleLineRequest(
            live.Id.Value,
            0.350m,
            UnitPriceSnapshot: 120m,
            UnitOfMeasure: nameof(UnitOfMeasure.Kilogram),
            SellingMode: nameof(SellingMode.ByWeight),
            LineTotal: 42.00m);

        var draft = CheckoutSaleLineSnapshots.TryCreateDraftFromSnapshot(line, live);
        Assert.True(draft.IsSuccess);
        Assert.Equal(0.350m, draft.Value!.Quantity);
        Assert.Equal(42.00m, SaleMoney.RoundMoney(draft.Value.UnitPrice * draft.Value.Quantity));
    }

    [Fact]
    public void Bangus_0_750_kg_at_220_equals_165()
    {
        var live = CatalogProduct.Create(
            Org,
            "Bangus",
            UnitOfMeasure.Kilogram,
            250m,
            Now,
            sellingMode: SellingMode.ByWeight);
        var line = new CheckoutSaleLineRequest(
            live.Id.Value,
            0.750m,
            UnitPriceSnapshot: 220m,
            UnitOfMeasure: nameof(UnitOfMeasure.Kilogram),
            SellingMode: nameof(SellingMode.ByWeight),
            LineTotal: 165.00m,
            NameSnapshot: "Bangus");

        var draft = CheckoutSaleLineSnapshots.TryCreateDraftFromSnapshot(line, live);
        Assert.True(draft.IsSuccess);
        Assert.Equal(165.00m, SaleMoney.RoundMoney(draft.Value!.UnitPrice * draft.Value.Quantity));
    }

    [Fact]
    public void Mixed_cart_snapshots_preserve_distinct_line_totals()
    {
        var coke = Coke(30m);
        var tomato = Tomato(150m);
        var cokeLine = new CheckoutSaleLineRequest(
            coke.Id.Value,
            2m,
            UnitPriceSnapshot: 25m,
            UnitOfMeasure: nameof(UnitOfMeasure.Bottle),
            SellingMode: nameof(SellingMode.PerItem),
            LineTotal: 50.00m,
            NameSnapshot: "Coke");
        var tomatoLine = new CheckoutSaleLineRequest(
            tomato.Id.Value,
            1.200m,
            UnitPriceSnapshot: 120m,
            UnitOfMeasure: nameof(UnitOfMeasure.Kilogram),
            SellingMode: nameof(SellingMode.ByWeight),
            LineTotal: 144.00m,
            NameSnapshot: "Tomato");

        var cokeDraft = CheckoutSaleLineSnapshots.TryCreateDraftFromSnapshot(cokeLine, coke);
        var tomatoDraft = CheckoutSaleLineSnapshots.TryCreateDraftFromSnapshot(tomatoLine, tomato);
        Assert.True(cokeDraft.IsSuccess);
        Assert.True(tomatoDraft.IsSuccess);
        var total = SaleMoney.RoundMoney(
            SaleMoney.RoundMoney(cokeDraft.Value!.UnitPrice * cokeDraft.Value.Quantity)
            + SaleMoney.RoundMoney(tomatoDraft.Value!.UnitPrice * tomatoDraft.Value.Quantity));
        Assert.Equal(194.00m, total);
    }

    [Fact]
    public void Rename_after_offline_sale_does_not_alter_name_snapshot()
    {
        var live = Tomato(150m, name: "Tomato Deluxe");
        var line = new CheckoutSaleLineRequest(
            live.Id.Value,
            1m,
            UnitPriceSnapshot: 120m,
            UnitOfMeasure: nameof(UnitOfMeasure.Kilogram),
            SellingMode: nameof(SellingMode.ByWeight),
            LineTotal: 120.00m,
            NameSnapshot: "Tomato");

        var draft = CheckoutSaleLineSnapshots.TryCreateDraftFromSnapshot(line, live);
        Assert.True(draft.IsSuccess);
        Assert.Equal("Tomato", draft.Value!.NameSnapshot);
        Assert.NotEqual(live.Name, draft.Value.NameSnapshot);
    }

    [Fact]
    public void SellingMode_change_on_live_product_does_not_alter_snapshot_mode()
    {
        // Live product somehow PerItem now (would be invalid with kg in real edits, but snapshot wins).
        var live = CatalogProduct.Create(Org, "Tomato", UnitOfMeasure.Kilogram, 150m, Now);
        Assert.Equal(SellingMode.PerItem, live.SellingMode);

        var line = new CheckoutSaleLineRequest(
            live.Id.Value,
            0.350m,
            UnitPriceSnapshot: 120m,
            UnitOfMeasure: nameof(UnitOfMeasure.Kilogram),
            SellingMode: nameof(SellingMode.ByWeight),
            LineTotal: 42.00m);

        var draft = CheckoutSaleLineSnapshots.TryCreateDraftFromSnapshot(line, live);
        Assert.True(draft.IsSuccess);
        Assert.Equal(SellingMode.ByWeight, draft.Value!.SellingModeSnapshot);
    }

    [Fact]
    public void Forged_inconsistent_line_total_is_rejected()
    {
        var live = Tomato();
        var line = new CheckoutSaleLineRequest(
            live.Id.Value,
            0.350m,
            UnitPriceSnapshot: 120m,
            UnitOfMeasure: nameof(UnitOfMeasure.Kilogram),
            SellingMode: nameof(SellingMode.ByWeight),
            LineTotal: 99.00m);

        var draft = CheckoutSaleLineSnapshots.TryCreateDraftFromSnapshot(line, live);
        Assert.False(draft.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.SaleSnapshotLineTotalMismatch, draft.ErrorCode);
    }

    [Fact]
    public void Incomplete_snapshot_is_rejected()
    {
        var live = Tomato();
        var line = new CheckoutSaleLineRequest(
            live.Id.Value,
            1m,
            UnitPriceSnapshot: 120m);

        var draft = CheckoutSaleLineSnapshots.TryCreateDraftFromSnapshot(line, live);
        Assert.False(draft.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.SaleSnapshotIncomplete, draft.ErrorCode);
    }

    [Fact]
    public void ByWeight_with_non_kilogram_uom_is_rejected()
    {
        var live = Tomato();
        var line = new CheckoutSaleLineRequest(
            live.Id.Value,
            1m,
            UnitPriceSnapshot: 120m,
            UnitOfMeasure: nameof(UnitOfMeasure.Bottle),
            SellingMode: nameof(SellingMode.ByWeight),
            LineTotal: 120m);

        var draft = CheckoutSaleLineSnapshots.TryCreateDraftFromSnapshot(line, live);
        Assert.False(draft.IsSuccess);
        Assert.Equal(DomainErrorCodes.InvalidSellingModeUnit, draft.ErrorCode);
    }

    [Fact]
    public void Invalid_quantity_precision_is_rejected()
    {
        var live = Tomato();
        var line = new CheckoutSaleLineRequest(
            live.Id.Value,
            0.1234m,
            UnitPriceSnapshot: 120m,
            UnitOfMeasure: nameof(UnitOfMeasure.Kilogram),
            SellingMode: nameof(SellingMode.ByWeight),
            LineTotal: 14.81m);

        var draft = CheckoutSaleLineSnapshots.TryCreateDraftFromSnapshot(line, live);
        Assert.False(draft.IsSuccess);
        Assert.Equal(DomainErrorCodes.InvalidSaleLineQuantity, draft.ErrorCode);
    }

    [Fact]
    public void RequestUsesTrustedSnapshots_detects_partial_fields()
    {
        Assert.False(CheckoutSaleLineSnapshots.RequestUsesTrustedSnapshots(
            [new CheckoutSaleLineRequest(Guid.NewGuid(), 1m)]));
        Assert.True(CheckoutSaleLineSnapshots.RequestUsesTrustedSnapshots(
            [new CheckoutSaleLineRequest(Guid.NewGuid(), 1m, UnitPriceSnapshot: 10m)]));
    }

    [Fact]
    public void Payload_version_current_is_immutable_snapshots()
    {
        Assert.Equal(1, OfflineOperationTypes.SaleCheckoutPayloadVersions.LegacyProductIdQuantityOnly);
        Assert.Equal(2, OfflineOperationTypes.SaleCheckoutPayloadVersions.ImmutableLineSnapshots);
        Assert.Equal(2, OfflineOperationTypes.SaleCheckoutPayloadVersions.Current);
    }
}
