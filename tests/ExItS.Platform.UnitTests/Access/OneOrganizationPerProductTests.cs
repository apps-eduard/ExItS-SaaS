using ExItS.Platform.Application.Access;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Payments;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Entitlements;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;
using ExItS.Platform.UnitTests.Support;

namespace ExItS.Platform.UnitTests.Access;

public sealed class OneOrganizationPerProductTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 5, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Owner_pos_affiliation_blocks_a_second_pos_organization()
    {
        var userId = PlatformUserId.New();
        var orgA = PlatformOrganizationId.New();
        var orgB = PlatformOrganizationId.New();
        var guard = GuardWithAccess(userId, orgA, ProductCode.PinoyBusinessPos, OrganizationRole.OrganizationOwner);

        var found = await guard.FindAsync(userId, ProductCode.Create(ProductCode.PinoyBusinessPos));

        Assert.NotNull(found);
        Assert.Equal(orgA.Value, found.OrganizationId);
        Assert.True(await guard.ConflictsWithOtherOrganizationAsync(
            [userId],
            ProductCode.Create(ProductCode.PinoyBusinessPos),
            orgB.Value));
    }

    [Fact]
    public async Task Staff_pos_affiliation_blocks_joining_another_pos_organization()
    {
        var userId = PlatformUserId.New();
        var orgA = PlatformOrganizationId.New();
        var orgB = PlatformOrganizationId.New();
        var guard = GuardWithAccess(userId, orgA, ProductCode.PinoyBusinessPos, OrganizationRole.OrganizationMember);

        Assert.True(await guard.ConflictsWithOtherOrganizationAsync(
            [userId],
            ProductCode.Create(ProductCode.PinoyBusinessPos),
            orgB.Value));
        Assert.DoesNotContain(orgA.Value.ToString("D"), UserProductAffiliationGuard.AlreadyAssociatedMessage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ABC Grocery", UserProductAffiliationGuard.AlreadyAssociatedMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Pos_affiliation_does_not_block_another_product()
    {
        var userId = PlatformUserId.New();
        var guard = GuardWithAccess(
            userId,
            PlatformOrganizationId.New(),
            ProductCode.PinoyBusinessPos,
            OrganizationRole.OrganizationOwner);

        var loan = await guard.FindAsync(userId, ProductCode.Create(ProductCode.PinoyLoanManager));
        var service = await guard.FindAsync(userId, ProductCode.Create(ProductCode.PinoyServicePro));

        Assert.Null(loan);
        Assert.Null(service);
    }

    [Fact]
    public async Task Personal_workspace_without_membership_is_not_an_affiliation()
    {
        var guard = new UserProductAffiliationGuard(
            new InMemoryProductAccessAssignmentRepository(),
            new InMemoryOrganizationMembershipRepository(),
            new InMemorySubscriptionRepository());

        var found = await guard.FindAsync(PlatformUserId.New(), ProductCode.Create(ProductCode.PinoyBusinessPos));

        Assert.Null(found);
    }

    [Fact]
    public async Task Removed_membership_allows_a_later_affiliation()
    {
        var userId = PlatformUserId.New();
        var orgA = PlatformOrganizationId.New();
        var memberships = new InMemoryOrganizationMembershipRepository();
        var access = new InMemoryProductAccessAssignmentRepository();
        var membership = OrganizationMembership.Create(orgA, userId, OrganizationRole.OrganizationMember, T0);
        membership.Remove(T0.AddMinutes(1), "ended");
        await memberships.AddAsync(membership);
        var assignment = ProductAccessAssignment.Grant(
            userId,
            orgA,
            membership.Id,
            ProductCode.Create(ProductCode.PinoyBusinessPos),
            "owner",
            T0,
            null);
        assignment.Revoke("owner", "ended", T0.AddMinutes(1));
        await access.AddAsync(assignment);
        var guard = new UserProductAffiliationGuard(access, memberships, new InMemorySubscriptionRepository());

        Assert.Null(await guard.FindAsync(userId, ProductCode.Create(ProductCode.PinoyBusinessPos)));
        Assert.Equal(membership.Id, assignment.MembershipId);
        Assert.Equal(ProductAccessStatus.Revoked, assignment.Status);
        Assert.Equal(MembershipStatus.Removed, membership.Status);
    }

    [Fact]
    public async Task Same_organization_is_not_a_conflict()
    {
        var userId = PlatformUserId.New();
        var orgA = PlatformOrganizationId.New();
        var guard = GuardWithAccess(userId, orgA, ProductCode.PinoyBusinessPos, OrganizationRole.OrganizationOwner);

        Assert.False(await guard.ConflictsWithOtherOrganizationAsync(
            [userId],
            ProductCode.Create(ProductCode.PinoyBusinessPos),
            orgA.Value));
    }

    [Fact]
    public void Affiliation_lock_keys_are_stable_per_product_and_email()
    {
        var pos = ProductAffiliationLocks.ForProduct(ProductCode.PinoyBusinessPos);
        Assert.Equal(pos, ProductAffiliationLocks.ForProduct("  " + ProductCode.PinoyBusinessPos.ToUpperInvariant()));
        Assert.NotEqual(pos, ProductAffiliationLocks.ForProduct(ProductCode.PinoyLoanManager));
        Assert.Equal(
            ProductAffiliationLocks.ForEmail("Person@Example.com"),
            ProductAffiliationLocks.ForEmail("person@example.com"));
    }

    [Fact]
    public async Task Staff_below_the_entitlement_limit_can_be_added()
    {
        var decision = await SeatsAsync(activeNonOwners: 1, ownerCount: 1, limit: 2);

        Assert.Equal(1, decision.ActiveStaffCount);
        Assert.False(decision.IsFull);
        Assert.False(decision.OverLimit);
    }

    [Fact]
    public async Task Staff_at_the_entitlement_limit_cannot_be_added_and_owners_are_excluded()
    {
        var decision = await SeatsAsync(activeNonOwners: 2, ownerCount: 1, limit: 2);

        Assert.Equal(2, decision.ActiveStaffCount);
        Assert.True(decision.IsFull);
    }

    [Fact]
    public async Task Missing_staff_entitlement_is_unlimited()
    {
        var orgId = PlatformOrganizationId.New();
        var memberships = new InMemoryOrganizationMembershipRepository();
        await memberships.AddAsync(OrganizationMembership.Create(orgId, PlatformUserId.New(), OrganizationRole.OrganizationMember, T0));
        var policy = new OrganizationStaffSeatPolicy(memberships, new InMemoryEntitlementSnapshotRepository());

        var decision = await policy.EvaluateAsync(orgId, ProductCode.Create(ProductCode.PinoyBusinessPos));

        Assert.Null(decision.StaffLimit);
        Assert.False(decision.IsFull);
    }

    [Fact]
    public async Task Downgrade_below_current_usage_does_not_remove_memberships()
    {
        var orgId = PlatformOrganizationId.New();
        var memberships = new InMemoryOrganizationMembershipRepository();
        await memberships.AddAsync(OrganizationMembership.Create(orgId, PlatformUserId.New(), OrganizationRole.OrganizationOwner, T0));
        await memberships.AddAsync(OrganizationMembership.Create(orgId, PlatformUserId.New(), OrganizationRole.OrganizationMember, T0));
        await memberships.AddAsync(OrganizationMembership.Create(orgId, PlatformUserId.New(), OrganizationRole.OrganizationMember, T0));
        var snapshots = new InMemoryEntitlementSnapshotRepository();
        await snapshots.AddAsync(Snapshot(orgId, limit: 1));
        var policy = new OrganizationStaffSeatPolicy(memberships, snapshots);

        var decision = await policy.EvaluateAsync(orgId, ProductCode.Create(ProductCode.PinoyBusinessPos));
        var remaining = await memberships.ListByOrganizationAsync(orgId, status: null, skip: 0, take: 20);

        Assert.True(decision.OverLimit);
        Assert.True(decision.IsFull);
        Assert.Equal(3, remaining.TotalCount);
    }

    [Fact]
    public async Task Active_subscription_blocks_another_checkout_and_reuses_the_organization()
    {
        var userId = PlatformUserId.New();
        var orgId = PlatformOrganizationId.New();
        var subscriptions = new InMemorySubscriptionRepository();
        var (plan, version, trial) = TrialCatalog();
        var subscription = Subscription.StartTrial(orgId, plan, version, trial, T0);
        await subscriptions.AddAsync(subscription);
        var guard = GuardWithAccess(userId, orgId, ProductCode.PinoyBusinessPos, OrganizationRole.OrganizationOwner, subscriptions);
        var checkout = new OrganizationProductCheckoutGuard(subscriptions, guard);

        var blocked = await checkout.ResolveAsync(userId, ProductCode.Create(ProductCode.PinoyBusinessPos), null);

        Assert.True(blocked.IsBlocked);
        Assert.Equal(ApplicationErrorCodes.ActiveSubscriptionConflict, blocked.ErrorCode);
        Assert.DoesNotContain(orgId.Value.ToString(), blocked.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Cancelled_subscription_reuses_the_same_organization_for_checkout()
    {
        var userId = PlatformUserId.New();
        var orgId = PlatformOrganizationId.New();
        var subscriptions = new InMemorySubscriptionRepository();
        var (plan, version, trial) = TrialCatalog();
        var subscription = Subscription.StartTrial(orgId, plan, version, trial, T0);
        subscription.ActivateFromTrial(T0, T0.AddDays(30), T0.AddMinutes(1));
        subscription.Cancel(T0.AddMinutes(2));
        await subscriptions.AddAsync(subscription);
        var guard = GuardWithAccess(userId, orgId, ProductCode.PinoyBusinessPos, OrganizationRole.OrganizationOwner, subscriptions);
        var checkout = new OrganizationProductCheckoutGuard(subscriptions, guard);

        var decision = await checkout.ResolveAsync(userId, ProductCode.Create(ProductCode.PinoyBusinessPos), null);

        Assert.False(decision.IsBlocked);
        Assert.Equal(orgId, decision.OrganizationId);
    }

    [Fact]
    public async Task Client_organization_id_cannot_bypass_membership()
    {
        var userId = PlatformUserId.New();
        var ownOrg = PlatformOrganizationId.New();
        var otherOrg = PlatformOrganizationId.New();
        var guard = GuardWithAccess(userId, ownOrg, ProductCode.PinoyBusinessPos, OrganizationRole.OrganizationOwner);
        var checkout = new OrganizationProductCheckoutGuard(new InMemorySubscriptionRepository(), guard);

        var decision = await checkout.ResolveAsync(
            userId,
            ProductCode.Create(ProductCode.PinoyBusinessPos),
            otherOrg);

        Assert.True(decision.IsBlocked);
        Assert.DoesNotContain("Grocery", decision.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Staff_without_billing_permission_cannot_start_checkout()
    {
        var userId = PlatformUserId.New();
        var orgId = PlatformOrganizationId.New();
        var guard = GuardWithAccess(userId, orgId, ProductCode.PinoyBusinessPos, OrganizationRole.OrganizationMember);
        var checkout = new OrganizationProductCheckoutGuard(new InMemorySubscriptionRepository(), guard);

        var decision = await checkout.ResolveAsync(userId, ProductCode.Create(ProductCode.PinoyBusinessPos), orgId);

        Assert.True(decision.IsBlocked);
        Assert.Equal(OrganizationProductCheckoutGuard.ManagedByOrganizationMessage, decision.ErrorMessage);
    }

    private static UserProductAffiliationGuard GuardWithAccess(
        PlatformUserId userId,
        PlatformOrganizationId organizationId,
        string productCode,
        OrganizationRole role,
        InMemorySubscriptionRepository? subscriptions = null)
    {
        var memberships = new InMemoryOrganizationMembershipRepository();
        var membership = OrganizationMembership.Create(organizationId, userId, role, T0);
        memberships.AddAsync(membership).GetAwaiter().GetResult();
        var access = new InMemoryProductAccessAssignmentRepository();
        access.AddAsync(ProductAccessAssignment.Grant(
            userId,
            organizationId,
            membership.Id,
            ProductCode.Create(productCode),
            "test",
            T0,
            null)).GetAwaiter().GetResult();
        return new UserProductAffiliationGuard(
            access,
            memberships,
            subscriptions ?? new InMemorySubscriptionRepository());
    }

    private static async Task<StaffSeatDecision> SeatsAsync(int activeNonOwners, int ownerCount, int? limit)
    {
        var orgId = PlatformOrganizationId.New();
        var memberships = new InMemoryOrganizationMembershipRepository();
        for (var i = 0; i < ownerCount; i++)
        {
            await memberships.AddAsync(OrganizationMembership.Create(
                orgId,
                PlatformUserId.New(),
                OrganizationRole.OrganizationOwner,
                T0));
        }

        for (var i = 0; i < activeNonOwners; i++)
        {
            await memberships.AddAsync(OrganizationMembership.Create(
                orgId,
                PlatformUserId.New(),
                OrganizationRole.OrganizationMember,
                T0));
        }

        var snapshots = new InMemoryEntitlementSnapshotRepository();
        if (limit is not null)
        {
            await snapshots.AddAsync(Snapshot(orgId, limit.Value));
        }

        var policy = new OrganizationStaffSeatPolicy(memberships, snapshots);
        return await policy.EvaluateAsync(orgId, ProductCode.Create(ProductCode.PinoyBusinessPos));
    }

    private static EntitlementSnapshot Snapshot(PlatformOrganizationId organizationId, int limit) =>
        EntitlementSnapshot.Create(
            organizationId,
            ProductCode.Create(ProductCode.PinoyBusinessPos),
            SubscriptionId.New(),
            PlanCode.Create("pos"),
            planVersionNumber: 1,
            snapshotVersion: 1,
            SubscriptionStatus.Active,
            inGracePeriod: false,
            T0,
            T0,
            T0.AddDays(30),
            sourceAggregateVersion: 1,
            [
                new EntitlementGrant(
                    FeatureCode.Create(FeatureCode.PlanMaxActiveStaff),
                    enabled: true,
                    EntitlementGrantSource.Plan,
                    T0,
                    numericLimit: limit)
            ]);

    private static (Plan Plan, PlanVersion Version, TrialDefinition Trial) TrialCatalog()
    {
        var plan = Plan.CreateDraft(
            ProductCode.Create(ProductCode.PinoyBusinessPos),
            PlanCode.Create("pos"),
            "POS",
            T0);
        plan.Activate(T0);
        var version = PlanVersion.CreateDraft(plan, 1, T0, BillingPeriod.Monthly, true, [], T0);
        version.Publish(T0);
        var trial = TrialDefinition.Create(
            plan.ProductCode,
            "Trial",
            TimeSpan.FromDays(14),
            [],
            [],
            T0,
            planId: plan.Id);
        return (plan, version, trial);
    }
}
