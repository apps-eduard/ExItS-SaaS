using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>Optional shortage reason. Member names are stable persistence codes.</summary>
public enum InventoryTransferDiscrepancyReason
{
    ShortShipment = 0,
    Damaged = 1,
    LostInTransit = 2,
    WrongItem = 3,
    Other = 4
}

public static class InventoryTransferDiscrepancyReasons
{
    public const int CodeMaxLength = 32;

    public static IReadOnlyList<string> Codes { get; } =
    [
        nameof(InventoryTransferDiscrepancyReason.ShortShipment),
        nameof(InventoryTransferDiscrepancyReason.Damaged),
        nameof(InventoryTransferDiscrepancyReason.LostInTransit),
        nameof(InventoryTransferDiscrepancyReason.WrongItem),
        nameof(InventoryTransferDiscrepancyReason.Other)
    ];

    public static string ToCode(InventoryTransferDiscrepancyReason reason) => reason.ToString();

    public static bool TryParse(string? code, out InventoryTransferDiscrepancyReason reason)
    {
        reason = InventoryTransferDiscrepancyReason.Other;
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var trimmed = code.Trim();
        var match = Codes.FirstOrDefault(c => string.Equals(c, trimmed, StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            return false;
        }

        reason = Enum.Parse<InventoryTransferDiscrepancyReason>(match, ignoreCase: false);
        return true;
    }

    public static InventoryTransferDiscrepancyReason Parse(string? code)
    {
        if (!TryParse(code, out var reason))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferDiscrepancyReason,
                $"Discrepancy reason must be one of: {string.Join(", ", Codes)}.");
        }

        return reason;
    }
}
