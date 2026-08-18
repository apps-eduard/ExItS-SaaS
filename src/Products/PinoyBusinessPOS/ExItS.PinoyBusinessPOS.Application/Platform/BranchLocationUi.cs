namespace ExItS.PinoyBusinessPOS.Application.Platform;

/// <summary>
/// UI-only branch location helpers. Does not change persistence or ISO country validation.
/// </summary>
public static class BranchLocationUi
{
    public static readonly string[] CountryCodes =
    [
        "PH", "US", "SG", "MY", "ID", "TH", "AU", "AE", "GB"
    ];

    public static IEnumerable<string> CodesFor(string? current)
    {
        var extra = current?.Trim();
        foreach (var code in CountryCodes)
        {
            yield return code;
        }

        if (!string.IsNullOrWhiteSpace(extra)
            && !CountryCodes.Contains(extra, StringComparer.OrdinalIgnoreCase))
        {
            yield return extra;
        }
    }
}
