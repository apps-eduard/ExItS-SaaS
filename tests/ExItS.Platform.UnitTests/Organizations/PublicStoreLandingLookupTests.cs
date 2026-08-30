using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Entitlements;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Entitlements;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;
using ExItS.Platform.UnitTests.Support;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class PublicStoreLandingLookupTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 27, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Active_organization_without_ordering_setup_returns_ordering_unavailable()
    {
        var orgs = new InMemoryPlatformOrganizationRepository();
        var audit = new NoOpAuditWriter();
        var org = PlatformOrganization.Create("Kizy Store", "kizy", T0);
        org.AssignPublicOrganizationId("ORG123456", T0);
        await orgs.AddAsync(org);

        var useCase = CreateUseCase(orgs, audit, new InMemoryOrganizationBranchRepository());
        var result = await useCase.ExecuteAsync("ORG123456");

        Assert.True(result.IsSuccess);
        Assert.Equal("ORG123456", result.Value!.PublicOrganizationId);
        Assert.Equal("Kizy Store", result.Value.DisplayName);
        Assert.False(result.Value.OrderingAvailable);

        var dtoType = typeof(PublicStoreLandingDto);
        Assert.Null(dtoType.GetProperty("OrganizationId"));
        Assert.Null(dtoType.GetProperty("ContactEmail"));
        Assert.Null(dtoType.GetProperty("ContactPhone"));
        Assert.Null(dtoType.GetProperty("LegalName"));
        Assert.Equal(3, dtoType.GetProperties().Length);
    }

    [Fact]
    public async Task Active_organization_with_ready_branch_returns_ordering_available()
    {
        var harness = await CreateReadyOrderingHarnessAsync();
        var useCase = CreateUseCase(
            harness.Orgs,
            new NoOpAuditWriter(),
            harness.Branches,
            harness.Hours,
            harness.Policies,
            harness.Entitlements);

        var result = await useCase.ExecuteAsync("ORG555666");

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.OrderingAvailable);
        Assert.Equal("Ready Ordering Store", result.Value.DisplayName);
    }

    [Fact]
    public async Task Active_organization_with_branch_but_ordering_disabled_returns_false()
    {
        var harness = await CreateReadyOrderingHarnessAsync(enableCustomerOrdering: false);
        var useCase = CreateUseCase(
            harness.Orgs,
            new NoOpAuditWriter(),
            harness.Branches,
            harness.Hours,
            harness.Policies,
            harness.Entitlements);

        var result = await useCase.ExecuteAsync("ORG555666");

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.OrderingAvailable);
    }

    [Fact]
    public async Task Active_organization_without_ordering_entitlement_returns_false()
    {
        var harness = await CreateReadyOrderingHarnessAsync(withOrderingEntitlement: false);
        var useCase = CreateUseCase(
            harness.Orgs,
            new NoOpAuditWriter(),
            harness.Branches,
            harness.Hours,
            harness.Policies,
            harness.Entitlements);

        var result = await useCase.ExecuteAsync("ORG555666");

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.OrderingAvailable);
    }

    [Fact]
    public async Task Unknown_organization_fails_safe_generic()
    {
        var useCase = CreateUseCase(
            new InMemoryPlatformOrganizationRepository(),
            new NoOpAuditWriter(),
            new InMemoryOrganizationBranchRepository());
        var result = await useCase.ExecuteAsync("ORG999999");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.OrganizationNotFound, result.ErrorCode);
        Assert.Equal("This store is unavailable.", result.ErrorMessage);
    }

    [Fact]
    public async Task Suspended_organization_fails_safe_without_reason_leak()
    {
        var orgs = new InMemoryPlatformOrganizationRepository();
        var org = PlatformOrganization.Create("Hidden Store", "hidden", T0);
        org.AssignPublicOrganizationId("ORG654321", T0);
        org.Suspend(T0);
        await orgs.AddAsync(org);

        var useCase = CreateUseCase(orgs, new NoOpAuditWriter(), new InMemoryOrganizationBranchRepository());
        var result = await useCase.ExecuteAsync("ORG654321");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.OrganizationNotFound, result.ErrorCode);
        Assert.DoesNotContain("suspend", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Legacy_organization_qr_payload_resolves_same_as_public_id()
    {
        var orgs = new InMemoryPlatformOrganizationRepository();
        var org = PlatformOrganization.Create("Legacy QR Store", "legacy", T0);
        org.AssignPublicOrganizationId("ORG111222", T0);
        await orgs.AddAsync(org);

        var useCase = CreateUseCase(orgs, new NoOpAuditWriter(), new InMemoryOrganizationBranchRepository());
        var payload = PublicOrganizationIdRules.BuildQrPayload("ORG111222");
        var result = await useCase.ExecuteAsync(payload);

        Assert.True(result.IsSuccess);
        Assert.Equal("ORG111222", result.Value!.PublicOrganizationId);
        Assert.False(result.Value.OrderingAvailable);
    }

    private static LookupPublicStoreLanding CreateUseCase(
        IPlatformOrganizationRepository orgs,
        IAuditWriter audit,
        IOrganizationBranchRepository branches,
        IBranchOperatingHoursRepository? hours = null,
        IBranchDeliveryPolicyRepository? policies = null,
        InMemoryEntitlementSnapshotRepository? entitlements = null) =>
        new(
            orgs,
            branches,
            hours ?? new InMemoryBranchOperatingHoursRepository(),
            policies ?? new InMemoryBranchDeliveryPolicyRepository(),
            new EntitlementQueryService(entitlements ?? new InMemoryEntitlementSnapshotRepository()),
            new BranchFulfillmentReadinessEvaluator(new BranchOperatingHoursEvaluator()),
            new FixedClock(T0),
            audit);

    private static async Task<ReadyOrderingHarness> CreateReadyOrderingHarnessAsync(
        bool enableCustomerOrdering = true,
        bool withOrderingEntitlement = true)
    {
        var orgs = new InMemoryPlatformOrganizationRepository();
        var org = PlatformOrganization.Create("Ready Ordering Store", "ready-order", T0);
        org.AssignPublicOrganizationId("ORG555666", T0);
        org.UpdateProfile(
            OrganizationProfile.Create(
                legalName: null,
                contactEmail: null,
                contactPhone: "+63 917 111 2222",
                addressLine1: null,
                addressLine2: null,
                city: null,
                region: null,
                postalCode: null,
                countryCode: null,
                timeZoneId: "Asia/Manila",
                locale: null,
                currencyCode: null),
            T0);
        await orgs.AddAsync(org);

        var branch = OrganizationBranch.Create(
            org.Id,
            "MAIN",
            "Main",
            T0,
            addressLine1: "Line 1",
            city: "Manila",
            countryCode: "PH");
        branch.UpdateContactPhone("+63 917 111 2222", T0);
        branch.UpdateCoordinates(14.5547m, 121.0244m, T0);
        if (enableCustomerOrdering)
        {
            branch.SetCustomerOrderingEnabled(true, T0);
            branch.SetFulfillmentCapabilities(pickupEnabled: true, deliveryEnabled: false, T0);
        }

        var branches = new InMemoryOrganizationBranchRepository();
        await branches.AddAsync(branch);

        var hours = new InMemoryBranchOperatingHoursRepository();
        await hours.UpsertAsync(
            BranchOperatingHoursSchedule.Create(
                branch.Id,
                Enumerable.Range(0, 7).Select(i => BranchDayOperatingHours.Open24Hours((DayOfWeek)i)).ToList()),
            org.Id);

        var policies = new InMemoryBranchDeliveryPolicyRepository();
        await policies.AddAsync(BranchDeliveryPolicy.CreateDefault(branch.Id, org.Id, T0));

        var entitlements = new InMemoryEntitlementSnapshotRepository();
        if (withOrderingEntitlement)
        {
            var grants = new List<EntitlementGrant>
            {
                new(
                    FeatureCode.Create(FeatureCode.StoreCustomerOrdering),
                    enabled: true,
                    EntitlementGrantSource.Plan,
                    T0)
            };
            await entitlements.AddAsync(EntitlementSnapshot.Create(
                org.Id,
                ProductCode.Create(ProductCode.PinoyBusinessPos),
                SubscriptionId.New(),
                PlanCode.Create("mvp-pos"),
                planVersionNumber: 1,
                snapshotVersion: 1,
                SubscriptionStatus.Active,
                inGracePeriod: false,
                generatedAtUtc: T0,
                effectiveAtUtc: T0,
                refreshByUtc: T0.AddDays(7),
                sourceAggregateVersion: 1,
                grants: grants));
        }

        return new ReadyOrderingHarness(orgs, branches, hours, policies, entitlements);
    }

    private sealed record ReadyOrderingHarness(
        InMemoryPlatformOrganizationRepository Orgs,
        InMemoryOrganizationBranchRepository Branches,
        InMemoryBranchOperatingHoursRepository Hours,
        InMemoryBranchDeliveryPolicyRepository Policies,
        InMemoryEntitlementSnapshotRepository Entitlements);

    private sealed class InMemoryOrganizationBranchRepository : IOrganizationBranchRepository
    {
        private readonly List<OrganizationBranch> _items = [];

        public Task<OrganizationBranch?> GetByIdAsync(
            OrganizationBranchId id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(b => b.Id == id));

        public Task<IReadOnlyList<OrganizationBranch>> ListByOrganizationAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganizationBranch>>(
                _items.Where(b => b.OrganizationId == organizationId).ToList());

        public Task<int> CountActiveAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Count(b =>
                b.OrganizationId == organizationId && b.Status == OrganizationBranchStatus.Active));

        public Task<OrganizationBranch?> GetPrimaryAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(b => b.OrganizationId == organizationId && b.IsPrimary));

        public Task AddAsync(OrganizationBranch branch, CancellationToken cancellationToken = default)
        {
            _items.Add(branch);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(OrganizationBranch branch, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryBranchOperatingHoursRepository : IBranchOperatingHoursRepository
    {
        private readonly Dictionary<Guid, BranchOperatingHoursSchedule> _items = new();

        public Task<BranchOperatingHoursSchedule?> GetByBranchIdAsync(
            OrganizationBranchId branchId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.TryGetValue(branchId.Value, out var s) ? s : null);

        public Task<IReadOnlyDictionary<Guid, BranchOperatingHoursSchedule>> ListByOrganizationAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, BranchOperatingHoursSchedule>>(_items);

        public Task UpsertAsync(
            BranchOperatingHoursSchedule schedule,
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default)
        {
            _items[schedule.BranchId.Value] = schedule;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryBranchDeliveryPolicyRepository : IBranchDeliveryPolicyRepository
    {
        private readonly List<BranchDeliveryPolicy> _items = [];

        public Task<BranchDeliveryPolicy?> GetByBranchIdAsync(
            OrganizationBranchId branchId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(p => p.BranchId == branchId));

        public Task<IReadOnlyList<BranchDeliveryPolicy>> ListByOrganizationAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BranchDeliveryPolicy>>(
                _items.Where(p => p.OrganizationId == organizationId).ToList());

        public Task AddAsync(BranchDeliveryPolicy policy, CancellationToken cancellationToken = default)
        {
            _items.Add(policy);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(BranchDeliveryPolicy policy, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
