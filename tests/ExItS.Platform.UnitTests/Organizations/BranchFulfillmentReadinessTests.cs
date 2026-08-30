using ExItS.Platform.Domain.Common;
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
            T0,
            HasActiveDeliveryServiceArea: false));

        Assert.False(result.DeliveryReady);
        Assert.Contains(FulfillmentReadinessReasonCodes.StoreHoursMissing, result.ReasonCodes);
        Assert.Contains(FulfillmentReadinessReasonCodes.MapLocationMissing, result.ReasonCodes);
        Assert.Contains(FulfillmentReadinessReasonCodes.DeliveryAreaMissing, result.ReasonCodes);
    }

    [Fact]
    public void Delivery_not_ready_without_service_area_even_when_otherwise_complete()
    {
        var (branch, hours, policy, instant) = CreateOtherwiseCompleteDeliverySetup();
        var readiness = new BranchFulfillmentReadinessEvaluator(new BranchOperatingHoursEvaluator());
        var result = readiness.Evaluate(new BranchFulfillmentReadinessInput(
            branch,
            hours,
            policy,
            "Asia/Manila",
            null,
            new BranchEntitlementCapabilities(true, true),
            instant,
            HasActiveDeliveryServiceArea: false));

        Assert.True(result.CustomerOrderingReady);
        Assert.True(result.PickupReady);
        Assert.False(result.DeliveryReady);
        Assert.Contains("delivery_area", result.MissingRequirements);
        Assert.Contains(FulfillmentReadinessReasonCodes.DeliveryAreaMissing, result.ReasonCodes);
        Assert.False(result.SetupSummary.DeliveryAreasComplete);
    }

    [Fact]
    public void Delivery_ready_with_active_service_area()
    {
        var (branch, hours, policy, instant) = CreateOtherwiseCompleteDeliverySetup();
        var readiness = new BranchFulfillmentReadinessEvaluator(new BranchOperatingHoursEvaluator());
        var result = readiness.Evaluate(new BranchFulfillmentReadinessInput(
            branch,
            hours,
            policy,
            "Asia/Manila",
            null,
            new BranchEntitlementCapabilities(true, true),
            instant,
            HasActiveDeliveryServiceArea: true));

        Assert.True(result.CustomerOrderingReady);
        Assert.True(result.DeliveryReady);
        Assert.False(result.DeliveryOperational);
        Assert.False(branch.DeliveryEnabled);
        Assert.True(result.SetupSummary.DeliveryAreasComplete);
        Assert.Equal(BranchFulfillmentSetupSummary.DeliverySectionCount, result.SetupSummary.DeliverySectionsComplete);
    }

    [Fact]
    public void Pickup_ready_without_delivery_service_areas()
    {
        var (branch, hours, policy, instant) = CreateOtherwiseCompleteDeliverySetup();
        var readiness = new BranchFulfillmentReadinessEvaluator(new BranchOperatingHoursEvaluator());
        var result = readiness.Evaluate(new BranchFulfillmentReadinessInput(
            branch,
            hours,
            policy,
            "Asia/Manila",
            null,
            new BranchEntitlementCapabilities(true, true),
            instant,
            HasActiveDeliveryServiceArea: false));

        Assert.True(result.PickupReady);
        Assert.False(result.DeliveryReady);
    }

    [Fact]
    public void Removing_final_area_makes_delivery_not_ready()
    {
        var (branch, hours, policy, instant) = CreateOtherwiseCompleteDeliverySetup();
        var readiness = new BranchFulfillmentReadinessEvaluator(new BranchOperatingHoursEvaluator());
        var withArea = readiness.Evaluate(new BranchFulfillmentReadinessInput(
            branch, hours, policy, "Asia/Manila", null,
            new BranchEntitlementCapabilities(true, true), instant, HasActiveDeliveryServiceArea: true));
        Assert.True(withArea.DeliveryReady);

        var withoutArea = readiness.Evaluate(new BranchFulfillmentReadinessInput(
            branch, hours, policy, "Asia/Manila", null,
            new BranchEntitlementCapabilities(true, true), instant, HasActiveDeliveryServiceArea: false));
        Assert.False(withoutArea.DeliveryReady);
        Assert.Contains(FulfillmentReadinessReasonCodes.DeliveryAreaMissing, withoutArea.ReasonCodes);
    }

    [Fact]
    public void Complete_setup_can_be_delivery_ready_without_auto_enable()
    {
        var (branch, hours, policy, mondayMorning) = CreateOtherwiseCompleteDeliverySetup();
        var readiness = new BranchFulfillmentReadinessEvaluator(new BranchOperatingHoursEvaluator());
        var result = readiness.Evaluate(new BranchFulfillmentReadinessInput(
            branch,
            hours,
            policy,
            "Asia/Manila",
            null,
            new BranchEntitlementCapabilities(true, true),
            mondayMorning,
            HasActiveDeliveryServiceArea: true));

        Assert.True(result.CustomerOrderingReady);
        Assert.True(result.DeliveryReady);
        Assert.False(result.DeliveryOperational);
        Assert.False(branch.DeliveryEnabled);
    }

    private static (
        OrganizationBranch Branch,
        BranchOperatingHoursSchedule Hours,
        BranchDeliveryPolicy Policy,
        DateTimeOffset Instant) CreateOtherwiseCompleteDeliverySetup()
    {
        var branch = OrganizationBranch.Create(Org, "MAIN", "Main", T0, "Line 1", city: "Manila", countryCode: "PH");
        branch.UpdateContactPhone("+63 917 000 0000", T0);
        branch.UpdateCoordinates(14.5547m, 121.0244m, T0);
        var hours = BranchOperatingHoursSchedule.Create(
            branch.Id,
            Enumerable.Range(0, 7).Select(i => BranchDayOperatingHours.Open24Hours((DayOfWeek)i)).ToList());
        var policy = BranchDeliveryPolicy.Create(
            branch.Id, Org, 0m, 49m, 2m, 10m, 15m, null, T0);
        var mondayMorning = new DateTimeOffset(2026, 8, 17, 1, 0, 0, TimeSpan.Zero);
        return (branch, hours, policy, mondayMorning);
    }
}

public sealed class BranchDeliveryServiceAreaDomainTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 31, 10, 0, 0, TimeSpan.Zero);
    private static readonly PlatformOrganizationId Org = PlatformOrganizationId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly OrganizationBranchId Branch = OrganizationBranchId.From(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));

    [Fact]
    public void Create_normalizes_country_and_city_key()
    {
        var area = BranchDeliveryServiceArea.Create(
            Org, Branch, " ph ", "  Makati   City ", T0, regionOrProvinceName: " Metro  Manila ");

        Assert.Equal("PH", area.CountryCode);
        Assert.Equal("Makati City", area.CityMunicipalityName);
        Assert.Equal("MAKATI CITY", area.NormalizedCityMunicipalityName);
        Assert.Equal("Metro Manila", area.RegionOrProvinceName);
        Assert.True(area.IsActive);
    }

    [Fact]
    public void Create_rejects_blank_city()
    {
        var ex = Assert.Throws<DomainException>(() =>
            BranchDeliveryServiceArea.Create(Org, Branch, "PH", "   ", T0));
        Assert.Equal(DomainErrorCodes.InvalidBranchDeliveryServiceArea, ex.ErrorCode);
    }

    [Fact]
    public void Create_rejects_duplicate_active_normalized_city()
    {
        var first = BranchDeliveryServiceArea.Create(Org, Branch, "PH", "Quezon City", T0);
        var ex = Assert.Throws<DomainException>(() =>
            BranchDeliveryServiceArea.Create(
                Org, Branch, "PH", "  quezon   city ", T0, existingActiveForBranch: [first]));
        Assert.Equal(DomainErrorCodes.BranchDeliveryServiceAreaDuplicate, ex.ErrorCode);
    }

    [Fact]
    public void Deactivate_frees_city_slot_for_new_active_area()
    {
        var first = BranchDeliveryServiceArea.Create(Org, Branch, "PH", "Pasig", T0);
        first.Deactivate(T0.AddMinutes(1));
        var second = BranchDeliveryServiceArea.Create(
            Org, Branch, "PH", "Pasig", T0.AddMinutes(2), existingActiveForBranch: [first]);
        Assert.True(second.IsActive);
        Assert.False(first.IsActive);
    }
}
