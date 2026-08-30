using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>Lifecycle of a direct purchase receipt. Member names are stable persistence codes.</summary>
public enum DirectPurchaseReceiptStatus
{
    Posted = 0,
    Voided = 1
}

public static class DirectPurchaseReceiptStatuses
{
    public const int CodeMaxLength = 16;

    public static IReadOnlyList<string> Codes { get; } =
    [
        nameof(DirectPurchaseReceiptStatus.Posted),
        nameof(DirectPurchaseReceiptStatus.Voided)
    ];

    public static string ToCode(DirectPurchaseReceiptStatus status) => status.ToString();

    public static bool TryParse(string? code, out DirectPurchaseReceiptStatus status)
    {
        status = DirectPurchaseReceiptStatus.Posted;
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

        status = Enum.Parse<DirectPurchaseReceiptStatus>(match, ignoreCase: false);
        return true;
    }

    public static DirectPurchaseReceiptStatus Parse(string? code)
    {
        if (!TryParse(code, out var status))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidDirectPurchaseReceiptStatus,
                $"Direct purchase receipt status must be one of: {string.Join(", ", Codes)}.");
        }

        return status;
    }
}
