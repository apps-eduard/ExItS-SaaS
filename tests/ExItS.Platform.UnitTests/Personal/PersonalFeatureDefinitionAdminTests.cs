using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Personal;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Personal;
using ExItS.Platform.UnitTests.Support;

namespace ExItS.Platform.UnitTests.Personal;

/// <summary>P24-WP11: Platform Admin Personal feature configuration + redemption price/duration authority.</summary>
public sealed class PersonalFeatureDefinitionAdminTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 12, 17, 0, 0, TimeSpan.Zero);
    private static readonly PlatformUserId User = PlatformUserId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

    [Fact]
    public async Task List_ensures_known_personal_features_and_returns_server_values()
    {
        var harness = new Harness();
        var result = await harness.List.ExecuteAsync();
        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(2, result.Value!.Count);
        Assert.Contains(result.Value, d => d.FeatureCode == PersonalFeatureCodes.DigitalRecordsExtended);
        Assert.Contains(result.Value, d => d.FeatureCode == PersonalFeatureCodes.AdFree);
        Assert.All(result.Value, d => Assert.True(d.IsActive));
        Assert.Equal(
            PersonalFeatureCodes.DigitalRecordsExtendedDefaultRewardPoints,
            result.Value.Single(d => d.FeatureCode == PersonalFeatureCodes.DigitalRecordsExtended).RewardPointsPrice);
        Assert.Equal(
            PersonalFeatureCodes.AdFreeDefaultRewardPoints,
            result.Value.Single(d => d.FeatureCode == PersonalFeatureCodes.AdFree).RewardPointsPrice);
        Assert.All(result.Value, d => Assert.Null(d.DefaultEntitlementDurationDays));
    }

    [Fact]
    public async Task Update_changes_reward_price_and_duration_for_future_redemption()
    {
        var harness = new Harness();
        await harness.List.ExecuteAsync();
        var before = (await harness.Get.ExecuteAsync(PersonalFeatureCodes.AdFree)).Value!;

        var updated = await harness.Update.ExecuteAsync(
            PersonalFeatureCodes.AdFree,
            new UpdatePersonalFeatureDefinitionCommand(
                "Ad-Free Personal (Admin)",
                IsActive: true,
                RewardPointsPrice: 200,
                DefaultEntitlementDurationDays: 30,
                ExpectedUpdatedAtUtc: before.UpdatedAtUtc));
        Assert.True(updated.IsSuccess, updated.ErrorMessage);
        Assert.Equal(200, updated.Value!.RewardPointsPrice);
        Assert.Equal(30, updated.Value.DefaultEntitlementDurationDays);
        Assert.Equal("Ad-Free Personal (Admin)", updated.Value.DisplayName);

        await harness.Award.ExecuteAsync(User.Value, 200, PersonalRewardSources.AdminAward, reason: "seed");
        var redeem = await harness.Redeem.ExecuteAsync(User, PersonalFeatureCodes.AdFree);
        Assert.True(redeem.IsSuccess, redeem.ErrorMessage);
        Assert.Equal(200, redeem.Value!.PointsDebited);
        Assert.Equal(0, redeem.Value.AvailablePoints);
        Assert.Equal(T0.AddDays(30), redeem.Value.Entitlement!.EndsAtUtc);

        // Historical debit remains 200 even if catalog price changes again.
        var afterPrice = (await harness.Get.ExecuteAsync(PersonalFeatureCodes.AdFree)).Value!;
        var priceChange = await harness.Update.ExecuteAsync(
            PersonalFeatureCodes.AdFree,
            new UpdatePersonalFeatureDefinitionCommand(
                afterPrice.DisplayName,
                true,
                RewardPointsPrice: 50,
                DefaultEntitlementDurationDays: 30,
                ExpectedUpdatedAtUtc: afterPrice.UpdatedAtUtc));
        Assert.True(priceChange.IsSuccess, priceChange.ErrorMessage);

        var activity = await harness.ListActivity.ExecuteAsync(User);
        Assert.True(activity.IsSuccess);
        Assert.Contains(activity.Value!.Items, i =>
            i.TransactionType == PersonalRewardTransactionType.Debit.ToString() && i.Points == 200);
        Assert.DoesNotContain(activity.Value.Items, i =>
            i.TransactionType == PersonalRewardTransactionType.Debit.ToString() && i.Points == 50);
    }

    [Fact]
    public async Task Update_does_not_rewrite_existing_entitlement_window()
    {
        var harness = new Harness();
        await harness.List.ExecuteAsync();
        var grant = await harness.Grant.ExecuteAsync(
            User.Value,
            PersonalFeatureCodes.DigitalRecordsExtended,
            PersonalFeatureGrantSource.AdminGrant,
            startsAtUtc: T0,
            endsAtUtc: T0.AddDays(7));
        Assert.True(grant.IsSuccess, grant.ErrorMessage);
        var originalEnds = grant.Value!.EndsAtUtc;

        var def = (await harness.Get.ExecuteAsync(PersonalFeatureCodes.DigitalRecordsExtended)).Value!;
        var updated = await harness.Update.ExecuteAsync(
            PersonalFeatureCodes.DigitalRecordsExtended,
            new UpdatePersonalFeatureDefinitionCommand(
                def.DisplayName,
                true,
                def.RewardPointsPrice,
                DefaultEntitlementDurationDays: 90,
                ExpectedUpdatedAtUtc: def.UpdatedAtUtc));
        Assert.True(updated.IsSuccess, updated.ErrorMessage);

        var still = await harness.Entitlements.GetByIdAsync(grant.Value.Id);
        Assert.Equal(originalEnds, still!.EndsAtUtc);
    }

    [Fact]
    public async Task Negative_or_zero_reward_price_rejected()
    {
        var harness = new Harness();
        await harness.List.ExecuteAsync();
        var def = (await harness.Get.ExecuteAsync(PersonalFeatureCodes.AdFree)).Value!;
        var result = await harness.Update.ExecuteAsync(
            PersonalFeatureCodes.AdFree,
            new UpdatePersonalFeatureDefinitionCommand(
                def.DisplayName,
                true,
                RewardPointsPrice: 0,
                def.DefaultEntitlementDurationDays,
                def.UpdatedAtUtc));
        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCodes.InvalidPersonalRewardPoints, result.ErrorCode);
    }

    [Fact]
    public async Task Invalid_duration_rejected()
    {
        var harness = new Harness();
        await harness.List.ExecuteAsync();
        var def = (await harness.Get.ExecuteAsync(PersonalFeatureCodes.AdFree)).Value!;
        var result = await harness.Update.ExecuteAsync(
            PersonalFeatureCodes.AdFree,
            new UpdatePersonalFeatureDefinitionCommand(
                def.DisplayName,
                true,
                def.RewardPointsPrice,
                DefaultEntitlementDurationDays: -1,
                def.UpdatedAtUtc));
        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCodes.InvalidPersonalFeatureDuration, result.ErrorCode);
    }

    [Fact]
    public async Task Unknown_feature_code_not_found()
    {
        var harness = new Harness();
        var result = await harness.Update.ExecuteAsync(
            "personal-not-a-real-feature",
            new UpdatePersonalFeatureDefinitionCommand("X", true, 10, null, T0));
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PersonalFeatureDefinitionNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task Concurrency_conflict_when_expected_updated_at_mismatches()
    {
        var harness = new Harness();
        await harness.List.ExecuteAsync();
        var def = (await harness.Get.ExecuteAsync(PersonalFeatureCodes.AdFree)).Value!;
        var result = await harness.Update.ExecuteAsync(
            PersonalFeatureCodes.AdFree,
            new UpdatePersonalFeatureDefinitionCommand(
                def.DisplayName,
                true,
                180,
                null,
                ExpectedUpdatedAtUtc: def.UpdatedAtUtc.AddSeconds(-1)));
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.ConcurrencyConflict, result.ErrorCode);
    }

    [Fact]
    public async Task Disable_feature_blocks_reward_redemption()
    {
        var harness = new Harness();
        await harness.List.ExecuteAsync();
        var def = (await harness.Get.ExecuteAsync(PersonalFeatureCodes.AdFree)).Value!;
        var updated = await harness.Update.ExecuteAsync(
            PersonalFeatureCodes.AdFree,
            new UpdatePersonalFeatureDefinitionCommand(
                def.DisplayName,
                IsActive: false,
                def.RewardPointsPrice,
                def.DefaultEntitlementDurationDays,
                def.UpdatedAtUtc));
        Assert.True(updated.IsSuccess, updated.ErrorMessage);

        await harness.Award.ExecuteAsync(User.Value, 500, PersonalRewardSources.AdminAward);
        var redeem = await harness.Redeem.ExecuteAsync(User, PersonalFeatureCodes.AdFree);
        Assert.False(redeem.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PersonalFeatureDefinitionInactive, redeem.ErrorCode);
    }

    [Fact]
    public void Domain_rejects_negative_duration_and_allows_null_indefinite()
    {
        var def = PersonalFeatureDefinition.Create(
            PersonalFeatureCodes.AdFreeCode, "Ad Free", T0, rewardPointsPrice: 10);
        Assert.Null(def.DefaultEntitlementDurationDays);
        Assert.Null(def.ComputeDefaultEndsAtUtc(T0));
        def.SetDefaultEntitlementDurationDays(14, T0.AddMinutes(1));
        Assert.Equal(T0.AddMinutes(1).AddDays(14), def.ComputeDefaultEndsAtUtc(T0.AddMinutes(1)));
        Assert.Throws<DomainException>(() => def.SetDefaultEntitlementDurationDays(0, T0.AddMinutes(2)));
    }

    [Fact]
    public void Known_personal_feature_guard_isolates_from_org_codes()
    {
        Assert.True(UpdatePersonalFeatureDefinition.IsKnownPersonalFeature(PersonalFeatureCodes.AdFree));
        Assert.False(UpdatePersonalFeatureDefinition.IsKnownPersonalFeature("pos.inventory"));
        Assert.False(UpdatePersonalFeatureDefinition.IsKnownPersonalFeature("personal-statements-export"));
    }

    private sealed class Harness
    {
        public InMemoryDefinitions Definitions { get; } = new();
        public InMemoryEntitlements Entitlements { get; } = new();
        public InMemoryBalances Balances { get; } = new();
        public InMemoryTransactions Transactions { get; } = new();
        public InMemoryPlatformUserRepository Users { get; } = new();
        public RecordingUnitOfWork UnitOfWork { get; }
        public EnsureKnownPersonalFeatureDefinitions EnsureKnown { get; }
        public ListPersonalFeatureDefinitions List { get; }
        public GetPersonalFeatureDefinition Get { get; }
        public UpdatePersonalFeatureDefinition Update { get; }
        public AwardPersonalRewardPoints Award { get; }
        public ListPersonalRewardPointsActivity ListActivity { get; }
        public RedeemPersonalFeatureWithRewardPoints Redeem { get; }
        public GrantPersonalFeature Grant { get; }

        public Harness()
        {
            Users.AddAsync(PlatformUser.Create("personal1", "Personal One", "personal1@example.com", T0, id: User));
            var clock = new FixedClock(T0);
            UnitOfWork = new RecordingUnitOfWork(this);
            EnsureKnown = new EnsureKnownPersonalFeatureDefinitions(Definitions, clock);
            List = new ListPersonalFeatureDefinitions(Definitions, EnsureKnown, UnitOfWork);
            Get = new GetPersonalFeatureDefinition(Definitions, EnsureKnown, UnitOfWork);
            Update = new UpdatePersonalFeatureDefinition(Definitions, UnitOfWork, clock);
            var entitlementService = new PersonalFeatureEntitlementService(Definitions, Entitlements);
            Award = new AwardPersonalRewardPoints(Balances, Transactions, UnitOfWork, clock);
            ListActivity = new ListPersonalRewardPointsActivity(Transactions);
            Redeem = new RedeemPersonalFeatureWithRewardPoints(
                Definitions, Entitlements, entitlementService, Balances, Transactions, Users, UnitOfWork, clock);
            Grant = new GrantPersonalFeature(Definitions, Entitlements, UnitOfWork, clock);
        }
    }

    private sealed class RecordingUnitOfWork(Harness harness) : IPlatformUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            harness.Definitions.Commit();
            harness.Entitlements.Commit();
            harness.Balances.Commit();
            harness.Transactions.Commit();
            return Task.CompletedTask;
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

        public Task<IReadOnlyList<PersonalFeatureDefinition>> ListAllAsync(
            CancellationToken cancellationToken = default)
        {
            var map = new Dictionary<string, PersonalFeatureDefinition>(_committed, StringComparer.Ordinal);
            foreach (var (key, value) in _pending)
            {
                map[key] = value;
            }

            return Task.FromResult<IReadOnlyList<PersonalFeatureDefinition>>(
                map.Values.OrderBy(d => d.FeatureCode.Value, StringComparer.Ordinal).ToList());
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
        private readonly List<PersonalFeatureEntitlement> _committed = [];
        private readonly List<PersonalFeatureEntitlement> _pending = [];

        public Task<PersonalFeatureEntitlement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(
                _pending.Concat(_committed).LastOrDefault(e => e.Id == id));

        public Task<IReadOnlyList<PersonalFeatureEntitlement>> ListByUserAndFeatureAsync(
            PlatformUserId personalUserId,
            FeatureCode featureCode,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PersonalFeatureEntitlement>>(
                _pending.Concat(_committed)
                    .Where(e => e.PersonalUserId == personalUserId && e.FeatureCode == featureCode)
                    .GroupBy(e => e.Id)
                    .Select(g => g.Last())
                    .ToList());

        public Task AddAsync(PersonalFeatureEntitlement entitlement, CancellationToken cancellationToken = default)
        {
            _pending.Add(entitlement);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(PersonalFeatureEntitlement entitlement, CancellationToken cancellationToken = default)
        {
            _pending.Add(entitlement);
            return Task.CompletedTask;
        }

        public void Commit()
        {
            foreach (var item in _pending)
            {
                var i = _committed.FindIndex(e => e.Id == item.Id);
                if (i >= 0)
                {
                    _committed[i] = item;
                }
                else
                {
                    _committed.Add(item);
                }
            }

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
            var filtered = Committed.Concat(_pending)
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
}
