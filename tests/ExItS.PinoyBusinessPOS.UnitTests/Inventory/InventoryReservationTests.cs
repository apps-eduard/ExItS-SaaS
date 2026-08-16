using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.UnitTests.Inventory;

public sealed class InventoryReservationTests
{
    private static readonly PosOrganizationId Org =
        PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly CatalogProductId Product =
        CatalogProductId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly Guid Actor = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly DateTimeOffset Utc = new(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Reserve_reduces_available_without_changing_on_hand()
    {
        var account = TrackedWithOnHand(10m);

        account.Reserve(4m);

        Assert.Equal(10m, account.OnHandQuantity);
        Assert.Equal(4m, account.ReservedQuantity);
        Assert.Equal(6m, account.AvailableQuantity);
    }

    [Fact]
    public void Release_restores_available()
    {
        var account = TrackedWithOnHand(10m);
        account.Reserve(4m);

        account.Release(3m);

        Assert.Equal(1m, account.ReservedQuantity);
        Assert.Equal(9m, account.AvailableQuantity);
        Assert.Equal(10m, account.OnHandQuantity);
    }

    [Fact]
    public void Consume_reservation_decreases_reserved_and_on_hand()
    {
        var account = TrackedWithOnHand(10m);
        account.Reserve(4m);

        account.ConsumeReservation(4m);

        Assert.Equal(0m, account.ReservedQuantity);
        Assert.Equal(6m, account.OnHandQuantity);
        Assert.Equal(6m, account.AvailableQuantity);
    }

    [Fact]
    public void Reserve_rejects_insufficient_available()
    {
        var account = TrackedWithOnHand(5m);
        account.Reserve(4m);

        var ex = Assert.Throws<DomainException>(() => account.Reserve(2m));
        Assert.Equal(DomainErrorCodes.InventoryInsufficientStock, ex.ErrorCode);
        Assert.Equal(4m, account.ReservedQuantity);
    }

    [Fact]
    public void Untracked_reservation_is_noop_success()
    {
        var account = InventoryAccount.CreateUntracked(Org, Product, Utc);

        account.Reserve(3m);
        account.Release(3m);
        account.ConsumeReservation(3m);

        Assert.Equal(0m, account.OnHandQuantity);
        Assert.Equal(0m, account.ReservedQuantity);
        Assert.False(account.IsTracked);
    }

    [Fact]
    public void Rehydrate_includes_reserved_quantity()
    {
        var id = InventoryAccountId.New();
        var account = InventoryAccount.Rehydrate(
            id,
            Org,
            Product,
            isTracked: true,
            reorderLevel: null,
            reorderQuantity: null,
            onHandQuantity: 8m,
            Utc,
            Utc,
            reservedQuantity: 3m);

        Assert.Equal(8m, account.OnHandQuantity);
        Assert.Equal(3m, account.ReservedQuantity);
        Assert.Equal(5m, account.AvailableQuantity);
    }

    private static InventoryAccount TrackedWithOnHand(decimal onHand)
    {
        var account = InventoryAccount.CreateUntracked(Org, Product, Utc);
        account.Enable(onHand, UnitOfMeasure.Piece, Actor, Utc, hasOpeningStockAlready: false);
        return account;
    }
}
