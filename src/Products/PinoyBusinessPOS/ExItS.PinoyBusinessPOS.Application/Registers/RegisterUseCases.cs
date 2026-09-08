using ExItS.PinoyBusinessPOS.Application.CashierShifts;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Options;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Registers;
using Microsoft.Extensions.Options;

namespace ExItS.PinoyBusinessPOS.Application.Registers;

public sealed record PosRegisterSummaryDto(
    Guid RegisterId,
    string RegisterCode,
    string Name,
    string Status);

public sealed record PosRegisterDto(
    Guid RegisterId,
    Guid OrganizationId,
    string RegisterCode,
    string Name,
    string? Description,
    string Status,
    DateTimeOffset CreatedAtUtc,
    Guid CreatedBy,
    DateTimeOffset UpdatedAtUtc,
    Guid UpdatedBy,
    bool HasOpenShift,
    Guid? OpenShiftActorId = null,
    Guid? OpenShiftId = null,
    DateTimeOffset? OpenShiftOpenedAtUtc = null);

public sealed record PosRegisterActivityDto(
    Guid RegisterId,
    string RegisterCode,
    string Name,
    string Status,
    int OpenShiftCount,
    int ClosedShiftCount,
    int CompletedSaleCount,
    decimal GrossSalesTotal,
    DateTimeOffset? ActivityFromUtc,
    DateTimeOffset? ActivityToUtc);

public sealed record CreateRegisterRequest(string Name, string? Description = null);

public sealed record UpdateRegisterRequest(
    string Name,
    DateTimeOffset ExpectedUpdatedAtUtc,
    string? Description = null);

public static class RegisterMapper
{
    public static PosRegisterDto Map(
        Register register,
        bool hasOpenShift = false,
        Guid? openShiftActorId = null,
        Guid? openShiftId = null,
        DateTimeOffset? openShiftOpenedAtUtc = null) =>
        new(
            register.Id.Value,
            register.OrganizationId.Value,
            register.RegisterCode,
            register.Name,
            register.Description,
            register.Status.ToString(),
            register.CreatedAtUtc,
            register.CreatedBy,
            register.UpdatedAtUtc,
            register.UpdatedBy,
            hasOpenShift,
            openShiftActorId,
            openShiftId,
            openShiftOpenedAtUtc);

    public static PosRegisterSummaryDto MapSummary(Register register) =>
        new(register.Id.Value, register.RegisterCode, register.Name, register.Status.ToString());
}

public sealed class RegisterQueryService
{
    private readonly IRegisterRepository _registers;
    private readonly ICashierShiftRepository _shifts;

    public RegisterQueryService(IRegisterRepository registers, ICashierShiftRepository shifts)
    {
        _registers = registers;
        _shifts = shifts;
    }

    public async Task<PosRegisterDto?> GetByIdAsync(
        Guid organizationId,
        Guid registerId,
        CancellationToken cancellationToken = default)
    {
        var org = PosOrganizationId.From(organizationId);
        var register = await _registers
            .GetByIdAsync(org, RegisterId.From(registerId), cancellationToken)
            .ConfigureAwait(false);
        if (register is null)
        {
            return null;
        }

        var openShift = await _shifts
            .FindOpenForRegisterAsync(org, register.Id.Value, cancellationToken)
            .ConfigureAwait(false);
        return RegisterMapper.Map(
            register,
            openShift is not null,
            openShift?.ActorId,
            openShift?.Id.Value,
            openShift?.OpenedAtUtc);
    }

    public async Task<PagedResult<PosRegisterDto>> ListAsync(
        Guid organizationId,
        RegisterFilter filter,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var org = PosOrganizationId.From(organizationId);
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var (items, total) = await _registers
            .ListAsync(org, filter, skip, take, cancellationToken)
            .ConfigureAwait(false);

        var mapped = new List<PosRegisterDto>(items.Count);
        foreach (var register in items)
        {
            var openShift = await _shifts
                .FindOpenForRegisterAsync(org, register.Id.Value, cancellationToken)
                .ConfigureAwait(false);
            mapped.Add(
                RegisterMapper.Map(
                    register,
                    openShift is not null,
                    openShift?.ActorId,
                    openShift?.Id.Value,
                    openShift?.OpenedAtUtc));
        }

        return new PagedResult<PosRegisterDto>(mapped, total, Math.Max(page ?? 1, 1), take);
    }

    public async Task<IReadOnlyList<PosRegisterSummaryDto>> ListAvailableForShiftAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var items = await _registers
            .ListAvailableForShiftAsync(PosOrganizationId.From(organizationId), cancellationToken)
            .ConfigureAwait(false);
        return items.Select(RegisterMapper.MapSummary).ToList();
    }

    public async Task<PosRegisterActivityDto?> GetActivityAsync(
        Guid organizationId,
        Guid registerId,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        CancellationToken cancellationToken = default)
    {
        var org = PosOrganizationId.From(organizationId);
        var id = RegisterId.From(registerId);
        var register = await _registers.GetByIdAsync(org, id, cancellationToken).ConfigureAwait(false);
        if (register is null)
        {
            return null;
        }

        var (shifts, _) = await _shifts
            .ListAsync(
                org,
                new CashierShiftFilter(RegisterId: registerId),
                0,
                500,
                cancellationToken)
            .ConfigureAwait(false);

        IEnumerable<CashierShift> scoped = shifts;
        if (fromUtc is not null)
        {
            scoped = scoped.Where(s => s.OpenedAtUtc >= fromUtc.Value);
        }

        if (toUtc is not null)
        {
            scoped = scoped.Where(s => s.OpenedAtUtc <= toUtc.Value);
        }

        var list = scoped.ToList();
        return new PosRegisterActivityDto(
            register.Id.Value,
            register.RegisterCode,
            register.Name,
            register.Status.ToString(),
            list.Count(s => s.Status == CashierShiftStatus.Open),
            list.Count(s => s.Status == CashierShiftStatus.Closed),
            0,
            0m,
            fromUtc,
            toUtc);
    }
}

public sealed class CreateRegister
{
    private readonly IRegisterRepository _registers;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly TimeProvider _clock;

    public CreateRegister(
        IRegisterRepository registers,
        IPosUnitOfWork unitOfWork,
        IPosCommercialAccessAccessor access,
        TimeProvider? clock = null)
    {
        _registers = registers;
        _unitOfWork = unitOfWork;
        _access = access;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<PosRegisterDto>> ExecuteAsync(
        Guid organizationId,
        Guid actorId,
        CreateRegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var gate = CommercialAccessGuard.Require(_access, UtangCapability.ManageRegisters);
        if (!gate.IsSuccess)
        {
            return ApplicationResult<PosRegisterDto>.Failure(gate.ErrorCode!, gate.ErrorMessage!);
        }

        if (actorId == Guid.Empty)
        {
            return ApplicationResult<PosRegisterDto>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to create a register.");
        }

        try
        {
            var org = PosOrganizationId.From(organizationId);
            var utcNow = _clock.GetUtcNow();
            var displayName = request.Name.Trim();
            var normalizedName = displayName.ToUpperInvariant();

            var existing = await _registers
                .FindByNormalizedNameAsync(org, normalizedName, cancellationToken)
                .ConfigureAwait(false);
            if (existing is not null)
            {
                return ApplicationResult<PosRegisterDto>.Failure(
                    ApplicationErrorCodes.RegisterNameConflict,
                    "A register with this name already exists in this organization.");
            }

            var code = await _registers.AllocateNextRegisterCodeAsync(org, cancellationToken).ConfigureAwait(false);
            var register = Register.Create(org, code, request.Name, actorId, utcNow, request.Description);
            await _registers.AddAsync(register, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PosRegisterDto>.Success(RegisterMapper.Map(register));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PosRegisterDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PosRegisterDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

/// <summary>
/// Narrow operational path for pure React PWA shift open: reuse any free Active register, or
/// auto-create the next <c>PWA-NNNN</c> display name. Requires <see cref="UtangCapability.ManageShifts"/>
/// only — does not grant general register management to cashiers.
/// </summary>
public sealed class EnsureAvailablePwaRegisterForShift
{
    private readonly IRegisterRepository _registers;
    private readonly ICashierShiftRepository _shifts;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly PosDeviceAuthorizationOptions _deviceAuthorization;
    private readonly TimeProvider _clock;

    public EnsureAvailablePwaRegisterForShift(
        IRegisterRepository registers,
        ICashierShiftRepository shifts,
        IPosUnitOfWork unitOfWork,
        IPosCommercialAccessAccessor access,
        IOptions<PosDeviceAuthorizationOptions> deviceAuthorization,
        TimeProvider? clock = null)
    {
        _registers = registers;
        _shifts = shifts;
        _unitOfWork = unitOfWork;
        _access = access;
        _deviceAuthorization = deviceAuthorization.Value;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<PosRegisterDto>> ExecuteAsync(
        Guid organizationId,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var gate = CommercialAccessGuard.Require(_access, UtangCapability.ManageShifts);
        if (!gate.IsSuccess)
        {
            return ApplicationResult<PosRegisterDto>.Failure(gate.ErrorCode!, gate.ErrorMessage!);
        }

        if (_deviceAuthorization.EnforcementEnabled)
        {
            return ApplicationResult<PosRegisterDto>.Failure(
                ApplicationErrorCodes.PwaRegisterEnsureDeviceEnforcementEnabled,
                "Automatic PWA register provisioning is disabled while POS device enforcement is enabled.");
        }

        if (actorId == Guid.Empty)
        {
            return ApplicationResult<PosRegisterDto>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to ensure a PWA register.");
        }

        var org = PosOrganizationId.From(organizationId);
        var blockedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var attempt = 0; attempt < PwaRegisterAllocation.MaxCreateAttempts; attempt++)
        {
            var free = await _registers
                .ListAvailableForShiftAsync(org, cancellationToken)
                .ConfigureAwait(false);
            if (free.Count > 0)
            {
                var chosen = free[0];
                var openShift = await _shifts
                    .FindOpenForRegisterAsync(org, chosen.Id.Value, cancellationToken)
                    .ConfigureAwait(false);
                return ApplicationResult<PosRegisterDto>.Success(
                    RegisterMapper.Map(chosen, openShift is not null, openShift?.ActorId));
            }

            var (allRegisters, _) = await _registers
                .ListAsync(org, new RegisterFilter(), 0, 500, cancellationToken)
                .ConfigureAwait(false);
            var candidateName = PwaRegisterAllocation.NextDisplayName(
                allRegisters.Select(r => r.Name).Concat(blockedNames));

            var existingNamed = await _registers
                .FindByNormalizedNameAsync(org, candidateName.ToUpperInvariant(), cancellationToken)
                .ConfigureAwait(false);
            if (existingNamed is not null)
            {
                if (existingNamed.Status == RegisterStatus.Active)
                {
                    var hasOpen = await _registers
                        .HasOpenShiftAsync(org, existingNamed.Id, cancellationToken)
                        .ConfigureAwait(false);
                    if (!hasOpen)
                    {
                        return ApplicationResult<PosRegisterDto>.Success(RegisterMapper.Map(existingNamed));
                    }
                }

                blockedNames.Add(candidateName);
                continue;
            }

            try
            {
                var utcNow = _clock.GetUtcNow();
                var code = await _registers.AllocateNextRegisterCodeAsync(org, cancellationToken).ConfigureAwait(false);
                var register = Register.Create(
                    org,
                    code,
                    candidateName,
                    actorId,
                    utcNow,
                    PwaRegisterAllocation.Description);
                await _registers.AddAsync(register, cancellationToken).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return ApplicationResult<PosRegisterDto>.Success(RegisterMapper.Map(register));
            }
            catch (DomainException ex) when (
                ex.ErrorCode is ApplicationErrorCodes.RegisterNameConflict
                    or ApplicationErrorCodes.RegisterCodeConflict)
            {
                blockedNames.Add(candidateName);
            }
            catch (PersistenceConflictException)
            {
                blockedNames.Add(candidateName);
            }
            catch (DomainException ex)
            {
                return ApplicationResult<PosRegisterDto>.Failure(ex.ErrorCode, ex.Message);
            }
        }

        var fallbackFree = await _registers
            .ListAvailableForShiftAsync(org, cancellationToken)
            .ConfigureAwait(false);
        if (fallbackFree.Count > 0)
        {
            return ApplicationResult<PosRegisterDto>.Success(RegisterMapper.Map(fallbackFree[0]));
        }

        return ApplicationResult<PosRegisterDto>.Failure(
            ApplicationErrorCodes.PwaRegisterEnsureExhausted,
            "Could not ensure an available PWA cash register after concurrent create retries.");
    }
}

public sealed class UpdateRegister
{
    private readonly IRegisterRepository _registers;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly TimeProvider _clock;

    public UpdateRegister(
        IRegisterRepository registers,
        IPosUnitOfWork unitOfWork,
        IPosCommercialAccessAccessor access,
        TimeProvider? clock = null)
    {
        _registers = registers;
        _unitOfWork = unitOfWork;
        _access = access;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<PosRegisterDto>> ExecuteAsync(
        Guid organizationId,
        Guid actorId,
        Guid registerId,
        UpdateRegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var gate = CommercialAccessGuard.Require(_access, UtangCapability.ManageRegisters);
        if (!gate.IsSuccess)
        {
            return ApplicationResult<PosRegisterDto>.Failure(gate.ErrorCode!, gate.ErrorMessage!);
        }

        try
        {
            var org = PosOrganizationId.From(organizationId);
            var id = RegisterId.From(registerId);
            var existing = await _registers.GetByIdAsync(org, id, cancellationToken).ConfigureAwait(false);
            if (existing is null)
            {
                return ApplicationResult<PosRegisterDto>.Failure(
                    ApplicationErrorCodes.RegisterNotFound,
                    "Register was not found in this organization.");
            }

            if (existing.UpdatedAtUtc != request.ExpectedUpdatedAtUtc)
            {
                return ApplicationResult<PosRegisterDto>.Failure(
                    ApplicationErrorCodes.RegisterConcurrencyConflict,
                    "Register was modified by another request. Refresh and retry.");
            }

            var normalizedName = request.Name.Trim().ToUpperInvariant();
            var conflict = await _registers
                .FindByNormalizedNameAsync(org, normalizedName, cancellationToken)
                .ConfigureAwait(false);
            if (conflict is not null && conflict.Id != existing.Id)
            {
                return ApplicationResult<PosRegisterDto>.Failure(
                    ApplicationErrorCodes.RegisterNameConflict,
                    "A register with this name already exists in this organization.");
            }

            existing.Update(request.Name, request.Description, actorId, _clock.GetUtcNow());
            await _registers.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            var hasOpen = await _registers.HasOpenShiftAsync(org, existing.Id, cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PosRegisterDto>.Success(RegisterMapper.Map(existing, hasOpen));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PosRegisterDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PosRegisterDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class DeactivateRegister
{
    private readonly IRegisterRepository _registers;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly TimeProvider _clock;

    public DeactivateRegister(
        IRegisterRepository registers,
        IPosUnitOfWork unitOfWork,
        IPosCommercialAccessAccessor access,
        TimeProvider? clock = null)
    {
        _registers = registers;
        _unitOfWork = unitOfWork;
        _access = access;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<PosRegisterDto>> ExecuteAsync(
        Guid organizationId,
        Guid actorId,
        Guid registerId,
        CancellationToken cancellationToken = default)
    {
        var gate = CommercialAccessGuard.Require(_access, UtangCapability.ManageRegisters);
        if (!gate.IsSuccess)
        {
            return ApplicationResult<PosRegisterDto>.Failure(gate.ErrorCode!, gate.ErrorMessage!);
        }

        try
        {
            var org = PosOrganizationId.From(organizationId);
            var id = RegisterId.From(registerId);
            var existing = await _registers.GetByIdAsync(org, id, cancellationToken).ConfigureAwait(false);
            if (existing is null)
            {
                return ApplicationResult<PosRegisterDto>.Failure(
                    ApplicationErrorCodes.RegisterNotFound,
                    "Register was not found in this organization.");
            }

            if (existing.Status == RegisterStatus.Inactive)
            {
                return ApplicationResult<PosRegisterDto>.Success(RegisterMapper.Map(existing));
            }

            if (await _registers.HasOpenShiftAsync(org, id, cancellationToken).ConfigureAwait(false))
            {
                return ApplicationResult<PosRegisterDto>.Failure(
                    DomainErrorCodes.RegisterDeactivateBlockedByOpenShift,
                    "Cannot deactivate a register while an open shift exists.");
            }

            existing.Deactivate(actorId, _clock.GetUtcNow());
            await _registers.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PosRegisterDto>.Success(RegisterMapper.Map(existing));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PosRegisterDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PosRegisterDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ActivateRegister
{
    private readonly IRegisterRepository _registers;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly TimeProvider _clock;

    public ActivateRegister(
        IRegisterRepository registers,
        IPosUnitOfWork unitOfWork,
        IPosCommercialAccessAccessor access,
        TimeProvider? clock = null)
    {
        _registers = registers;
        _unitOfWork = unitOfWork;
        _access = access;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<PosRegisterDto>> ExecuteAsync(
        Guid organizationId,
        Guid actorId,
        Guid registerId,
        CancellationToken cancellationToken = default)
    {
        var gate = CommercialAccessGuard.Require(_access, UtangCapability.ManageRegisters);
        if (!gate.IsSuccess)
        {
            return ApplicationResult<PosRegisterDto>.Failure(gate.ErrorCode!, gate.ErrorMessage!);
        }

        try
        {
            var org = PosOrganizationId.From(organizationId);
            var id = RegisterId.From(registerId);
            var existing = await _registers.GetByIdAsync(org, id, cancellationToken).ConfigureAwait(false);
            if (existing is null)
            {
                return ApplicationResult<PosRegisterDto>.Failure(
                    ApplicationErrorCodes.RegisterNotFound,
                    "Register was not found in this organization.");
            }

            if (existing.Status == RegisterStatus.Active)
            {
                return ApplicationResult<PosRegisterDto>.Success(RegisterMapper.Map(existing));
            }

            existing.Activate(actorId, _clock.GetUtcNow());
            await _registers.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PosRegisterDto>.Success(RegisterMapper.Map(existing));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PosRegisterDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PosRegisterDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
