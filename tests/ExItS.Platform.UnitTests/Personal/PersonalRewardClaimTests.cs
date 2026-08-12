using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Personal;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Personal;
using ExItS.Platform.UnitTests.Support;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.UnitTests.Personal;

public sealed class PersonalRewardClaimTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 12, 16, 0, 0, TimeSpan.Zero);
    private static readonly PlatformUserId User = PlatformUserId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly PlatformUserId Other = PlatformUserId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private const string ClaimKey = "dev-claim-0001";

    [Fact]
    public async Task First_ad_claim_credits_configured_points_once()
    {
        var harness = new Harness();
        var result = await harness.Claim.ExecuteAsync(User, ClaimKey);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.False(result.Value!.AlreadyClaimed);
        Assert.Equal(10, result.Value.PointsAwarded);
        Assert.Equal(10, result.Value.AvailablePoints);
        Assert.Equal(PersonalRewardClaimTypes.AdReward, result.Value.ClaimType);
        Assert.Equal(PersonalRewardSources.AdReward, harness.Transactions.Committed.Single().Source);
        Assert.Single(harness.Claims.Committed);
    }

    [Fact]
    public async Task Duplicate_ad_claim_is_idempotent()
    {
        var harness = new Harness();
        var first = await harness.Claim.ExecuteAsync(User, ClaimKey);
        var second = await harness.Claim.ExecuteAsync(User, ClaimKey);
        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess, second.ErrorMessage);
        Assert.True(second.Value!.AlreadyClaimed);
        Assert.Equal(first.Value!.RewardTransactionId, second.Value.RewardTransactionId);
        Assert.Equal(10, second.Value.AvailablePoints);
        Assert.Single(harness.Transactions.Committed);
        Assert.Single(harness.Claims.Committed);
    }

    [Fact]
    public async Task Concurrent_duplicate_claim_credits_once()
    {
        var harness = new Harness();
        harness.OnBeforeCommit = () =>
        {
            // Peer already committed the same claim.
            var peerTx = PersonalRewardTransaction.Rehydrate(
                Guid.NewGuid(),
                User,
                PersonalRewardTransactionType.Credit,
                10,
                10,
                10,
                PersonalRewardSources.AdReward,
                "peer",
                ClaimKey,
                PersonalRewardClaim.BuildLedgerIdempotencyKey(PersonalRewardClaimTypes.AdReward, ClaimKey),
                T0);
            harness.Transactions.CommitDirect(peerTx);
            harness.Balances.CommitDirect(PersonalRewardBalance.Rehydrate(User, 10, T0, T0, version: 2));
            harness.Claims.CommitDirect(PersonalRewardClaim.Create(
                User,
                PersonalRewardClaimTypes.AdReward,
                ClaimKey,
                10,
                peerTx.Id,
                T0));
            throw new PersistenceConflictException(
                ApplicationErrorCodes.PersonalRewardBalanceConflict,
                "conflict");
        };

        var result = await harness.Claim.ExecuteAsync(User, ClaimKey);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.True(result.Value!.AlreadyClaimed);
        Assert.Equal(10, result.Value.AvailablePoints);
        Assert.Single(harness.Transactions.Committed);
        Assert.Single(harness.Claims.Committed);
    }

    [Fact]
    public async Task Organization_context_claim_is_rejected()
    {
        var harness = new Harness();
        var result = await harness.Claim.ExecuteAsync(
            User,
            ClaimKey,
            organizationId: Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.OrganizationRewardRedemptionUnsupported, result.ErrorCode);
        Assert.Empty(harness.Transactions.Committed);
        Assert.Empty(harness.Claims.Committed);
    }

    [Fact]
    public async Task Organization_staff_cannot_claim_ad_reward()
    {
        var harness = new Harness();
        var staff = PlatformUser.CreateOrganizationStaff(
            "staff2",
            "staff2@ORG000001",
            "staff2@example.com",
            PlatformOrganizationId.From(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd")),
            "Staff Two",
            T0,
            id: PlatformUserId.From(Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff")));
        await harness.Users.AddAsync(staff);
        var result = await harness.Claim.ExecuteAsync(staff.Id, ClaimKey);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.OrganizationRewardRedemptionUnsupported, result.ErrorCode);
        Assert.Empty(harness.Claims.Committed);
    }

    [Fact]
    public async Task Other_user_cannot_consume_another_users_claim_row()
    {
        var harness = new Harness();
        await harness.Claim.ExecuteAsync(User, ClaimKey);
        var other = await harness.Claim.ExecuteAsync(Other, ClaimKey);
        Assert.True(other.IsSuccess);
        Assert.False(other.Value!.AlreadyClaimed);
        Assert.Equal(2, harness.Claims.Committed.Count);
        Assert.Equal(2, harness.Transactions.Committed.Count);
    }

    [Fact]
    public async Task Invalid_claim_key_does_not_credit()
    {
        var harness = new Harness();
        var result = await harness.Claim.ExecuteAsync(User, "short");
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PersonalRewardClaimInvalid, result.ErrorCode);
        Assert.Empty(harness.Transactions.Committed);
    }

    [Fact]
    public async Task Ad_free_entitlement_makes_claim_ineligible()
    {
        var harness = new Harness();
        await harness.Definitions.AddAsync(
            PersonalFeatureDefinition.Create(PersonalFeatureCodes.AdFreeCode, "Ad Free", T0));
        await harness.Entitlements.AddAsync(
            PersonalFeatureEntitlement.Grant(
                User,
                PersonalFeatureCodes.AdFreeCode,
                PersonalFeatureGrantSource.AdminGrant,
                T0,
                null,
                T0));
        await harness.UnitOfWork.SaveChangesAsync();

        var result = await harness.Claim.ExecuteAsync(User, ClaimKey);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PersonalRewardClaimNotEligible, result.ErrorCode);
        Assert.Empty(harness.Transactions.Committed);
    }

    [Fact]
    public async Task Null_provider_disabled_rejects_claim()
    {
        var harness = new Harness(nullProviderEnabled: false);
        var result = await harness.Claim.ExecuteAsync(User, ClaimKey);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PersonalRewardClaimProviderUnavailable, result.ErrorCode);
        Assert.Empty(harness.Transactions.Committed);
    }

    [Fact]
    public async Task Redeem_still_works_after_ad_claim_credit()
    {
        var harness = new Harness(adRewardPoints: 100);
        var claim = await harness.Claim.ExecuteAsync(User, ClaimKey);
        Assert.True(claim.IsSuccess);
        var redeem = await harness.Redeem.ExecuteAsync(User, PersonalFeatureCodes.DigitalRecordsExtended);
        Assert.True(redeem.IsSuccess, redeem.ErrorMessage);
        Assert.Equal(0, redeem.Value!.AvailablePoints);
        Assert.Equal(nameof(PersonalFeatureGrantSource.RewardPoints), redeem.Value.Entitlement!.GrantSource);
    }

    private sealed class Harness
    {
        public InMemoryDefinitions Definitions { get; } = new();
        public InMemoryEntitlements Entitlements { get; } = new();
        public InMemoryBalances Balances { get; } = new();
        public InMemoryTransactions Transactions { get; } = new();
        public InMemoryClaims Claims { get; } = new();
        public InMemoryPlatformUserRepository Users { get; } = new();
        public ControllableUnitOfWork UnitOfWork { get; }
        public ClaimPersonalAdReward Claim { get; }
        public RedeemPersonalFeatureWithRewardPoints Redeem { get; }
        public Action? OnBeforeCommit { get; set; }

        public Harness(bool nullProviderEnabled = true, int adRewardPoints = 10)
        {
            Users.AddAsync(PlatformUser.Create("personal1", "Personal One", "personal1@example.com", T0, id: User));
            Users.AddAsync(PlatformUser.Create("personal2", "Personal Two", "personal2@example.com", T0, id: Other));
            var clock = new FixedClock(T0);
            UnitOfWork = new ControllableUnitOfWork(this);
            var entitlementService = new PersonalFeatureEntitlementService(Definitions, Entitlements);
            var options = Options.Create(new PersonalRewardClaimOptions
            {
                AdRewardPoints = adRewardPoints,
                NullProviderClaimsEnabled = nullProviderEnabled
            });
            Claim = new ClaimPersonalAdReward(
                Claims,
                Balances,
                Transactions,
                new NullRewardedAdClaimVerifier(options),
                new DefaultPersonalAdEligibility(entitlementService),
                Users,
                UnitOfWork,
                clock);
            Redeem = new RedeemPersonalFeatureWithRewardPoints(
                Definitions,
                Entitlements,
                entitlementService,
                Balances,
                Transactions,
                Users,
                UnitOfWork,
                clock);
        }
    }

    private sealed class ControllableUnitOfWork(Harness harness) : IPlatformUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (harness.OnBeforeCommit is not null)
                {
                    var action = harness.OnBeforeCommit;
                    harness.OnBeforeCommit = null;
                    action();
                }

                harness.Definitions.Commit();
                harness.Entitlements.Commit();
                harness.Balances.Commit();
                harness.Transactions.Commit();
                harness.Claims.Commit();
                return Task.CompletedTask;
            }
            catch
            {
                harness.Definitions.Rollback();
                harness.Entitlements.Rollback();
                harness.Balances.Rollback();
                harness.Transactions.Rollback();
                harness.Claims.Rollback();
                throw;
            }
        }
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class InMemoryDefinitions : IPersonalFeatureDefinitionRepository
    {
        private readonly Dictionary<string, PersonalFeatureDefinition> _committed = new(StringComparer.Ordinal);
        private readonly Dictionary<string, PersonalFeatureDefinition> _pending = new(StringComparer.Ordinal);

        public Task<PersonalFeatureDefinition?> GetByCodeAsync(
            FeatureCode featureCode,
            CancellationToken cancellationToken = default)
        {
            if (_pending.TryGetValue(featureCode.Value, out var pending))
            {
                return Task.FromResult<PersonalFeatureDefinition?>(pending);
            }

            return Task.FromResult(_committed.TryGetValue(featureCode.Value, out var d) ? d : null);
        }

        public Task AddAsync(PersonalFeatureDefinition definition, CancellationToken cancellationToken = default)
        {
            _pending[definition.FeatureCode.Value] = definition;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(PersonalFeatureDefinition definition, CancellationToken cancellationToken = default)
        {
            _pending[definition.FeatureCode.Value] = definition;
            return Task.CompletedTask;
        }

        public void Commit()
        {
            foreach (var (key, value) in _pending)
            {
                _committed[key] = value;
            }

            _pending.Clear();
        }

        public void Rollback() => _pending.Clear();
    }

    private sealed class InMemoryEntitlements : IPersonalFeatureEntitlementRepository
    {
        public List<PersonalFeatureEntitlement> Committed { get; } = [];
        private readonly List<PersonalFeatureEntitlement> _pending = [];

        public Task<PersonalFeatureEntitlement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Committed.Concat(_pending).FirstOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<PersonalFeatureEntitlement>> ListByUserAndFeatureAsync(
            PlatformUserId personalUserId,
            FeatureCode featureCode,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PersonalFeatureEntitlement>>(
                Committed.Concat(_pending)
                    .Where(x => x.PersonalUserId == personalUserId && x.FeatureCode == featureCode)
                    .ToList());

        public Task AddAsync(PersonalFeatureEntitlement entitlement, CancellationToken cancellationToken = default)
        {
            _pending.Add(entitlement);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(PersonalFeatureEntitlement entitlement, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public void Commit()
        {
            Committed.AddRange(_pending);
            _pending.Clear();
        }

        public void Rollback() => _pending.Clear();
    }

    private sealed class InMemoryBalances : IPersonalRewardBalanceRepository
    {
        private readonly Dictionary<Guid, PersonalRewardBalance> _committed = new();
        private readonly Dictionary<Guid, PersonalRewardBalance> _pending = new();

        public Task<PersonalRewardBalance?> GetByUserAsync(
            PlatformUserId personalUserId,
            CancellationToken cancellationToken = default)
        {
            if (_pending.TryGetValue(personalUserId.Value, out var pending))
            {
                return Task.FromResult<PersonalRewardBalance?>(Clone(pending));
            }

            return Task.FromResult(
                _committed.TryGetValue(personalUserId.Value, out var b) ? Clone(b) : null);
        }

        public Task AddAsync(PersonalRewardBalance balance, CancellationToken cancellationToken = default)
        {
            _pending[balance.PersonalUserId.Value] = Clone(balance);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            PersonalRewardBalance balance,
            int expectedVersion,
            CancellationToken cancellationToken = default)
        {
            var current = _pending.TryGetValue(balance.PersonalUserId.Value, out var p)
                ? p
                : _committed.GetValueOrDefault(balance.PersonalUserId.Value);
            if (current is null || current.Version != expectedVersion)
            {
                throw new PersistenceConflictException(
                    ApplicationErrorCodes.PersonalRewardBalanceConflict,
                    "version mismatch");
            }

            _pending[balance.PersonalUserId.Value] = Clone(balance);
            return Task.CompletedTask;
        }

        public void Commit()
        {
            foreach (var (key, value) in _pending)
            {
                _committed[key] = Clone(value);
            }

            _pending.Clear();
        }

        public void Rollback() => _pending.Clear();

        public void CommitDirect(PersonalRewardBalance balance) =>
            _committed[balance.PersonalUserId.Value] = Clone(balance);

        private static PersonalRewardBalance Clone(PersonalRewardBalance b) =>
            PersonalRewardBalance.Rehydrate(
                b.PersonalUserId, b.AvailablePoints, b.CreatedAtUtc, b.UpdatedAtUtc, b.Version);
    }

    private sealed class InMemoryTransactions : IPersonalRewardTransactionRepository
    {
        public List<PersonalRewardTransaction> Committed { get; } = [];
        private readonly List<PersonalRewardTransaction> _pending = [];

        public Task AddAsync(PersonalRewardTransaction transaction, CancellationToken cancellationToken = default)
        {
            _pending.Add(transaction);
            return Task.CompletedTask;
        }

        public Task<PersonalRewardTransaction?> FindByIdempotencyKeyAsync(
            PlatformUserId personalUserId,
            string idempotencyKey,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Committed.Concat(_pending).FirstOrDefault(x =>
                x.PersonalUserId == personalUserId
                && string.Equals(x.IdempotencyKey, idempotencyKey, StringComparison.Ordinal)));

        public Task<(IReadOnlyList<PersonalRewardTransaction> Items, int TotalCount)> ListByUserDescendingAsync(
            PlatformUserId personalUserId,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            var filtered = Committed
                .Where(x => x.PersonalUserId == personalUserId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .ThenByDescending(x => x.Id)
                .ToList();
            return Task.FromResult<(IReadOnlyList<PersonalRewardTransaction>, int)>(
                (filtered.Skip(skip).Take(take).ToList(), filtered.Count));
        }

        public void Commit()
        {
            Committed.AddRange(_pending);
            _pending.Clear();
        }

        public void Rollback() => _pending.Clear();

        public void CommitDirect(PersonalRewardTransaction tx) => Committed.Add(tx);
    }

    private sealed class InMemoryClaims : IPersonalRewardClaimRepository
    {
        public List<PersonalRewardClaim> Committed { get; } = [];
        private readonly List<PersonalRewardClaim> _pending = [];

        public Task<PersonalRewardClaim?> FindByUserTypeAndKeyAsync(
            PlatformUserId personalUserId,
            string claimType,
            string claimKey,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Committed.Concat(_pending).FirstOrDefault(x =>
                x.PersonalUserId == personalUserId
                && string.Equals(x.ClaimType, claimType, StringComparison.Ordinal)
                && string.Equals(x.ClaimKey, claimKey, StringComparison.Ordinal)));

        public Task AddAsync(PersonalRewardClaim claim, CancellationToken cancellationToken = default)
        {
            _pending.Add(claim);
            return Task.CompletedTask;
        }

        public void Commit()
        {
            Committed.AddRange(_pending);
            _pending.Clear();
        }

        public void Rollback() => _pending.Clear();

        public void CommitDirect(PersonalRewardClaim claim) => Committed.Add(claim);
    }
}
