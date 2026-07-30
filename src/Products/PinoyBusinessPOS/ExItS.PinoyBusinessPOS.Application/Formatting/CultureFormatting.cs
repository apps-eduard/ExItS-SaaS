using System.Globalization;

namespace ExItS.PinoyBusinessPOS.Application.Formatting;

/// <summary>
/// Culture-aware display formatting for POS UI. Does not mutate stored values.
/// Pass UTC <see cref="DateTimeOffset"/> for timestamps; callers choose UTC vs local labels.
/// </summary>
public static class CultureFormatting
{
    public const string UtcLabel = "UTC";

    public static CultureInfo ActiveCulture =>
        CultureInfo.CurrentUICulture ?? CultureInfo.GetCultureInfo("en");

    public static string FormatDate(DateTimeOffset value, CultureInfo? culture = null, bool asUtc = true)
    {
        var c = culture ?? ActiveCulture;
        var dt = asUtc ? value.UtcDateTime : value.ToLocalTime().DateTime;
        return dt.ToString("d", c);
    }

    public static string FormatTime(DateTimeOffset value, CultureInfo? culture = null, bool asUtc = true)
    {
        var c = culture ?? ActiveCulture;
        var dt = asUtc ? value.UtcDateTime : value.ToLocalTime().DateTime;
        return dt.ToString("t", c);
    }

    public static string FormatDateTime(DateTimeOffset value, CultureInfo? culture = null, bool asUtc = true, bool includeZoneLabel = true)
    {
        var c = culture ?? ActiveCulture;
        var dt = asUtc ? value.UtcDateTime : value.ToLocalTime().DateTime;
        var formatted = dt.ToString("g", c);
        if (!includeZoneLabel)
        {
            return formatted;
        }

        return asUtc ? $"{formatted} {UtcLabel}" : $"{formatted} ({c.Name})";
    }

    public static string FormatNumber(decimal value, CultureInfo? culture = null, string? format = "N2")
    {
        var c = culture ?? ActiveCulture;
        return value.ToString(format, c);
    }

    public static string FormatPercent(decimal value, CultureInfo? culture = null, int decimals = 0)
    {
        // value is a fraction (0.15 => 15%). Does not change the input.
        var c = culture ?? ActiveCulture;
        return value.ToString($"P{decimals}", c);
    }

    /// <summary>
    /// Formats a monetary amount for display only. Does not introduce business currency logic
    /// or convert rates — uses the UI culture's currency pattern with an optional ISO code suffix.
    /// </summary>
    public static string FormatCurrency(decimal value, string currencyCode = "PHP", CultureInfo? culture = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);
        var c = culture ?? ActiveCulture;
        var amount = value.ToString("N2", c);
        return $"{currencyCode} {amount}";
    }
}
