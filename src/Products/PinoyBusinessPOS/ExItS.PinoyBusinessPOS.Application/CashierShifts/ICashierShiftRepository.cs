using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Application.CashierShifts;

public sealed record CashierShiftFilter(
    CashierShiftStatus? Status = null,
    Guid? ActorId = null,
    string? ShiftNumber = null,
    DateOnly? FromBusinessDate = null,
    DateOnly? ToBusinessDate = null,
    Guid? RegisterId = null);

public interface ICashierShiftRepository
{
    Task<CashierShift?> GetByIdAsync(
        PosOrganizationId organizationId,
        CashierShiftId shiftId,
        CancellationToken cancellationToken = default);

    Task<bool> HasOpenShiftForActorAsync(
        PosOrganizationId organizationId,
        Guid actorId,
        CancellationToken cancellationToken = default);

    Task<CashierShift?> FindOpenForActorAsync(
        PosOrganizationId organizationId,
        Guid actorId,
        CancellationToken cancellationToken = default);

    Task<CashierShift?> FindOpenForRegisterAsync(
        PosOrganizationId organizationId,
        Guid registerId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<CashierShift> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        CashierShiftFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<CashierShift> OpenAsync(
        PosOrganizationId organizationId,
        DateOnly businessDateUtc,
        Guid actorId,
        decimal openingCashAmount,
        Guid openedBy,
        Func<string, CashierShift> createShift,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(CashierShift shift, CancellationToken cancellationToken = default);

    Task AddMovementAsync(CashierShiftMovement movement, CancellationToken cancellationToken = default);

    Task<CashierShiftMovement?> GetMovementByIdAsync(
        PosOrganizationId organizationId,
        CashierShiftMovementId movementId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CashierShiftMovement>> ListMovementsAsync(
        PosOrganizationId organizationId,
        CashierShiftId shiftId,
        CancellationToken cancellationToken = default);

    Task<bool> HasLinkedSalesAsync(
        PosOrganizationId organizationId,
        CashierShiftId shiftId,
        CancellationToken cancellationToken = default);

    Task<CashierShiftSalesTotals> GetSalesTotalsAsync(
        PosOrganizationId organizationId,
        CashierShiftId shiftId,
        CancellationToken cancellationToken = default);
}

public sealed record CashierShiftSalesTotals(
    decimal NetCashSales,
    decimal CashSalesTotal,
    decimal GCashSalesTotal,
    decimal UtangSalesTotal,
    decimal CashRefundsTotal,
    int CompletedCashCount,
    int VoidedCashCount,
    int CompletedGCashCount,
    int CompletedUtangCount);
