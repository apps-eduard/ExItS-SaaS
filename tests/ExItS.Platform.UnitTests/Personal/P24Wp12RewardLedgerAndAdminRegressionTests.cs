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

/// <summary>P24-WP12: reward ledger arithmetic + admin commercial authority regressions.</summary>
public sealed class P24Wp12RewardLedgerAndAdminRegressionTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 12, 18, 0, 0, TimeSpan.Zero);
    private static readonly PlatformUserId User = PlatformUserId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

    [Fact]
    public async Task Ledger_balance_equals_sum_of_signed_transaction_deltas()
    {
        var harness = new Harness();
        await harness.List.ExecuteAsync();

        await harness.Award.ExecuteAsync(User.Value, 250, PersonalRewardSources.AdminAward, reason: "seed");
        await harness.Award.ExecuteAsync(User.Value, 50, PersonalRewardSources.AdminAward, reason: "bonus");

        var def = (await harness.Get.ExecuteAsync(PersonalFeatureCodes.DigitalRecordsExtended)).Value!;
        await harness.Update.ExecuteAsync(
            PersonalFeatureCodes.DigitalRecordsExtended,
            new UpdatePersonalFeatureDefinitionCommand(
                def.DisplayName, true, RewardPointsPrice: 120, null, def.UpdatedAtUtc));

        var redeem = await harness.Redeem.ExecuteAsync(User, PersonalFeatureCodes.DigitalRecordsExtended);
        Assert.True(redeem.IsSuccess, redeem.ErrorMessage);
        Assert.Equal(120, redeem.Value!.PointsDebited);

        var balance = await harness.GetBalance.ExecuteAsync(User);
        Assert.True(balance.IsSuccess);
        Assert.Equal(180, balance.Value!.AvailablePoints);

        var activity = await harness.ListActivity.ExecuteAsync(User, page: 1, pageSize: 50);
        Assert.True(activity.IsSuccess);
        var sum = activity.Value!.Items.Sum(i => i.SignedDelta);
        Assert.Equal(balance.Value.AvailablePoints, sum);
    }

    [Fact]
    public async Task Expired_entitlement_allows_subsequent_redemption_at_current_server_price()
    {
        var harness = new Harness();
        await harness.List.ExecuteAsync();

        var grant = await harness.Grant.ExecuteAsync(
            User.Value,
            PersonalFeatureCodes.AdFree,
            PersonalFeatureGrantSource.AdminGrant,
            startsAtUtc: T0.AddDays(-10),
            endsAtUtc: T0.AddDays(-1));
        Assert.True(grant.IsSuccess, grant.ErrorMessage);
        Assert.False(grant.Value!.IsActiveAtQueryTime);

        var def = (await harness.Get.ExecuteAsync(PersonalFeatureCodes.AdFree)).Value!;
        await harness.Update.ExecuteAsync(
            PersonalFeatureCodes.AdFree,
            new UpdatePersonalFeatureDefinitionCommand(
                def.DisplayName, true, RewardPointsPrice: 175, DefaultEntitlementDurationDays: 7, def.UpdatedAtUtc));

        await harness.Award.ExecuteAsync(User.Value, 175, PersonalRewardSources.AdminAward);
        var redeem = await harness.Redeem.ExecuteAsync(User, PersonalFeatureCodes.AdFree);
        Assert.True(redeem.IsSuccess, redeem.ErrorMessage);
        Assert.Equal(175, redeem.Value!.PointsDebited);
        Assert.Equal(T0.AddDays(7), redeem.Value.Entitlement!.EndsAtUtc);
    }

    [Fact]
    public async Task Organization_feature_codes_remain_rejected_for_personal_admin_update()
    {
        var harness = new Harness();
        var result = await harness.Update.ExecuteAsync(
            FeatureCode.CustomerCreditView,
            new UpdatePersonalFeatureDefinitionCommand("Nope", true, 10, null, T0));
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PersonalFeatureDefinitionNotFound, result.ErrorCode);
    }

    private sealed class Harness
    {
        public InMemoryDefinitions Definitions { get; } = new();
        public InMemoryEntitlements Entitlements { get; } = new();
        public InMemoryBalances Balances { get; } = new();
        public InMemoryTransactions Transactions { get; } = new();
        public InMemoryPlatformUserRepository Users { get; } = new();
        public RecordingUnitOfWork UnitOfWork { get; }
        public ListPersonalFeatureDefinitions List { get; }
        public GetPersonalFeatureDefinition Get { get; }
        public UpdatePersonalFeatureDefinition Update { get; }
        public AwardPersonalRewardPoints Award { get; }
        public GetPersonalRewardPointsBalance GetBalance { get; }
        public ListPersonalRewardPointsActivity ListActivity { get; }
        public RedeemPersonalFeatureWithRewardPoints Redeem { get; }
        public GrantPersonalFeature Grant { get; }

        public Harness()
        {
            Users.AddAsync(PlatformUser.Create("personal1", "Personal One", "personal1@example.com", T0, id: User));
            var clock = new FixedClock(T0);
            UnitOfWork = new RecordingUnitOfWork(this);
            var ensure = new EnsureKnownPersonalFeatureDefinitions(Definitions, clock);
            List = new ListPersonalFeatureDefinitions(Definitions, ensure, UnitOfWork);
            Get = new GetPersonalFeatureDefinition(Definitions, ensure, UnitOfWork);
            Update = new UpdatePersonalFeatureDefinition(Definitions, UnitOfWork, clock);
            var entitlementService = new PersonalFeatureEntitlementService(Definitions, Entitlements);
            Award = new AwardPersonalRewardPoints(Balances, Transactions, UnitOfWork, clock);
            GetBalance = new GetPersonalRewardPointsBalance(Balances);
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
            FeatureCode featureCode, CancellationToken cancellationToken = default)
        {
            if (_pending.TryGetValue(featureCode.Value, out var pending))
            {
                return Task.FromResult<PersonalFeatureDefinition?>(pending);
            }

            return Task.FromResult(_committed.TryGetValue(featureCode.Value, out var d) ? d : null);
        }

        public Task<IReadOnlyList<PersonalFeatureDefinition>> ListAllAsync(CancellationToken cancellationToken = default)
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
            Task.FromResult(_pending.Concat(_committed).LastOrDefault(e => e.Id == id));

        public Task<IReadOnlyList<PersonalFeatureEntitlement>> ListByUserAndFeatureAsync(
            PlatformUserId personalUserId, FeatureCode featureCode, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PersonalFeatureEntitlement>>(
                _pending.Concat(_committed)
                    .Where(e => e.PersonalUserId == personalUserId && e.FeatureCode == featureCode)
                    .GroupBy(e => e.Id).Select(g => g.Last()).ToList());

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
            PlatformUserId personalUserId, CancellationToken cancellationToken = default)
        {
            if (_pending.TryGetValue(personalUserId.Value, out var pending))
            {
                return Task.FromResult<PersonalRewardBalance?>(Clone(pending));
            }

            return Task.FromResult(_committed.TryGetValue(personalUserId.Value, out var b) ? Clone(b) : null);
        }

        public Task AddAsync(PersonalRewardBalance balance, CancellationToken cancellationToken = default)
        {
            _pending[balance.PersonalUserId.Value] = Clone(balance);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            PersonalRewardBalance balance, int expectedVersion, CancellationToken cancellationToken = default)
        {
            var current = _pending.TryGetValue(balance.PersonalUserId.Value, out var p)
                ? p
                : _committed.GetValueOrDefault(balance.PersonalUserId.Value);
            if (current is null || current.Version != expectedVersion)
            {
                throw new PersistenceConflictException(
                    ApplicationErrorCodes.PersonalRewardBalanceConflict, "version mismatch");
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
            PlatformUserId personalUserId, string idempotencyKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(Committed.Concat(_pending).FirstOrDefault(x =>
                x.PersonalUserId == personalUserId
                && string.Equals(x.IdempotencyKey, idempotencyKey, StringComparison.Ordinal)));

        public Task<(IReadOnlyList<PersonalRewardTransaction> Items, int TotalCount)> ListByUserDescendingAsync(
            PlatformUserId personalUserId, int skip, int take, CancellationToken cancellationToken = default)
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
