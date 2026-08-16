using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class BranchFulfillmentAndDeliveryPolicyTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);
    private static readonly PlatformOrganizationId Org = PlatformOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

    [Fact]
    public void Invalid_coordinates_are_rejected()
    {
        var branch = OrganizationBranch.Create(Org, "BGC", "BGC", T0);
        Assert.Throws<DomainException>(() => branch.UpdateCoordinates(91m, 121m, T0.AddMinutes(1)));
        Assert.Throws<DomainException>(() => branch.UpdateCoordinates(14.5m, 181m, T0.AddMinutes(1)));
        Assert.Throws<DomainException>(() => branch.UpdateCoordinates(14.5m, null, T0.AddMinutes(1)));
    }

    [Fact]
    public void Delivery_requires_coordinates_before_enable()
    {
        var branch = OrganizationBranch.Create(Org, "BGC", "BGC", T0);
        var ex = Assert.Throws<DomainException>(() => branch.SetFulfillmentCapabilities(true, true, T0.AddMinutes(1)));
        Assert.Equal(DomainErrorCodes.OrganizationBranchDeliveryLocationRequired, ex.ErrorCode);

        branch.UpdateCoordinates(14.5547m, 121.0244m, T0.AddMinutes(2));
        branch.SetFulfillmentCapabilities(true, true, T0.AddMinutes(3));
        Assert.True(branch.CanOfferDeliveryLocation);
        Assert.True(branch.CanOfferPickup);
    }

    [Fact]
    public void Archived_branch_cannot_be_used_for_new_fulfillment()
    {
        var branch = OrganizationBranch.Create(Org, "OLD", "Old", T0);
        branch.Archive(T0.AddMinutes(1));
        Assert.Throws<DomainException>(() => branch.EnsureUsableForNewFulfillment());
    }

    [Fact]
    public void Delivery_fee_within_included_distance_is_base_only()
    {
        var policy = BranchDeliveryPolicy.Create(
            OrganizationBranchId.New(), Org, 300m, 50m, 3m, 12m, 15m, 1500m, T0);
        var quote = policy.CalculateFee(merchandiseSubtotal: 400m, distanceKm: 2.5m);
        Assert.Equal(50m, quote.DeliveryFee);
        Assert.Equal(0m, quote.ExtraDistanceKm);
        Assert.False(quote.FreeDeliveryApplied);
    }

    [Fact]
    public void Delivery_fee_beyond_included_distance_adds_per_km()
    {
        var policy = BranchDeliveryPolicy.Create(
            OrganizationBranchId.New(), Org, 300m, 50m, 3m, 12m, 15m, null, T0);
        var quote = policy.CalculateFee(850m, 5m);
        Assert.Equal(2m, quote.ExtraDistanceKm);
        Assert.Equal(24m, quote.DistanceCharge);
        Assert.Equal(74m, quote.DeliveryFee);
    }

    [Fact]
    public void Free_threshold_and_min_order_and_max_distance_rules()
    {
        var policy = BranchDeliveryPolicy.Create(
            OrganizationBranchId.New(), Org, 300m, 50m, 3m, 12m, 15m, 1500m, T0);

        Assert.Throws<DomainException>(() => policy.CalculateFee(245m, 4m));
        Assert.Throws<DomainException>(() => policy.CalculateFee(400m, 16m));

        var free = policy.CalculateFee(1500m, 10m);
        Assert.True(free.FreeDeliveryApplied);
        Assert.Equal(0m, free.DeliveryFee);
    }

    [Fact]
    public void Negative_policy_amounts_are_rejected()
    {
        Assert.Throws<DomainException>(() => BranchDeliveryPolicy.Create(
            OrganizationBranchId.New(), Org, -1m, 50m, 3m, 12m, 15m, null, T0));
        Assert.Throws<DomainException>(() => BranchDeliveryPolicy.Create(
            OrganizationBranchId.New(), Org, 0m, 0m, 20m, 0m, 10m, null, T0));
    }

    [Fact]
    public void Haversine_distance_is_positive_and_rounded()
    {
        IDeliveryDistanceCalculator calc = new HaversineDeliveryDistanceCalculator();
        var km = calc.CalculateDistanceKm(14.5547m, 121.0244m, 14.5995m, 120.9842m);
        Assert.True(km > 0m);
        Assert.Equal(BranchDeliveryPolicy.RoundDistance(km), km);
    }

    [Fact]
    public void Main_branch_defaults_pickup_enabled_delivery_disabled()
    {
        var main = OrganizationBranch.CreateMainBranch(Org, T0);
        Assert.True(main.PickupEnabled);
        Assert.False(main.DeliveryEnabled);
        Assert.True(main.CanOfferPickup);
        Assert.False(main.CanOfferDeliveryLocation);
    }
}
