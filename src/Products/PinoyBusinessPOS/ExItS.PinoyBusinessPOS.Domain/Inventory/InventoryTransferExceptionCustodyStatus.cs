using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

public enum InventoryTransferExceptionCustodyStatus
{
    HeldAtDestination = 0,
    AwaitingReturn = 1,
    ReturnInTransit = 2,
    ReceivedAtSource = 3,
    AwaitingInspection = 4,
    Inspected = 5
}

public static class InventoryTransferExceptionCustodyStatuses
{
    public const int CodeMaxLength = 32;

    public static IReadOnlyList<string> Codes { get; } =
    [
        nameof(InventoryTransferExceptionCustodyStatus.HeldAtDestination),
        nameof(InventoryTransferExceptionCustodyStatus.AwaitingReturn),
        nameof(InventoryTransferExceptionCustodyStatus.ReturnInTransit),
        nameof(InventoryTransferExceptionCustodyStatus.ReceivedAtSource),
        nameof(InventoryTransferExceptionCustodyStatus.AwaitingInspection),
        nameof(InventoryTransferExceptionCustodyStatus.Inspected)
    ];

    public static string ToCode(InventoryTransferExceptionCustodyStatus status) => status.ToString();

    public static bool TryParse(string? code, out InventoryTransferExceptionCustodyStatus status)
    {
        status = InventoryTransferExceptionCustodyStatus.HeldAtDestination;
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var match = Codes.FirstOrDefault(c => string.Equals(c, code.Trim(), StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            return false;
        }

        status = Enum.Parse<InventoryTransferExceptionCustodyStatus>(match, ignoreCase: false);
        return true;
    }

    public static InventoryTransferExceptionCustodyStatus Parse(string? code)
    {
        if (!TryParse(code, out var status))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferExceptionCustodyStatus,
                "Exception custody status is not recognized.");
        }

        return status;
    }
}
