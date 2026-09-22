using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

public enum InventoryTransferDamageCustodyStatus
{
    HeldAtDestination = 0,
    AwaitingReturn = 1,
    ReturnInTransit = 2,
    ReceivedAtSource = 3,
    AwaitingInspection = 4,
    Inspected = 5
}

public static class InventoryTransferDamageCustodyStatuses
{
    public const int CodeMaxLength = 32;

    public static IReadOnlyList<string> Codes { get; } =
    [
        nameof(InventoryTransferDamageCustodyStatus.HeldAtDestination),
        nameof(InventoryTransferDamageCustodyStatus.AwaitingReturn),
        nameof(InventoryTransferDamageCustodyStatus.ReturnInTransit),
        nameof(InventoryTransferDamageCustodyStatus.ReceivedAtSource),
        nameof(InventoryTransferDamageCustodyStatus.AwaitingInspection),
        nameof(InventoryTransferDamageCustodyStatus.Inspected)
    ];

    public static string ToCode(InventoryTransferDamageCustodyStatus status) => status.ToString();

    public static bool TryParse(string? code, out InventoryTransferDamageCustodyStatus status)
    {
        status = InventoryTransferDamageCustodyStatus.HeldAtDestination;
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

        status = Enum.Parse<InventoryTransferDamageCustodyStatus>(match, ignoreCase: false);
        return true;
    }

    public static InventoryTransferDamageCustodyStatus Parse(string? code)
    {
        if (!TryParse(code, out var status))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferDamageCustodyStatus,
                $"Damage custody status must be one of: {string.Join(", ", Codes)}.");
        }

        return status;
    }
}
