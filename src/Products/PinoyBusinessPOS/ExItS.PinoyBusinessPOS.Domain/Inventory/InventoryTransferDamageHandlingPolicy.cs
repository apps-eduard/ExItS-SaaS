using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>
/// Sender-selected policy for how damaged quantities may be handled at receive.
/// </summary>
public enum InventoryTransferDamageHandlingPolicy
{
    ReceiverMayDecide = 0,
    ReturnToSourceRequired = 1,
    KeepAtDestination = 2
}

public static class InventoryTransferDamageHandlingPolicies
{
    public const int CodeMaxLength = 32;

    public static IReadOnlyList<string> Codes { get; } =
    [
        nameof(InventoryTransferDamageHandlingPolicy.ReceiverMayDecide),
        nameof(InventoryTransferDamageHandlingPolicy.ReturnToSourceRequired),
        nameof(InventoryTransferDamageHandlingPolicy.KeepAtDestination)
    ];

    public static string ToCode(InventoryTransferDamageHandlingPolicy policy) => policy.ToString();

    public static bool TryParse(string? code, out InventoryTransferDamageHandlingPolicy policy)
    {
        policy = InventoryTransferDamageHandlingPolicy.ReceiverMayDecide;
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

        policy = Enum.Parse<InventoryTransferDamageHandlingPolicy>(match, ignoreCase: false);
        return true;
    }

    public static InventoryTransferDamageHandlingPolicy Parse(string? code)
    {
        if (!TryParse(code, out var policy))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferDamageHandlingPolicy,
                $"Damage handling policy must be one of: {string.Join(", ", Codes)}.");
        }

        return policy;
    }
}
