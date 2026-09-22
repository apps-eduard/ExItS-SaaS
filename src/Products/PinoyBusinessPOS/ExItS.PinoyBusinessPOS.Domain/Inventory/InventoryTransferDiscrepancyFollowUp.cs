using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

public enum InventoryTransferDiscrepancyFollowUp
{
    RequestReplacement = 0,
    AcceptShortage = 1
}

public static class InventoryTransferDiscrepancyFollowUps
{
    public const int CodeMaxLength = 32;

    public static IReadOnlyList<string> Codes { get; } =
    [
        nameof(InventoryTransferDiscrepancyFollowUp.RequestReplacement),
        nameof(InventoryTransferDiscrepancyFollowUp.AcceptShortage)
    ];

    public static string ToCode(InventoryTransferDiscrepancyFollowUp followUp) => followUp.ToString();

    public static bool TryParse(string? code, out InventoryTransferDiscrepancyFollowUp followUp)
    {
        followUp = InventoryTransferDiscrepancyFollowUp.RequestReplacement;
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

        followUp = Enum.Parse<InventoryTransferDiscrepancyFollowUp>(match, ignoreCase: false);
        return true;
    }

    public static InventoryTransferDiscrepancyFollowUp Parse(string? code)
    {
        if (!TryParse(code, out var followUp))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferDiscrepancyFollowUp,
                $"Discrepancy follow-up must be one of: {string.Join(", ", Codes)}.");
        }

        return followUp;
    }
}
