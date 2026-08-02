using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public sealed record BusinessCustomerDto(
    Guid Id,
    Guid OrganizationId,
    string DisplayName,
    string? Email,
    string? Phone,
    string? Notes,
    string? OwningProductCode,
    string Status,
    Guid? LinkedUserIdentityId,
    bool IsOrganizationStaff,
    bool IsCreditCustomer,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record CreateBusinessCustomerRequest(
    string DisplayName,
    string? Email = null,
    string? Phone = null,
    string? Notes = null,
    string? OwningProductCode = null);

public sealed record UpdateBusinessCustomerRequest(
    string DisplayName,
    string? Email = null,
    string? Phone = null,
    string? Notes = null);

public sealed class BusinessCustomerQueryService
{
    private readonly IBusinessCustomerRepository _customers;
    private readonly ICreditCustomerRepository _creditCustomers;

    public BusinessCustomerQueryService(
        IBusinessCustomerRepository customers,
        ICreditCustomerRepository creditCustomers)
    {
        _customers = customers;
        _creditCustomers = creditCustomers;
    }

    public async Task<BusinessCustomerDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await _customers.GetByIdAsync(BusinessCustomerId.From(id), cancellationToken)
            .ConfigureAwait(false);
        if (customer is null)
        {
            return null;
        }

        CustomerStaffSeparationGuard.EnsureNotTreatedAsStaff(customer);
        var credit = await _creditCustomers
            .FindActiveByBusinessCustomerAsync(customer.Id, cancellationToken)
            .ConfigureAwait(false);
        return Map(customer, credit is not null);
    }

    public async Task<PagedResult<BusinessCustomerDto>> ListAsync(
        Guid organizationId,
        string? owningProductCode,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = CatalogPagination.Normalize(page, pageSize);
        var (items, total) = await _customers
            .ListByOrganizationAsync(
                PlatformOrganizationId.From(organizationId),
                string.IsNullOrWhiteSpace(owningProductCode) ? null : owningProductCode.Trim().ToLowerInvariant(),
                skip,
                take,
                cancellationToken)
            .ConfigureAwait(false);

        var mapped = new List<BusinessCustomerDto>(items.Count);
        foreach (var item in items)
        {
            CustomerStaffSeparationGuard.EnsureNotTreatedAsStaff(item);
            var credit = await _creditCustomers
                .FindActiveByBusinessCustomerAsync(item.Id, cancellationToken)
                .ConfigureAwait(false);
            mapped.Add(Map(item, credit is not null));
        }

        return new PagedResult<BusinessCustomerDto>(mapped, total, Math.Max(page ?? 1, 1), take);
    }

    public static BusinessCustomerDto Map(BusinessCustomer customer, bool isCreditCustomer) =>
        new(
            customer.Id.Value,
            customer.OrganizationId.Value,
            customer.DisplayName,
            customer.NormalizedEmail,
            customer.Phone,
            customer.Notes,
            customer.OwningProductCode,
            customer.Status.ToString(),
            customer.LinkedUserIdentityId?.Value,
            IsOrganizationStaff: false,
            isCreditCustomer,
            customer.CreatedAtUtc,
            customer.UpdatedAtUtc);
}

public sealed class CreateBusinessCustomer
{
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IBusinessCustomerRepository _customers;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreateBusinessCustomer(
        IPlatformOrganizationRepository organizations,
        IBusinessCustomerRepository customers,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _organizations = organizations;
        _customers = customers;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<BusinessCustomerDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        CreateBusinessCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        var organization = await _organizations.GetByIdAsync(organizationId, cancellationToken).ConfigureAwait(false);
        if (organization is null)
        {
            return ApplicationResult<BusinessCustomerDto>.Failure(
                ApplicationErrorCodes.OrganizationNotFound,
                "Platform Organization was not found.");
        }

        if (organization.Status != OrganizationStatus.Active)
        {
            return ApplicationResult<BusinessCustomerDto>.Failure(
                DomainErrorCodes.OrganizationNotActive,
                "Business customers can only be created for an active organization.");
        }

        try
        {
            var customer = BusinessCustomer.Create(
                organizationId,
                request.DisplayName,
                _clock.UtcNow,
                request.Email,
                request.Phone,
                request.Notes,
                request.OwningProductCode);
            CustomerStaffSeparationGuard.EnsureNotTreatedAsStaff(customer);
            await _customers.AddAsync(customer, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<BusinessCustomerDto>.Success(
                BusinessCustomerQueryService.Map(customer, isCreditCustomer: false));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<BusinessCustomerDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class UpdateBusinessCustomer
{
    private readonly IBusinessCustomerRepository _customers;
    private readonly ICreditCustomerRepository _creditCustomers;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpdateBusinessCustomer(
        IBusinessCustomerRepository customers,
        ICreditCustomerRepository creditCustomers,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _customers = customers;
        _creditCustomers = creditCustomers;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<BusinessCustomerDto>> ExecuteAsync(
        BusinessCustomerId customerId,
        PlatformOrganizationId organizationId,
        UpdateBusinessCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        var customer = await _customers.GetByIdAsync(customerId, cancellationToken).ConfigureAwait(false);
        if (customer is null || customer.OrganizationId != organizationId)
        {
            return ApplicationResult<BusinessCustomerDto>.Failure(
                ApplicationErrorCodes.BusinessCustomerNotFound,
                "Business customer was not found.");
        }

        try
        {
            customer.UpdateProfile(
                request.DisplayName,
                request.Email,
                request.Phone,
                request.Notes,
                _clock.UtcNow);
            await _customers.UpdateAsync(customer, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            var credit = await _creditCustomers
                .FindActiveByBusinessCustomerAsync(customer.Id, cancellationToken)
                .ConfigureAwait(false);
            return ApplicationResult<BusinessCustomerDto>.Success(
                BusinessCustomerQueryService.Map(customer, credit is not null));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<BusinessCustomerDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ArchiveBusinessCustomer
{
    private readonly IBusinessCustomerRepository _customers;
    private readonly ICreditCustomerRepository _creditCustomers;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ArchiveBusinessCustomer(
        IBusinessCustomerRepository customers,
        ICreditCustomerRepository creditCustomers,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _customers = customers;
        _creditCustomers = creditCustomers;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<BusinessCustomerDto>> ExecuteAsync(
        BusinessCustomerId customerId,
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var customer = await _customers.GetByIdAsync(customerId, cancellationToken).ConfigureAwait(false);
        if (customer is null || customer.OrganizationId != organizationId)
        {
            return ApplicationResult<BusinessCustomerDto>.Failure(
                ApplicationErrorCodes.BusinessCustomerNotFound,
                "Business customer was not found.");
        }

        try
        {
            customer.Archive(_clock.UtcNow);
            await _customers.UpdateAsync(customer, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            var credit = await _creditCustomers
                .FindActiveByBusinessCustomerAsync(customer.Id, cancellationToken)
                .ConfigureAwait(false);
            return ApplicationResult<BusinessCustomerDto>.Success(
                BusinessCustomerQueryService.Map(customer, credit is not null));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<BusinessCustomerDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

/// <summary>Always denied: customer records cannot be promoted to staff.</summary>
public sealed class RejectPromoteBusinessCustomerToStaff
{
    public ApplicationResult<object> Execute()
    {
        try
        {
            CustomerStaffSeparationGuard.RejectCustomerToStaffConversion();
            return ApplicationResult<object>.Failure(
                DomainErrorCodes.CustomerToStaffConversionDenied,
                "Unexpected.");
        }
        catch (DomainException ex)
        {
            return ApplicationResult<object>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
