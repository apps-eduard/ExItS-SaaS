using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.CashierShifts;

public sealed class CashierShiftQueryService
{
    private readonly ICashierShiftRepository _shifts;

    public CashierShiftQueryService(ICashierShiftRepository shifts) => _shifts = shifts;

    public async Task<PosCashierShiftDto?> GetByIdAsync(
        Guid organizationId,
        Guid shiftId,
        CancellationToken cancellationToken = default)
    {
        var shift = await _shifts
            .GetByIdAsync(PosOrganizationId.From(organizationId), CashierShiftId.From(shiftId), cancellationToken)
            .ConfigureAwait(false);
        return shift is null ? null : Map(shift);
    }

    public async Task<PosCashierShiftDto?> GetCurrentOpenForActorAsync(
        Guid organizationId,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var shift = await _shifts
            .FindOpenForActorAsync(PosOrganizationId.From(organizationId), actorId, cancellationToken)
            .ConfigureAwait(false);
        return shift is null ? null : Map(shift);
    }

    public async Task<PagedResult<PosCashierShiftDto>> ListAsync(
        Guid organizationId,
        CashierShiftFilter filter,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var (items, total) = await _shifts
            .ListAsync(PosOrganizationId.From(organizationId), filter, skip, take, cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<PosCashierShiftDto>(
            items.Select(Map).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }

    public async Task<PosCashierShiftSummaryDto?> GetSummaryAsync(
        Guid organizationId,
        Guid shiftId,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var id = CashierShiftId.From(shiftId);
        var shift = await _shifts.GetByIdAsync(orgId, id, cancellationToken).ConfigureAwait(false);
        if (shift is null)
        {
            return null;
        }

        var movements = await _shifts.ListMovementsAsync(orgId, id, cancellationToken).ConfigureAwait(false);
        var salesTotals = await _shifts.GetSalesTotalsAsync(orgId, id, cancellationToken).ConfigureAwait(false);
        var expected = shift.Status == CashierShiftStatus.Open
            ? CashierShiftExpectedCash.Compute(
                shift.OpeningCashAmount,
                salesTotals.NetCashSales,
                movements,
                salesTotals.CashRefundsTotal)
            : shift.ExpectedCashAmountSnapshot ?? 0m;

        return new PosCashierShiftSummaryDto(
            shift.Id.Value,
            shift.ShiftNumber,
            shift.Status.ToString(),
            shift.OpeningCashAmount,
            salesTotals.NetCashSales,
            salesTotals.CashSalesTotal,
            salesTotals.GCashSalesTotal,
            salesTotals.UtangSalesTotal,
            salesTotals.CashRefundsTotal,
            movements.Where(m => m.MovementType == CashierShiftMovementType.CashIn).Sum(m => m.Amount),
            movements.Where(m => m.MovementType == CashierShiftMovementType.CashOut).Sum(m => m.Amount),
            expected,
            shift.ClosingCashAmount,
            shift.ExpectedCashAmountSnapshot,
            shift.CashVarianceAmount,
            salesTotals.CompletedCashCount,
            salesTotals.VoidedCashCount,
            salesTotals.CompletedGCashCount,
            salesTotals.CompletedUtangCount,
            movements.Select(MapMovement).ToList());
    }

    public static PosCashierShiftDto Map(CashierShift shift) =>
        new(
            shift.Id.Value,
            shift.OrganizationId.Value,
            shift.ShiftNumber,
            shift.Status.ToString(),
            shift.ActorId,
            shift.BusinessDate,
            shift.OpeningCashAmount,
            shift.OpenedAtUtc,
            shift.OpenedBy,
            shift.ClosingCashAmount,
            shift.ExpectedCashAmountSnapshot,
            shift.CashVarianceAmount,
            shift.ClosingNotes,
            shift.ClosedAtUtc,
            shift.ClosedBy,
            shift.CancelledAtUtc,
            shift.CancelledBy,
            shift.CreatedAtUtc,
            shift.UpdatedAtUtc);

    public static PosCashierShiftMovementDto MapMovement(CashierShiftMovement movement) =>
        new(
            movement.Id.Value,
            movement.ShiftId.Value,
            movement.OrganizationId.Value,
            movement.MovementType.ToString(),
            movement.Amount,
            movement.Reason,
            movement.Reference,
            movement.RecordedAtUtc,
            movement.RecordedBy);
}

public sealed class OpenCashierShift
{
    private readonly ICashierShiftRepository _shifts;
    private readonly IClock _clock;

    public OpenCashierShift(ICashierShiftRepository shifts, IClock clock)
    {
        _shifts = shifts;
        _clock = clock;
    }

    public async Task<ApplicationResult<CashierShift>> ExecuteAsync(
        Guid organizationId,
        Guid actorId,
        decimal openingCashAmount,
        DateOnly? businessDate = null,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<CashierShift>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to open a shift.");
        }

        try
        {
            var orgId = PosOrganizationId.From(organizationId);
            var existing = await _shifts
                .FindOpenForActorAsync(orgId, actorId, cancellationToken)
                .ConfigureAwait(false);
            if (existing is not null)
            {
                return ApplicationResult<CashierShift>.Failure(
                    ApplicationErrorCodes.CashierShiftOpenConflict,
                    "This cashier already has an open shift.");
            }

            var utcNow = _clock.UtcNow;
            var date = businessDate ?? CashierShiftNumbers.BusinessDateOf(utcNow);
            var shift = await _shifts
                .OpenAsync(
                    orgId,
                    date,
                    actorId,
                    openingCashAmount,
                    actorId,
                    number => CashierShift.Open(orgId, number, actorId, openingCashAmount, utcNow, date),
                    cancellationToken)
                .ConfigureAwait(false);

            return ApplicationResult<CashierShift>.Success(shift);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<CashierShift>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<CashierShift>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class CloseCashierShift
{
    private readonly ICashierShiftRepository _shifts;
    private readonly IClock _clock;

    public CloseCashierShift(ICashierShiftRepository shifts, IClock clock)
    {
        _shifts = shifts;
        _clock = clock;
    }

    public async Task<ApplicationResult<CashierShift>> ExecuteAsync(
        Guid organizationId,
        Guid shiftId,
        decimal closingCashAmount,
        Guid actorId,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<CashierShift>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to close a shift.");
        }

        var orgId = PosOrganizationId.From(organizationId);
        var id = CashierShiftId.From(shiftId);

        try
        {
            var shift = await _shifts.GetByIdAsync(orgId, id, cancellationToken).ConfigureAwait(false);
            if (shift is null)
            {
                return ApplicationResult<CashierShift>.Failure(
                    ApplicationErrorCodes.CashierShiftNotFound,
                    "Cashier shift was not found.");
            }

            if (shift.Status == CashierShiftStatus.Closed)
            {
                return ApplicationResult<CashierShift>.Success(shift);
            }

            var movements = await _shifts.ListMovementsAsync(orgId, id, cancellationToken).ConfigureAwait(false);
            var salesTotals = await _shifts.GetSalesTotalsAsync(orgId, id, cancellationToken).ConfigureAwait(false);
            var expected = CashierShiftExpectedCash.Compute(
                shift.OpeningCashAmount,
                salesTotals.NetCashSales,
                movements,
                salesTotals.CashRefundsTotal);

            shift.Close(closingCashAmount, expected, actorId, _clock.UtcNow, notes);
            await _shifts.UpdateAsync(shift, cancellationToken).ConfigureAwait(false);
            return ApplicationResult<CashierShift>.Success(shift);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<CashierShift>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<CashierShift>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class CancelCashierShift
{
    private readonly ICashierShiftRepository _shifts;
    private readonly IClock _clock;

    public CancelCashierShift(ICashierShiftRepository shifts, IClock clock)
    {
        _shifts = shifts;
        _clock = clock;
    }

    public async Task<ApplicationResult<CashierShift>> ExecuteAsync(
        Guid organizationId,
        Guid shiftId,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<CashierShift>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to cancel a shift.");
        }

        var orgId = PosOrganizationId.From(organizationId);
        var id = CashierShiftId.From(shiftId);

        try
        {
            var shift = await _shifts.GetByIdAsync(orgId, id, cancellationToken).ConfigureAwait(false);
            if (shift is null)
            {
                return ApplicationResult<CashierShift>.Failure(
                    ApplicationErrorCodes.CashierShiftNotFound,
                    "Cashier shift was not found.");
            }

            if (shift.Status == CashierShiftStatus.Cancelled)
            {
                return ApplicationResult<CashierShift>.Success(shift);
            }

            var hasSales = await _shifts.HasLinkedSalesAsync(orgId, id, cancellationToken).ConfigureAwait(false);
            var movements = await _shifts.ListMovementsAsync(orgId, id, cancellationToken).ConfigureAwait(false);
            shift.Cancel(actorId, _clock.UtcNow, hasSales, movements.Count > 0);
            await _shifts.UpdateAsync(shift, cancellationToken).ConfigureAwait(false);
            return ApplicationResult<CashierShift>.Success(shift);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<CashierShift>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<CashierShift>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class RecordCashierShiftMovement
{
    private readonly ICashierShiftRepository _shifts;
    private readonly IClock _clock;

    public RecordCashierShiftMovement(ICashierShiftRepository shifts, IClock clock)
    {
        _shifts = shifts;
        _clock = clock;
    }

    public async Task<ApplicationResult<CashierShiftMovement>> ExecuteAsync(
        Guid organizationId,
        Guid shiftId,
        string movementType,
        decimal amount,
        string reason,
        Guid actorId,
        string? reference = null,
        Guid? clientMovementId = null,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<CashierShiftMovement>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to record a cash movement.");
        }

        var orgId = PosOrganizationId.From(organizationId);
        var id = CashierShiftId.From(shiftId);

        try
        {
            if (clientMovementId is not null)
            {
                var existing = await _shifts
                    .GetMovementByIdAsync(orgId, CashierShiftMovementId.From(clientMovementId.Value), cancellationToken)
                    .ConfigureAwait(false);
                if (existing is not null && existing.ShiftId == id)
                {
                    return ApplicationResult<CashierShiftMovement>.Success(existing);
                }
            }

            var shift = await _shifts.GetByIdAsync(orgId, id, cancellationToken).ConfigureAwait(false);
            if (shift is null)
            {
                return ApplicationResult<CashierShiftMovement>.Failure(
                    ApplicationErrorCodes.CashierShiftNotFound,
                    "Cashier shift was not found.");
            }

            if (shift.Status != CashierShiftStatus.Open)
            {
                return ApplicationResult<CashierShiftMovement>.Failure(
                    DomainErrorCodes.InvalidCashierShiftStatusTransition,
                    "Cash movements can only be recorded on an open shift.");
            }

            if (!Enum.TryParse<CashierShiftMovementType>(movementType?.Trim(), ignoreCase: true, out var parsedType))
            {
                return ApplicationResult<CashierShiftMovement>.Failure(
                    DomainErrorCodes.InvalidCashierShiftMovementAmount,
                    "Movement type must be CashIn or CashOut.");
            }

            var movements = await _shifts.ListMovementsAsync(orgId, id, cancellationToken).ConfigureAwait(false);
            var salesTotals = await _shifts.GetSalesTotalsAsync(orgId, id, cancellationToken).ConfigureAwait(false);
            var projected = CashierShiftExpectedCash.Compute(
                shift.OpeningCashAmount,
                salesTotals.NetCashSales,
                movements,
                salesTotals.CashRefundsTotal);

            if (parsedType == CashierShiftMovementType.CashOut && projected - amount < 0m)
            {
                return ApplicationResult<CashierShiftMovement>.Failure(
                    DomainErrorCodes.CashierShiftExpectedCashNegative,
                    "This cash out would make expected physical cash negative.");
            }

            var utcNow = _clock.UtcNow;
            var movement = CashierShiftMovement.Create(
                id,
                orgId,
                parsedType,
                amount,
                reason,
                actorId,
                utcNow,
                reference,
                clientMovementId is null ? null : CashierShiftMovementId.From(clientMovementId.Value));

            await _shifts.AddMovementAsync(movement, cancellationToken).ConfigureAwait(false);
            return ApplicationResult<CashierShiftMovement>.Success(movement);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<CashierShiftMovement>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<CashierShiftMovement>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
