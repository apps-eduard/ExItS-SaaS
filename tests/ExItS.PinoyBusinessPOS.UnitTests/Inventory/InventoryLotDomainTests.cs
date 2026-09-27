using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.UnitTests.Inventory;

public sealed class InventoryLotDomainTests
{
    private static readonly PosOrganizationId Org = PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly CatalogProductId Product = CatalogProductId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly DateTimeOffset Utc = new(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Utc.UtcDateTime);

    [Fact]
    public void Product_defaults_tracks_expiration_false()
    {
        var product = CatalogProduct.Create(Org, "USB Cable", UnitOfMeasure.Piece, 100m, Utc);
        Assert.False(product.TracksExpiration);
        Assert.Null(product.ExpirationWarningDays);
    }

    [Fact]
    public void Enabling_tracking_defaults_warning_days_to_seven()
    {
        var product = CatalogProduct.Create(Org, "Milk 1L", UnitOfMeasure.Piece, 80m, Utc);
        product.SetExpirationTracking(true, null, Utc);
        Assert.True(product.TracksExpiration);
        Assert.Equal(7, product.ExpirationWarningDays);
    }

    [Fact]
    public void Lot_number_is_optional()
    {
        var lot = InventoryLot.Create(Org, Product, Today.AddDays(10), 20m, Utc);
        Assert.Null(lot.LotNumber);
        Assert.Equal(string.Empty, lot.NormalizedLotNumber);
    }

    [Fact]
    public void Different_expiry_dates_remain_separate_lots()
    {
        var a = InventoryLot.Create(Org, Product, new DateOnly(2026, 8, 20), 20m, Utc, lotNumber: "LOT-A");
        var b = InventoryLot.Create(Org, Product, new DateOnly(2026, 9, 5), 30m, Utc, lotNumber: "LOT-B");
        Assert.NotEqual(a.ExpirationDate, b.ExpirationDate);
        Assert.Equal(50m, InventoryLotFefo.TotalOnHand([a, b]));
    }

    [Fact]
    public void Sellable_excludes_expired_stock()
    {
        var expired = InventoryLot.Create(Org, Product, Today.AddDays(-1), 10m, Utc);
        var valid = InventoryLot.Create(Org, Product, Today.AddDays(10), 5m, Utc);
        Assert.Equal(5m, InventoryLotFefo.SellableQuantity([expired, valid], Today));
        Assert.Equal(10m, InventoryLotFefo.ExpiredQuantity([expired, valid], Today));
        Assert.Equal(15m, InventoryLotFefo.TotalOnHand([expired, valid]));
        Assert.True(expired.QuantityOnHand > 0m);
    }

    [Fact]
    public void Fefo_consumes_earliest_expiring_valid_lot_first()
    {
        var later = InventoryLot.Create(Org, Product, new DateOnly(2026, 9, 5), 30m, Utc);
        var earlier = InventoryLot.Create(Org, Product, new DateOnly(2026, 8, 20), 20m, Utc);
        var allocations = InventoryLotFefo.AllocateSellable([later, earlier], 5m, Today);
        Assert.Single(allocations);
        Assert.Equal(earlier.Id, allocations[0].Lot.Id);
        Assert.Equal(5m, allocations[0].Quantity);
    }

    [Fact]
    public void Fefo_continues_to_next_lot_when_first_exhausted()
    {
        var earlier = InventoryLot.Create(Org, Product, Today.AddDays(2), 4m, Utc);
        var later = InventoryLot.Create(Org, Product, Today.AddDays(20), 30m, Utc);
        var allocations = InventoryLotFefo.AllocateSellable([later, earlier], 6m, Today);
        Assert.Equal(2, allocations.Count);
        Assert.Equal(earlier.Id, allocations[0].Lot.Id);
        Assert.Equal(4m, allocations[0].Quantity);
        Assert.Equal(later.Id, allocations[1].Lot.Id);
        Assert.Equal(2m, allocations[1].Quantity);
    }

    [Fact]
    public void Expired_lot_cannot_be_sold()
    {
        var expired = InventoryLot.Create(Org, Product, Today.AddDays(-1), 10m, Utc);
        var ex = Assert.Throws<DomainException>(() => InventoryLotFefo.AllocateSellable([expired], 1m, Today));
        Assert.Equal(DomainErrorCodes.InventoryInsufficientStock, ex.ErrorCode);
    }

    [Fact]
    public void Near_expiry_lot_can_still_be_sold()
    {
        var near = InventoryLot.Create(Org, Product, Today.AddDays(3), 8m, Utc);
        Assert.True(near.IsNearExpiry(Today, 7));
        Assert.True(near.IsSellable(Today));
        var allocations = InventoryLotFefo.AllocateSellable([near], 2m, Today);
        Assert.Equal(2m, allocations[0].Quantity);
    }

    [Fact]
    public void Insufficient_non_expired_stock_rejects_allocation()
    {
        var valid = InventoryLot.Create(Org, Product, Today.AddDays(10), 5m, Utc);
        var expired = InventoryLot.Create(Org, Product, Today.AddDays(-2), 10m, Utc);
        var ex = Assert.Throws<DomainException>(() => InventoryLotFefo.AllocateSellable([valid, expired], 10m, Today));
        Assert.Equal(DomainErrorCodes.InventoryInsufficientStock, ex.ErrorCode);
        Assert.Equal(5m, valid.QuantityOnHand);
        Assert.Equal(10m, expired.QuantityOnHand);
    }

    [Fact]
    public void Expired_adjustment_reduces_correct_lot()
    {
        var expired = InventoryLot.Create(Org, Product, Today.AddDays(-1), 5m, Utc);
        var valid = InventoryLot.Create(Org, Product, Today.AddDays(20), 30m, Utc);
        expired.Apply(-5m, Utc);
        Assert.Equal(0m, expired.QuantityOnHand);
        Assert.Equal(30m, valid.QuantityOnHand);
    }

    [Fact]
    public void Lot_apply_rejects_negative_quantity()
    {
        var lot = InventoryLot.Create(Org, Product, Today.AddDays(10), 2m, Utc);
        var ex = Assert.Throws<DomainException>(() => lot.Apply(-3m, Utc));
        Assert.Equal(DomainErrorCodes.InventoryInsufficientStock, ex.ErrorCode);
        Assert.Equal(2m, lot.QuantityOnHand);
    }

    [Fact]
    public void CorrectIdentity_changes_expiry_and_lot_number()
    {
        var lot = InventoryLot.Create(Org, Product, Today.AddDays(10), 50m, Utc, lotNumber: null);
        var later = Utc.AddMinutes(5);
        Assert.True(lot.CorrectIdentity(Today.AddDays(40), "LOT-A", later));
        Assert.Equal(Today.AddDays(40), lot.ExpirationDate);
        Assert.Equal("LOT-A", lot.LotNumber);
        Assert.Equal("LOT-A", lot.NormalizedLotNumber);
        Assert.Equal(50m, lot.QuantityOnHand);
        Assert.Equal(later, lot.UpdatedAtUtc);
        Assert.Equal(Org, lot.OrganizationId);
        Assert.Equal(Product, lot.ProductId);
    }

    [Fact]
    public void CorrectIdentity_allows_blank_to_value_and_value_to_blank()
    {
        var lot = InventoryLot.Create(Org, Product, Today.AddDays(10), 10m, Utc);
        Assert.True(lot.CorrectIdentity(Today.AddDays(10), "lot-b", Utc.AddMinutes(1)));
        Assert.Equal("lot-b", lot.LotNumber);
        Assert.Equal("LOT-B", lot.NormalizedLotNumber);

        Assert.True(lot.CorrectIdentity(Today.AddDays(10), null, Utc.AddMinutes(2)));
        Assert.Null(lot.LotNumber);
        Assert.Equal(string.Empty, lot.NormalizedLotNumber);
        Assert.Equal(10m, lot.QuantityOnHand);
    }

    [Fact]
    public void CorrectIdentity_preserves_normalization_and_rejects_too_long_lot()
    {
        var lot = InventoryLot.Create(Org, Product, Today.AddDays(10), 5m, Utc, lotNumber: "A");
        Assert.True(lot.CorrectIdentity(Today.AddDays(11), "  abc  ", Utc.AddMinutes(1)));
        Assert.Equal("abc", lot.LotNumber);
        Assert.Equal("ABC", lot.NormalizedLotNumber);

        var tooLong = new string('x', InventoryLot.LotNumberMaxLength + 1);
        var ex = Assert.Throws<DomainException>(() => lot.CorrectIdentity(Today.AddDays(11), tooLong, Utc.AddMinutes(2)));
        Assert.Equal(DomainErrorCodes.InvalidInventoryLotNumber, ex.ErrorCode);
        Assert.Equal(5m, lot.QuantityOnHand);
    }

    [Fact]
    public void CorrectIdentity_noop_when_unchanged()
    {
        var lot = InventoryLot.Create(Org, Product, Today.AddDays(10), 8m, Utc, lotNumber: "LOT-1");
        var before = lot.UpdatedAtUtc;
        Assert.False(lot.CorrectIdentity(Today.AddDays(10), "LOT-1", Utc.AddHours(1)));
        Assert.Equal(before, lot.UpdatedAtUtc);
        Assert.Equal(8m, lot.QuantityOnHand);
    }

    [Fact]
    public void Warning_days_out_of_range_are_rejected()
    {
        var ex = Assert.Throws<DomainException>(() => InventoryLot.NormalizeWarningDays(0));
        Assert.Equal(DomainErrorCodes.InvalidExpirationWarningDays, ex.ErrorCode);
    }
}
