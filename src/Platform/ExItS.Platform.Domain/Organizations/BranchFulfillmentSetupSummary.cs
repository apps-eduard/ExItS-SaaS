namespace ExItS.Platform.Domain.Organizations;

/// <summary>
/// Setup checklist completeness for branch fulfillment (independent of entitlement / operational toggles).
/// Pickup requires 2 sections; delivery requires 5.
/// </summary>
public sealed record BranchFulfillmentSetupSummary(
    bool BranchDetailsComplete,
    bool OperatingHoursComplete,
    bool DeliveryLocationComplete,
    bool DeliveryPolicyComplete,
    bool DeliveryAreasComplete,
    int PickupSectionsComplete,
    int PickupSectionsTotal,
    int DeliverySectionsComplete,
    int DeliverySectionsTotal)
{
    public const int PickupSectionCount = 2;
    public const int DeliverySectionCount = 5;

    public static BranchFulfillmentSetupSummary Compute(BranchFulfillmentReadinessInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var branch = input.Branch;

        var timezoneResolved = !string.IsNullOrWhiteSpace(
            string.IsNullOrWhiteSpace(branch.TimeZoneId) ? input.OrganizationTimeZoneId : branch.TimeZoneId);
        var contactResolved = !string.IsNullOrWhiteSpace(
            string.IsNullOrWhiteSpace(branch.ContactPhone) ? input.OrganizationContactPhone : branch.ContactPhone);

        var branchDetailsComplete = branch.HasCompleteStructuredAddress && contactResolved && timezoneResolved;
        var operatingHoursComplete = input.OperatingHours is not null && input.OperatingHours.IsConfigured;
        var deliveryLocationComplete = branch.HasValidFulfillmentCoordinates;
        var deliveryPolicyComplete = input.DeliveryPolicy is not null
                                     && IsDeliveryPolicyComplete(input.DeliveryPolicy);
        var deliveryAreasComplete = input.HasActiveDeliveryServiceArea;

        var pickupComplete = (branchDetailsComplete ? 1 : 0) + (operatingHoursComplete ? 1 : 0);
        var deliveryComplete = pickupComplete
                               + (deliveryLocationComplete ? 1 : 0)
                               + (deliveryPolicyComplete ? 1 : 0)
                               + (deliveryAreasComplete ? 1 : 0);

        return new(
            branchDetailsComplete,
            operatingHoursComplete,
            deliveryLocationComplete,
            deliveryPolicyComplete,
            deliveryAreasComplete,
            pickupComplete,
            PickupSectionCount,
            deliveryComplete,
            DeliverySectionCount);
    }

    /// <summary>Same completeness rule used by <see cref="BranchFulfillmentReadinessEvaluator"/>.</summary>
    internal static bool IsDeliveryPolicyComplete(BranchDeliveryPolicy policy) =>
        policy.MaximumDeliveryDistanceKm > 0m
        && policy.BaseDeliveryFee >= 0m
        && policy.IncludedDistanceKm >= 0m
        && policy.AdditionalFeePerKm >= 0m
        && policy.MinimumOrderAmount >= 0m;
}
