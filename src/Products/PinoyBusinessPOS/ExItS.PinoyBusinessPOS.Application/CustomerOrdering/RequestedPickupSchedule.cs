using System.Globalization;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;

namespace ExItS.PinoyBusinessPOS.Application.CustomerOrdering;

/// <summary>
/// Turns an optional branch-local pickup date and time into an immutable order snapshot.
/// Empty date and time means pickup as soon as possible.
/// </summary>
public static class RequestedPickupSchedule
{
    public const string LocalFormat = "yyyy-MM-dd HH:mm";

    public static ApplicationResult<(string? Local, string? TimeZoneId, DateTimeOffset? AtUtc)> Resolve(
        CustomerOrderFulfillmentType fulfillmentType,
        string? date,
        string? time,
        string? branchTimeZoneId,
        DateTimeOffset utcNow,
        IReadOnlyList<CustomerOrderBranchHoursDaySnapshot>? operatingHours = null)
    {
        var hasDate = !string.IsNullOrWhiteSpace(date);
        var hasTime = !string.IsNullOrWhiteSpace(time);
        if (!hasDate && !hasTime)
        {
            return ApplicationResult<(string?, string?, DateTimeOffset?)>.Success((null, null, null));
        }

        if (fulfillmentType != CustomerOrderFulfillmentType.Pickup || !hasDate || !hasTime)
        {
            return ApplicationResult<(string?, string?, DateTimeOffset?)>.Failure(
                DomainErrorCodes.InvalidCustomerOrderPickupRequest,
                "Choose a pickup date and time, or leave both empty for pickup as soon as possible.");
        }

        if (string.IsNullOrWhiteSpace(branchTimeZoneId))
        {
            return ApplicationResult<(string?, string?, DateTimeOffset?)>.Failure(
                DomainErrorCodes.InvalidCustomerOrderPickupRequest,
                "This store cannot accept a requested pickup time right now.");
        }

        if (!DateOnly.TryParseExact(date!.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day)
            || !TimeOnly.TryParseExact(time!.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var clock))
        {
            return ApplicationResult<(string?, string?, DateTimeOffset?)>.Failure(
                DomainErrorCodes.InvalidCustomerOrderPickupRequest,
                "The requested pickup time is not valid for this store.");
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
                "This store cannot accept a requested pickup time right now.");
        }
        catch (InvalidTimeZoneException)
        {
            return ApplicationResult<(string?, string?, DateTimeOffset?)>.Failure(
                DomainErrorCodes.InvalidCustomerOrderPickupRequest,
                "This store cannot accept a requested pickup time right now.");
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
                "The requested pickup time is not valid for this store.");
        }

        var utc = TimeZoneInfo.ConvertTimeToUtc(unspecified, zone);
        var instant = new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc));
        if (instant < utcNow)
        {
            return ApplicationResult<(string?, string?, DateTimeOffset?)>.Failure(
                DomainErrorCodes.InvalidCustomerOrderPickupRequest,
                "Choose a pickup time that is still ahead for this store.");
        }

        var local = unspecified.ToString(LocalFormat, CultureInfo.InvariantCulture);
        return ApplicationResult<(string?, string?, DateTimeOffset?)>.Success((local, zone.Id, instant));
    }

    private static string? ValidateAgainstStoreHours(
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
            || !TimeOnly.TryParseExact(match.CloseTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var close)
            || close <= open)
        {
            return null;
        }

        if (clock > close)
        {
            return "Choose a pickup time before this store closes.";
        }

        return null;
    }
}
