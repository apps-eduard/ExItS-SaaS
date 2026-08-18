using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class BranchFulfillmentReadinessTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 18, 13, 0, 0, TimeSpan.Zero);
    private static readonly PlatformOrganizationId Org = PlatformOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

    [Fact]
    public void Operating_hours_open_closed_and_24h_evaluate_in_branch_timezone()
    {
        var evaluator = new BranchOperatingHoursEvaluator();
        var schedule = BranchOperatingHoursSchedule.Create(
            OrganizationBranchId.New(),
            [
                BranchDayOperatingHours.Interval(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(21, 0)),
                BranchDayOperatingHours.Closed(DayOfWeek.Tuesday),
                BranchDayOperatingHours.Open24Hours(DayOfWeek.Wednesday),
                BranchDayOperatingHours.Closed(DayOfWeek.Thursday),
                BranchDayOperatingHours.Closed(DayOfWeek.Friday),
                BranchDayOperatingHours.Closed(DayOfWeek.Saturday),
                BranchDayOperatingHours.Closed(DayOfWeek.Sunday)
            ]);

        var mondayOpen = evaluator.Evaluate(schedule, "Asia/Manila", new DateTimeOffset(2026, 8, 17, 1, 0, 0, TimeSpan.Zero));
        Assert.True(mondayOpen.IsOpenNow);

        var mondayClosed = evaluator.Evaluate(schedule, "Asia/Manila", new DateTimeOffset(2026, 8, 17, 14, 30, 0, TimeSpan.Zero));
        Assert.False(mondayClosed.IsOpenNow);

        var wednesday = evaluator.Evaluate(schedule, "Asia/Manila", new DateTimeOffset(2026, 8, 19, 2, 0, 0, TimeSpan.Zero));
        Assert.True(wednesday.IsOpenNow);
    }

    [Fact]
    public void Delivery_not_ready_without_hours_coordinates_or_entitlement()
    {
        var branch = OrganizationBranch.Create(Org, "MAIN", "Main", T0, "Line 1", city: "Manila", countryCode: "PH");
        branch.UpdateContactPhone("+63 917 000 0000", T0);
        var readiness = new BranchFulfillmentReadinessEvaluator(new BranchOperatingHoursEvaluator());
        var result = readiness.Evaluate(new BranchFulfillmentReadinessInput(
            branch,
            OperatingHours: null,
            DeliveryPolicy: null,
            OrganizationTimeZoneId: "Asia/Manila",
            OrganizationContactPhone: null,
            new BranchEntitlementCapabilities(CanUseCustomerOrdering: true, CanUseDelivery: true),
            T0));

        Assert.False(result.DeliveryReady);
        Assert.Contains(FulfillmentReadinessReasonCodes.StoreHoursMissing, result.ReasonCodes);
        Assert.Contains(FulfillmentReadinessReasonCodes.MapLocationMissing, result.ReasonCodes);
    }

    [Fact]
    public void Complete_setup_can_be_delivery_ready_without_auto_enable()
    {
        var branch = OrganizationBranch.Create(Org, "MAIN", "Main", T0, "Line 1", city: "Manila", countryCode: "PH");
        branch.UpdateContactPhone("+63 917 000 0000", T0);
        branch.UpdateCoordinates(14.5547m, 121.0244m, T0);
        var hours = BranchOperatingHoursSchedule.Create(
            branch.Id,
            Enumerable.Range(0, 7).Select(i => BranchDayOperatingHours.Open24Hours((DayOfWeek)i)).ToList());
        var policy = BranchDeliveryPolicy.Create(
            branch.Id, Org, 0m, 49m, 2m, 10m, 15m, null, T0);
        var readiness = new BranchFulfillmentReadinessEvaluator(new BranchOperatingHoursEvaluator());
        var mondayMorning = new DateTimeOffset(2026, 8, 17, 1, 0, 0, TimeSpan.Zero);
        var result = readiness.Evaluate(new BranchFulfillmentReadinessInput(
            branch,
            hours,
            policy,
            "Asia/Manila",
            null,
            new BranchEntitlementCapabilities(true, true),
            mondayMorning));

        Assert.True(result.CustomerOrderingReady);
        Assert.True(result.DeliveryReady);
        Assert.False(result.DeliveryOperational);
        Assert.False(branch.DeliveryEnabled);
    }
}
