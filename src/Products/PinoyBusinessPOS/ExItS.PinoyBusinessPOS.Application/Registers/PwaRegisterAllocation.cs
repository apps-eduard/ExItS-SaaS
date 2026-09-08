using System.Globalization;
using System.Text.RegularExpressions;

namespace ExItS.PinoyBusinessPOS.Application.Registers;

/// <summary>
/// Deterministic PWA display-name allocation for pure React shift open.
/// Convention: next ordinal = max existing <c>PWA-NNNN</c> + 1 (gaps are not reused).
/// Server still owns <c>REG-NNNNNN</c> codes; these names are friendly labels only.
/// </summary>
public static partial class PwaRegisterAllocation
{
    public const string Description = "Auto-created cash register for web POS (PWA).";
    public const int MaxCreateAttempts = 8;

    [GeneratedRegex(@"^PWA-(\d{4})$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex PwaNameRegex();

    public static bool IsPwaDisplayName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        return PwaNameRegex().IsMatch(name.Trim());
    }

    public static string FormatName(int ordinal) =>
        string.Create(CultureInfo.InvariantCulture, $"PWA-{ordinal:D4}");

    /// <summary>
    /// Returns the next PWA display name after the highest existing <c>PWA-NNNN</c> ordinal.
    /// Unrelated register names are ignored. Empty set → <c>PWA-0001</c>.
    /// </summary>
    public static string NextDisplayName(IEnumerable<string?> existingNames)
    {
        var max = 0;
        foreach (var name in existingNames)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var match = PwaNameRegex().Match(name.Trim());
            if (!match.Success)
            {
                continue;
            }

            if (int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var ordinal)
                && ordinal > max)
            {
                max = ordinal;
            }
        }

        return FormatName(max + 1);
    }
}
