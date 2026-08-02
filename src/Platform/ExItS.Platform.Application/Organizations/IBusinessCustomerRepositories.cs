using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public interface IBusinessCustomerRepository
{
    Task<BusinessCustomer?> GetByIdAsync(BusinessCustomerId id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<BusinessCustomer> Items, int TotalCount)> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        string? owningProductCode,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(BusinessCustomer customer, CancellationToken cancellationToken = default);

    Task UpdateAsync(BusinessCustomer customer, CancellationToken cancellationToken = default);
}

public interface ICreditCustomerRepository
{
    Task<CreditCustomer?> GetByIdAsync(CreditCustomerId id, CancellationToken cancellationToken = default);

    Task<CreditCustomer?> FindActiveByBusinessCustomerAsync(
        BusinessCustomerId businessCustomerId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<CreditCustomer> Items, int TotalCount)> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(CreditCustomer creditCustomer, CancellationToken cancellationToken = default);

    Task UpdateAsync(CreditCustomer creditCustomer, CancellationToken cancellationToken = default);
}

public interface ICustomerLinkRequestRepository
{
    Task<CustomerLinkRequest?> GetByIdAsync(CustomerLinkRequestId id, CancellationToken cancellationToken = default);

    Task<CustomerLinkRequest?> FindPendingByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task<CustomerLinkRequest?> FindPendingByBusinessCustomerAsync(
        BusinessCustomerId businessCustomerId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<CustomerLinkRequest> Items, int TotalCount)> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        CustomerLinkRequestStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(CustomerLinkRequest request, CancellationToken cancellationToken = default);

    Task UpdateAsync(CustomerLinkRequest request, CancellationToken cancellationToken = default);
}

public interface ILinkedCustomerAppUserRepository
{
    Task<LinkedCustomerAppUser?> GetByIdAsync(
        LinkedCustomerAppUserId id,
        CancellationToken cancellationToken = default);

    Task<LinkedCustomerAppUser?> FindActiveByBusinessCustomerAsync(
        BusinessCustomerId businessCustomerId,
        CancellationToken cancellationToken = default);

    Task<LinkedCustomerAppUser?> FindActiveByUserAndOrganizationAsync(
        PlatformUserId userIdentityId,
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<LinkedCustomerAppUser> Items, int TotalCount)> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(LinkedCustomerAppUser link, CancellationToken cancellationToken = default);

    Task UpdateAsync(LinkedCustomerAppUser link, CancellationToken cancellationToken = default);
}
