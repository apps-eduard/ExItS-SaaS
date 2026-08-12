using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Personal;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Personal;
using ExItS.Platform.UnitTests.Support;

namespace ExItS.Platform.UnitTests.Personal;

public sealed class PersonalRewardPointsTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 12, 15, 0, 0, TimeSpan.Zero);
    private static readonly PlatformUserId User = PlatformUserId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly PlatformUserId Other = PlatformUserId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

    [Fact]
    public void Credit_increases_balance()
    {
        var balance = PersonalRewardBalance.Create(User, T0);
        var tx = balance.Credit(50, PersonalRewardSources.AdminAward, T0, reason: "test");
        Assert.Equal(50, balance.AvailablePoints);
        Assert.Equal(PersonalRewardTransactionType.Credit, tx.TransactionType);
        Assert.Equal(50, tx.SignedDelta);
        Assert.Equal(50, tx.BalanceAfter);
        Assert.Equal(User.Value, tx.PersonalUserId.Value);
    }

    [Fact]
    public void Debit_decreases_balance()
    {
        var balance = PersonalRewardBalance.Create(User, T0);
        balance.Credit(100, PersonalRewardSources.AdminAward, T0);
        var tx = balance.Debit(40, PersonalRewardSources.FeatureRedemption, T0.AddMinutes(1));
        Assert.Equal(60, balance.AvailablePoints);
        Assert.Equal(PersonalRewardTransactionType.Debit, tx.TransactionType);
        Assert.Equal(-40, tx.SignedDelta);
        Assert.Equal(60, tx.BalanceAfter);
    }

    [Fact]
    public void Insufficient_balance_is_rejected_and_never_negative()
    {
        var balance = PersonalRewardBalance.Create(User, T0);
        balance.Credit(10, PersonalRewardSources.AdminAward, T0);
        var ex = Assert.Throws<DomainException>(() =>
            balance.Debit(11, PersonalRewardSources.FeatureRedemption, T0.AddMinutes(1)));
        Assert.Equal(DomainErrorCodes.InsufficientPersonalRewardPoints, ex.ErrorCode);
        Assert.Equal(10, balance.AvailablePoints);
    }

    [Fact]
    public async Task Award_creates_credit_and_balance()
    {
        var harness = new Harness();
        var result = await harness.Award.ExecuteAsync(
            User.Value,
            100,
            PersonalRewardSources.AdminAward,
            reason: "seed");
        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(100, result.Value!.Points);
        Assert.Equal(PersonalRewardTransactionType.Credit.ToString(), result.Value.TransactionType);

        var balance = await harness.GetBalance.ExecuteAsync(User);
        Assert.True(balance.IsSuccess);
        Assert.Equal(100, balance.Value!.AvailablePoints);
    }

    [Fact]
    public async Task Award_idempotency_key_does_not_double_credit()
    {
        var harness = new Harness();
        var first = await harness.Award.ExecuteAsync(
            User.Value, 25, PersonalRewardSources.AdminAward, idempotencyKey: "award-1");
        var second = await harness.Award.ExecuteAsync(
            User.Value, 25, PersonalRewardSources.AdminAward, idempotencyKey: "award-1");
        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value!.Id, second.Value!.Id);
        Assert.Equal(25, (await harness.GetBalance.ExecuteAsync(User)).Value!.AvailablePoints);
        Assert.Single(harness.Transactions.Committed.Where(t => t.PersonalUserId == User));
    }

    [Fact]
    public async Task Activity_is_scoped_to_personal_user_and_paged()
    {
        var harness = new Harness();
        await harness.Award.ExecuteAsync(User.Value, 10, PersonalRewardSources.AdminAward, reason: "u1-a");
        await harness.Award.ExecuteAsync(User.Value, 20, PersonalRewardSources.AdminAward, reason: "u1-b");
        await harness.Award.ExecuteAsync(User.Value, 30, PersonalRewardSources.AdminAward, reason: "u1-c");
        await harness.Award.ExecuteAsync(Other.Value, 999, PersonalRewardSources.AdminAward, reason: "other");

        var page = await harness.ListActivity.ExecuteAsync(User, page: 1, pageSize: 2);
        Assert.True(page.IsSuccess);
        Assert.Equal(3, page.Value!.TotalCount);
        Assert.Equal(2, page.Value.Items.Count);
        Assert.True(page.Value.HasMore);
        Assert.All(page.Value.Items, i => Assert.Equal(User.Value, i.PersonalUserId));
        Assert.DoesNotContain(page.Value.Items, i => i.Points == 999);

        var page2 = await harness.ListActivity.ExecuteAsync(User, page: 2, pageSize: 2);
        Assert.Single(page2.Value!.Items);
        Assert.False(page2.Value.HasMore);
    }

    [Fact]
    public async Task Redeem_with_enough_points_debits_and_grants_reward_points_source()
    {
        var harness = new Harness();
        await harness.Award.ExecuteAsync(User.Value, 150, PersonalRewardSources.AdminAward);
        var redeem = await harness.Redeem.ExecuteAsync(User, PersonalFeatureCodes.DigitalRecordsExtended);
        Assert.True(redeem.IsSuccess, redeem.ErrorMessage);
        Assert.False(redeem.Value!.AlreadyActive);
        Assert.Equal(100, redeem.Value.PointsDebited);
        Assert.Equal(50, redeem.Value.AvailablePoints);
        Assert.Equal(nameof(PersonalFeatureGrantSource.RewardPoints), redeem.Value.Entitlement!.GrantSource);
        Assert.True(await harness.EntitlementService.HasActiveEntitlementAsync(
            User, PersonalFeatureCodes.DigitalRecordsExtended, T0));
    }

    [Fact]
    public async Task Insufficient_points_redeems_nothing()
    {
        var harness = new Harness();
        await harness.Award.ExecuteAsync(User.Value, 10, PersonalRewardSources.AdminAward);
        var redeem = await harness.Redeem.ExecuteAsync(User, PersonalFeatureCodes.DigitalRecordsExtended);
        Assert.False(redeem.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.InsufficientPersonalRewardPoints, redeem.ErrorCode);
        Assert.Equal(10, (await harness.GetBalance.ExecuteAsync(User)).Value!.AvailablePoints);
        Assert.False(await harness.EntitlementService.HasActiveEntitlementAsync(
            User, PersonalFeatureCodes.DigitalRecordsExtended, T0));
        Assert.DoesNotContain(
            harness.Transactions.Committed,
            t => t.TransactionType == PersonalRewardTransactionType.Debit);
    }

    [Fact]
    public async Task Unknown_feature_does_not_debit()
    {
        var harness = new Harness();
        await harness.Award.ExecuteAsync(User.Value, 200, PersonalRewardSources.AdminAward);
        var redeem = await harness.Redeem.ExecuteAsync(User, "personal-unknown-feature");
        Assert.False(redeem.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PersonalFeatureDefinitionNotFound, redeem.ErrorCode);
        Assert.Equal(200, (await harness.GetBalance.ExecuteAsync(User)).Value!.AvailablePoints);
    }

    [Fact]
    public async Task Non_redeemable_feature_does_not_debit()
    {
        var harness = new Harness();
        await harness.Definitions.AddAsync(
            PersonalFeatureDefinition.Create(
                FeatureCode.Create("personal-promo-only"),
                "Promo Only",
                T0,
                isActive: true,
                rewardPointsPrice: null));
        await harness.UnitOfWork.SaveChangesAsync();
        await harness.Award.ExecuteAsync(User.Value, 200, PersonalRewardSources.AdminAward);
        var redeem = await harness.Redeem.ExecuteAsync(User, "personal-promo-only");
        Assert.False(redeem.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PersonalFeatureNotRewardRedeemable, redeem.ErrorCode);
        Assert.Equal(200, (await harness.GetBalance.ExecuteAsync(User)).Value!.AvailablePoints);
    }

    [Fact]
    public async Task Already_entitled_user_is_not_charged_again()
    {
        var harness = new Harness();
        await harness.Award.ExecuteAsync(User.Value, 300, PersonalRewardSources.AdminAward);
        var first = await harness.Redeem.ExecuteAsync(User, PersonalFeatureCodes.DigitalRecordsExtended);
        Assert.True(first.IsSuccess);
        var second = await harness.Redeem.ExecuteAsync(User, PersonalFeatureCodes.DigitalRecordsExtended);
        Assert.True(second.IsSuccess, second.ErrorMessage);
        Assert.True(second.Value!.AlreadyActive);
        Assert.Null(second.Value.PointsDebited);
        Assert.Equal(200, second.Value.AvailablePoints);
        Assert.Single(harness.Transactions.Committed.Where(t => t.TransactionType == PersonalRewardTransactionType.Debit));
    }

    [Fact]
    public async Task Concurrent_redeem_when_already_active_does_not_double_debit()
    {
        var harness = new Harness();
        await harness.Award.ExecuteAsync(User.Value, 100, PersonalRewardSources.AdminAward);
        var first = await harness.Redeem.ExecuteAsync(User, PersonalFeatureCodes.DigitalRecordsExtended);
        Assert.True(first.IsSuccess);

        var second = await harness.Redeem.ExecuteAsync(User, PersonalFeatureCodes.DigitalRecordsExtended);
        Assert.True(second.IsSuccess, second.ErrorMessage);
        Assert.True(second.Value!.AlreadyActive);
        Assert.Null(second.Value.PointsDebited);
        Assert.Equal(0, second.Value.AvailablePoints);
        Assert.Single(harness.Transactions.Committed.Where(t => t.TransactionType == PersonalRewardTransactionType.Debit));
    }

    [Fact]
    public async Task Concurrent_save_conflict_when_peer_won_returns_already_active_without_extra_debit()
    {
        var harness = new Harness();
        await harness.Definitions.AddAsync(
            PersonalFeatureDefinition.Create(
                PersonalFeatureCodes.DigitalRecordsExtendedCode,
                "Digital Records Extended History",
                T0,
                isActive: true,
                rewardPointsPrice: PersonalFeatureCodes.DigitalRecordsExtendedDefaultRewardPoints));
        await harness.UnitOfWork.SaveChangesAsync();
        await harness.Award.ExecuteAsync(User.Value, 100, PersonalRewardSources.AdminAward);

        harness.OnBeforeCommit = () =>
        {
            // Peer already committed entitlement + debit; this SaveChanges must roll back.
            harness.Entitlements.CommitDirect(
                PersonalFeatureEntitlement.Grant(
                    User,
                    PersonalFeatureCodes.DigitalRecordsExtendedCode,
                    PersonalFeatureGrantSource.RewardPoints,
                    T0,
                    null,
                    T0));
            harness.Balances.CommitDirect(
                PersonalRewardBalance.Rehydrate(User, 0, T0, T0, version: 3));
            harness.Transactions.CommitDirect(
                PersonalRewardTransaction.Rehydrate(
                    Guid.NewGuid(),
                    User,
                    PersonalRewardTransactionType.Debit,
                    100,
                    -100,
                    0,
                    PersonalRewardSources.FeatureRedemption,
                    "peer",
                    PersonalFeatureCodes.DigitalRecordsExtended,
                    null,
                    T0));
            throw new PersistenceConflictException(
                ApplicationErrorCodes.PersonalRewardBalanceConflict,
                "conflict");
        };

        var result = await harness.Redeem.ExecuteAsync(User, PersonalFeatureCodes.DigitalRecordsExtended);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.True(result.Value!.AlreadyActive);
        Assert.Null(result.Value.PointsDebited);
        Assert.Equal(0, result.Value.AvailablePoints);
        Assert.Single(harness.Transactions.Committed, t => t.TransactionType == PersonalRewardTransactionType.Debit);
        Assert.Single(harness.Entitlements.Committed);
    }

    [Fact]
    public async Task Successful_redeem_unlocks_extended_history_entitlement_check()
    {
        var harness = new Harness();
        await harness.Award.ExecuteAsync(User.Value, 100, PersonalRewardSources.AdminAward);
        Assert.False(await harness.EntitlementService.HasActiveEntitlementAsync(
            User, PersonalFeatureCodes.DigitalRecordsExtended, T0));
        await harness.Redeem.ExecuteAsync(User, PersonalFeatureCodes.DigitalRecordsExtended);
        Assert.True(await harness.EntitlementService.HasActiveEntitlementAsync(
            User, PersonalFeatureCodes.DigitalRecordsExtended, T0));
    }

    [Fact]
    public async Task Other_user_balance_and_activity_stay_isolated()
    {
        var harness = new Harness();
        await harness.Award.ExecuteAsync(User.Value, 40, PersonalRewardSources.AdminAward);
        await harness.Award.ExecuteAsync(Other.Value, 70, PersonalRewardSources.AdminAward);

        Assert.Equal(40, (await harness.GetBalance.ExecuteAsync(User)).Value!.AvailablePoints);
        Assert.Equal(70, (await harness.GetBalance.ExecuteAsync(Other)).Value!.AvailablePoints);

        var userActivity = await harness.ListActivity.ExecuteAsync(User);
        Assert.All(userActivity.Value!.Items, i => Assert.Equal(User.Value, i.PersonalUserId));
        Assert.DoesNotContain(userActivity.Value.Items, i => i.Points == 70);
    }

    [Fact]
    public async Task Organization_context_redemption_is_rejected_without_debit_or_entitlement()
    {
        var harness = new Harness();
        await harness.Award.ExecuteAsync(User.Value, 150, PersonalRewardSources.AdminAward);
        var orgId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var redeem = await harness.Redeem.ExecuteAsync(
            User,
            PersonalFeatureCodes.DigitalRecordsExtended,
            organizationId: orgId);
        Assert.False(redeem.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.OrganizationRewardRedemptionUnsupported, redeem.ErrorCode);
        Assert.Equal(150, (await harness.GetBalance.ExecuteAsync(User)).Value!.AvailablePoints);
        Assert.Empty(harness.Transactions.Committed.Where(t => t.TransactionType == PersonalRewardTransactionType.Debit));
        Assert.False(await harness.EntitlementService.HasActiveEntitlementAsync(
            User, PersonalFeatureCodes.DigitalRecordsExtended, T0));
    }

    [Fact]
    public async Task Organization_staff_identity_cannot_redeem_personal_rewards()
    {
        var harness = new Harness();
        var staff = PlatformUser.CreateOrganizationStaff(
            "staff1",
            "staff1@ORG000001",
            "staff1@example.com",
            PlatformOrganizationId.From(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd")),
            "Staff One",
            T0,
            id: PlatformUserId.From(Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee")));
        harness.Users.AddAsync(staff);
        await harness.Award.ExecuteAsync(staff.Id.Value, 150, PersonalRewardSources.AdminAward);
        var redeem = await harness.Redeem.ExecuteAsync(staff.Id, PersonalFeatureCodes.DigitalRecordsExtended);
        Assert.False(redeem.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.OrganizationRewardRedemptionUnsupported, redeem.ErrorCode);
        Assert.Equal(150, (await harness.GetBalance.ExecuteAsync(staff.Id)).Value!.AvailablePoints);
        Assert.False(await harness.EntitlementService.HasActiveEntitlementAsync(
            staff.Id, PersonalFeatureCodes.DigitalRecordsExtended, T0));
    }

    [Fact]
    public void Organization_feature_unlock_rejects_reward_points_source()
    {
        var result = OrganizationRewardRedemptionGuard.RejectOrganizationFeatureRewardPoints("RewardPoints");
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.OrganizationRewardRedemptionUnsupported, result.ErrorCode);
        Assert.True(OrganizationRewardRedemptionGuard.RejectOrganizationFeatureRewardPoints("Plan").IsSuccess);
    }

    private sealed class Harness
    {
        public InMemoryDefinitions Definitions { get; } = new();
        public InMemoryEntitlements Entitlements { get; } = new();
        public InMemoryBalances Balances { get; } = new();
        public InMemoryTransactions Transactions { get; } = new();
        public InMemoryPlatformUserRepository Users { get; } = new();
        public ControllableUnitOfWork UnitOfWork { get; }
        public PersonalFeatureEntitlementService EntitlementService { get; }
        public AwardPersonalRewardPoints Award { get; }
        public GetPersonalRewardPointsBalance GetBalance { get; }
        public ListPersonalRewardPointsActivity ListActivity { get; }
        public RedeemPersonalFeatureWithRewardPoints Redeem { get; }
        public Action? OnBeforeCommit { get; set; }

        public Harness()
        {
            Users.AddAsync(PlatformUser.Create("personal1", "Personal One", "personal1@example.com", T0, id: User));
            Users.AddAsync(PlatformUser.Create("personal2", "Personal Two", "personal2@example.com", T0, id: Other));
            var clock = new FixedClock(T0);
            UnitOfWork = new ControllableUnitOfWork(this);
            EntitlementService = new PersonalFeatureEntitlementService(Definitions, Entitlements);
            Award = new AwardPersonalRewardPoints(Balances, Transactions, UnitOfWork, clock);
            GetBalance = new GetPersonalRewardPointsBalance(Balances);
            ListActivity = new ListPersonalRewardPointsActivity(Transactions);
            Redeem = new RedeemPersonalFeatureWithRewardPoints(
                Definitions, Entitlements, EntitlementService, Balances, Transactions, Users, UnitOfWork, clock);
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
                return Task.CompletedTask;
            }
            catch
            {
                harness.Definitions.Rollback();
                harness.Entitlements.Rollback();
                harness.Balances.Rollback();
                harness.Transactions.Rollback();
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

        public void CommitDirect(PersonalFeatureEntitlement entitlement) => Committed.Add(entitlement);
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
}
