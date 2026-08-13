using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>Member names are stable persistence codes.</summary>
public enum InventoryTransferStatus
{
    Draft = 0,
    InTransit = 1,
    PartiallyReceived = 2,
    Received = 3,
    Cancelled = 4
}

public static class InventoryTransferStatuses
{
    public const int CodeMaxLength = 32;

    public static IReadOnlyList<string> Codes { get; } =
    [
        nameof(InventoryTransferStatus.Draft),
        nameof(InventoryTransferStatus.InTransit),
        nameof(InventoryTransferStatus.PartiallyReceived),
        nameof(InventoryTransferStatus.Received),
        nameof(InventoryTransferStatus.Cancelled)
    ];

    public static string ToCode(InventoryTransferStatus status) => status.ToString();

    public static bool TryParse(string? code, out InventoryTransferStatus status)
    {
        status = InventoryTransferStatus.Draft;
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

        status = Enum.Parse<InventoryTransferStatus>(match, ignoreCase: false);
        return true;
    }

    public static InventoryTransferStatus Parse(string? code)
    {
        if (!TryParse(code, out var status))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferStatus,
                $"Transfer status must be one of: {string.Join(", ", Codes)}.");
        }

        return status;
    }
}
