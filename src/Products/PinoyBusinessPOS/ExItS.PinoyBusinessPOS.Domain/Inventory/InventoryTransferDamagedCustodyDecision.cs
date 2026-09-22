using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>Receiver (or forced) decision for damaged qty custody after receive.</summary>
public enum InventoryTransferDamagedCustodyDecision
{
    KeepAtDestination = 0,
    ReturnToSource = 1
}

public static class InventoryTransferDamagedCustodyDecisions
{
    public const int CodeMaxLength = 32;

    public static IReadOnlyList<string> Codes { get; } =
    [
        nameof(InventoryTransferDamagedCustodyDecision.KeepAtDestination),
        nameof(InventoryTransferDamagedCustodyDecision.ReturnToSource)
    ];

    public static string ToCode(InventoryTransferDamagedCustodyDecision decision) => decision.ToString();

    public static bool TryParse(string? code, out InventoryTransferDamagedCustodyDecision decision)
    {
        decision = InventoryTransferDamagedCustodyDecision.KeepAtDestination;
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

        decision = Enum.Parse<InventoryTransferDamagedCustodyDecision>(match, ignoreCase: false);
        return true;
    }

    public static InventoryTransferDamagedCustodyDecision Parse(string? code)
    {
        if (!TryParse(code, out var decision))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferDamagedCustodyDecision,
                $"Damaged custody decision must be one of: {string.Join(", ", Codes)}.");
        }

        return decision;
    }

    public static InventoryTransferDamagedCustodyDecision Resolve(
        InventoryTransferDamageHandlingPolicy policy,
        InventoryTransferDamagedCustodyDecision? requested)
    {
        return policy switch
        {
            InventoryTransferDamageHandlingPolicy.ReturnToSourceRequired =>
                InventoryTransferDamagedCustodyDecision.ReturnToSource,
            InventoryTransferDamageHandlingPolicy.KeepAtDestination =>
                InventoryTransferDamagedCustodyDecision.KeepAtDestination,
            InventoryTransferDamageHandlingPolicy.ReceiverMayDecide =>
                requested
                ?? throw new DomainException(
                    DomainErrorCodes.InvalidInventoryTransferDamagedCustodyDecision,
                    "A damaged custody decision is required when the receiver may decide."),
            _ => throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferDamageHandlingPolicy,
                "Damage handling policy is not recognized.")
        };
    }
}
