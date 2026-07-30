using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.Customers;

public sealed record POSCustomerDto(
    Guid CustomerId,
    Guid OrganizationId,
    string DisplayName,
    string? MobileNumber,
    string? Address,
    string? Notes,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record CustomerSyncPageDto(
    List<POSCustomerDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    DateTimeOffset? NextCheckpointUtc);

public sealed class POSCustomerQueryService
{
    private readonly IPOSCustomerRepository _customers;

    public POSCustomerQueryService(IPOSCustomerRepository customers) => _customers = customers;

    public async Task<POSCustomerDto?> GetByIdAsync(
        Guid organizationId,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var customer = await _customers
            .GetByIdAsync(PosOrganizationId.From(organizationId), POSCustomerId.From(customerId), cancellationToken)
            .ConfigureAwait(false);
        return customer is null ? null : Map(customer);
    }

    public async Task<PagedResult<POSCustomerDto>> ListAsync(
        Guid organizationId,
        CustomerStatus? status,
        string? search,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var (items, total) = await _customers
            .ListAsync(PosOrganizationId.From(organizationId), status, search, skip, take, cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<POSCustomerDto>(
            items.Select(Map).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }

    public async Task<CustomerSyncPageDto> ListForSyncAsync(
        Guid organizationId,
        DateTimeOffset? sinceUtc,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var (items, total) = await _customers
            .ListUpdatedSinceAsync(PosOrganizationId.From(organizationId), sinceUtc, skip, take, cancellationToken)
            .ConfigureAwait(false);

        var mapped = items.Select(Map).ToList();
        DateTimeOffset? nextCheckpoint = mapped.Count > 0
            ? mapped.Max(c => c.UpdatedAtUtc)
            : null;

        return new CustomerSyncPageDto(mapped, total, Math.Max(page ?? 1, 1), take, nextCheckpoint);
    }

    public static POSCustomerDto Map(POSCustomer customer) =>
        new(
            customer.Id.Value,
            customer.OrganizationId.Value,
            customer.DisplayName,
            customer.MobileNumber,
            customer.Address,
            customer.Notes,
            customer.Status.ToString(),
            customer.CreatedAtUtc,
            customer.UpdatedAtUtc);
}

public sealed class CreatePOSCustomer
{
    private readonly IPOSCustomerRepository _customers;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreatePOSCustomer(IPOSCustomerRepository customers, IPosUnitOfWork unitOfWork, IClock clock)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<POSCustomer>> ExecuteAsync(
        Guid organizationId,
        string displayName,
        string? mobileNumber,
        string? address,
        string? notes,
        Guid? clientCustomerId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var orgId = PosOrganizationId.From(organizationId);

            if (clientCustomerId is not null)
            {
                var existingById = await _customers
                    .GetByIdAsync(orgId, POSCustomerId.From(clientCustomerId.Value), cancellationToken)
                    .ConfigureAwait(false);
                if (existingById is not null)
                {
                    return ApplicationResult<POSCustomer>.Success(existingById);
                }
            }

            var customer = clientCustomerId is null
                ? POSCustomer.Create(orgId, displayName, _clock.UtcNow, mobileNumber, address, notes)
                : POSCustomer.Create(
                    orgId,
                    displayName,
                    _clock.UtcNow,
                    mobileNumber,
                    address,
                    notes,
                    id: POSCustomerId.From(clientCustomerId.Value));

            if (customer.NormalizedMobile is not null)
            {
                var existing = await _customers
                    .FindActiveByNormalizedMobileAsync(orgId, customer.NormalizedMobile, cancellationToken)
                    .ConfigureAwait(false);
                if (existing is not null)
                {
                    return ApplicationResult<POSCustomer>.Failure(
                        ApplicationErrorCodes.MobileConflict,
                        "An active customer with this mobile number already exists in this organization.");
                }
            }

            await _customers.AddAsync(customer, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<POSCustomer>.Success(customer);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<POSCustomer>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<POSCustomer>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class UpdatePOSCustomer
{
    private readonly IPOSCustomerRepository _customers;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpdatePOSCustomer(IPOSCustomerRepository customers, IPosUnitOfWork unitOfWork, IClock clock)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<POSCustomer>> ExecuteAsync(
        Guid organizationId,
        Guid customerId,
        string displayName,
        string? mobileNumber,
        string? address,
        string? notes,
        DateTimeOffset? expectedUpdatedAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var customer = await _customers
            .GetByIdAsync(orgId, POSCustomerId.From(customerId), cancellationToken)
            .ConfigureAwait(false);
        if (customer is null)
        {
            return ApplicationResult<POSCustomer>.Failure(
                ApplicationErrorCodes.CustomerNotFound,
                "Customer was not found.");
        }

        if (expectedUpdatedAtUtc is not null)
        {
            var expected = expectedUpdatedAtUtc.Value.ToUniversalTime();
            var actual = customer.UpdatedAtUtc.ToUniversalTime();
            if (expected.UtcTicks != actual.UtcTicks)
            {
                return ApplicationResult<POSCustomer>.Failure(
                    ApplicationErrorCodes.CustomerConcurrencyConflict,
                    "The customer was updated concurrently. Reload the latest version and try again.");
            }
        }

        try
        {
            var (_, normalizedMobile) = POSCustomer.NormalizeOptionalMobile(mobileNumber);
            if (normalizedMobile is not null
                && !string.Equals(normalizedMobile, customer.NormalizedMobile, StringComparison.Ordinal))
            {
                var existing = await _customers
                    .FindActiveByNormalizedMobileAsync(orgId, normalizedMobile, cancellationToken)
                    .ConfigureAwait(false);
                if (existing is not null && existing.Id != customer.Id)
                {
                    return ApplicationResult<POSCustomer>.Failure(
                        ApplicationErrorCodes.MobileConflict,
                        "An active customer with this mobile number already exists in this organization.");
                }
            }

            customer.UpdateProfile(displayName, mobileNumber, address, notes, _clock.UtcNow);
            await _customers.UpdateAsync(customer, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<POSCustomer>.Success(customer);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<POSCustomer>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<POSCustomer>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class DeactivatePOSCustomer
{
    private readonly IPOSCustomerRepository _customers;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public DeactivatePOSCustomer(IPOSCustomerRepository customers, IPosUnitOfWork unitOfWork, IClock clock)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<POSCustomer>> ExecuteAsync(
        Guid organizationId,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var customer = await _customers
            .GetByIdAsync(PosOrganizationId.From(organizationId), POSCustomerId.From(customerId), cancellationToken)
            .ConfigureAwait(false);
        if (customer is null)
        {
            return ApplicationResult<POSCustomer>.Failure(
                ApplicationErrorCodes.CustomerNotFound,
                "Customer was not found.");
        }

        try
        {
            customer.Deactivate(_clock.UtcNow);
            await _customers.UpdateAsync(customer, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<POSCustomer>.Success(customer);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<POSCustomer>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<POSCustomer>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ReactivatePOSCustomer
{
    private readonly IPOSCustomerRepository _customers;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ReactivatePOSCustomer(IPOSCustomerRepository customers, IPosUnitOfWork unitOfWork, IClock clock)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<POSCustomer>> ExecuteAsync(
        Guid organizationId,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var customer = await _customers
            .GetByIdAsync(orgId, POSCustomerId.From(customerId), cancellationToken)
            .ConfigureAwait(false);
        if (customer is null)
        {
            return ApplicationResult<POSCustomer>.Failure(
                ApplicationErrorCodes.CustomerNotFound,
                "Customer was not found.");
        }

        try
        {
            if (customer.NormalizedMobile is not null)
            {
                var existing = await _customers
                    .FindActiveByNormalizedMobileAsync(orgId, customer.NormalizedMobile, cancellationToken)
                    .ConfigureAwait(false);
                if (existing is not null && existing.Id != customer.Id)
                {
                    return ApplicationResult<POSCustomer>.Failure(
                        ApplicationErrorCodes.MobileConflict,
                        "An active customer with this mobile number already exists in this organization.");
                }
            }

            customer.Reactivate(_clock.UtcNow);
            await _customers.UpdateAsync(customer, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<POSCustomer>.Success(customer);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<POSCustomer>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<POSCustomer>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
