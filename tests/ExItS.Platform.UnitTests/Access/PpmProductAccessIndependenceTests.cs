using ExItS.Platform.Application.Access;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Entitlements;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Entitlements;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;
using ExItS.Platform.UnitTests.Support;
using ExItS.Platform.UnitTests.TestSupport;

namespace ExItS.Platform.UnitTests.Access;

public sealed class PpmProductAccessIndependenceTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 27, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Pos_and_plm_access_do_not_grant_ppm()
    {
        var harness = await Harness.CreateAsync();

        Assert.True((await harness.Grant.ExecuteAsync(
            harness.Organization.Id,
            harness.User.Id,
            ProductCode.PinoyBusinessPos,
            "dev-admin")).IsSuccess);
        Assert.True((await harness.Grant.ExecuteAsync(
            harness.Organization.Id,
            harness.User.Id,
            ProductCode.PinoyLoanManager,
            "dev-admin")).IsSuccess);

        var pos = await harness.Evaluate.ExecuteAsync(
            harness.User.Id, harness.Organization.Id, ProductCode.PinoyBusinessPos);
        var plm = await harness.Evaluate.ExecuteAsync(
            harness.User.Id, harness.Organization.Id, ProductCode.PinoyLoanManager);
        var ppm = await harness.Evaluate.ExecuteAsync(
            harness.User.Id, harness.Organization.Id, ProductCode.PinoyPawnManager);

        Assert.True(pos.Allowed);
        Assert.True(plm.Allowed);
        Assert.False(ppm.Allowed);
        Assert.Equal(EffectiveAccessReasonCodes.ProductAssignmentMissing, ppm.ReasonCode);
    }

    [Fact]
    public async Task Ppm_access_is_recognized_independently_of_pos_plm_and_bnpl()
    {
        var harness = await Harness.CreateAsync();

        Assert.True((await harness.Grant.ExecuteAsync(
            harness.Organization.Id,
            harness.User.Id,
            ProductCode.PinoyPawnManager,
            "dev-admin")).IsSuccess);

        var ppm = await harness.Evaluate.ExecuteAsync(
            harness.User.Id, harness.Organization.Id, ProductCode.PinoyPawnManager);
        var pos = await harness.Evaluate.ExecuteAsync(
            harness.User.Id, harness.Organization.Id, ProductCode.PinoyBusinessPos);
        var plm = await harness.Evaluate.ExecuteAsync(
            harness.User.Id, harness.Organization.Id, ProductCode.PinoyLoanManager);
        var bnpl = await harness.Evaluate.ExecuteAsync(
            harness.User.Id, harness.Organization.Id, "pinoy-buy-now-pay-later");

        Assert.True(ppm.Allowed);
        Assert.Equal(EffectiveAccessReasonCodes.Allowed, ppm.ReasonCode);
        Assert.False(pos.Allowed);
        Assert.Equal(EffectiveAccessReasonCodes.ProductAssignmentMissing, pos.ReasonCode);
        Assert.False(plm.Allowed);
        Assert.Equal(EffectiveAccessReasonCodes.ProductAssignmentMissing, plm.ReasonCode);
        Assert.False(bnpl.Allowed);
    }

    private sealed class Harness
    {
        public required GrantProductAccess Grant { get; init; }
        public required EvaluateEffectiveProductAccess Evaluate { get; init; }
        public required PlatformUser User { get; init; }
        public required PlatformOrganization Organization { get; init; }

        public static async Task<Harness> CreateAsync()
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
            var plans = new InMemoryPlanRepository();
            var trials = new InMemoryTrialDefinitionRepository();
            var overrides = new InMemoryFeatureOverrideRepository();

            var org = (await new CreatePlatformOrganization(orgs, new FakePublicOrganizationIdGenerator(), uow, clock)
                .ExecuteAsync("Independence Org", "independence-org")).Value!;
            var user = PlatformUser.CreateOrganizationStaff(
                "ppm_staff",
                $"ppm@{org.PublicOrganizationId}",
                "ppm.staff@example.com",
                org.Id,
                "PPM Staff",
                T0);
            await users.AddAsync(user);
            _ = (await new AddOrganizationMembership(
                    users,
                    orgs,
                    memberships,
                    new InMemoryOrganizationMembershipBranchAssignmentRepository(),
                    new EnsureAccountProfilesForUser(
                        new InMemoryAccountProfileRepository(),
                        new InMemoryPlatformRoleAssignmentRepository(),
                        memberships,
                        uow,
                        clock),
                    uow,
                    clock)
                .ExecuteAsync(org.Id, user.Id, OrganizationRole.OrganizationMember)).Value!;

            await SeedProductAsync(
                products, plans, trials, subscriptions, snapshots, org.Id,
                ProductCode.PinoyBusinessPos, "Pinoy Business POS", "pos-independence",
                UtangTrialTestFactory.ActiveGrants());
            await SeedProductAsync(
                products, plans, trials, subscriptions, snapshots, org.Id,
                ProductCode.PinoyLoanManager, "Pinoy Loan Manager", "plm-independence",
                []);
            await SeedProductAsync(
                products, plans, trials, subscriptions, snapshots, org.Id,
                ProductCode.PinoyPawnManager, "Pinoy Pawn Manager", "ppm-independence",
                []);

            var generateSnapshot = new GenerateEntitlementSnapshot(
                subscriptions, plans, trials, overrides, snapshots, new ProvisionalEntitlementRefreshPolicy(), uow, clock);
            var grant = new GrantProductAccess(
                users, orgs, memberships, products, subscriptions, snapshots, assignments, uow, clock);
            var evaluate = new EvaluateEffectiveProductAccess(
                users, orgs, memberships, products, assignments, subscriptions, snapshots, generateSnapshot, clock);

            return new Harness
            {
                Grant = grant,
                Evaluate = evaluate,
                User = user,
                Organization = org
            };
        }

        private static async Task SeedProductAsync(
            InMemoryProductRepository products,
            InMemoryPlanRepository plans,
            InMemoryTrialDefinitionRepository trials,
            InMemorySubscriptionRepository subscriptions,
            InMemoryEntitlementSnapshotRepository snapshots,
            PlatformOrganizationId organizationId,
            string productCodeValue,
            string displayName,
            string planCodeValue,
            FeatureGrantSpec[] grants)
        {
            var product = Product.Create(ProductCode.Create(productCodeValue), displayName, T0);
            await products.AddAsync(product);

            var plan = Plan.CreateDraft(product.Code, PlanCode.Create(planCodeValue), displayName, T0);
            plan.Activate(T0);
            var version = PlanVersion.CreateDraft(plan, 1, T0, BillingPeriod.None, true, grants, T0);
            version.Publish(T0);
            var trial = TrialDefinition.Create(
                product.Code,
                displayName + " Trial",
                TimeSpan.FromDays(14),
                grants,
                Array.Empty<FeatureGrantSpec>(),
                T0,
                plan.Id);
            var subscription = Subscription.StartTrial(organizationId, plan, version, trial, T0);

            await plans.AddAsync(plan);
            await plans.AddVersionAsync(version);
            await trials.AddAsync(trial);
            await subscriptions.AddAsync(subscription);

            var entitlementGrants = grants
                .Select(g => new EntitlementGrant(g.FeatureCode, g.Enabled, EntitlementGrantSource.Trial, T0))
                .ToArray();
            var snapshot = EntitlementSnapshot.Create(
                organizationId,
                product.Code,
                subscription.Id,
                plan.Code,
                version.VersionNumber,
                snapshotVersion: 1,
                subscription.Status,
                inGracePeriod: false,
                generatedAtUtc: T0,
                effectiveAtUtc: T0,
                refreshByUtc: T0.AddDays(7),
                sourceAggregateVersion: subscription.Version,
                grants: entitlementGrants);
            await snapshots.AddAsync(snapshot);
        }
    }
}
