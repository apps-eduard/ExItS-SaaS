using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.OperationalSetup;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.OperationalSetup;
using ExItS.PinoyBusinessPOS.Domain.Registers;

namespace ExItS.PinoyBusinessPOS.UnitTests.OperationalSetup;

public sealed class OperationalSetupTaxConfigurationWriteTests
{
    private static readonly Guid OrgId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ActorId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly DateTimeOffset T0 = new(2026, 8, 16, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Complete_rejects_tax_write_when_capability_disabled()
    {
        var setups = new FakeSetups();
        var useCase = CreateComplete(setups, taxEnabled: false);

        var result = await useCase.ExecuteAsync(
            OrgId,
            ActorId,
            new CompleteOperationalSetupRequest("Corner Store", "PHP", "TaxExclusive", 12m));

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.TaxConfigurationNotEnabled, result.ErrorCode);
        Assert.Null(setups.Stored);
    }

    [Fact]
    public async Task Complete_allows_zero_exclusive_when_capability_disabled()
    {
        var setups = new FakeSetups();
        var useCase = CreateComplete(setups, taxEnabled: false);

        var result = await useCase.ExecuteAsync(
            OrgId,
            ActorId,
            new CompleteOperationalSetupRequest("Corner Store", "PHP", "TaxExclusive", 0m));

        Assert.True(result.IsSuccess, $"{result.ErrorCode}: {result.ErrorMessage}");
        Assert.NotNull(result.Value);
        Assert.False(result.Value!.TaxConfigurationEnabled);
        Assert.Equal(0m, result.Value.TaxRatePercent);
        Assert.Equal(nameof(TaxPricingMode.TaxExclusive), result.Value.TaxPricingMode);
        Assert.NotNull(setups.Stored);
        Assert.Equal(0m, setups.Stored!.TaxRatePercent);
    }

    [Fact]
    public async Task Complete_allows_tax_write_when_capability_enabled()
    {
        var setups = new FakeSetups();
        var useCase = CreateComplete(setups, taxEnabled: true);

        var result = await useCase.ExecuteAsync(
            OrgId,
            ActorId,
            new CompleteOperationalSetupRequest("Corner Store", "PHP", "TaxInclusive", 12m));

        Assert.True(result.IsSuccess, $"{result.ErrorCode}: {result.ErrorMessage}");
        Assert.NotNull(result.Value);
        Assert.True(result.Value!.TaxConfigurationEnabled);
        Assert.Equal(12m, result.Value.TaxRatePercent);
        Assert.Equal(nameof(TaxPricingMode.TaxInclusive), result.Value.TaxPricingMode);
    }

    [Fact]
    public async Task Update_rejects_tax_change_when_capability_disabled()
    {
        var completed = CompleteSetup(taxMode: TaxPricingMode.TaxExclusive, taxRate: 0m);
        var setups = new FakeSetups { Stored = completed };
        var useCase = CreateUpdate(setups, taxEnabled: false);

        var result = await useCase.ExecuteAsync(
            OrgId,
            ActorId,
            new UpdateOperationalSetupRequest(
                "Corner Store",
                "PHP",
                "TaxExclusive",
                12m,
                completed.UpdatedAtUtc));

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.TaxConfigurationNotEnabled, result.ErrorCode);
        Assert.Equal(0m, setups.Stored!.TaxRatePercent);
    }

    [Fact]
    public async Task Update_keeps_existing_tax_when_capability_disabled_and_unchanged()
    {
        var completed = CompleteSetup(taxMode: TaxPricingMode.TaxInclusive, taxRate: 12m);
        var setups = new FakeSetups { Stored = completed };
        var useCase = CreateUpdate(setups, taxEnabled: false);

        var result = await useCase.ExecuteAsync(
            OrgId,
            ActorId,
            new UpdateOperationalSetupRequest(
                "Renamed Store",
                "PHP",
                "TaxInclusive",
                12m,
                completed.UpdatedAtUtc,
                ReceiptHeader: "Hello"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Renamed Store", result.Value!.StoreDisplayName);
        Assert.Equal(12m, result.Value.TaxRatePercent);
        Assert.Equal(nameof(TaxPricingMode.TaxInclusive), result.Value.TaxPricingMode);
        Assert.False(result.Value.TaxConfigurationEnabled);
    }

    [Fact]
    public async Task Update_allows_tax_change_when_capability_enabled()
    {
        var completed = CompleteSetup(taxMode: TaxPricingMode.TaxExclusive, taxRate: 0m);
        var setups = new FakeSetups { Stored = completed };
        var useCase = CreateUpdate(setups, taxEnabled: true);

        var result = await useCase.ExecuteAsync(
            OrgId,
            ActorId,
            new UpdateOperationalSetupRequest(
                "Corner Store",
                "PHP",
                "TaxInclusive",
                12m,
                completed.UpdatedAtUtc));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.TaxConfigurationEnabled);
        Assert.Equal(12m, result.Value.TaxRatePercent);
        Assert.Equal(nameof(TaxPricingMode.TaxInclusive), result.Value.TaxPricingMode);
    }

    [Fact]
    public async Task Get_exposes_tax_configuration_flag_from_reader()
    {
        var setups = new FakeSetups { Stored = CompleteSetup(TaxPricingMode.TaxExclusive, 0m) };
        var query = new GetOperationalSetupQuery(setups, new FakeTaxReader(true), FixedTimeProvider.Instance);

        var dto = await query.ExecuteAsync(OrgId, ActorId);

        Assert.True(dto.TaxConfigurationEnabled);
        Assert.True(dto.IsCompleted);
    }

    private static CompleteOperationalSetup CreateComplete(FakeSetups setups, bool taxEnabled) =>
        new(
            setups,
            new FakeRegisters(),
            new FakeDenominations(),
            new FakeUow(),
            new FakeAccess(),
            new FakeTaxReader(taxEnabled),
            FixedTimeProvider.Instance);

    private static UpdateOperationalSetup CreateUpdate(FakeSetups setups, bool taxEnabled) =>
        new(
            setups,
            new FakeUow(),
            new FakeAccess(),
            new FakeTaxReader(taxEnabled),
            FixedTimeProvider.Instance);

    private static PosOperationalSetup CompleteSetup(TaxPricingMode taxMode, decimal taxRate)
    {
        var setup = PosOperationalSetup.CreateIncomplete(PosOrganizationId.From(OrgId), ActorId, T0);
        setup.Complete(
            "Corner Store",
            "PHP",
            taxMode,
            taxRate,
            receiptHeader: null,
            receiptFooter: null,
            businessAddress: null,
            contactPhone: null,
            RegisterId.New(),
            ActorId,
            T0);
        return setup;
    }

    private sealed class FakeTaxReader(bool enabled) : IOrganizationTaxConfigurationCapabilityReader
    {
        public Task<bool> IsTaxConfigurationEnabledAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(enabled);
    }

    private sealed class FakeAccess : IPosCommercialAccessAccessor
    {
        public PosCommercialAccess Current { get; set; } = PosCommercialAccess.DevelopmentDefault;
    }

    private sealed class FakeUow : IPosUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<T> ExecuteInSerializableTransactionAsync<T>(
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken = default) =>
            action(cancellationToken);
    }

    private sealed class FakeSetups : IPosOperationalSetupRepository
    {
        public PosOperationalSetup? Stored { get; set; }

        public Task<PosOperationalSetup?> GetByOrganizationIdAsync(
            PosOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Stored);

        public Task AddAsync(PosOperationalSetup setup, CancellationToken cancellationToken = default)
        {
            Stored = setup;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(PosOperationalSetup setup, CancellationToken cancellationToken = default)
        {
            Stored = setup;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeRegisters : IRegisterRepository
    {
        private Register? _register;

        public Task AddAsync(Register register, CancellationToken cancellationToken = default)
        {
            _register = register;
            return Task.CompletedTask;
        }

        public Task<string> AllocateNextRegisterCodeAsync(
            PosOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult("REG-000001");

        public Task<Register?> FindByNormalizedNameAsync(
            PosOrganizationId organizationId,
            string normalizedName,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Register?>(null);

        public Task<Register?> GetByIdAsync(
            PosOrganizationId organizationId,
            RegisterId registerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_register);

        public Task<bool> HasOpenShiftAsync(
            PosOrganizationId organizationId,
            RegisterId registerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<(IReadOnlyList<Register> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            RegisterFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<Register>, int)>(
                (_register is null ? [] : [_register], _register is null ? 0 : 1));

        public Task<IReadOnlyList<Register>> ListAvailableForShiftAsync(
            PosOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Register>>([]);

        public Task UpdateAsync(Register register, CancellationToken cancellationToken = default)
        {
            _register = register;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeDenominations : IOrganizationCashDenominationRepository
    {
        private IReadOnlyList<OrganizationCashDenomination> _items = [];

        public Task<IReadOnlyList<OrganizationCashDenomination>> ListAsync(
            PosOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items);

        public Task ReplaceAsync(
            PosOrganizationId organizationId,
            IReadOnlyList<OrganizationCashDenomination> denominations,
            CancellationToken cancellationToken = default)
        {
            _items = denominations;
            return Task.CompletedTask;
        }
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public static readonly FixedTimeProvider Instance = new();

        public override DateTimeOffset GetUtcNow() => T0;
    }
}
