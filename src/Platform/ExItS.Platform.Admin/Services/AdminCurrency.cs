using System.Globalization;

namespace ExItS.Platform.Admin.Services;

/// <summary>Philippine peso display helpers for Platform Admin.</summary>
public static class AdminCurrency
{
    private static readonly CultureInfo PhilippineCulture = CultureInfo.GetCultureInfo("en-PH");

    public static string FormatPhp(decimal? value) =>
        value is null ? "—" : string.Format(PhilippineCulture, "₱{0:N2}", value.Value);
}
