using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Personal;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Personal;

namespace ExItS.Platform.UnitTests.Personal;

public sealed class PersonalFeatureEntitlementTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 12, 12, 0, 0, TimeSpan.Zero);
    private static readonly PlatformUserId User = PlatformUserId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    [Fact]
    public async Task No_entitlement_returns_false()
    {
        var harness = new Harness();
        Assert.False(await harness.Service.HasActiveEntitlementAsync(
            User, PersonalFeatureCodes.DigitalRecordsExtended, T0));
    }

    [Fact]
    public async Task Active_entitlement_returns_true()
    {
        var harness = new Harness();
        var grant = await harness.Grant.ExecuteAsync(
            User.Value,
            PersonalFeatureCodes.DigitalRecordsExtended,
            PersonalFeatureGrantSource.AdminGrant,
            startsAtUtc: T0.AddDays(-1),
            endsAtUtc: null);
        Assert.True(grant.IsSuccess, grant.ErrorMessage);
        Assert.True(await harness.Service.HasActiveEntitlementAsync(
            User, PersonalFeatureCodes.DigitalRecordsExtended, T0));
    }

    [Fact]
    public async Task Expired_entitlement_returns_false()
    {
        var harness = new Harness();
        await harness.Grant.ExecuteAsync(
            User.Value,
            PersonalFeatureCodes.DigitalRecordsExtended,
            PersonalFeatureGrantSource.AdminGrant,
            startsAtUtc: T0.AddMonths(-2),
            endsAtUtc: T0.AddDays(-1));
        Assert.False(await harness.Service.HasActiveEntitlementAsync(
            User, PersonalFeatureCodes.DigitalRecordsExtended, T0));
    }

    [Fact]
    public async Task Future_entitlement_returns_false()
    {
        var harness = new Harness();
        await harness.Grant.ExecuteAsync(
            User.Value,
            PersonalFeatureCodes.DigitalRecordsExtended,
            PersonalFeatureGrantSource.Promotion,
            startsAtUtc: T0.AddDays(1),
            endsAtUtc: null);
        Assert.False(await harness.Service.HasActiveEntitlementAsync(
            User, PersonalFeatureCodes.DigitalRecordsExtended, T0));
    }

    [Fact]
    public async Task Indefinite_entitlement_returns_true()
    {
        var harness = new Harness();
        await harness.Grant.ExecuteAsync(
            User.Value,
            PersonalFeatureCodes.DigitalRecordsExtended,
            PersonalFeatureGrantSource.CashPurchase,
            startsAtUtc: T0.AddDays(-10),
            endsAtUtc: null);
        Assert.True(await harness.Service.HasActiveEntitlementAsync(
            User, PersonalFeatureCodes.DigitalRecordsExtended, T0));
    }

    [Fact]
    public async Task Revoked_entitlement_returns_false()
    {
        var harness = new Harness();
        var grant = await harness.Grant.ExecuteAsync(
            User.Value,
            PersonalFeatureCodes.DigitalRecordsExtended,
            PersonalFeatureGrantSource.AdminGrant,
            startsAtUtc: T0.AddDays(-1));
        Assert.True(grant.IsSuccess);
        var revoked = await harness.Revoke.ExecuteAsync(grant.Value!.Id, "test");
        Assert.True(revoked.IsSuccess);
        Assert.False(await harness.Service.HasActiveEntitlementAsync(
            User, PersonalFeatureCodes.DigitalRecordsExtended, T0));
    }

    [Fact]
    public async Task Grant_is_idempotent_for_overlapping_active_window()
    {
        var harness = new Harness();
        var first = await harness.Grant.ExecuteAsync(
            User.Value,
            PersonalFeatureCodes.DigitalRecordsExtended,
            PersonalFeatureGrantSource.AdminGrant,
            startsAtUtc: T0.AddDays(-1),
            endsAtUtc: null);
        var second = await harness.Grant.ExecuteAsync(
            User.Value,
            PersonalFeatureCodes.DigitalRecordsExtended,
            PersonalFeatureGrantSource.AdminGrant,
            startsAtUtc: T0,
            endsAtUtc: null);
        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value!.Id, second.Value!.Id);
        Assert.Single(harness.Entitlements.All);
    }

    [Fact]
    public async Task Organization_feature_code_does_not_satisfy_personal_entitlement()
    {
        var harness = new Harness();
        await harness.Grant.ExecuteAsync(
            User.Value,
            FeatureCode.CustomerCreditView,
            PersonalFeatureGrantSource.AdminGrant,
            startsAtUtc: T0.AddDays(-1));
        Assert.False(await harness.Service.HasActiveEntitlementAsync(
            User, PersonalFeatureCodes.DigitalRecordsExtended, T0));
    }

    [Fact]
    public async Task Different_personal_user_does_not_inherit_entitlement()
    {
        var harness = new Harness();
        await harness.Grant.ExecuteAsync(
            User.Value,
            PersonalFeatureCodes.DigitalRecordsExtended,
            PersonalFeatureGrantSource.AdminGrant,
            startsAtUtc: T0.AddDays(-1));
        var other = PlatformUserId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        Assert.False(await harness.Service.HasActiveEntitlementAsync(
            other, PersonalFeatureCodes.DigitalRecordsExtended, T0));
    }

    private sealed class Harness
    {
        public InMemoryDefinitions Definitions { get; } = new();
        public InMemoryEntitlements Entitlements { get; } = new();
        public PersonalFeatureEntitlementService Service { get; }
        public GrantPersonalFeature Grant { get; }
        public RevokePersonalFeature Revoke { get; }

        public Harness()
        {
            var clock = new FixedClock(T0);
            var uow = new NoopUnitOfWork();
            Service = new PersonalFeatureEntitlementService(Definitions, Entitlements);
            Grant = new GrantPersonalFeature(Definitions, Entitlements, uow, clock);
            Revoke = new RevokePersonalFeature(Entitlements, uow, clock);
        }
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class NoopUnitOfWork : IPlatformUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class InMemoryDefinitions : IPersonalFeatureDefinitionRepository
    {
        private readonly Dictionary<string, PersonalFeatureDefinition> _items = new(StringComparer.Ordinal);

        public Task<PersonalFeatureDefinition?> GetByCodeAsync(
            FeatureCode featureCode,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.TryGetValue(featureCode.Value, out var d) ? d : null);

        public Task<IReadOnlyList<PersonalFeatureDefinition>> ListAllAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PersonalFeatureDefinition>>(
                _items.Values.OrderBy(d => d.FeatureCode.Value, StringComparer.Ordinal).ToList());

        public Task AddAsync(PersonalFeatureDefinition definition, CancellationToken cancellationToken = default)
        {
            _items[definition.FeatureCode.Value] = definition;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(PersonalFeatureDefinition definition, CancellationToken cancellationToken = default)
        {
            _items[definition.FeatureCode.Value] = definition;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryEntitlements : IPersonalFeatureEntitlementRepository
    {
        public List<PersonalFeatureEntitlement> All { get; } = [];

        public Task<PersonalFeatureEntitlement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(All.FirstOrDefault(e => e.Id == id));

        public Task<IReadOnlyList<PersonalFeatureEntitlement>> ListByUserAndFeatureAsync(
            PlatformUserId personalUserId,
            FeatureCode featureCode,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PersonalFeatureEntitlement>>(
                All.Where(e => e.PersonalUserId == personalUserId && e.FeatureCode == featureCode).ToList());

        public Task AddAsync(PersonalFeatureEntitlement entitlement, CancellationToken cancellationToken = default)
        {
            All.Add(entitlement);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(PersonalFeatureEntitlement entitlement, CancellationToken cancellationToken = default)
        {
            var i = All.FindIndex(e => e.Id == entitlement.Id);
            if (i >= 0)
            {
                All[i] = entitlement;
            }

            return Task.CompletedTask;
        }
    }
}
