using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.Application.Suppliers;

public sealed record PosSupplierDto(
    Guid SupplierId,
    Guid OrganizationId,
    string SupplierCode,
    string Name,
    string? ContactPerson,
    string? MobileNumber,
    string? TelephoneNumber,
    string? Email,
    string? AddressLine1,
    string? AddressLine2,
    string? CityMunicipality,
    string? Province,
    string? PostalCode,
    string? TaxOrRegistrationNumber,
    string? Notes,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record CreateSupplierRequest(
    string Name,
    string? ContactPerson = null,
    string? MobileNumber = null,
    string? TelephoneNumber = null,
    string? Email = null,
    string? AddressLine1 = null,
    string? AddressLine2 = null,
    string? CityMunicipality = null,
    string? Province = null,
    string? PostalCode = null,
    string? TaxOrRegistrationNumber = null,
    string? Notes = null);

public sealed record UpdateSupplierRequest(
    string Name,
    DateTimeOffset ExpectedUpdatedAtUtc,
    string? ContactPerson = null,
    string? MobileNumber = null,
    string? TelephoneNumber = null,
    string? Email = null,
    string? AddressLine1 = null,
    string? AddressLine2 = null,
    string? CityMunicipality = null,
    string? Province = null,
    string? PostalCode = null,
    string? TaxOrRegistrationNumber = null,
    string? Notes = null);

public static class SupplierMapper
{
    public static PosSupplierDto Map(Supplier supplier) =>
        new(
            supplier.Id.Value,
            supplier.OrganizationId.Value,
            supplier.SupplierCode,
            supplier.Name,
            supplier.ContactPerson,
            supplier.MobileNumber,
            supplier.TelephoneNumber,
            supplier.Email,
            supplier.AddressLine1,
            supplier.AddressLine2,
            supplier.CityMunicipality,
            supplier.Province,
            supplier.PostalCode,
            supplier.TaxOrRegistrationNumber,
            supplier.Notes,
            supplier.Status.ToString(),
            supplier.CreatedAtUtc,
            supplier.UpdatedAtUtc);
}

public sealed class SupplierQueryService
{
    private readonly ISupplierRepository _suppliers;

    public SupplierQueryService(ISupplierRepository suppliers) => _suppliers = suppliers;

    public async Task<PosSupplierDto?> GetByIdAsync(
        Guid organizationId,
        Guid supplierId,
        CancellationToken cancellationToken = default)
    {
        var supplier = await _suppliers
            .GetByIdAsync(PosOrganizationId.From(organizationId), SupplierId.From(supplierId), cancellationToken)
            .ConfigureAwait(false);
        return supplier is null ? null : SupplierMapper.Map(supplier);
    }

    public async Task<PagedResult<PosSupplierDto>> ListAsync(
        Guid organizationId,
        SupplierFilter filter,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var (items, total) = await _suppliers
            .ListAsync(PosOrganizationId.From(organizationId), filter, skip, take, cancellationToken)
            .ConfigureAwait(false);
        return new PagedResult<PosSupplierDto>(
            items.Select(SupplierMapper.Map).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }
}

internal static class SupplierDuplicateGuard
{
    public static async Task<ApplicationResult?> FindConflictAsync(
        ISupplierRepository repository,
        PosOrganizationId organizationId,
        string normalizedName,
        string? normalizedEmail,
        string? normalizedMobile,
        string? normalizedTax,
        SupplierId? excludeId,
        CancellationToken cancellationToken)
    {
        var byName = await repository
            .FindActiveByNormalizedNameAsync(organizationId, normalizedName, cancellationToken)
            .ConfigureAwait(false);
        if (byName is not null && (excludeId is null || byName.Id != excludeId))
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.SupplierNameConflict,
                "An active supplier with this name already exists in this organization.");
        }

        if (normalizedEmail is not null)
        {
            var byEmail = await repository
                .FindActiveByNormalizedEmailAsync(organizationId, normalizedEmail, cancellationToken)
                .ConfigureAwait(false);
            if (byEmail is not null && (excludeId is null || byEmail.Id != excludeId))
            {
                return ApplicationResult.Failure(
                    ApplicationErrorCodes.SupplierEmailConflict,
                    "An active supplier with this email already exists in this organization.");
            }
        }

        if (normalizedMobile is not null)
        {
            var byMobile = await repository
                .FindActiveByNormalizedMobileAsync(organizationId, normalizedMobile, cancellationToken)
                .ConfigureAwait(false);
            if (byMobile is not null && (excludeId is null || byMobile.Id != excludeId))
            {
                return ApplicationResult.Failure(
                    ApplicationErrorCodes.SupplierMobileConflict,
                    "An active supplier with this mobile number already exists in this organization.");
            }
        }

        if (normalizedTax is not null)
        {
            var byTax = await repository
                .FindActiveByNormalizedTaxAsync(organizationId, normalizedTax, cancellationToken)
                .ConfigureAwait(false);
            if (byTax is not null && (excludeId is null || byTax.Id != excludeId))
            {
                return ApplicationResult.Failure(
                    ApplicationErrorCodes.SupplierTaxConflict,
                    "An active supplier with this tax/registration number already exists in this organization.");
            }
        }

        return null;
    }
}

public sealed class CreateSupplier
{
    private readonly ISupplierRepository _suppliers;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly TimeProvider _clock;

    public CreateSupplier(
        ISupplierRepository suppliers,
        IPosUnitOfWork unitOfWork,
        IPosCommercialAccessAccessor access,
        TimeProvider? clock = null)
    {
        _suppliers = suppliers;
        _unitOfWork = unitOfWork;
        _access = access;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<PosSupplierDto>> ExecuteAsync(
        Guid organizationId,
        CreateSupplierRequest request,
        CancellationToken cancellationToken = default)
    {
        var gate = CommercialAccessGuard.Require(_access, UtangCapability.ManageSuppliers);
        if (!gate.IsSuccess)
        {
            return ApplicationResult<PosSupplierDto>.Failure(gate.ErrorCode!, gate.ErrorMessage!);
        }

        try
        {
            var org = PosOrganizationId.From(organizationId);
            var utcNow = _clock.GetUtcNow();
            var name = Supplier.NormalizeName(request.Name);
            var normalizedName = Supplier.Normalize(name);
            var (mobile, normalizedMobile) = POSCustomer.NormalizeOptionalMobile(request.MobileNumber);
            var (email, normalizedEmail) = Supplier.NormalizeOptionalEmail(request.Email);
            var (tax, normalizedTax) = Supplier.NormalizeOptionalTax(request.TaxOrRegistrationNumber);

            var conflict = await SupplierDuplicateGuard
                .FindConflictAsync(_suppliers, org, normalizedName, normalizedEmail, normalizedMobile, normalizedTax, null, cancellationToken)
                .ConfigureAwait(false);
            if (conflict is not null)
            {
                return ApplicationResult<PosSupplierDto>.Failure(conflict.ErrorCode!, conflict.ErrorMessage!);
            }

            var code = await _suppliers.AllocateNextSupplierCodeAsync(org, cancellationToken).ConfigureAwait(false);
            var supplier = Supplier.Create(
                org,
                code,
                name,
                utcNow,
                request.ContactPerson,
                mobile,
                request.TelephoneNumber,
                email,
                request.AddressLine1,
                request.AddressLine2,
                request.CityMunicipality,
                request.Province,
                request.PostalCode,
                tax,
                request.Notes);

            await _suppliers.AddAsync(supplier, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PosSupplierDto>.Success(SupplierMapper.Map(supplier));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PosSupplierDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PosSupplierDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class UpdateSupplier
{
    private readonly ISupplierRepository _suppliers;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly TimeProvider _clock;

    public UpdateSupplier(
        ISupplierRepository suppliers,
        IPosUnitOfWork unitOfWork,
        IPosCommercialAccessAccessor access,
        TimeProvider? clock = null)
    {
        _suppliers = suppliers;
        _unitOfWork = unitOfWork;
        _access = access;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<PosSupplierDto>> ExecuteAsync(
        Guid organizationId,
        Guid supplierId,
        UpdateSupplierRequest request,
        CancellationToken cancellationToken = default)
    {
        var gate = CommercialAccessGuard.Require(_access, UtangCapability.ManageSuppliers);
        if (!gate.IsSuccess)
        {
            return ApplicationResult<PosSupplierDto>.Failure(gate.ErrorCode!, gate.ErrorMessage!);
        }

        try
        {
            var org = PosOrganizationId.From(organizationId);
            var id = SupplierId.From(supplierId);
            var existing = await _suppliers.GetByIdAsync(org, id, cancellationToken).ConfigureAwait(false);
            if (existing is null)
            {
                return ApplicationResult<PosSupplierDto>.Failure(
                    ApplicationErrorCodes.SupplierNotFound,
                    "Supplier was not found in this organization.");
            }

            if (existing.UpdatedAtUtc != request.ExpectedUpdatedAtUtc)
            {
                return ApplicationResult<PosSupplierDto>.Failure(
                    ApplicationErrorCodes.SupplierConcurrencyConflict,
                    "Supplier was modified by another request. Refresh and retry.");
            }

            var name = Supplier.NormalizeName(request.Name);
            var normalizedName = Supplier.Normalize(name);
            var (mobile, normalizedMobile) = POSCustomer.NormalizeOptionalMobile(request.MobileNumber);
            var (email, normalizedEmail) = Supplier.NormalizeOptionalEmail(request.Email);
            var (tax, normalizedTax) = Supplier.NormalizeOptionalTax(request.TaxOrRegistrationNumber);

            var conflict = await SupplierDuplicateGuard
                .FindConflictAsync(_suppliers, org, normalizedName, normalizedEmail, normalizedMobile, normalizedTax, id, cancellationToken)
                .ConfigureAwait(false);
            if (conflict is not null)
            {
                return ApplicationResult<PosSupplierDto>.Failure(conflict.ErrorCode!, conflict.ErrorMessage!);
            }

            existing.UpdateProfile(
                name,
                _clock.GetUtcNow(),
                request.ContactPerson,
                mobile,
                request.TelephoneNumber,
                email,
                request.AddressLine1,
                request.AddressLine2,
                request.CityMunicipality,
                request.Province,
                request.PostalCode,
                tax,
                request.Notes);

            await _suppliers.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PosSupplierDto>.Success(SupplierMapper.Map(existing));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PosSupplierDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PosSupplierDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class DeactivateSupplier
{
    private readonly ISupplierRepository _suppliers;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly TimeProvider _clock;

    public DeactivateSupplier(
        ISupplierRepository suppliers,
        IPosUnitOfWork unitOfWork,
        IPosCommercialAccessAccessor access,
        TimeProvider? clock = null)
    {
        _suppliers = suppliers;
        _unitOfWork = unitOfWork;
        _access = access;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<PosSupplierDto>> ExecuteAsync(
        Guid organizationId,
        Guid supplierId,
        CancellationToken cancellationToken = default)
    {
        var gate = CommercialAccessGuard.Require(_access, UtangCapability.ManageSuppliers);
        if (!gate.IsSuccess)
        {
            return ApplicationResult<PosSupplierDto>.Failure(gate.ErrorCode!, gate.ErrorMessage!);
        }

        try
        {
            var org = PosOrganizationId.From(organizationId);
            var existing = await _suppliers
                .GetByIdAsync(org, SupplierId.From(supplierId), cancellationToken)
                .ConfigureAwait(false);
            if (existing is null)
            {
                return ApplicationResult<PosSupplierDto>.Failure(
                    ApplicationErrorCodes.SupplierNotFound,
                    "Supplier was not found in this organization.");
            }

            existing.Deactivate(_clock.GetUtcNow());
            await _suppliers.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PosSupplierDto>.Success(SupplierMapper.Map(existing));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PosSupplierDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PosSupplierDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ActivateSupplier
{
    private readonly ISupplierRepository _suppliers;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly TimeProvider _clock;

    public ActivateSupplier(
        ISupplierRepository suppliers,
        IPosUnitOfWork unitOfWork,
        IPosCommercialAccessAccessor access,
        TimeProvider? clock = null)
    {
        _suppliers = suppliers;
        _unitOfWork = unitOfWork;
        _access = access;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<PosSupplierDto>> ExecuteAsync(
        Guid organizationId,
        Guid supplierId,
        CancellationToken cancellationToken = default)
    {
        var gate = CommercialAccessGuard.Require(_access, UtangCapability.ManageSuppliers);
        if (!gate.IsSuccess)
        {
            return ApplicationResult<PosSupplierDto>.Failure(gate.ErrorCode!, gate.ErrorMessage!);
        }

        try
        {
            var org = PosOrganizationId.From(organizationId);
            var id = SupplierId.From(supplierId);
            var existing = await _suppliers.GetByIdAsync(org, id, cancellationToken).ConfigureAwait(false);
            if (existing is null)
            {
                return ApplicationResult<PosSupplierDto>.Failure(
                    ApplicationErrorCodes.SupplierNotFound,
                    "Supplier was not found in this organization.");
            }

            var conflict = await SupplierDuplicateGuard
                .FindConflictAsync(
                    _suppliers,
                    org,
                    existing.NormalizedName,
                    existing.NormalizedEmail,
                    existing.NormalizedMobile,
                    existing.NormalizedTaxOrRegistrationNumber,
                    id,
                    cancellationToken)
                .ConfigureAwait(false);
            if (conflict is not null)
            {
                return ApplicationResult<PosSupplierDto>.Failure(conflict.ErrorCode!, conflict.ErrorMessage!);
            }

            existing.Reactivate(_clock.GetUtcNow());
            await _suppliers.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PosSupplierDto>.Success(SupplierMapper.Map(existing));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PosSupplierDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PosSupplierDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
