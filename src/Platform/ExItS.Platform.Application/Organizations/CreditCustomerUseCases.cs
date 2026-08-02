using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public sealed record CreditCustomerDto(
    Guid Id,
    Guid OrganizationId,
    Guid BusinessCustomerId,
    string CurrencyCode,
    string Status,
    bool IsOrganizationStaff,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed class CreditCustomerQueryService
{
    private readonly ICreditCustomerRepository _creditCustomers;

    public CreditCustomerQueryService(ICreditCustomerRepository creditCustomers) =>
        _creditCustomers = creditCustomers;

    public async Task<PagedResult<CreditCustomerDto>> ListAsync(
        Guid organizationId,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = CatalogPagination.Normalize(page, pageSize);
        var (items, total) = await _creditCustomers
            .ListByOrganizationAsync(PlatformOrganizationId.From(organizationId), skip, take, cancellationToken)
            .ConfigureAwait(false);
        return new PagedResult<CreditCustomerDto>(
            items.Select(Map).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }

    public static CreditCustomerDto Map(CreditCustomer credit) =>
        new(
            credit.Id.Value,
            credit.OrganizationId.Value,
            credit.BusinessCustomerId.Value,
            credit.CurrencyCode,
            credit.Status.ToString(),
            IsOrganizationStaff: false,
            credit.CreatedAtUtc,
            credit.UpdatedAtUtc);
}

public sealed class EnableCreditCustomer
{
    private readonly IBusinessCustomerRepository _customers;
    private readonly ICreditCustomerRepository _creditCustomers;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public EnableCreditCustomer(
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

    public async Task<ApplicationResult<CreditCustomerDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        BusinessCustomerId businessCustomerId,
        string? currencyCode,
        CancellationToken cancellationToken = default)
    {
        var customer = await _customers.GetByIdAsync(businessCustomerId, cancellationToken).ConfigureAwait(false);
        if (customer is null || customer.OrganizationId != organizationId)
        {
            return ApplicationResult<CreditCustomerDto>.Failure(
                ApplicationErrorCodes.BusinessCustomerNotFound,
                "Business customer was not found.");
        }

        CustomerStaffSeparationGuard.EnsureNotTreatedAsStaff(customer);

        var existing = await _creditCustomers
            .FindActiveByBusinessCustomerAsync(businessCustomerId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            return ApplicationResult<CreditCustomerDto>.Failure(
                ApplicationErrorCodes.CreditCustomerConflict,
                "An active credit customer already exists for this business customer.");
        }

        try
        {
            var credit = CreditCustomer.Create(
                organizationId,
                businessCustomerId,
                _clock.UtcNow,
                currencyCode ?? "PHP");
            await _creditCustomers.AddAsync(credit, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<CreditCustomerDto>.Success(CreditCustomerQueryService.Map(credit));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<CreditCustomerDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class CloseCreditCustomer
{
    private readonly ICreditCustomerRepository _creditCustomers;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CloseCreditCustomer(
        ICreditCustomerRepository creditCustomers,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _creditCustomers = creditCustomers;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<CreditCustomerDto>> ExecuteAsync(
        CreditCustomerId creditCustomerId,
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var credit = await _creditCustomers.GetByIdAsync(creditCustomerId, cancellationToken).ConfigureAwait(false);
        if (credit is null || credit.OrganizationId != organizationId)
        {
            return ApplicationResult<CreditCustomerDto>.Failure(
                ApplicationErrorCodes.CreditCustomerNotFound,
                "Credit customer was not found.");
        }

        try
        {
            credit.Close(_clock.UtcNow);
            await _creditCustomers.UpdateAsync(credit, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<CreditCustomerDto>.Success(CreditCustomerQueryService.Map(credit));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<CreditCustomerDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
