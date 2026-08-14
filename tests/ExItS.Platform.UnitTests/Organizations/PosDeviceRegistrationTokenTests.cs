using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Qr;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;
using ExItS.Platform.UnitTests.Support;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class PosDeviceRegistrationTokenTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 14, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Domain_create_redeem_expire_and_reuse_rejection()
    {
        var org = PlatformOrganizationId.New();
        var user = PlatformUserId.New();
        const string opaque = "abcdefghijklmnopqrstuvwxyz012345";
        var token = PosDeviceRegistrationToken.Create(org, user, opaque, T0);

        Assert.Equal(PosDeviceRegistrationTokenStatus.Active, token.Status);
        Assert.Equal(PosDeviceRegistrationToken.HashToken(opaque), token.TokenHash);

        var deviceId = PosDeviceId.New();
        token.Redeem(deviceId, "install-1", T0.AddMinutes(1), org);
        Assert.Equal(PosDeviceRegistrationTokenStatus.Redeemed, token.Status);
        Assert.Equal(deviceId, token.RedeemedPosDeviceId);

        Assert.Throws<DomainException>(() =>
            token.Redeem(PosDeviceId.New(), "install-2", T0.AddMinutes(2), org));

        var expired = PosDeviceRegistrationToken.Create(org, user, opaque + "x", T0, TimeSpan.FromMinutes(1));
        Assert.Throws<DomainException>(() =>
            expired.EnsureRedeemable(T0.AddMinutes(2), org));
        Assert.Equal(PosDeviceRegistrationTokenStatus.Expired, expired.Status);
    }

    [Fact]
    public void Domain_rejects_org_mismatch_on_redeem()
    {
        var orgA = PlatformOrganizationId.New();
        var orgB = PlatformOrganizationId.New();
        var token = PosDeviceRegistrationToken.Create(
            orgA,
            PlatformUserId.New(),
            "abcdefghijklmnopqrstuvwxyz012345",
            T0);

        var ex = Assert.Throws<DomainException>(() =>
            token.EnsureRedeemable(T0.AddMinutes(1), orgB));
        Assert.Equal(DomainErrorCodes.PosDeviceRegistrationTokenOrganizationMismatch, ex.ErrorCode);
    }

    [Fact]
    public async Task Create_and_redeem_happy_path_and_reuse_rejected()
    {
        var harness = await Harness.CreateAsync();
        var created = await harness.CreateToken.ExecuteAsync(harness.OrgA.Id, harness.OwnerA.Id);
        Assert.True(created.IsSuccess);
        Assert.StartsWith("exits://qr/v1/pos-device-registration/", created.Value!.QrPayload, StringComparison.Ordinal);

        var redeemed = await harness.RedeemToken.ExecuteAsync(
            harness.OrgA.Id,
            harness.StaffA.Id,
            new RedeemPosDeviceRegistrationTokenCommand(
                created.Value.Token,
                harness.BranchA.Id.Value,
                "device-install-a",
                "Front counter"));
        Assert.True(redeemed.IsSuccess);
        Assert.Equal("device-install-a", redeemed.Value!.InstallationDeviceId);

        var reuse = await harness.RedeemToken.ExecuteAsync(
            harness.OrgA.Id,
            harness.StaffA.Id,
            new RedeemPosDeviceRegistrationTokenCommand(
                created.Value.Token,
                harness.BranchA.Id.Value,
                "device-install-b",
                "Other"));
        Assert.False(reuse.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PosDeviceRegistrationTokenAlreadyRedeemed, reuse.ErrorCode);
    }

    [Fact]
    public async Task Org_A_token_cannot_redeem_into_Org_B()
    {
        var harness = await Harness.CreateAsync();
        var created = await harness.CreateToken.ExecuteAsync(harness.OrgA.Id, harness.OwnerA.Id);
        Assert.True(created.IsSuccess);

        var mismatch = await harness.RedeemToken.ExecuteAsync(
            harness.OrgB.Id,
            harness.StaffB.Id,
            new RedeemPosDeviceRegistrationTokenCommand(
                created.Value!.Token,
                harness.BranchB.Id.Value,
                "device-install-b",
                "Wrong org"));
        Assert.False(mismatch.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PosDeviceRegistrationTokenOrganizationMismatch, mismatch.ErrorCode);
    }

    [Fact]
    public async Task Resolve_rejects_purpose_mismatches()
    {
        var harness = await Harness.CreateAsync();
        var resolve = new ResolveExItsQr(
            harness.Users,
            harness.Organizations,
            harness.Tokens,
            harness.Clock,
            new NoOpAuditWriter());

        var personalQr = ExItsQrEnvelope.Build(ExItsQrPurpose.Personal, "EX-0000-0001");
        var personalMismatch = await resolve.ExecuteAsync(
            harness.OwnerA.Id,
            new ResolveExItsQrRequest(personalQr, nameof(ExItsQrPurpose.PosDeviceRegistration)));
        Assert.False(personalMismatch.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.QrPurposeMismatch, personalMismatch.ErrorCode);

        var orgQr = PublicOrganizationIdRules.BuildQrPayload(harness.OrgA.PublicOrganizationId!);
        var orgMismatch = await resolve.ExecuteAsync(
            harness.OwnerA.Id,
            new ResolveExItsQrRequest(orgQr, nameof(ExItsQrPurpose.Personal)));
        Assert.False(orgMismatch.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.QrPurposeMismatch, orgMismatch.ErrorCode);
    }

    private sealed class Harness
    {
        public required InMemoryPlatformUserRepository Users { get; init; }
        public required InMemoryPlatformOrganizationRepository Organizations { get; init; }
        public required InMemoryPosDeviceRegistrationTokenRepository Tokens { get; init; }
        public required FixedClock Clock { get; init; }
        public required CreatePosDeviceRegistrationToken CreateToken { get; init; }
        public required RedeemPosDeviceRegistrationToken RedeemToken { get; init; }
        public required PlatformOrganization OrgA { get; init; }
        public required PlatformOrganization OrgB { get; init; }
        public required OrganizationBranch BranchA { get; init; }
        public required OrganizationBranch BranchB { get; init; }
        public required PlatformUser OwnerA { get; init; }
        public required PlatformUser StaffA { get; init; }
        public required PlatformUser StaffB { get; init; }

        public static async Task<Harness> CreateAsync()
        {
            var clock = new FixedClock(T0);
            var users = new InMemoryPlatformUserRepository();
            var orgs = new InMemoryPlatformOrganizationRepository();
            var memberships = new InMemoryOrganizationMembershipRepository();
            var tokens = new InMemoryPosDeviceRegistrationTokenRepository();
            var devices = new InMemoryPosDeviceRepository();
            var branches = new InMemoryOrganizationBranchRepository();
            var uow = new NoOpUnitOfWork();
            var audit = new NoOpAuditWriter();
            var tokenService = new StubSessionTokenService();
            var plan = Plan.CreateDraft(
                ProductCode.Create(ProductCode.PinoyBusinessPos),
                PlanCode.Create("starter"),
                "Starter",
                T0,
                maxBranches: 3,
                maxActiveStaff: 10,
                maxActivePosDevices: 5);
            var plans = new StubPlanRepository(plan);
            var subscriptions = new StubSubscriptionRepository(plan.Id);

            var ownerA = PlatformUser.Create("ownera", "Owner A", "ownera@example.com", T0);
            ownerA.AssignPublicUserId("EX-0000-0001", T0);
            var staffA = PlatformUser.Create("staffa", "Staff A", "staffa@example.com", T0);
            var staffB = PlatformUser.Create("staffb", "Staff B", "staffb@example.com", T0);
            await users.AddAsync(ownerA);
            await users.AddAsync(staffA);
            await users.AddAsync(staffB);

            var orgA = PlatformOrganization.Create("Org A", "org-a", T0);
            orgA.AssignPublicOrganizationId("ORG000001", T0);
            var orgB = PlatformOrganization.Create("Org B", "org-b", T0);
            orgB.AssignPublicOrganizationId("ORG000002", T0);
            await orgs.AddAsync(orgA);
            await orgs.AddAsync(orgB);

            await memberships.AddAsync(OrganizationMembership.Create(orgA.Id, ownerA.Id, OrganizationRole.OrganizationOwner, T0));
            await memberships.AddAsync(OrganizationMembership.Create(orgA.Id, staffA.Id, OrganizationRole.OrganizationMember, T0));
            await memberships.AddAsync(OrganizationMembership.Create(orgB.Id, staffB.Id, OrganizationRole.OrganizationMember, T0));

            var branchA = OrganizationBranch.CreateMainBranch(orgA.Id, T0);
            var branchB = OrganizationBranch.CreateMainBranch(orgB.Id, T0);
            await branches.AddAsync(branchA);
            await branches.AddAsync(branchB);

            subscriptions.Register(orgA.Id);
            subscriptions.Register(orgB.Id);

            return new Harness
            {
                Users = users,
                Organizations = orgs,
                Tokens = tokens,
                Clock = clock,
                CreateToken = new CreatePosDeviceRegistrationToken(
                    tokens, devices, subscriptions, plans, tokenService, uow, clock, audit),
                RedeemToken = new RedeemPosDeviceRegistrationToken(
                    tokens, devices, branches, memberships, subscriptions, plans, uow, clock, audit),
                OrgA = orgA,
                OrgB = orgB,
                BranchA = branchA,
                BranchB = branchB,
                OwnerA = ownerA,
                StaffA = staffA,
                StaffB = staffB
            };
        }
    }

    private sealed class InMemoryPosDeviceRegistrationTokenRepository : IPosDeviceRegistrationTokenRepository
    {
        private readonly Dictionary<Guid, PosDeviceRegistrationToken> _byId = new();
        private readonly Dictionary<string, Guid> _byHash = new(StringComparer.Ordinal);

        public Task<PosDeviceRegistrationToken?> GetByIdAsync(
            PosDeviceRegistrationTokenId id,
            CancellationToken cancellationToken = default)
        {
            _byId.TryGetValue(id.Value, out var token);
            return Task.FromResult(token);
        }

        public Task<PosDeviceRegistrationToken?> GetByTokenHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default)
        {
            if (_byHash.TryGetValue(tokenHash, out var id) && _byId.TryGetValue(id, out var token))
            {
                return Task.FromResult<PosDeviceRegistrationToken?>(token);
            }

            return Task.FromResult<PosDeviceRegistrationToken?>(null);
        }

        public Task AddAsync(PosDeviceRegistrationToken token, CancellationToken cancellationToken = default)
        {
            _byId[token.Id.Value] = token;
            _byHash[token.TokenHash] = token.Id.Value;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(PosDeviceRegistrationToken token, CancellationToken cancellationToken = default)
        {
            _byId[token.Id.Value] = token;
            _byHash[token.TokenHash] = token.Id.Value;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryPosDeviceRepository : IPosDeviceRepository
    {
        private readonly List<PosDevice> _items = [];

        public Task<PosDevice?> GetByIdAsync(PosDeviceId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

        public Task<PosDevice?> GetByInstallationDeviceIdAsync(
            PlatformOrganizationId organizationId,
            string installationDeviceId,
            CancellationToken cancellationToken = default)
        {
            var value = PosDevice.NormalizeInstallationDeviceId(installationDeviceId);
            return Task.FromResult(_items.FirstOrDefault(x =>
                x.OrganizationId == organizationId && x.InstallationDeviceId == value));
        }

        public Task<IReadOnlyList<PosDevice>> ListByOrganizationAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PosDevice>>(_items.Where(x => x.OrganizationId == organizationId).ToList());

        public Task<int> CountActiveAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Count(x => x.OrganizationId == organizationId && x.Status == PosDeviceStatus.Active));

        public Task AddAsync(PosDevice device, CancellationToken cancellationToken = default)
        {
            _items.Add(device);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(PosDevice device, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class InMemoryOrganizationBranchRepository : IOrganizationBranchRepository
    {
        private readonly Dictionary<Guid, OrganizationBranch> _byId = new();

        public Task<OrganizationBranch?> GetByIdAsync(
            OrganizationBranchId id,
            CancellationToken cancellationToken = default)
        {
            _byId.TryGetValue(id.Value, out var branch);
            return Task.FromResult(branch);
        }

        public Task<OrganizationBranch?> GetPrimaryAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_byId.Values.FirstOrDefault(x => x.OrganizationId == organizationId && x.IsPrimary));

        public Task<IReadOnlyList<OrganizationBranch>> ListByOrganizationAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganizationBranch>>(
                _byId.Values.Where(x => x.OrganizationId == organizationId).ToList());

        public Task<int> CountActiveAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_byId.Values.Count(x =>
                x.OrganizationId == organizationId && x.Status == OrganizationBranchStatus.Active));

        public Task AddAsync(OrganizationBranch branch, CancellationToken cancellationToken = default)
        {
            _byId[branch.Id.Value] = branch;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(OrganizationBranch branch, CancellationToken cancellationToken = default)
        {
            _byId[branch.Id.Value] = branch;
            return Task.CompletedTask;
        }
    }

    private sealed class StubPlanRepository(Plan plan) : IPlanRepository
    {
        public Task<Plan?> GetByIdAsync(PlanId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(plan.Id == id ? plan : null);

        public Task<Plan?> GetByProductAndCodeAsync(ProductCode productCode, PlanCode planCode, CancellationToken cancellationToken = default) =>
            Task.FromResult<Plan?>(plan);

        public Task<IReadOnlyList<Plan>> ListByProductAsync(ProductCode productCode, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Plan>>([plan]);

        public Task<(IReadOnlyList<Plan> Items, int TotalCount)> ListAsync(
            ProductCode? productCode,
            PlanStatus? status,
            string? search,
            CatalogListSortBy sortBy,
            bool sortDescending,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<Plan>, int)>(([plan], 1));

        public Task AddAsync(Plan entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(Plan entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<PlanVersion?> GetVersionByIdAsync(PlanVersionId id, CancellationToken cancellationToken = default) =>
            Task.FromResult<PlanVersion?>(null);
        public Task<PlanVersion?> GetVersionByPlanAndNumberAsync(PlanId planId, int versionNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult<PlanVersion?>(null);
        public Task<IReadOnlyList<PlanVersion>> ListVersionsAsync(PlanId planId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PlanVersion>>([]);
        public Task<PlanVersion?> GetLatestPublishedVersionAsync(PlanId planId, CancellationToken cancellationToken = default) =>
            Task.FromResult<PlanVersion?>(null);
        public Task<int> GetMaxVersionNumberAsync(PlanId planId, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
        public Task AddVersionAsync(PlanVersion version, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateVersionAsync(PlanVersion version, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class StubSubscriptionRepository(PlanId planId) : ISubscriptionRepository
    {
        private readonly HashSet<Guid> _orgs = [];

        public void Register(PlatformOrganizationId organizationId) => _orgs.Add(organizationId.Value);

        public Task<Subscription?> GetByIdAsync(SubscriptionId id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Subscription?>(null);

        public Task<Subscription?> GetCurrentForOrganizationProductAsync(
            PlatformOrganizationId organizationId,
            ProductCode productCode,
            CancellationToken cancellationToken = default)
        {
            if (!_orgs.Contains(organizationId.Value))
            {
                return Task.FromResult<Subscription?>(null);
            }

            return Task.FromResult<Subscription?>(Subscription.Rehydrate(
                SubscriptionId.New(),
                organizationId,
                productCode,
                planId,
                PlanVersionId.New(),
                TrialDefinitionId.New(),
                SubscriptionStatus.Trialing,
                T0,
                T0.AddDays(14),
                paidPeriodStartUtc: null,
                paidPeriodEndUtc: null,
                gracePeriodEndUtc: null,
                suspendedAtUtc: null,
                cancelledAtUtc: null,
                pastDueAtUtc: null,
                expiredAtUtc: null,
                billingCycle: BillingCycle.Monthly,
                agreedPrice: 0m,
                currencyCode: "PHP",
                priceEffectiveFromUtc: null,
                pendingPlanId: null,
                pendingPlanEffectiveAtUtc: null,
                createdAtUtc: T0,
                updatedAtUtc: T0,
                version: 1));
        }

        public Task<(IReadOnlyList<Subscription> Items, int TotalCount)> ListByOrganizationAsync(
            PlatformOrganizationId organizationId,
            SubscriptionStatus? status,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<Subscription>, int)>(([], 0));

        public Task<(IReadOnlyList<Subscription> Items, int TotalCount)> ListByProductAsync(
            ProductCode productCode,
            SubscriptionStatus? status,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<Subscription>, int)>(([], 0));

        public Task<(IReadOnlyList<Subscription> Items, int TotalCount)> ListExpiringTrialsAsync(
            DateTimeOffset asOfUtc,
            DateTimeOffset throughUtc,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<Subscription>, int)>(([], 0));

        public Task<(IReadOnlyList<Subscription> Items, int TotalCount)> ListByStatusAsync(
            SubscriptionStatus status,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<Subscription>, int)>(([], 0));

        public Task<(IReadOnlyList<Subscription> Items, int TotalCount)> ListAsync(
            PlatformOrganizationId? organizationId,
            ProductCode? productCode,
            SubscriptionStatus? status,
            string? search,
            bool? isTrial,
            Guid? planId,
            SubscriptionListSortBy sortBy,
            bool sortDescending,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<Subscription>, int)>(([], 0));

        public Task<bool> ExistsActiveLikeAsync(
            PlatformOrganizationId organizationId,
            ProductCode productCode,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_orgs.Contains(organizationId.Value));

        public Task<bool> HasConsumedTrialAsync(
            PlatformOrganizationId organizationId,
            ProductCode productCode,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<IReadOnlyList<Subscription>> ListDuePendingPlanChangesAsync(
            DateTimeOffset asOfUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Subscription>>([]);

        public Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(Subscription subscription, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
