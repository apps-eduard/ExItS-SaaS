using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;

namespace ExItS.PinoyBusinessPOS.Application.CustomerOrdering;

/// <summary>
/// Pickup-facing wrapper over <see cref="RequestedFulfillmentSchedule"/>.
/// Empty date and time means pickup as soon as possible.
/// </summary>
public static class RequestedPickupSchedule
{
    public const string LocalFormat = RequestedFulfillmentSchedule.LocalFormat;

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
        if (fulfillmentType != CustomerOrderFulfillmentType.Pickup)
        {
            return ApplicationResult<(string?, string?, DateTimeOffset?)>.Success((null, null, null));
        }

        return RequestedFulfillmentSchedule.Resolve(
            CustomerOrderFulfillmentType.Pickup,
            date,
            time,
            branchTimeZoneId,
            utcNow,
            operatingHours,
            minPreparationLeadMinutes,
            maxAdvanceScheduleDays);
    }
}
