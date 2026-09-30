using ExItS.PinoyBusinessPOS.Application.Inventory;

namespace ExItS.PinoyBusinessPOS.UnitTests.Inventory;

public sealed class StockMovementHistoricalSellableTests
{
    private static readonly DateTimeOffset T0 = new(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid Id1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Id2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Id3 = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid Id4 = Guid.Parse("44444444-4444-4444-4444-444444444444");

    [Fact]
    public void Opening_stock_sets_sellable_after_to_opening_qty()
    {
        var balances = StockMovementHistoricalSellable.ComputeBalances(
        [
            Entry(Id1, T0, "OpeningStock", 100m),
        ]);

        Assert.Equal(0m, balances[Id1].Before);
        Assert.Equal(100m, balances[Id1].Delta);
        Assert.Equal(100m, balances[Id1].After);
    }

    [Fact]
    public void Transfer_out_reduces_running_sellable()
    {
        var balances = StockMovementHistoricalSellable.ComputeBalances(
        [
            Entry(Id1, T0, "OpeningStock", 100m),
            Entry(Id2, T0.AddMinutes(1), "TransferOut", -10m),
        ]);

        Assert.Equal(100m, balances[Id1].After);
        Assert.Equal(100m, balances[Id2].Before);
        Assert.Equal(-10m, balances[Id2].Delta);
        Assert.Equal(90m, balances[Id2].After);
    }

    [Fact]
    public void Exception_expected_restore_increases_sellable()
    {
        var balances = StockMovementHistoricalSellable.ComputeBalances(
        [
            Entry(Id1, T0, "OpeningStock", 100m),
            Entry(Id2, T0.AddMinutes(1), "TransferOut", -10m),
            Entry(Id3, T0.AddMinutes(2), "TransferExceptionExpectedRestore", 5m),
        ]);

        Assert.Equal(90m, balances[Id3].Before);
        Assert.Equal(5m, balances[Id3].Delta);
        Assert.Equal(95m, balances[Id3].After);
    }

    [Fact]
    public void Exception_actual_out_reduces_sellable_on_wrong_item_sku()
    {
        var balances = StockMovementHistoricalSellable.ComputeBalances(
        [
            Entry(Id1, T0, "OpeningStock", 50m),
            Entry(Id2, T0.AddMinutes(1), "TransferExceptionActualOut", -5m),
        ]);

        Assert.Equal(50m, balances[Id2].Before);
        Assert.Equal(-5m, balances[Id2].Delta);
        Assert.Equal(45m, balances[Id2].After);
    }

    [Fact]
    public void Physical_only_movement_leaves_sellable_after_unchanged()
    {
        var balances = StockMovementHistoricalSellable.ComputeBalances(
        [
            Entry(Id1, T0, "OpeningStock", 45m),
            Entry(Id2, T0.AddMinutes(1), "TransferDamageHold", 5m),
        ]);

        Assert.Equal(45m, balances[Id2].Before);
        Assert.Equal(0m, balances[Id2].Delta);
        Assert.Equal(45m, balances[Id2].After);
        Assert.Equal(5m, StockMovementPresentation.DescribeBucketEffects("TransferDamageHold", 5m).PhysicalDelta);
    }

    [Fact]
    public void Same_timestamp_uses_movement_id_as_deterministic_tie_breaker()
    {
        var sameTime = T0;
        var idA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var idB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        // Input deliberately reverse of stable order; result must still be deterministic.
        var balances = StockMovementHistoricalSellable.ComputeBalances(
        [
            Entry(idB, sameTime, "TransferOut", -10m),
            Entry(idA, sameTime, "OpeningStock", 100m),
        ]);

        Assert.Equal(0m, balances[idA].Before);
        Assert.Equal(100m, balances[idA].After);
        Assert.Equal(100m, balances[idB].Before);
        Assert.Equal(-10m, balances[idB].Delta);
        Assert.Equal(90m, balances[idB].After);
    }

    [Fact]
    public void Page_window_subset_still_reflects_full_ledger_prefix()
    {
        // Full history: opening 100 → out 10 → restore 5 → actual out 5
        var full = StockMovementHistoricalSellable.ComputeBalances(
        [
            Entry(Id1, T0, "OpeningStock", 100m),
            Entry(Id2, T0.AddMinutes(1), "TransferOut", -10m),
            Entry(Id3, T0.AddMinutes(2), "TransferExceptionExpectedRestore", 5m),
            Entry(Id4, T0.AddMinutes(3), "TransferExceptionActualOut", -5m),
        ]);

        // "Page 2" would show only Id3/Id4 — balances must still come from full ledger.
        Assert.Equal(95m, full[Id3].After);
        Assert.Equal(95m, full[Id4].Before);
        Assert.Equal(-5m, full[Id4].Delta);
        Assert.Equal(90m, full[Id4].After);

        // Restarting from zero on the page alone would incorrectly yield After=0 for Id4.
        var pageOnly = StockMovementHistoricalSellable.ComputeBalances(
        [
            Entry(Id3, T0.AddMinutes(2), "TransferExceptionExpectedRestore", 5m),
            Entry(Id4, T0.AddMinutes(3), "TransferExceptionActualOut", -5m),
        ]);
        Assert.Equal(0m, pageOnly[Id4].After);
        Assert.NotEqual(full[Id4].After, pageOnly[Id4].After);
    }

    [Fact]
    public void Wrong_item_return_restock_restores_sellable()
    {
        var balances = StockMovementHistoricalSellable.ComputeBalances(
        [
            Entry(Id1, T0, "OpeningStock", 50m),
            Entry(Id2, T0.AddMinutes(1), "TransferExceptionActualOut", -5m),
            Entry(Id3, T0.AddMinutes(2), "TransferExceptionReturnRestock", 5m),
        ]);

        Assert.Equal(45m, balances[Id2].After);
        Assert.Equal(45m, balances[Id3].Before);
        Assert.Equal(5m, balances[Id3].Delta);
        Assert.Equal(50m, balances[Id3].After);
    }

    private static StockMovementHistoricalSellable.LedgerEntry Entry(
        Guid id,
        DateTimeOffset at,
        string type,
        decimal qty) =>
        new(id, at, type, qty);
}
