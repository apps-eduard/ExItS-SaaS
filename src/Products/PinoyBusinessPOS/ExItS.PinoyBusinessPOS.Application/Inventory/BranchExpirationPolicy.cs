namespace ExItS.PinoyBusinessPOS.Application.Inventory;

/// <summary>
/// Effective expiration policy for one organization + branch + product.
/// Authoritative for all branch inventory operations (receive, transfer, sale FEFO, etc.).
/// </summary>
public readonly record struct BranchExpirationPolicy(
    bool TracksExpiration,
    int? ExpirationWarningDays)
{
    public static BranchExpirationPolicy Off { get; } = new(false, null);

    public int EffectiveWarningDays =>
        TracksExpiration
            ? (ExpirationWarningDays ?? Domain.Inventory.InventoryLot.DefaultWarningDays)
            : Domain.Inventory.InventoryLot.DefaultWarningDays;
}
