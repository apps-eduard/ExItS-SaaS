namespace ExItS.PinoyBusinessPOS.Application.Platform;

/// <summary>
/// UI-only branch location helpers. Does not change persistence or ISO country validation.
/// </summary>
public static class BranchLocationUi
{
    /// <summary>
    /// Native Blazor/WebView <c>select</c> rejects a bound null or empty value even when
    /// <c>&lt;option value=""&gt;</c> exists. Keep this sentinel in the option list.
    /// </summary>
    public const string UnspecifiedCode = "none";

    public static readonly string[] CountryCodes =
    [
        "PH", "US", "SG", "MY", "ID", "TH", "AU", "AE", "GB"
    ];

    public static string BindCode(string? stored)
    {
        var extra = stored?.Trim();
        if (string.IsNullOrWhiteSpace(extra))
        {
            return UnspecifiedCode;
        }

        foreach (var code in CountryCodes)
        {
            if (string.Equals(code, extra, StringComparison.OrdinalIgnoreCase))
            {
                return code;
            }
        }

        return extra;
    }

    public static string? ToStoredCode(string? bound)
    {
        var extra = bound?.Trim();
        return string.IsNullOrWhiteSpace(extra)
               || string.Equals(extra, UnspecifiedCode, StringComparison.OrdinalIgnoreCase)
            ? null
            : extra;
    }

    public static IEnumerable<string> CodesFor(string? current)
    {
        foreach (var code in CountryCodes)
        {
            yield return code;
        }

        var extra = current?.Trim();
        if (!string.IsNullOrWhiteSpace(extra)
            && !string.Equals(extra, UnspecifiedCode, StringComparison.OrdinalIgnoreCase)
            && !CountryCodes.Contains(extra, StringComparer.OrdinalIgnoreCase))
        {
            yield return extra;
        }
    }

    public static string BindStatus(string? status)
    {
        if (string.Equals(status, "Inactive", StringComparison.OrdinalIgnoreCase))
        {
            return "Inactive";
        }

        if (string.Equals(status, "Archived", StringComparison.OrdinalIgnoreCase))
        {
            return "Archived";
        }

        return "Active";
    }

    public static IEnumerable<string> StatusCodesFor(string? current)
    {
        yield return "Active";
        yield return "Inactive";
        if (string.Equals(current, "Archived", StringComparison.OrdinalIgnoreCase))
        {
            yield return "Archived";
        }
    }
}
