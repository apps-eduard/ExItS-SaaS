using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.GlobalCatalog;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.UnitTests.Catalog;

public sealed class PlanVersionBusinessTypeGrantTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 11, 12, 0, 0, TimeSpan.Zero);

    private static Plan CreatePlan() =>
        Plan.CreateDraft(
            ProductCode.Create(ProductCode.PinoyBusinessPos),
            PlanCode.Create("starter"),
            "Starter",
            T0);

    [Fact]
    public void PlanVersion_can_grant_one_business_type()
    {
        var bt = BusinessTypeId.New();
        var version = PlanVersion.CreateDraft(
            CreatePlan(),
            1,
            T0,
            BillingPeriod.Monthly,
            true,
            Array.Empty<FeatureGrantSpec>(),
            T0,
            businessTypeGrants: [bt]);

        Assert.Single(version.BusinessTypeGrants);
        Assert.Equal(bt, version.BusinessTypeGrants[0]);
    }

    [Fact]
    public void PlanVersion_can_grant_multiple_business_types()
    {
        var a = BusinessTypeId.New();
        var b = BusinessTypeId.New();
        var version = PlanVersion.CreateDraft(
            CreatePlan(),
            1,
            T0,
            BillingPeriod.Monthly,
            true,
            Array.Empty<FeatureGrantSpec>(),
            T0,
            businessTypeGrants: [a, b]);

        Assert.Equal(2, version.BusinessTypeGrants.Count);
        Assert.Contains(a, version.BusinessTypeGrants);
        Assert.Contains(b, version.BusinessTypeGrants);
    }

    [Fact]
    public void PlanVersion_rejects_duplicate_business_type_grants()
    {
        var bt = BusinessTypeId.New();
        var ex = Assert.Throws<DomainException>(() =>
            PlanVersion.CreateDraft(
                CreatePlan(),
                1,
                T0,
                BillingPeriod.Monthly,
                true,
                Array.Empty<FeatureGrantSpec>(),
                T0,
                businessTypeGrants: [bt, bt]));

        Assert.Equal(DomainErrorCodes.DuplicateBusinessTypeGrant, ex.ErrorCode);
    }

    [Fact]
    public void ReplaceDraftBusinessTypeGrants_is_immutable_after_publish()
    {
        var version = PlanVersion.CreateDraft(
            CreatePlan(),
            1,
            T0,
            BillingPeriod.Monthly,
            true,
            Array.Empty<FeatureGrantSpec>(),
            T0,
            businessTypeGrants: [BusinessTypeId.New()]);
        version.Publish(T0.AddMinutes(1));

        var ex = Assert.Throws<DomainException>(() =>
            version.ReplaceDraftBusinessTypeGrants([BusinessTypeId.New()], T0.AddMinutes(2)));
        Assert.Equal(DomainErrorCodes.PlanVersionImmutable, ex.ErrorCode);
    }

    [Fact]
    public void Existing_CreateDraft_call_sites_default_to_empty_business_type_grants()
    {
        var version = PlanVersion.CreateDraft(
            CreatePlan(),
            1,
            T0,
            BillingPeriod.Monthly,
            false,
            Array.Empty<FeatureGrantSpec>(),
            T0);

        Assert.Empty(version.BusinessTypeGrants);
    }
}

public sealed class OrganizationBusinessTypeActivationTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 11, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Organization_can_store_additional_activation()
    {
        var orgId = PlatformOrganizationId.New();
        var bt = BusinessTypeId.New();
        var activation = OrganizationBusinessTypeActivation.Activate(orgId, bt, T0);

        Assert.Equal(orgId, activation.OrganizationId);
        Assert.Equal(bt, activation.BusinessTypeId);
        Assert.Equal(T0, activation.ActivatedAtUtc);
    }

    [Fact]
    public void Duplicate_activations_in_a_set_are_rejected()
    {
        var orgId = PlatformOrganizationId.New();
        var bt = BusinessTypeId.New();
        var a = OrganizationBusinessTypeActivation.Activate(orgId, bt, T0);
        var b = OrganizationBusinessTypeActivation.Activate(orgId, bt, T0.AddMinutes(1));

        var ex = Assert.Throws<DomainException>(() =>
            OrganizationBusinessTypeActivation.EnsureUnique([a, b]));
        Assert.Equal(DomainErrorCodes.DuplicateBusinessTypeActivation, ex.ErrorCode);
    }

    [Fact]
    public void Activating_primary_business_type_is_forbidden()
    {
        var orgId = PlatformOrganizationId.New();
        var primary = BusinessTypeId.New();

        var ex = Assert.Throws<DomainException>(() =>
            OrganizationBusinessTypeActivation.Activate(orgId, primary, T0, primaryBusinessTypeId: primary));
        Assert.Equal(DomainErrorCodes.PrimaryBusinessTypeActivationForbidden, ex.ErrorCode);
    }

    [Fact]
    public void PrimaryBusinessTypeId_on_organization_remains_unchanged_by_activation_model()
    {
        var org = PlatformOrganization.Create("Acme", "acme-bt-act", T0);
        var primary = BusinessTypeId.New();
        org.AssignPrimaryBusinessType(primary, T0.AddMinutes(1));

        var addOn = BusinessTypeId.New();
        _ = OrganizationBusinessTypeActivation.Activate(org.Id, addOn, T0.AddMinutes(2), org.PrimaryBusinessTypeId);

        Assert.Equal(primary, org.PrimaryBusinessTypeId);
    }
}
