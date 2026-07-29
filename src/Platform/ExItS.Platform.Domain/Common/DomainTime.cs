using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Common;

internal static class DomainTime
{
    public static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidUtcTimestamp,
                "Timestamps must be UTC (offset zero).");
        }
    }

    public static string NormalizeDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidDisplayName,
                "Display name cannot be blank.");
        }

        var trimmed = System.Text.RegularExpressions.Regex.Replace(displayName.Trim(), @"\s+", " ");
        if (trimmed.Length is < 2 or > 100)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidDisplayName,
                "Display name must be 2–100 characters.");
        }

        return trimmed;
    }
}
