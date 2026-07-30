using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.UnitTests.Inventory;

public sealed class InventoryAccountDomainTests
{
    private static readonly PosOrganizationId Org = PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly CatalogProductId Product = CatalogProductId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly Guid Actor = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly DateTimeOffset Utc = new(2026, 7, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Enable_with_opening_sets_tracked_and_on_hand()
    {
        var account = InventoryAccount.CreateUntracked(Org, Product, Utc);
        var opening = account.Enable(10m, UnitOfMeasure.Piece, Actor, Utc, hasOpeningStockAlready: false);

        Assert.True(account.IsTracked);
        Assert.Equal(10m, account.OnHandQuantity);
        Assert.NotNull(opening);
        Assert.Equal(StockMovementType.OpeningStock, opening!.MovementType);
        Assert.Equal(10m, opening.QuantityEffect);
    }

    [Fact]
    public void Enable_when_already_tracked_is_noop()
    {
        var account = InventoryAccount.CreateUntracked(Org, Product, Utc);
        account.Enable(5m, UnitOfMeasure.Piece, Actor, Utc, hasOpeningStockAlready: false);
        var second = account.Enable(99m, UnitOfMeasure.Piece, Actor, Utc.AddMinutes(1), hasOpeningStockAlready: true);

        Assert.Null(second);
        Assert.Equal(5m, account.OnHandQuantity);
    }

    [Fact]
    public void Enable_zero_opening_tracks_without_movement()
    {
        var account = InventoryAccount.CreateUntracked(Org, Product, Utc);
        var opening = account.Enable(0m, UnitOfMeasure.Piece, Actor, Utc, hasOpeningStockAlready: false);

        Assert.True(account.IsTracked);
        Assert.Equal(0m, account.OnHandQuantity);
        Assert.Null(opening);
    }

    [Fact]
    public void Disable_requires_zero_on_hand()
    {
        var account = InventoryAccount.CreateUntracked(Org, Product, Utc);
        account.Enable(2m, UnitOfMeasure.Piece, Actor, Utc, hasOpeningStockAlready: false);

        var ex = Assert.Throws<DomainException>(() => account.Disable(Utc.AddMinutes(1)));
        Assert.Equal(DomainErrorCodes.InventoryDisableRequiresZero, ex.ErrorCode);

        account.ApplyMovementEffect(-2m);
        account.Disable(Utc.AddMinutes(2));
        Assert.False(account.IsTracked);
    }

    [Fact]
    public void ApplyMovementEffect_rejects_negative_result()
    {
        var account = InventoryAccount.CreateUntracked(Org, Product, Utc);
        account.Enable(1m, UnitOfMeasure.Piece, Actor, Utc, hasOpeningStockAlready: false);

        var ex = Assert.Throws<DomainException>(() => account.ApplyMovementEffect(-2m));
        Assert.Equal(DomainErrorCodes.InventoryInsufficientStock, ex.ErrorCode);
        Assert.Equal(1m, account.OnHandQuantity);
    }

    [Fact]
    public void SetReorderLevel_validates_uom_precision()
    {
        var account = InventoryAccount.CreateUntracked(Org, Product, Utc);
        account.Enable(null, UnitOfMeasure.Piece, Actor, Utc, hasOpeningStockAlready: false);

        var ex = Assert.Throws<DomainException>(() =>
            account.SetReorderLevel(1.5m, UnitOfMeasure.Piece, Utc.AddMinutes(1)));
        Assert.Equal(DomainErrorCodes.InventoryReorderLevelInvalid, ex.ErrorCode);

        account.SetReorderLevel(2m, UnitOfMeasure.Piece, Utc.AddMinutes(2));
        Assert.Equal(2m, account.ReorderLevel);
        Assert.True(account.IsLowStock);
    }

    [Fact]
    public void Manual_decrease_requires_reason_and_signed_effect()
    {
        var accountId = InventoryAccountId.New();
        var ex = Assert.Throws<DomainException>(() =>
            StockMovement.ManualDecrease(Org, Product, accountId, 1m, UnitOfMeasure.Piece, " ", Actor, Utc));
        Assert.Equal(DomainErrorCodes.InventoryAdjustmentReasonRequired, ex.ErrorCode);

        var movement = StockMovement.ManualDecrease(
            Org, Product, accountId, 1.25m, UnitOfMeasure.Kilogram, "Spoilage", Actor, Utc);
        Assert.Equal(-1.25m, movement.QuantityEffect);
        Assert.Equal(StockMovementType.ManualDecrease, movement.MovementType);
    }

    [Fact]
    public void Sale_deduction_and_void_restoration_are_signed_opposites()
    {
        var accountId = InventoryAccountId.New();
        var saleId = Guid.NewGuid();
        var deduction = StockMovement.SaleDeduction(
            Org, Product, accountId, 3m, UnitOfMeasure.Piece, saleId, Actor, Utc);
        var restore = StockMovement.SaleVoidRestoration(
            Org, Product, accountId, 3m, UnitOfMeasure.Piece, saleId, Actor, Utc.AddMinutes(1), "Mistake");

        Assert.Equal(-3m, deduction.QuantityEffect);
        Assert.Equal(3m, restore.QuantityEffect);
        Assert.Equal(saleId, deduction.SourceId);
        Assert.Equal(saleId, restore.SourceId);
    }

    [Fact]
    public void Measured_uom_rejects_excess_decimals_on_opening()
    {
        var accountId = InventoryAccountId.New();
        var ex = Assert.Throws<DomainException>(() =>
            StockMovement.OpeningStock(Org, Product, accountId, 1.1234m, UnitOfMeasure.Kilogram, Actor, Utc));
        Assert.Equal(DomainErrorCodes.InvalidSaleLineQuantity, ex.ErrorCode);
    }
}
