using System.Globalization;

namespace ExItS.Platform.Admin.Services;

/// <summary>
/// Captures the signed-in browser IANA time zone for Admin local date/time display.
/// API and database values remain UTC; conversion happens only in the UI.
/// </summary>
public sealed class UserTimeZoneState
{
    private TimeZoneInfo _timeZone = TimeZoneInfo.Utc;

    public string TimeZoneId { get; private set; } = "UTC";

    public bool IsResolved { get; private set; }

    public void SetBrowserTimeZone(string? ianaOrWindowsId)
    {
        if (string.IsNullOrWhiteSpace(ianaOrWindowsId))
        {
            return;
        }

        var trimmed = ianaOrWindowsId.Trim();
        if (TimeZoneInfo.TryFindSystemTimeZoneById(trimmed, out var tz))
        {
            _timeZone = tz;
            TimeZoneId = tz.Id;
            IsResolved = true;
            return;
        }

        // Some hosts expose Windows IDs; accept if convertible.
        try
        {
            _timeZone = TimeZoneInfo.FindSystemTimeZoneById(trimmed);
            TimeZoneId = _timeZone.Id;
            IsResolved = true;
        }
        catch (TimeZoneNotFoundException)
        {
            // Keep previous / UTC.
        }
        catch (InvalidTimeZoneException)
        {
            // Keep previous / UTC.
        }
    }

    public DateTimeOffset ToLocal(DateTimeOffset utc)
    {
        var normalized = utc.ToUniversalTime();
        return TimeZoneInfo.ConvertTime(normalized, _timeZone);
    }

    public string FormatLocal(DateTimeOffset? utc)
    {
        if (utc is null)
        {
            return "—";
        }

        var local = ToLocal(utc.Value);
        return local.ToString("dd MMM yyyy, h:mm tt", CultureInfo.InvariantCulture);
    }

    public string FormatUtcTooltip(DateTimeOffset? utc)
    {
        if (utc is null)
        {
            return "—";
        }

        return utc.Value.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " UTC";
    }
}
