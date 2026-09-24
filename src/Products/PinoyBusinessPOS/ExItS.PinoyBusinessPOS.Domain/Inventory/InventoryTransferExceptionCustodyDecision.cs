using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

public enum InventoryTransferExceptionCustodyDecision
{
    KeepAtDestination = 0,
    ReturnToSource = 1
}

public static class InventoryTransferExceptionCustodyDecisions
{
    public const int CodeMaxLength = 32;

    public static IReadOnlyList<string> Codes { get; } =
    [
        nameof(InventoryTransferExceptionCustodyDecision.KeepAtDestination),
        nameof(InventoryTransferExceptionCustodyDecision.ReturnToSource)
    ];

    public static string ToCode(InventoryTransferExceptionCustodyDecision decision) => decision.ToString();

    public static bool TryParse(string? code, out InventoryTransferExceptionCustodyDecision decision)
    {
        decision = InventoryTransferExceptionCustodyDecision.KeepAtDestination;
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var match = Codes.FirstOrDefault(c => string.Equals(c, code.Trim(), StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            return false;
        }

        decision = Enum.Parse<InventoryTransferExceptionCustodyDecision>(match, ignoreCase: false);
        return true;
    }

    public static InventoryTransferExceptionCustodyDecision Parse(string? code)
    {
        if (!TryParse(code, out var decision))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferExceptionCustodyDecision,
                "Exception custody decision is not recognized.");
        }

        return decision;
    }
}
