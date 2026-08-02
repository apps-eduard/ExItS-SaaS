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

public sealed class ProductAuthorizationAndDiscoveryTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 2, 7, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Product_local_role_codes_map_to_pos_boundary()
    {
        Assert.Equal("Owner", ProductLocalRoleCodes.MapToPosRoleCode("Owner"));
        Assert.Equal("StoreManager", ProductLocalRoleCodes.MapToPosRoleCode("Manager"));
        Assert.Equal("Cashier", ProductLocalRoleCodes.MapToPosRoleCode("Cashier"));
        Assert.Equal("ReportingUser", ProductLocalRoleCodes.MapToPosRoleCode("Viewer"));
        Assert.Throws<DomainException>(() => ProductLocalRoleCodes.MapToPosRoleCode("Doctor"));
    }

    [Fact]
    public void Product_local_role_grant_revoke_preserves_history()
    {
        var orgId = PlatformOrganizationId.New();
        var userId = PlatformUserId.New();
        var grant = ProductLocalRoleGrant.Create(
            orgId,
            userId,
            ProductCode.PinoyBusinessPos,
            ProductLocalRoleCodes.Owner,
            userId,
            T0);
        Assert.Equal(ProductLocalRoleGrantStatus.Active, grant.Status);
        Assert.Equal("Owner", grant.MappedPosRoleCode);

        grant.Revoke(userId, "left product", T0.AddMinutes(1));
        Assert.Equal(ProductLocalRoleGrantStatus.Revoked, grant.Status);
        Assert.Equal(T0.AddMinutes(1), grant.RevokedAtUtc);
        Assert.Equal("left product", grant.Reason);
    }

    [Fact]
    public async Task Authorization_requires_entitlement_and_role_separately()
    {
        var harness = await AuthHarness.CreateAsync();
        Assert.True((await harness.GrantAccess.ExecuteAsync(
            harness.Organization.Id,
            harness.User.Id,
            harness.Product.Code.Value,
            "dev-admin")).IsSuccess);

        var withoutRole = await harness.Authorize.ExecuteAsync(
            harness.User.Id,
            harness.Organization.Id,
            harness.Product.Code.Value);
        Assert.True(withoutRole.EntitlementAllowed);
        Assert.True(withoutRole.ProductAccessAssigned);
        Assert.False(withoutRole.ProductLocalRoleGranted);
        Assert.False(withoutRole.CanOperate);
        Assert.Equal(EffectiveAccessReasonCodes.ProductLocalRoleMissing, withoutRole.ReasonCode);

        Assert.True((await harness.AssignRole.ExecuteAsync(
            harness.Organization.Id,
            harness.User.Id,
            harness.Product.Code.Value,
            ProductLocalRoleCodes.Cashier,
            harness.User.Id)).IsSuccess);

        var withRole = await harness.Authorize.ExecuteAsync(
            harness.User.Id,
            harness.Organization.Id,
            harness.Product.Code.Value);
        Assert.True(withRole.CanOperate);
        Assert.Equal(ProductLocalRoleCodes.Cashier, withRole.ProductLocalRoleCode);
        Assert.Equal("Cashier", withRole.MappedPosRoleCode);
        Assert.Equal(EffectiveAccessReasonCodes.Allowed, withRole.ReasonCode);
    }

    [Fact]
    public async Task Discovery_lists_subscribed_product_and_launch_requires_role()
    {
        var harness = await AuthHarness.CreateAsync();
        Assert.True((await harness.GrantAccess.ExecuteAsync(
            harness.Organization.Id,
            harness.User.Id,
            harness.Product.Code.Value,
            "dev-admin")).IsSuccess);

        var discovered = await harness.Discover.ExecuteAsync(harness.User.Id, harness.Organization.Id);
        Assert.True(discovered.IsSuccess);
        var item = Assert.Single(discovered.Value!);
        Assert.Equal(ProductCode.PinoyBusinessPos, item.ProductCode);
        Assert.True(item.EntitlementActive);
        Assert.False(item.CanLaunch);

        Assert.True((await harness.AssignRole.ExecuteAsync(
            harness.Organization.Id,
            harness.User.Id,
            harness.Product.Code.Value,
            ProductLocalRoleCodes.Owner,
            harness.User.Id)).IsSuccess);

        discovered = await harness.Discover.ExecuteAsync(harness.User.Id, harness.Organization.Id);
        item = Assert.Single(discovered.Value!);
        Assert.True(item.CanLaunch);
        Assert.Equal(ProductLocalRoleCodes.Owner, item.ProductLocalRoleCode);
    }

    [Fact]
    public async Task Revoking_role_disables_individual_operation_while_entitlement_remains()
    {
        var harness = await AuthHarness.CreateAsync();
        Assert.True((await harness.GrantAccess.ExecuteAsync(
            harness.Organization.Id,
            harness.User.Id,
            harness.Product.Code.Value,
            "dev-admin")).IsSuccess);
        var assigned = await harness.AssignRole.ExecuteAsync(
            harness.Organization.Id,
            harness.User.Id,
            harness.Product.Code.Value,
            ProductLocalRoleCodes.Viewer,
            harness.User.Id);
        Assert.True(assigned.IsSuccess);

        harness.Clock.UtcNow = T0.AddMinutes(2);
        Assert.True((await harness.RevokeRole.ExecuteAsync(
            assigned.Value!.Id,
            harness.User.Id,
            "remove cashier")).IsSuccess);

        var auth = await harness.Authorize.ExecuteAsync(
            harness.User.Id,
            harness.Organization.Id,
            harness.Product.Code.Value);
        Assert.True(auth.EntitlementAllowed);
        Assert.False(auth.CanOperate);
        Assert.Equal(EffectiveAccessReasonCodes.ProductLocalRoleMissing, auth.ReasonCode);
        Assert.Equal("ReportingUser", ProductLocalRoleCodes.MapToPosRoleCode(ProductLocalRoleCodes.Viewer));
    }

    private sealed class AuthHarness
    {
        public required PlatformUser User { get; init; }
        public required PlatformOrganization Organization { get; init; }
        public required Product Product { get; init; }
        public required GrantProductAccess GrantAccess { get; init; }
        public required EvaluateProductAuthorization Authorize { get; init; }
        public required DiscoverEnabledProducts Discover { get; init; }
        public required AssignProductLocalRole AssignRole { get; init; }
        public required RevokeProductLocalRole RevokeRole { get; init; }
        public required FixedClock Clock { get; init; }

        public static async Task<AuthHarness> CreateAsync()
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
            var roleGrants = new InMemoryProductLocalRoleGrantRepository();

            var user = (await new CreatePlatformUser(users, uow, clock)
                .ExecuteAsync("owner", "Org Owner", "owner@example.com")).Value!;
            var org = (await new CreatePlatformOrganization(orgs, uow, clock)
                .ExecuteAsync("Launch Co", "launch-co")).Value!;
            _ = (await new AddOrganizationMembership(users, orgs, memberships, uow, clock)
                .ExecuteAsync(org.Id, user.Id, OrganizationRole.OrganizationOwner)).Value!;

            var product = Product.Create(ProductCode.Create(ProductCode.PinoyBusinessPos), "PinoyBusinessPOS", T0);
            await products.AddAsync(product);

            var plan = Plan.CreateDraft(product.Code, PlanCode.Create("utang-trial"), "Utang Trial", T0);
            plan.Activate(T0);
            var version = PlanVersion.CreateDraft(plan, 1, T0, BillingPeriod.None, true, UtangTrialTestFactory.ActiveGrants(), T0);
            version.Publish(T0);
            var trial = UtangTrialTestFactory.CreateConfigured(T0, TimeSpan.FromDays(14), plan.Id);
            var subscription = Subscription.StartTrial(org.Id, plan, version, trial, T0);
            await subscriptions.AddAsync(subscription);

            var snapshot = EntitlementSnapshot.Create(
                org.Id,
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
                grants:
                [
                    new EntitlementGrant(
                        FeatureCode.Create(FeatureCode.CustomerCreditView),
                        enabled: true,
                        EntitlementGrantSource.Plan,
                        T0)
                ]);
            await snapshots.AddAsync(snapshot);

            var grantAccess = new GrantProductAccess(
                users, orgs, memberships, products, subscriptions, snapshots, assignments, uow, clock);
            var commercial = new EvaluateEffectiveProductAccess(
                users, orgs, memberships, products, assignments, subscriptions, snapshots, clock);
            var authorize = new EvaluateProductAuthorization(commercial, roleGrants, clock);
            var discover = new DiscoverEnabledProducts(memberships, subscriptions, products, authorize);
            var assignRole = new AssignProductLocalRole(users, orgs, memberships, products, roleGrants, uow, clock);
            var revokeRole = new RevokeProductLocalRole(roleGrants, uow, clock);

            return new AuthHarness
            {
                User = user,
                Organization = org,
                Product = product,
                GrantAccess = grantAccess,
                Authorize = authorize,
                Discover = discover,
                AssignRole = assignRole,
                RevokeRole = revokeRole,
                Clock = clock
            };
        }
    }

    private sealed class InMemoryProductLocalRoleGrantRepository : IProductLocalRoleGrantRepository
    {
        private readonly List<ProductLocalRoleGrant> _items = [];

        public Task<ProductLocalRoleGrant?> GetByIdAsync(ProductLocalRoleGrantId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(x => x.Id.Equals(id)));

        public Task<ProductLocalRoleGrant?> FindAsync(
            PlatformOrganizationId organizationId,
            PlatformUserId userIdentityId,
            string productCode,
            string roleCode,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(x =>
                x.OrganizationId.Equals(organizationId)
                && x.UserIdentityId.Equals(userIdentityId)
                && string.Equals(x.ProductCode, productCode, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.RoleCode, roleCode, StringComparison.Ordinal)
                && x.Status == ProductLocalRoleGrantStatus.Active));

        public Task<ProductLocalRoleGrant?> FindActiveByUserOrganizationProductAsync(
            PlatformOrganizationId organizationId,
            PlatformUserId userIdentityId,
            string productCode,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(x =>
                x.OrganizationId.Equals(organizationId)
                && x.UserIdentityId.Equals(userIdentityId)
                && string.Equals(x.ProductCode, productCode, StringComparison.OrdinalIgnoreCase)
                && x.Status == ProductLocalRoleGrantStatus.Active));

        public Task<IReadOnlyList<ProductLocalRoleGrant>> ListByOrganizationAsync(
            PlatformOrganizationId organizationId,
            ProductLocalRoleGrantStatus? status = null,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<ProductLocalRoleGrant> query = _items.Where(x => x.OrganizationId.Equals(organizationId));
            if (status is not null)
            {
                query = query.Where(x => x.Status == status);
            }

            return Task.FromResult<IReadOnlyList<ProductLocalRoleGrant>>(query.ToList());
        }

        public Task<IReadOnlyList<ProductLocalRoleGrant>> ListActiveByUserOrganizationAsync(
            PlatformOrganizationId organizationId,
            PlatformUserId userIdentityId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ProductLocalRoleGrant>>(
                _items.Where(x =>
                        x.OrganizationId.Equals(organizationId)
                        && x.UserIdentityId.Equals(userIdentityId)
                        && x.Status == ProductLocalRoleGrantStatus.Active)
                    .ToList());

        public Task AddAsync(ProductLocalRoleGrant grant, CancellationToken cancellationToken = default)
        {
            _items.Add(grant);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ProductLocalRoleGrant grant, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
