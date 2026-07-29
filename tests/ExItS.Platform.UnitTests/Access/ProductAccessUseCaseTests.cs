using ExItS.Platform.Application.Access;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Entitlements;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;
using ExItS.Platform.UnitTests.Support;

namespace ExItS.Platform.UnitTests.Access;

public sealed class ProductAccessUseCaseTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ProductAccessAssignment_grant_and_revoke_preserve_history()
    {
        var userId = PlatformUserId.New();
        var orgId = PlatformOrganizationId.New();
        var membershipId = OrganizationMembershipId.New();
        var code = ProductCode.Create(ProductCode.PinoyBusinessPos);

        var assignment = ProductAccessAssignment.Grant(userId, orgId, membershipId, code, "dev-admin", T0, "initial");
        Assert.Equal(ProductAccessStatus.Active, assignment.Status);

        assignment.Revoke("dev-admin", "revoked", T0.AddMinutes(1));
        Assert.Equal(ProductAccessStatus.Revoked, assignment.Status);
        Assert.Equal(T0.AddMinutes(1), assignment.RevokedAtUtc);
        Assert.Equal("dev-admin", assignment.RevokedByActor);
        Assert.Equal(T0, assignment.GrantedAtUtc);
    }

    [Fact]
    public void ProductAccessEligibility_allows_only_trialing_and_active()
    {
        Assert.True(ProductAccessEligibility.IsSubscriptionEligible(SubscriptionStatus.Trialing));
        Assert.True(ProductAccessEligibility.IsSubscriptionEligible(SubscriptionStatus.Active));
        Assert.False(ProductAccessEligibility.IsSubscriptionEligible(SubscriptionStatus.GracePeriod));
        Assert.False(ProductAccessEligibility.IsSubscriptionEligible(SubscriptionStatus.PastDue));
        Assert.False(ProductAccessEligibility.IsSubscriptionEligible(SubscriptionStatus.Suspended));
    }

    [Fact]
    public async Task Grant_requires_active_membership_and_eligible_subscription_snapshot()
    {
        var harness = await AccessHarness.CreateAsync();
        var grant = harness.Grant;

        var missingMembership = await grant.ExecuteAsync(
            harness.Organization.Id,
            PlatformUserId.New(),
            harness.Product.Code.Value,
            "dev-admin");
        Assert.Equal(ApplicationErrorCodes.UserNotFound, missingMembership.ErrorCode);

        var inactiveUser = PlatformUser.Create("inactive1", "Inactive User", "inactive@example.com", T0);
        inactiveUser.Suspend(T0.AddMinutes(1), "hold");
        await harness.Users.AddAsync(inactiveUser);
        var inactive = await grant.ExecuteAsync(
            harness.Organization.Id,
            inactiveUser.Id,
            harness.Product.Code.Value,
            "dev-admin");
        Assert.Equal(DomainErrorCodes.UserNotActive, inactive.ErrorCode);
        Assert.Equal(0, harness.Assignments.AddCount);
    }

    [Fact]
    public async Task Grant_revoke_and_evaluate_effective_access()
    {
        var harness = await AccessHarness.CreateAsync();

        var granted = await harness.Grant.ExecuteAsync(
            harness.Organization.Id,
            harness.User.Id,
            harness.Product.Code.Value,
            "dev-admin",
            "commercial access");
        Assert.True(granted.IsSuccess);
        Assert.Equal(1, harness.Assignments.AddCount);

        var duplicate = await harness.Grant.ExecuteAsync(
            harness.Organization.Id,
            harness.User.Id,
            harness.Product.Code.Value,
            "dev-admin");
        Assert.Equal(ApplicationErrorCodes.ProductAccessConflict, duplicate.ErrorCode);

        var allowed = await harness.Evaluate.ExecuteAsync(
            harness.User.Id,
            harness.Organization.Id,
            harness.Product.Code.Value);
        Assert.True(allowed.Allowed);
        Assert.Equal(EffectiveAccessReasonCodes.Allowed, allowed.ReasonCode);

        harness.Clock.UtcNow = T0.AddMinutes(5);
        var revoked = await harness.Revoke.ExecuteAsync(granted.Value!.Id, "dev-admin", "cleanup");
        Assert.True(revoked.IsSuccess);

        var denied = await harness.Evaluate.ExecuteAsync(
            harness.User.Id,
            harness.Organization.Id,
            harness.Product.Code.Value);
        Assert.False(denied.Allowed);
        Assert.Equal(EffectiveAccessReasonCodes.ProductAssignmentMissing, denied.ReasonCode);
    }

    [Fact]
    public async Task Evaluate_denies_when_membership_suspended_even_if_assignment_active()
    {
        var harness = await AccessHarness.CreateAsync();
        Assert.True((await harness.Grant.ExecuteAsync(
            harness.Organization.Id,
            harness.User.Id,
            harness.Product.Code.Value,
            "dev-admin")).IsSuccess);

        harness.Clock.UtcNow = T0.AddMinutes(2);
        Assert.True((await new SuspendOrganizationMembership(harness.Memberships, harness.UnitOfWork, harness.Clock)
            .ExecuteAsync(harness.Membership.Id)).IsSuccess);

        var denied = await harness.Evaluate.ExecuteAsync(
            harness.User.Id,
            harness.Organization.Id,
            harness.Product.Code.Value);
        Assert.False(denied.Allowed);
        Assert.Equal(EffectiveAccessReasonCodes.MembershipInactive, denied.ReasonCode);
    }

    [Fact]
    public async Task Grant_fails_closed_for_grace_period_subscription()
    {
        var harness = await AccessHarness.CreateAsync(activateThenGrace: true);
        var result = await harness.Grant.ExecuteAsync(
            harness.Organization.Id,
            harness.User.Id,
            harness.Product.Code.Value,
            "dev-admin");
        Assert.Equal(ApplicationErrorCodes.SubscriptionIneligible, result.ErrorCode);
        Assert.Equal(0, harness.Assignments.AddCount);
    }

    [Fact]
    public async Task Revoke_membership_cascades_product_access_revocation()
    {
        var harness = await AccessHarness.CreateAsync();
        Assert.True((await harness.Grant.ExecuteAsync(
            harness.Organization.Id,
            harness.User.Id,
            harness.Product.Code.Value,
            "dev-admin")).IsSuccess);

        harness.Clock.UtcNow = T0.AddMinutes(3);
        var revokeMembership = new RevokeOrganizationMembership(
            harness.Memberships,
            harness.Assignments,
            harness.UnitOfWork,
            harness.Clock);
        Assert.True((await revokeMembership.ExecuteAsync(harness.Membership.Id, "left org", "dev-admin")).IsSuccess);

        var assignment = (await harness.Assignments.ListByOrganizationAsync(
            harness.Organization.Id, null, 0, 10)).Items.Single();
        Assert.Equal(ProductAccessStatus.Revoked, assignment.Status);
    }

    private sealed class AccessHarness
    {
        public required InMemoryPlatformUserRepository Users { get; init; }
        public required InMemoryPlatformOrganizationRepository Organizations { get; init; }
        public required InMemoryOrganizationMembershipRepository Memberships { get; init; }
        public required InMemoryProductRepository Products { get; init; }
        public required InMemorySubscriptionRepository Subscriptions { get; init; }
        public required InMemoryEntitlementSnapshotRepository Snapshots { get; init; }
        public required InMemoryProductAccessAssignmentRepository Assignments { get; init; }
        public required NoOpUnitOfWork UnitOfWork { get; init; }
        public required FixedClock Clock { get; init; }
        public required PlatformUser User { get; init; }
        public required PlatformOrganization Organization { get; init; }
        public required OrganizationMembership Membership { get; init; }
        public required Product Product { get; init; }
        public required GrantProductAccess Grant { get; init; }
        public required RevokeProductAccess Revoke { get; init; }
        public required EvaluateEffectiveProductAccess Evaluate { get; init; }

        public static async Task<AccessHarness> CreateAsync(bool activateThenGrace = false)
        {
            var clock = new FixedClock(T0);
            var uow = new NoOpUnitOfWork();
            var users = new InMemoryPlatformUserRepository();
            var orgs = new InMemoryPlatformOrganizationRepository();
            var memberships = new InMemoryOrganizationMembershipRepository();
            var products = new InMemoryProductRepository();
            var subscriptions = new InMemorySubscriptionRepository();
            var snapshots = new InMemoryEntitlementSnapshotRepository();
            var assignments = new InMemoryProductAccessAssignmentRepository();

            var user = (await new CreatePlatformUser(users, uow, clock)
                .ExecuteAsync("ada", "Ada Lovelace", "ada@example.com")).Value!;
            var org = (await new CreatePlatformOrganization(orgs, uow, clock)
                .ExecuteAsync("Acme Group", "acme-access")).Value!;
            var membership = (await new AddOrganizationMembership(users, orgs, memberships, uow, clock)
                .ExecuteAsync(org.Id, user.Id, OrganizationRole.OrganizationMember)).Value!;

            var product = Product.Create(ProductCode.Create(ProductCode.PinoyBusinessPos), "PinoyBusinessPOS", T0);
            await products.AddAsync(product);

            var plan = Plan.CreateDraft(product.Code, PlanCode.Create("utang-trial"), "Utang Trial", T0);
            plan.Activate(T0);
            var version = PlanVersion.CreateDraft(plan, 1, T0, BillingPeriod.None, true, UtangTrialTestFactory.ActiveGrants(), T0);
            version.Publish(T0);
            var trial = UtangTrialTestFactory.CreateConfigured(T0, TimeSpan.FromDays(14), plan.Id);
            var subscription = Subscription.StartTrial(org.Id, plan, version, trial, T0);
            if (activateThenGrace)
            {
                subscription.ActivateFromTrial(T0, T0.AddDays(30), T0.AddMinutes(1));
                subscription.EnterGracePeriod(T0.AddDays(30), T0.AddMinutes(2));
            }

            await subscriptions.AddAsync(subscription);

            var snapshot = EntitlementSnapshot.Create(
                org.Id,
                product.Code,
                subscription.Id,
                plan.Code,
                version.VersionNumber,
                snapshotVersion: 1,
                subscription.Status,
                inGracePeriod: subscription.Status == SubscriptionStatus.GracePeriod,
                generatedAtUtc: T0,
                effectiveAtUtc: T0,
                refreshByUtc: T0.AddDays(7),
                sourceAggregateVersion: subscription.Version,
                grants: [new EntitlementGrant(
                    FeatureCode.Create(FeatureCode.CustomerCreditView),
                    enabled: true,
                    EntitlementGrantSource.Plan,
                    T0)]);
            await snapshots.AddAsync(snapshot);

            var grant = new GrantProductAccess(
                users, orgs, memberships, products, subscriptions, snapshots, assignments, uow, clock);
            var revoke = new RevokeProductAccess(assignments, uow, clock);
            var evaluate = new EvaluateEffectiveProductAccess(
                users, orgs, memberships, products, assignments, subscriptions, snapshots, clock);

            return new AccessHarness
            {
                Users = users,
                Organizations = orgs,
                Memberships = memberships,
                Products = products,
                Subscriptions = subscriptions,
                Snapshots = snapshots,
                Assignments = assignments,
                UnitOfWork = uow,
                Clock = clock,
                User = user,
                Organization = org,
                Membership = membership,
                Product = product,
                Grant = grant,
                Revoke = revoke,
                Evaluate = evaluate
            };
        }
    }
}
