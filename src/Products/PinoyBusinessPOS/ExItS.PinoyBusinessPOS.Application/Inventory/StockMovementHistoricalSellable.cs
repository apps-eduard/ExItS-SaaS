using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

/// <summary>
/// Historical sellable running balances for movement history.
/// Uses authoritative bucket sellable deltas (not raw <see cref="StockMovement.QuantityEffect"/>).
/// Ordering: <c>RecordedAtUtc</c> ascending, then movement id ascending.
/// </summary>
public static class StockMovementHistoricalSellable
{
    public readonly record struct Balance(decimal Before, decimal Delta, decimal After);

    public readonly record struct LedgerEntry(
        Guid MovementId,
        DateTimeOffset RecordedAtUtc,
        string MovementTypeCode,
        decimal QuantityEffect);

    /// <summary>
    /// Computes sellable before/delta/after for every ledger entry using stable chronological order.
    /// Entries not present in the input are omitted. Running balance starts at zero before the
    /// earliest movement (opening stock establishes the first balance).
    /// </summary>
    public static IReadOnlyDictionary<Guid, Balance> ComputeBalances(
        IEnumerable<LedgerEntry> ledgerEntries)
    {
        ArgumentNullException.ThrowIfNull(ledgerEntries);

        var ordered = ledgerEntries
            .OrderBy(e => e.RecordedAtUtc)
            .ThenBy(e => e.MovementId)
            .ToList();

        var result = new Dictionary<Guid, Balance>(ordered.Count);
        var running = 0m;
        foreach (var entry in ordered)
        {
            var delta = StockMovementPresentation
                .DescribeBucketEffects(entry.MovementTypeCode, entry.QuantityEffect)
                .SellableDelta;
            var before = running;
            var after = running + delta;
            result[entry.MovementId] = new Balance(before, delta, after);
            running = after;
        }

        return result;
    }

    public static IReadOnlyDictionary<Guid, Balance> ComputeBalances(
        IEnumerable<StockMovement> movements)
    {
        ArgumentNullException.ThrowIfNull(movements);
        return ComputeBalances(
            movements.Select(m => new LedgerEntry(
                m.Id.Value,
                m.RecordedAtUtc,
                StockMovementTypes.ToCode(m.MovementType),
                m.QuantityEffect)));
    }
}
