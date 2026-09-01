using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.Inventory;

public sealed class AdvancedInventoryDomainTests
{
    private static readonly PosOrganizationId Org = PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly CatalogProductId Product = CatalogProductId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly Guid Actor = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly PosBranchId Branch = PosBranchId.From(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"));
    private static readonly DateTimeOffset Utc = new(2026, 7, 31, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Stock_status_derivation_covers_reorder_suggested()
    {
        var account = InventoryAccount.CreateUntracked(Org, Product, Utc);
        account.Enable(5m, UnitOfMeasure.Piece, Actor, Utc, hasOpeningStockAlready: false);
        account.SetReorderConfiguration(10m, 20m, UnitOfMeasure.Piece, Utc.AddMinutes(1));
        account.ApplyMovementEffect(-3m);

        Assert.Equal(InventoryStockStatus.LowStock, account.StockStatus);
        Assert.True(account.IsLowStock);
        Assert.True(account.IsReorderSuggested);
        Assert.Equal(20m, account.SuggestedOrderQuantity);
    }

    [Fact]
    public void Out_of_stock_with_reorder_level_is_out_of_stock_and_reorder_suggested()
    {
        var account = InventoryAccount.CreateUntracked(Org, Product, Utc);
        account.Enable(0m, UnitOfMeasure.Piece, Actor, Utc, hasOpeningStockAlready: false);
        account.SetReorderConfiguration(5m, null, UnitOfMeasure.Piece, Utc.AddMinutes(1));

        Assert.Equal(InventoryStockStatus.OutOfStock, account.StockStatus);
        Assert.False(account.IsLowStock);
        Assert.True(account.IsReorderSuggested);
        Assert.Equal(5m, account.SuggestedOrderQuantity);
    }

    [Fact]
    public void SetReorderConfiguration_requires_tracked_account()
    {
        var account = InventoryAccount.CreateUntracked(Org, Product, Utc);
        var ex = Assert.Throws<DomainException>(() =>
            account.SetReorderConfiguration(1m, 5m, UnitOfMeasure.Piece, Utc));
        Assert.Equal(DomainErrorCodes.InventoryNotTracked, ex.ErrorCode);
    }

    [Fact]
    public void InventoryReorderChange_rejects_unchanged_values()
    {
        var accountId = InventoryAccountId.New();
        var ex = Assert.Throws<DomainException>(() =>
            InventoryReorderChange.Create(
                Org,
                accountId,
                Product,
                2m,
                2m,
                5m,
                5m,
                "No change",
                Actor,
                Utc));
        Assert.Equal(DomainErrorCodes.InventoryReorderUnchanged, ex.ErrorCode);
    }

    [Fact]
    public void StockCount_start_snapshots_on_hand_and_allocates_number()
    {
        var draft = StockCount.CreateDraft(
            Org,
            [new StockCountLineDraft(Product, null)],
            Utc,
            "Weekly count",
            Actor,
            Branch);
        draft.Start("CNT-20260731-000001", new Dictionary<Guid, decimal> { [Product.Value] = 7m }, Actor, Utc.AddMinutes(1));

        Assert.Equal(StockCountStatus.InProgress, draft.Status);
        Assert.Equal("CNT-20260731-000001", draft.CountNumber);
        Assert.Equal(7m, draft.Lines[0].SystemOnHandSnapshot);
        Assert.Equal("Weekly count", draft.Title);
    }

    [Fact]
    public void StockCount_complete_requires_counted_quantities()
    {
        var draft = StockCount.CreateDraft(
            Org,
            [new StockCountLineDraft(Product, null)],
            Utc,
            "Monthly count",
            Actor,
            Branch);
        draft.Start("CNT-20260731-000002", new Dictionary<Guid, decimal> { [Product.Value] = 4m }, Actor, Utc.AddMinutes(1));

        var ex = Assert.Throws<DomainException>(() => draft.MarkCompleted(Actor, Utc.AddMinutes(2)));
        Assert.Equal(DomainErrorCodes.StockCountCountedQuantityRequired, ex.ErrorCode);
    }

    [Fact]
    public void StockCount_variance_movements_are_signed()
    {
        var accountId = InventoryAccountId.New();
        var countId = Guid.NewGuid();
        var increase = StockMovement.StockCountVarianceIncrease(
            Org, Product, accountId, 2m, UnitOfMeasure.Piece, countId, Actor, Utc);
        var decrease = StockMovement.StockCountVarianceDecrease(
            Org, Product, accountId, 1m, UnitOfMeasure.Piece, countId, Actor, Utc.AddMinutes(1));

        Assert.Equal(StockMovementType.StockCountVarianceIncrease, increase.MovementType);
        Assert.Equal(2m, increase.QuantityEffect);
        Assert.Equal(-1m, decrease.QuantityEffect);
        Assert.Equal(StockMovementSourceType.StockCount, increase.SourceType);
    }

    [Fact]
    public void StockCountNumbers_format_matches_pattern()
    {
        Assert.Equal("CNT-20260731-01", StockCountNumbers.Format(new DateOnly(2026, 7, 31), 1));
    }
}
