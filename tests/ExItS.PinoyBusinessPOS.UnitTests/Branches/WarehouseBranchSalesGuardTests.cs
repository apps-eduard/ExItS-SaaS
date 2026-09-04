using ExItS.PinoyBusinessPOS.Application.Branches;
using ExItS.PinoyBusinessPOS.Application.CashierShifts;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.UnitTests.Branches;

public sealed class WarehouseBranchSalesGuardTests
{
    private static readonly Guid Org = Guid.NewGuid();
    private static readonly Guid Warehouse = Guid.NewGuid();
    private static readonly Guid Retail = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();

    [Fact]
    public async Task Guard_rejects_warehouse_branch_with_exact_message()
    {
        var result = await BranchRetailSalesGuard.RejectIfWarehouseAsync(
            new TypedBranches(),
            Org,
            Warehouse);

        Assert.NotNull(result);
        Assert.Equal(ApplicationErrorCodes.WarehouseBranchSalesForbidden, result!.ErrorCode);
        Assert.Equal(BranchRetailSalesGuard.WarehouseSalesMessage, result.ErrorMessage);
    }

    [Fact]
    public async Task Guard_allows_retail_branch()
    {
        var result = await BranchRetailSalesGuard.RejectIfWarehouseAsync(
            new TypedBranches(),
            Org,
            Retail);

        Assert.Null(result);
    }

    [Fact]
    public async Task SelectOperationalBranch_returns_warehouse_type()
    {
        var sut = new SelectOperationalBranch(new NoOpenShifts(), new TypedBranches());

        var result = await sut.ExecuteAsync(Org, Actor, Warehouse, null, null);

        Assert.True(result.IsSuccess);
        Assert.Equal("Warehouse", result.Value!.BranchType);
    }

    [Fact]
    public async Task SelectOperationalBranch_returns_retail_type_by_default()
    {
        var sut = new SelectOperationalBranch(new NoOpenShifts(), new TypedBranches());

        var result = await sut.ExecuteAsync(Org, Actor, Retail, null, null);

        Assert.True(result.IsSuccess);
        Assert.Equal("Retail", result.Value!.BranchType);
    }

    private sealed class TypedBranches : IOrganizationBranchDirectory
    {
        public Task<bool> ExistsInOrganizationAsync(
            Guid organizationId,
            Guid branchId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(
            Guid organizationId,
            IReadOnlyCollection<Guid> branchIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(
                branchIds.ToDictionary(
                    id => id,
                    id => id == Warehouse ? "Central Warehouse" : "Retail Branch"));

        public Task<bool> IsActiveInOrganizationAsync(
            Guid organizationId,
            Guid branchId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<string> GetBranchTypeAsync(
            Guid organizationId,
            Guid branchId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(branchId == Warehouse ? "Warehouse" : "Retail");
    }

    private sealed class NoOpenShifts : ICashierShiftRepository
    {
        public Task<CashierShift?> GetByIdAsync(
            PosOrganizationId organizationId,
            CashierShiftId shiftId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CashierShift?>(null);

        public Task<bool> HasOpenShiftForActorAsync(
            PosOrganizationId organizationId,
            Guid actorId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<CashierShift?> FindOpenForActorAsync(
            PosOrganizationId organizationId,
            Guid actorId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CashierShift?>(null);

        public Task<CashierShift?> FindOpenForRegisterAsync(
            PosOrganizationId organizationId,
            Guid registerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CashierShift?>(null);

        public Task<(IReadOnlyList<CashierShift> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            CashierShiftFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<CashierShift>, int)>(([], 0));

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
            Task.CompletedTask;

        public Task AddMovementAsync(CashierShiftMovement movement, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<CashierShiftMovement?> GetMovementByIdAsync(
            PosOrganizationId organizationId,
            CashierShiftMovementId movementId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CashierShiftMovement?>(null);

        public Task<IReadOnlyList<CashierShiftMovement>> ListMovementsAsync(
            PosOrganizationId organizationId,
            CashierShiftId shiftId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CashierShiftMovement>>([]);

        public Task<bool> HasLinkedSalesAsync(
            PosOrganizationId organizationId,
            CashierShiftId shiftId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<CashierShiftSalesTotals> GetSalesTotalsAsync(
            PosOrganizationId organizationId,
            CashierShiftId shiftId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new CashierShiftSalesTotals(0, 0, 0, 0, 0, 0, 0, 0, 0));
    }
}
