using ExItS.PinoyBusinessPOS.Application.Branches;
using ExItS.PinoyBusinessPOS.Application.CashierShifts;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Registers;

namespace ExItS.PinoyBusinessPOS.UnitTests.Branches;

public sealed class SelectOperationalBranchTests
{
    private static readonly Guid Org = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly Guid Main = Guid.NewGuid();
    private static readonly Guid BranchB = Guid.NewGuid();
    private static readonly DateTimeOffset T0 = new(2026, 8, 18, 20, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Selects_active_branch_when_no_open_shift()
    {
        var sut = new SelectOperationalBranch(new FakeShifts(), new FakeBranches());

        var result = await sut.ExecuteAsync(Org, Actor, BranchB, Main, Main);

        Assert.True(result.IsSuccess);
        Assert.Equal(BranchB, result.Value!.BranchId);
        Assert.False(result.Value.DeviceMatchesSelectedBranch);
        Assert.False(result.Value.OpenCashierShiftPresent);
    }

    [Fact]
    public async Task Open_shift_blocks_switch_to_another_branch()
    {
        var shift = CashierShift.Rehydrate(
            CashierShiftId.New(),
            PosOrganizationId.From(Org),
            "S-1",
            Actor,
            RegisterId.New(),
            CashierShiftStatus.Open,
            DateOnly.FromDateTime(T0.UtcDateTime),
            100m,
            T0,
            Actor,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            T0,
            T0);
        var sut = new SelectOperationalBranch(new FakeShifts(shift), new FakeBranches());

        var result = await sut.ExecuteAsync(Org, Actor, BranchB, Main, Main);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.OperationalBranchSwitchBlocked, result.ErrorCode);
    }

    [Fact]
    public async Task Foreign_or_unknown_branch_is_rejected()
    {
        var sut = new SelectOperationalBranch(new FakeShifts(), new FakeBranches());

        var result = await sut.ExecuteAsync(Org, Actor, Guid.NewGuid(), Main, Main);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CustomerOrderBranchNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task Device_match_is_reported_when_selected_equals_device_branch()
    {
        var sut = new SelectOperationalBranch(new FakeShifts(), new FakeBranches());

        var result = await sut.ExecuteAsync(Org, Actor, Main, Main, Main);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.DeviceMatchesSelectedBranch);
    }

    [Fact]
    public async Task Open_shift_allows_reselecting_the_same_operational_branch()
    {
        var shift = CashierShift.Rehydrate(
            CashierShiftId.New(),
            PosOrganizationId.From(Org),
            "S-1",
            Actor,
            RegisterId.New(),
            CashierShiftStatus.Open,
            DateOnly.FromDateTime(T0.UtcDateTime),
            100m,
            T0,
            Actor,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            T0,
            T0);
        var sut = new SelectOperationalBranch(new FakeShifts(shift), new FakeBranches());

        var result = await sut.ExecuteAsync(Org, Actor, Main, Main, Main);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.OpenCashierShiftPresent);
    }

    private sealed class FakeBranches : IOrganizationBranchDirectory
    {
        public Task<bool> ExistsInOrganizationAsync(
            Guid organizationId,
            Guid branchId,
            CancellationToken cancellationToken = default) =>
            IsActiveInOrganizationAsync(organizationId, branchId, cancellationToken);

        public Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(
            Guid organizationId,
            IReadOnlyCollection<Guid> branchIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(
                branchIds
                    .Where(id => id == Main || id == BranchB)
                    .ToDictionary(id => id, id => id == Main ? "Main Branch" : "Branch B"));

        public Task<bool> IsActiveInOrganizationAsync(
            Guid organizationId,
            Guid branchId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(organizationId == Org && (branchId == Main || branchId == BranchB));
    }

    private sealed class FakeShifts(CashierShift? open = null) : ICashierShiftRepository
    {
        public Task<bool> HasOpenShiftForActorAsync(
            PosOrganizationId organizationId,
            Guid actorId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(open is not null);

        public Task<CashierShift?> FindOpenForActorAsync(
            PosOrganizationId organizationId,
            Guid actorId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(open);

        public Task<CashierShift?> GetByIdAsync(
            PosOrganizationId organizationId,
            CashierShiftId shiftId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<CashierShift?> FindOpenForRegisterAsync(
            PosOrganizationId organizationId,
            Guid registerId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(IReadOnlyList<CashierShift> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            CashierShiftFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<CashierShift> OpenAsync(
            PosOrganizationId organizationId,
            DateOnly businessDateUtc,
            Guid actorId,
            decimal openingCashAmount,
            Guid openedBy,
            Func<string, CashierShift> createShift,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateAsync(CashierShift shift, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task AddMovementAsync(CashierShiftMovement movement, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<CashierShiftMovement?> GetMovementByIdAsync(
            PosOrganizationId organizationId,
            CashierShiftMovementId movementId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<CashierShiftMovement>> ListMovementsAsync(
            PosOrganizationId organizationId,
            CashierShiftId shiftId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> HasLinkedSalesAsync(
            PosOrganizationId organizationId,
            CashierShiftId shiftId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<CashierShiftSalesTotals> GetSalesTotalsAsync(
            PosOrganizationId organizationId,
            CashierShiftId shiftId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
