namespace ExItS.Platform.Domain.Organizations;

public sealed record BranchEntitlementCapabilities(
    bool CanUseCustomerOrdering,
    bool CanUseDelivery);

public sealed record BranchFulfillmentReadinessResult(
    bool CustomerOrderingReady,
    bool PickupReady,
    bool DeliveryReady,
    bool CustomerOrderingOperational,
    bool PickupOperational,
    bool DeliveryOperational,
    IReadOnlyList<string> MissingRequirements,
    IReadOnlyList<string> ReasonCodes,
    BranchStoreOpenState? StoreOpenState);

public sealed record BranchFulfillmentReadinessInput(
    OrganizationBranch Branch,
    BranchOperatingHoursSchedule? OperatingHours,
    BranchDeliveryPolicy? DeliveryPolicy,
    string? OrganizationTimeZoneId,
    string? OrganizationContactPhone,
    BranchEntitlementCapabilities Entitlements,
    DateTimeOffset EvaluationInstantUtc);

public interface IBranchFulfillmentReadinessEvaluator
{
    BranchFulfillmentReadinessResult Evaluate(BranchFulfillmentReadinessInput input);
}

public sealed class BranchFulfillmentReadinessEvaluator : IBranchFulfillmentReadinessEvaluator
{
    private readonly IBranchOperatingHoursEvaluator _hours;

    public BranchFulfillmentReadinessEvaluator(IBranchOperatingHoursEvaluator hours) => _hours = hours;

    public BranchFulfillmentReadinessResult Evaluate(BranchFulfillmentReadinessInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var branch = input.Branch;
        var missing = new List<string>();
        var reasons = new List<string>();

        var effectiveTimeZone = ResolveEffectiveTimeZone(branch, input.OrganizationTimeZoneId);
        if (string.IsNullOrWhiteSpace(effectiveTimeZone))
        {
            Add(missing, reasons, "timezone", FulfillmentReadinessReasonCodes.TimezoneMissing);
        }

        if (branch.Status != OrganizationBranchStatus.Active)
        {
            Add(missing, reasons, "branch_active", FulfillmentReadinessReasonCodes.BranchInactive);
        }

        if (!branch.HasCompleteStructuredAddress)
        {
            Add(missing, reasons, "branch_address", FulfillmentReadinessReasonCodes.BranchAddressIncomplete);
        }

        if (input.OperatingHours is null || !input.OperatingHours.IsConfigured)
        {
            Add(missing, reasons, "store_hours", FulfillmentReadinessReasonCodes.StoreHoursMissing);
        }

        var contactPhone = ResolveContactPhone(branch, input.OrganizationContactPhone);
        if (string.IsNullOrWhiteSpace(contactPhone))
        {
            Add(missing, reasons, "store_contact", FulfillmentReadinessReasonCodes.StoreContactMissing);
        }

        if (!input.Entitlements.CanUseCustomerOrdering)
        {
            Add(missing, reasons, "ordering_entitlement", FulfillmentReadinessReasonCodes.OrderingEntitlementMissing);
        }

        var orderingReady = missing.Count == 0;

        BranchStoreOpenState? openState = null;
        var isOpenNow = false;
        if (!string.IsNullOrWhiteSpace(effectiveTimeZone) && input.OperatingHours is not null)
        {
            openState = _hours.Evaluate(input.OperatingHours, effectiveTimeZone!, input.EvaluationInstantUtc);
            isOpenNow = openState.IsOpenNow;
        }

        var orderingOperational = orderingReady
                                && branch.CustomerOrderingEnabled
                                && !branch.OnlineOrdersPaused
                                && isOpenNow;

        var pickupReady = orderingReady;
        var pickupOperational = orderingOperational && branch.PickupEnabled;

        var deliveryMissing = new List<string>(missing);
        var deliveryReasons = new List<string>(reasons);

        if (!input.Entitlements.CanUseDelivery)
        {
            Add(deliveryMissing, deliveryReasons, "delivery_entitlement", FulfillmentReadinessReasonCodes.DeliveryEntitlementMissing);
        }

        if (!branch.HasValidFulfillmentCoordinates)
        {
            Add(deliveryMissing, deliveryReasons, "map_location", FulfillmentReadinessReasonCodes.MapLocationMissing);
        }

        if (input.DeliveryPolicy is null)
        {
            Add(deliveryMissing, deliveryReasons, "delivery_policy", FulfillmentReadinessReasonCodes.DeliveryPolicyMissing);
        }
        else if (!IsDeliveryPolicyComplete(input.DeliveryPolicy))
        {
            Add(deliveryMissing, deliveryReasons, "delivery_policy", FulfillmentReadinessReasonCodes.DeliveryPolicyIncomplete);
        }

        var deliveryReady = deliveryMissing.Count == 0;
        var deliveryOperational = deliveryReady
                                  && branch.DeliveryEnabled
                                  && orderingOperational;

        if (!branch.CustomerOrderingEnabled)
        {
            reasons.Add(FulfillmentReadinessReasonCodes.CustomerOrderingDisabled);
        }

        if (!branch.PickupEnabled)
        {
            reasons.Add(FulfillmentReadinessReasonCodes.PickupDisabled);
        }

        if (!branch.DeliveryEnabled)
        {
            deliveryReasons.Add(FulfillmentReadinessReasonCodes.DeliveryDisabled);
        }

        if (branch.OnlineOrdersPaused)
        {
            reasons.Add(FulfillmentReadinessReasonCodes.OnlineOrdersPaused);
            deliveryReasons.Add(FulfillmentReadinessReasonCodes.OnlineOrdersPaused);
        }

        if (!isOpenNow && orderingReady)
        {
            reasons.Add(FulfillmentReadinessReasonCodes.StoreClosed);
            deliveryReasons.Add(FulfillmentReadinessReasonCodes.StoreClosed);
        }

        return new BranchFulfillmentReadinessResult(
            orderingReady,
            pickupReady,
            deliveryReady,
            orderingOperational,
            pickupOperational,
            deliveryOperational,
            missing.Concat(deliveryMissing.Where(x => !missing.Contains(x))).Distinct(StringComparer.Ordinal).ToList(),
            reasons.Concat(deliveryReasons).Distinct(StringComparer.Ordinal).ToList(),
            openState);
    }

    private static bool IsDeliveryPolicyComplete(BranchDeliveryPolicy policy) =>
        policy.MaximumDeliveryDistanceKm > 0m
        && policy.BaseDeliveryFee >= 0m
        && policy.IncludedDistanceKm >= 0m
        && policy.AdditionalFeePerKm >= 0m
        && policy.MinimumOrderAmount >= 0m;

    private static string? ResolveEffectiveTimeZone(OrganizationBranch branch, string? organizationTimeZoneId) =>
        string.IsNullOrWhiteSpace(branch.TimeZoneId) ? organizationTimeZoneId : branch.TimeZoneId;

    private static string? ResolveContactPhone(OrganizationBranch branch, string? organizationContactPhone) =>
        string.IsNullOrWhiteSpace(branch.ContactPhone) ? organizationContactPhone : branch.ContactPhone;

    private static void Add(List<string> missing, List<string> reasons, string requirement, string reasonCode)
    {
        missing.Add(requirement);
        reasons.Add(reasonCode);
    }
}
