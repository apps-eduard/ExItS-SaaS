using System.Globalization;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

/// <summary>
/// User-facing stock-count display helpers. Domain status codes and stored UTC timestamps stay unchanged.
/// </summary>
public static class StockCountDisplay
{
    public const string CustomPresetValue = "Custom";

    public static readonly IReadOnlyList<string> PresetTitles =
    [
        "Weekly count",
        "Monthly count",
        "Quarterly count",
        "Midyear count",
        "Year-end count"
    ];

    public static string DisplayTitle(string? title) =>
        string.IsNullOrWhiteSpace(title) ? StockCount.HistoricalTitle : title.Trim();

    public static string FormatLocalTimestamp(DateTimeOffset utc) =>
        utc.ToLocalTime().ToString("MMM d, yyyy · h:mm tt", CultureInfo.GetCultureInfo("en-US"));

    public static DateTimeOffset PrimaryTimestamp(PosStockCountDto count) =>
        count.CompletedAtUtc
        ?? count.StartedAtUtc
        ?? count.CreatedAtUtc;

    public static int DifferenceCount(IEnumerable<PosStockCountLineDto> lines) =>
        lines.Count(line => line.Variance is not null && line.Variance.Value != 0m);

    public static string FormatDifference(decimal? variance)
    {
        if (variance is null)
        {
            return "—";
        }

        return variance.Value.ToString("+0.###;-0.###;0", CultureInfo.InvariantCulture);
    }

    public static decimal? LiveDifference(decimal? systemQty, string? countedText)
    {
        if (systemQty is null || string.IsNullOrWhiteSpace(countedText))
        {
            return null;
        }

        if (!decimal.TryParse(countedText, NumberStyles.Number, CultureInfo.CurrentCulture, out var counted)
            && !decimal.TryParse(countedText, NumberStyles.Number, CultureInfo.InvariantCulture, out counted))
        {
            return null;
        }

        return counted - systemQty.Value;
    }
}
