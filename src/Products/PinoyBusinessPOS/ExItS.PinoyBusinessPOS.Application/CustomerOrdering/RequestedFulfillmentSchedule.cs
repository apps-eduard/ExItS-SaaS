using System.Globalization;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;

namespace ExItS.PinoyBusinessPOS.Application.CustomerOrdering;

/// <summary>
/// Resolves an optional branch-local fulfillment date/time into an immutable order snapshot.
/// Empty date and time means fulfill as soon as possible (only valid while the store is open).
/// </summary>
public static class RequestedFulfillmentSchedule
{
    public const string LocalFormat = "yyyy-MM-dd HH:mm";

    public static ApplicationResult<(string? Local, string? TimeZoneId, DateTimeOffset? AtUtc)> Resolve(
        CustomerOrderFulfillmentType fulfillmentType,
        string? date,
        string? time,
        string? branchTimeZoneId,
        DateTimeOffset utcNow,
        IReadOnlyList<CustomerOrderBranchHoursDaySnapshot>? operatingHours = null,
        int minPreparationLeadMinutes = 0,
        int maxAdvanceScheduleDays = 0)
    {
        var hasDate = !string.IsNullOrWhiteSpace(date);
        var hasTime = !string.IsNullOrWhiteSpace(time);
        if (!hasDate && !hasTime)
        {
            return ApplicationResult<(string?, string?, DateTimeOffset?)>.Success((null, null, null));
        }

        if (!hasDate || !hasTime)
        {
            return ApplicationResult<(string?, string?, DateTimeOffset?)>.Failure(
                DomainErrorCodes.InvalidCustomerOrderPickupRequest,
                fulfillmentType == CustomerOrderFulfillmentType.Delivery
                    ? "Choose a delivery date and time, or leave both empty for delivery as soon as possible."
                    : "Choose a pickup date and time, or leave both empty for pickup as soon as possible.");
        }

        if (string.IsNullOrWhiteSpace(branchTimeZoneId))
        {
            return ApplicationResult<(string?, string?, DateTimeOffset?)>.Failure(
                DomainErrorCodes.InvalidCustomerOrderPickupRequest,
                "This store cannot accept a requested fulfillment time right now.");
        }

        if (!DateOnly.TryParseExact(date!.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day)
            || !TimeOnly.TryParseExact(time!.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var clock))
        {
            return ApplicationResult<(string?, string?, DateTimeOffset?)>.Failure(
                DomainErrorCodes.InvalidCustomerOrderPickupRequest,
                "The requested fulfillment time is not valid for this store.");
        }

        TimeZoneInfo zone;
        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById(branchTimeZoneId.Trim());
        }
        catch (TimeZoneNotFoundException)
        {
            return ApplicationResult<(string?, string?, DateTimeOffset?)>.Failure(
                DomainErrorCodes.InvalidCustomerOrderPickupRequest,
                "This store cannot accept a requested fulfillment time right now.");
        }
        catch (InvalidTimeZoneException)
        {
            return ApplicationResult<(string?, string?, DateTimeOffset?)>.Failure(
                DomainErrorCodes.InvalidCustomerOrderPickupRequest,
                "This store cannot accept a requested fulfillment time right now.");
        }

        var hoursError = ValidateAgainstStoreHours(day, clock, operatingHours);
        if (hoursError is not null)
        {
            return ApplicationResult<(string?, string?, DateTimeOffset?)>.Failure(
                DomainErrorCodes.InvalidCustomerOrderPickupRequest,
                hoursError);
        }

        var unspecified = day.ToDateTime(clock, DateTimeKind.Unspecified);
        if (zone.IsInvalidTime(unspecified))
        {
            return ApplicationResult<(string?, string?, DateTimeOffset?)>.Failure(
                DomainErrorCodes.InvalidCustomerOrderPickupRequest,
                "The requested fulfillment time is not valid for this store.");
        }

        var utc = TimeZoneInfo.ConvertTimeToUtc(unspecified, zone);
        var instant = new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc));
        var earliest = utcNow.AddMinutes(Math.Max(0, minPreparationLeadMinutes));
        if (instant < earliest)
        {
            return ApplicationResult<(string?, string?, DateTimeOffset?)>.Failure(
                DomainErrorCodes.InvalidCustomerOrderPickupRequest,
                minPreparationLeadMinutes > 0
                    ? $"Choose a time at least {minPreparationLeadMinutes} minutes ahead for this store."
                    : "Choose a fulfillment time that is still ahead for this store.");
        }

        if (maxAdvanceScheduleDays > 0)
        {
            var latest = utcNow.AddDays(maxAdvanceScheduleDays);
            if (instant > latest)
            {
                return ApplicationResult<(string?, string?, DateTimeOffset?)>.Failure(
                    DomainErrorCodes.InvalidCustomerOrderPickupRequest,
                    $"Choose a time within the next {maxAdvanceScheduleDays} days for this store.");
            }
        }

        var local = unspecified.ToString(LocalFormat, CultureInfo.InvariantCulture);
        return ApplicationResult<(string?, string?, DateTimeOffset?)>.Success((local, zone.Id, instant));
    }

    /// <summary>Returns null when hours are unknown or the clock falls inside the open window.</summary>
    internal static string? ValidateAgainstStoreHours(
        DateOnly day,
        TimeOnly clock,
        IReadOnlyList<CustomerOrderBranchHoursDaySnapshot>? operatingHours)
    {
        if (operatingHours is null || operatingHours.Count == 0)
        {
            return null;
        }

        var match = operatingHours.FirstOrDefault(item =>
            Enum.TryParse<DayOfWeek>(item.DayOfWeek, true, out var weekday) && weekday == day.DayOfWeek);
        if (match is null)
        {
            return null;
        }

        if (match.IsClosed)
        {
            return "This store is closed on that day.";
        }

        if (match.IsOpen24Hours)
        {
            return null;
        }

        if (!TimeOnly.TryParseExact(match.OpenTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var open)
            || !TimeOnly.TryParseExact(match.CloseTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var close))
        {
            return null;
        }

        // Same-day window (open < close): [open, close].
        if (close > open)
        {
            if (clock < open)
            {
                return "Choose a time after this store opens.";
            }

            if (clock > close)
            {
                return "Choose a time before this store closes.";
            }

            return null;
        }

        // Overnight window (close < open): open late → next morning close.
        if (close < open)
        {
            if (clock >= open || clock <= close)
            {
                return null;
            }

            return "Choose a time while this store is open.";
        }

        // open == close with IsOpen24Hours false: treat as invalid / closed for scheduling.
        return "This store is closed on that day.";
    }
}
