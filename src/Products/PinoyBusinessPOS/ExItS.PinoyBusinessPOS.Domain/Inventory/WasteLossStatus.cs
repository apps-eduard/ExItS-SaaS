using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>Lifecycle of a waste/loss document. Member names are stable persistence codes.</summary>
public enum WasteLossStatus
{
    Posted = 0,
    Voided = 1
}

public static class WasteLossStatuses
{
    public const int CodeMaxLength = 16;

    public static IReadOnlyList<string> Codes { get; } =
    [
        nameof(WasteLossStatus.Posted),
        nameof(WasteLossStatus.Voided)
    ];

    public static string ToCode(WasteLossStatus status) => status.ToString();

    public static bool TryParse(string? code, out WasteLossStatus status)
    {
        status = WasteLossStatus.Posted;
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

        status = Enum.Parse<WasteLossStatus>(match, ignoreCase: false);
        return true;
    }

    public static WasteLossStatus Parse(string? code)
    {
        if (!TryParse(code, out var status))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidWasteLossStatus,
                $"Waste/loss status must be one of: {string.Join(", ", Codes)}.");
        }

        return status;
    }
}
