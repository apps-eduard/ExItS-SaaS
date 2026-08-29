using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>Primary reason for waste/loss. Member names are stable persistence codes.</summary>
public enum WasteLossReason
{
    Spoiled = 0,
    Expired = 1,
    Damaged = 2,
    Broken = 3,
    Spillage = 4,
    MissingOrShrinkage = 5,
    Other = 6
}

public static class WasteLossReasons
{
    public const int CodeMaxLength = 32;

    public static IReadOnlyList<string> Codes { get; } =
    [
        nameof(WasteLossReason.Spoiled),
        nameof(WasteLossReason.Expired),
        nameof(WasteLossReason.Damaged),
        nameof(WasteLossReason.Broken),
        nameof(WasteLossReason.Spillage),
        nameof(WasteLossReason.MissingOrShrinkage),
        nameof(WasteLossReason.Other)
    ];

    public static string ToCode(WasteLossReason reason) => reason.ToString();

    public static bool TryParse(string? code, out WasteLossReason reason)
    {
        reason = WasteLossReason.Other;
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

        reason = Enum.Parse<WasteLossReason>(match, ignoreCase: false);
        return true;
    }

    public static WasteLossReason Parse(string? code)
    {
        if (!TryParse(code, out var reason))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidWasteLossReason,
                $"Waste/loss reason must be one of: {string.Join(", ", Codes)}.");
        }

        return reason;
    }
}
