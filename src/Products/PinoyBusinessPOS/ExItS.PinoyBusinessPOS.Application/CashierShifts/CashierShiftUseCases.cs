using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.OperationalSetup;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Registers;

namespace ExItS.PinoyBusinessPOS.Application.CashierShifts;

public sealed class CashierShiftQueryService
{
    private readonly ICashierShiftRepository _shifts;
    private readonly IRegisterRepository _registers;

    public CashierShiftQueryService(ICashierShiftRepository shifts, IRegisterRepository registers)
    {
        _shifts = shifts;
        _registers = registers;
    }

    public async Task<PosCashierShiftDto?> GetByIdAsync(
        Guid organizationId,
        Guid shiftId,
        CancellationToken cancellationToken = default)
    {
        var shift = await _shifts
            .GetByIdAsync(PosOrganizationId.From(organizationId), CashierShiftId.From(shiftId), cancellationToken)
            .ConfigureAwait(false);
        return shift is null ? null : await MapAsync(shift, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PosCashierShiftDto?> GetCurrentOpenForActorAsync(
        Guid organizationId,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var shift = await _shifts
            .FindOpenForActorAsync(PosOrganizationId.From(organizationId), actorId, cancellationToken)
            .ConfigureAwait(false);
        return shift is null ? null : await MapAsync(shift, cancellationToken).ConfigureAwait(false);
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

        var mapped = new List<PosCashierShiftDto>(items.Count);
        foreach (var shift in items)
        {
            mapped.Add(await MapAsync(shift, cancellationToken).ConfigureAwait(false));
        }

        return new PagedResult<PosCashierShiftDto>(
            mapped,
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
            shift.OpeningCashCounted,
            shift.EffectiveCashCountMode.ToString(),
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
            shift.Status == CashierShiftStatus.Closed
                ? CashCountModes.ClosingState(shift.EffectiveClosingCashCountMode, shift.ClosingCashAmount)
                : null,
            salesTotals.CompletedCashCount,
            salesTotals.VoidedCashCount,
            salesTotals.CompletedGCashCount,
            salesTotals.CompletedUtangCount,
            movements.Select(MapMovement).ToList(),
            shift.OpeningDenominationLines.Select(CashDenominationMapper.Map).ToList(),
            shift.ClosingDenominationLines.Select(CashDenominationMapper.Map).ToList(),
            shift.EffectiveOpeningCashCountMode.ToString(),
            shift.EffectiveClosingCashCountMode.ToString());
    }

    public static PosCashierShiftDto Map(CashierShift shift) =>
        Map(shift, null);

    public static PosCashierShiftDto Map(CashierShift shift, Register? register) =>
        new(
            shift.Id.Value,
            shift.OrganizationId.Value,
            shift.ShiftNumber,
            shift.Status.ToString(),
            shift.ActorId,
            shift.RegisterId?.Value,
            register?.RegisterCode,
            register?.Name,
            shift.BusinessDate,
            shift.OpeningCashAmount,
            shift.OpeningCashCounted,
            shift.EffectiveCashCountMode.ToString(),
            shift.OpenedAtUtc,
            shift.OpenedBy,
            shift.ClosingCashAmount,
            shift.ExpectedCashAmountSnapshot,
            shift.CashVarianceAmount,
            shift.Status == CashierShiftStatus.Closed
                ? CashCountModes.ClosingState(shift.EffectiveClosingCashCountMode, shift.ClosingCashAmount)
                : null,
            shift.ClosingNotes,
            shift.ClosedAtUtc,
            shift.ClosedBy,
            shift.CancelledAtUtc,
            shift.CancelledBy,
            shift.CreatedAtUtc,
            shift.UpdatedAtUtc,
            shift.OpeningDenominationLines.Select(CashDenominationMapper.Map).ToList(),
            shift.ClosingDenominationLines.Select(CashDenominationMapper.Map).ToList(),
            shift.EffectiveOpeningCashCountMode.ToString(),
            shift.EffectiveClosingCashCountMode.ToString());

    private async Task<PosCashierShiftDto> MapAsync(CashierShift shift, CancellationToken cancellationToken)
    {
        Register? register = null;
        if (shift.RegisterId is not null)
        {
            register = await _registers
                .GetByIdAsync(shift.OrganizationId, shift.RegisterId, cancellationToken)
                .ConfigureAwait(false);
        }

        return Map(shift, register);
    }

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
    private readonly IRegisterRepository _registers;
    private readonly IPosOperationalSetupRepository _setups;
    private readonly IOrganizationCashDenominationRepository _denominations;
    private readonly IClock _clock;

    public OpenCashierShift(
        ICashierShiftRepository shifts,
        IRegisterRepository registers,
        IPosOperationalSetupRepository setups,
        IOrganizationCashDenominationRepository denominations,
        IClock clock)
    {
        _shifts = shifts;
        _registers = registers;
        _setups = setups;
        _denominations = denominations;
        _clock = clock;
    }

    public async Task<ApplicationResult<CashierShift>> ExecuteAsync(
        Guid organizationId,
        Guid actorId,
        Guid registerId,
        decimal? openingCashAmount,
        DateOnly? businessDate = null,
        IReadOnlyList<CashCountDenominationLineDto>? denominationLines = null,
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
            var regId = RegisterId.From(registerId);
            var register = await _registers.GetByIdAsync(orgId, regId, cancellationToken).ConfigureAwait(false);
            if (register is null)
            {
                return ApplicationResult<CashierShift>.Failure(
                    ApplicationErrorCodes.RegisterNotFound,
                    "Register was not found in this organization.");
            }

            register.EnsureActiveForShift();

            var existingActor = await _shifts
                .FindOpenForActorAsync(orgId, actorId, cancellationToken)
                .ConfigureAwait(false);
            if (existingActor is not null)
            {
                return ApplicationResult<CashierShift>.Failure(
                    ApplicationErrorCodes.CashierShiftOpenConflict,
                    "This cashier already has an open shift.");
            }

            var existingRegister = await _shifts
                .FindOpenForRegisterAsync(orgId, registerId, cancellationToken)
                .ConfigureAwait(false);
            if (existingRegister is not null)
            {
                return ApplicationResult<CashierShift>.Failure(
                    DomainErrorCodes.CashierShiftRegisterConflict,
                    "This register already has an open shift.",
                    new Dictionary<string, string>
                    {
                        ["openShiftActorId"] = existingRegister.ActorId.ToString("D"),
                        ["openShiftId"] = existingRegister.Id.Value.ToString("D"),
                        ["registerId"] = register.Id.Value.ToString("D"),
                        ["registerCode"] = register.RegisterCode,
                        ["registerName"] = register.Name,
                    });
            }

            var utcNow = _clock.UtcNow;
            var date = businessDate ?? CashierShiftNumbers.BusinessDateOf(utcNow);
            var setup = await _setups.GetByOrganizationIdAsync(orgId, cancellationToken).ConfigureAwait(false);
            var openingMode = CashCountModes.ForNewShift(setup?.OpeningCashCountMode ?? CashCountModes.Default);
            var closingMode = CashCountModes.ForNewShift(setup?.ClosingCashCountMode ?? CashCountModes.Default);
            var openingLines = CashDenominationMapper.ParseSubmittedLines(denominationLines, openingCashAmount);
            if (openingLines.Count > 0)
            {
                var configured = await _denominations.ListAsync(orgId, cancellationToken).ConfigureAwait(false);
                CashCountDenominationBreakdown.EnsureConfigured(
                    openingLines,
                    configured.Where(d => d.IsEnabled).Select(d => d.Value).ToHashSet());
            }

            var shift = await _shifts
                .OpenAsync(
                    orgId,
                    date,
                    actorId,
                    openingCashAmount ?? 0m,
                    actorId,
                    number => CashierShift.Open(
                        orgId,
                        number,
                        actorId,
                        regId,
                        openingCashAmount,
                        utcNow,
                        date,
                        openingCashCountMode: openingMode,
                        closingCashCountMode: closingMode,
                        openingDenominationLines: openingLines),
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
    private readonly IOrganizationCashDenominationRepository _denominations;
    private readonly IClock _clock;

    public CloseCashierShift(
        ICashierShiftRepository shifts,
        IOrganizationCashDenominationRepository denominations,
        IClock clock)
    {
        _shifts = shifts;
        _denominations = denominations;
        _clock = clock;
    }

    public async Task<ApplicationResult<CashierShift>> ExecuteAsync(
        Guid organizationId,
        Guid shiftId,
        decimal? closingCashAmount,
        Guid actorId,
        string? notes = null,
        IReadOnlyList<CashCountDenominationLineDto>? denominationLines = null,
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

            var closingLines = CashDenominationMapper.ParseSubmittedLines(denominationLines, closingCashAmount);
            if (closingLines.Count > 0)
            {
                var configured = await _denominations.ListAsync(orgId, cancellationToken).ConfigureAwait(false);
                CashCountDenominationBreakdown.EnsureConfigured(
                    closingLines,
                    configured.Where(d => d.IsEnabled).Select(d => d.Value).ToHashSet());
            }

            shift.Close(closingCashAmount, expected, actorId, _clock.UtcNow, notes, closingLines);
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
