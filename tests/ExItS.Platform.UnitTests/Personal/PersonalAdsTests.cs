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

public sealed class PersonalAdsTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 12, 17, 0, 0, TimeSpan.Zero);
    private static readonly PlatformUserId User = PlatformUserId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

    [Fact]
    public async Task Without_ad_free_user_is_eligible_and_provider_not_configured()
    {
        var harness = new Harness();
        var result = await harness.GetEligibility.ExecuteAsync(User);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.True(result.Value!.Eligible);
        Assert.False(result.Value.AdFreeActive);
        Assert.False(result.Value.ProviderConfigured);
        Assert.Null(result.Value.ReasonCode);
    }

    [Fact]
    public async Task Active_ad_free_makes_ads_ineligible()
    {
        var harness = new Harness();
        await harness.GrantAdFreeAsync(endsAtUtc: null);
        var result = await harness.GetEligibility.ExecuteAsync(User);
        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.Eligible);
        Assert.True(result.Value.AdFreeActive);
        Assert.Equal(ApplicationErrorCodes.PersonalAdsAdFreeActive, result.Value.ReasonCode);
    }

    [Fact]
    public async Task Expired_ad_free_restores_eligibility()
    {
        var harness = new Harness();
        await harness.GrantAdFreeAsync(endsAtUtc: T0.AddMinutes(-1));
        var result = await harness.GetEligibility.ExecuteAsync(User);
        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Eligible);
        Assert.False(result.Value.AdFreeActive);
    }

    [Fact]
    public async Task Organization_context_eligibility_is_rejected()
    {
        var harness = new Harness();
        var result = await harness.GetEligibility.ExecuteAsync(
            User,
            organizationId: Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.OrganizationRewardRedemptionUnsupported, result.ErrorCode);
    }

    [Fact]
    public async Task Organization_staff_cannot_query_eligibility()
    {
        var harness = new Harness();
        var staff = PlatformUser.CreateOrganizationStaff(
            "staff9",
            "staff9@ORG000001",
            "staff9@example.com",
            PlatformOrganizationId.From(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd")),
            "Staff Nine",
            T0,
            id: PlatformUserId.From(Guid.Parse("99999999-9999-9999-9999-999999999999")));
        await harness.Users.AddAsync(staff);
        var result = await harness.GetEligibility.ExecuteAsync(staff.Id);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.OrganizationRewardRedemptionUnsupported, result.ErrorCode);
    }

    [Fact]
    public void Null_verifier_never_fabricates_success_by_default()
    {
        var verifier = new NullRewardedAdClaimVerifier(Options.Create(new PersonalRewardClaimOptions
        {
            AdRewardPoints = 10,
            NullProviderClaimsEnabled = false
        }));
        var result = verifier.VerifyAsync(User, "dev-claim-0001").GetAwaiter().GetResult();
        Assert.False(result.IsValid);
        Assert.Null(result.Points);
        Assert.Equal(ApplicationErrorCodes.PersonalRewardClaimProviderUnavailable, result.ErrorCode);
    }

    [Fact]
    public async Task Ad_free_reward_redemption_grants_entitlement()
    {
        var harness = new Harness();
        await harness.Award.ExecuteAsync(
            User.Value,
            PersonalFeatureCodes.AdFreeDefaultRewardPoints,
            PersonalRewardSources.AdminAward);
        var redeem = await harness.Redeem.ExecuteAsync(User, PersonalFeatureCodes.AdFree);
        Assert.True(redeem.IsSuccess, redeem.ErrorMessage);
        Assert.Equal(0, redeem.Value!.AvailablePoints);
        Assert.Equal(nameof(PersonalFeatureGrantSource.RewardPoints), redeem.Value.Entitlement!.GrantSource);
        Assert.True(await harness.EntitlementService.HasActiveEntitlementAsync(
            User, PersonalFeatureCodes.AdFree, T0));

        var eligibility = await harness.GetEligibility.ExecuteAsync(User);
        Assert.False(eligibility.Value!.Eligible);
        Assert.True(eligibility.Value.AdFreeActive);
    }

    [Fact]
    public async Task Ad_free_already_active_redemption_does_not_double_debit()
    {
        var harness = new Harness();
        await harness.Award.ExecuteAsync(
            User.Value,
            PersonalFeatureCodes.AdFreeDefaultRewardPoints * 2,
            PersonalRewardSources.AdminAward);
        var first = await harness.Redeem.ExecuteAsync(User, PersonalFeatureCodes.AdFree);
        var second = await harness.Redeem.ExecuteAsync(User, PersonalFeatureCodes.AdFree);
        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.True(second.Value!.AlreadyActive);
        Assert.Null(second.Value.PointsDebited);
        Assert.Equal(PersonalFeatureCodes.AdFreeDefaultRewardPoints, second.Value.AvailablePoints);
    }

    [Fact]
    public async Task Claim_with_trusted_verifier_blocked_when_ad_free_active()
    {
        var harness = new Harness();
        await harness.GrantAdFreeAsync(endsAtUtc: null);
        var claim = await harness.Claim.ExecuteAsync(User, "dev-claim-0001");
        Assert.False(claim.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PersonalAdsAdFreeActive, claim.ErrorCode);
        Assert.Empty(harness.Transactions.Committed);
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
        public PersonalFeatureEntitlementService EntitlementService { get; }
        public GetPersonalAdEligibility GetEligibility { get; }
        public ClaimPersonalAdReward Claim { get; }
        public RedeemPersonalFeatureWithRewardPoints Redeem { get; }
        public AwardPersonalRewardPoints Award { get; }

        public Harness()
        {
            Users.AddAsync(PlatformUser.Create("personal1", "Personal One", "personal1@example.com", T0, id: User));
            var clock = new FixedClock(T0);
            UnitOfWork = new ControllableUnitOfWork(this);
            EntitlementService = new PersonalFeatureEntitlementService(Definitions, Entitlements);
            var adsOptions = Options.Create(new PersonalAdsOptions { ProviderMode = "None", SurfaceEnabled = true });
            var claimOptions = Options.Create(new PersonalRewardClaimOptions
            {
                AdRewardPoints = 10,
                NullProviderClaimsEnabled = false
            });
            var eligibility = new DefaultPersonalAdEligibility(EntitlementService, adsOptions);
            GetEligibility = new GetPersonalAdEligibility(
                eligibility, Users, clock, adsOptions, claimOptions);
            Claim = new ClaimPersonalAdReward(
                Claims,
                Balances,
                Transactions,
                new TrustedVerifier(10),
                eligibility,
                Users,
                UnitOfWork,
                clock);
            Redeem = new RedeemPersonalFeatureWithRewardPoints(
                Definitions, Entitlements, EntitlementService, Balances, Transactions, Users, UnitOfWork, clock);
            Award = new AwardPersonalRewardPoints(Balances, Transactions, UnitOfWork, clock);
        }

        public async Task GrantAdFreeAsync(DateTimeOffset? endsAtUtc)
        {
            await Definitions.AddAsync(
                PersonalFeatureDefinition.Create(
                    PersonalFeatureCodes.AdFreeCode,
                    "Ad-Free Personal",
                    T0,
                    isActive: true,
                    rewardPointsPrice: PersonalFeatureCodes.AdFreeDefaultRewardPoints));
            await Entitlements.AddAsync(
                PersonalFeatureEntitlement.Grant(
                    User,
                    PersonalFeatureCodes.AdFreeCode,
                    PersonalFeatureGrantSource.AdminGrant,
                    T0.AddDays(-1),
                    endsAtUtc,
                    T0));
            await UnitOfWork.SaveChangesAsync();
        }
    }

    private sealed class TrustedVerifier(int points) : IRewardedAdClaimVerifier
    {
        public Task<RewardedAdClaimVerification> VerifyAsync(
            PlatformUserId personalUserId,
            string claimKey,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new RewardedAdClaimVerification(true, points, null, null, "test"));
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class ControllableUnitOfWork(Harness harness) : IPlatformUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            harness.Definitions.Commit();
            harness.Entitlements.Commit();
            harness.Balances.Commit();
            harness.Transactions.Commit();
            harness.Claims.Commit();
            return Task.CompletedTask;
        }
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
    }
}
