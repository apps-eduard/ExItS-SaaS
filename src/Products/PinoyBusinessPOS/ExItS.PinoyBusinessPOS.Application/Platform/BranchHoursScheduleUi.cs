using System.Globalization;

namespace ExItS.PinoyBusinessPOS.Application.Platform;

/// <summary>
/// UI-only operating-hours editor helpers. Does not change persistence or evaluation.
/// </summary>
public static class BranchHoursScheduleUi
{
    public const string ModeOpen = "Open";
    public const string ModeOpen24 = "Open24";
    public const string ModeClosed = "Closed";

    public static readonly string[] OrderedDays =
    [
        nameof(DayOfWeek.Monday),
        nameof(DayOfWeek.Tuesday),
        nameof(DayOfWeek.Wednesday),
        nameof(DayOfWeek.Thursday),
        nameof(DayOfWeek.Friday),
        nameof(DayOfWeek.Saturday),
        nameof(DayOfWeek.Sunday)
    ];

    public static string FormatClock(TimeOnly value) =>
        value.ToString("h:mm tt", CultureInfo.InvariantCulture);

    public static string FormatSummary(
        bool isClosed,
        bool isOpen24Hours,
        TimeOnly openTime,
        TimeOnly closeTime,
        string closedLabel,
        string open24Label)
    {
        if (isClosed)
        {
            return closedLabel;
        }

        if (isOpen24Hours)
        {
            return open24Label;
        }

        return $"{FormatClock(openTime)} – {FormatClock(closeTime)}";
    }

    public static bool ShowsTimes(bool isClosed, bool isOpen24Hours) =>
        !isClosed && !isOpen24Hours;

    public static bool HasConfiguredHours(IEnumerable<BranchHoursDayDraft> days) =>
        days.Any(d => !d.IsClosed);

    public static HashSet<string> DefaultCopyTargets(string sourceDay)
    {
        var targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.Equals(sourceDay, nameof(DayOfWeek.Monday), StringComparison.OrdinalIgnoreCase))
        {
            targets.Add(nameof(DayOfWeek.Tuesday));
            targets.Add(nameof(DayOfWeek.Wednesday));
            targets.Add(nameof(DayOfWeek.Thursday));
            targets.Add(nameof(DayOfWeek.Friday));
            return targets;
        }

        foreach (var day in OrderedDays)
        {
            if (!string.Equals(day, sourceDay, StringComparison.OrdinalIgnoreCase)
                && IsWeekday(day))
            {
                targets.Add(day);
            }
        }

        return targets;
    }

    public static bool IsWeekday(string day) =>
        Enum.TryParse<DayOfWeek>(day, true, out var parsed)
        && parsed is >= DayOfWeek.Monday and <= DayOfWeek.Friday;

    public static void CopyToSelected(
        IReadOnlyList<BranchHoursDayDraft> days,
        string sourceDay,
        IReadOnlyCollection<string> targetDays)
    {
        var source = days.FirstOrDefault(d =>
            string.Equals(d.DayOfWeek, sourceDay, StringComparison.OrdinalIgnoreCase));
        if (source is null || targetDays.Count == 0)
        {
            return;
        }

        foreach (var day in days)
        {
            if (string.Equals(day.DayOfWeek, source.DayOfWeek, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!targetDays.Contains(day.DayOfWeek, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            day.CopyScheduleFrom(source);
        }
    }

    public static BranchOperatingHoursDayDto ToDto(BranchHoursDayDraft day) =>
        new(
            day.DayOfWeek,
            day.IsClosed,
            day.IsOpen24Hours,
            day.IsClosed || day.IsOpen24Hours ? null : FormatStoredTime(day.OpenTime),
            day.IsClosed || day.IsOpen24Hours ? null : FormatStoredTime(day.CloseTime));

    public static string FormatStoredTime(TimeOnly value) =>
        value.ToString("HH:mm", CultureInfo.InvariantCulture);

    public static TimeOnly ParseTime(string? value, TimeOnly fallback)
    {
        if (TimeOnly.TryParse(value, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return fallback;
    }
}

public sealed class BranchHoursDayDraft
{
    public string DayOfWeek { get; set; } = nameof(System.DayOfWeek.Monday);
    public bool IsClosed { get; set; }
    public bool IsOpen24Hours { get; set; }
    public TimeOnly OpenTime { get; set; } = new(8, 0);
    public TimeOnly CloseTime { get; set; } = new(21, 0);

    public string Mode
    {
        get => IsClosed
            ? BranchHoursScheduleUi.ModeClosed
            : IsOpen24Hours
                ? BranchHoursScheduleUi.ModeOpen24
                : BranchHoursScheduleUi.ModeOpen;
        set
        {
            IsClosed = value == BranchHoursScheduleUi.ModeClosed;
            IsOpen24Hours = value == BranchHoursScheduleUi.ModeOpen24;
        }
    }

    public BranchHoursDayDraft Clone() => new()
    {
        DayOfWeek = DayOfWeek,
        IsClosed = IsClosed,
        IsOpen24Hours = IsOpen24Hours,
        OpenTime = OpenTime,
        CloseTime = CloseTime
    };

    public void CopyScheduleFrom(BranchHoursDayDraft source)
    {
        IsClosed = source.IsClosed;
        IsOpen24Hours = source.IsOpen24Hours;
        OpenTime = source.OpenTime;
        CloseTime = source.CloseTime;
    }

    public void ApplyFrom(BranchHoursDayDraft source)
    {
        CopyScheduleFrom(source);
        DayOfWeek = source.DayOfWeek;
    }
}
