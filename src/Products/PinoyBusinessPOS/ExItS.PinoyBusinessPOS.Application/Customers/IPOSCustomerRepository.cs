using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.Customers;

public interface IPOSCustomerRepository
{
    Task<POSCustomer?> GetByIdAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        CancellationToken cancellationToken = default);

    Task<POSCustomer?> FindActiveByNormalizedMobileAsync(
        PosOrganizationId organizationId,
        string normalizedMobile,
        CancellationToken cancellationToken = default);

    Task<POSCustomer?> FindByPlatformBusinessCustomerIdAsync(
        PosOrganizationId organizationId,
        Guid platformBusinessCustomerId,
        CancellationToken cancellationToken = default);

    Task<int> CountByPlatformBusinessCustomerIdAsync(
        PosOrganizationId organizationId,
        Guid platformBusinessCustomerId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<POSCustomer> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        CustomerStatus? status,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<POSCustomer> Items, int TotalCount)> ListUpdatedSinceAsync(
        PosOrganizationId organizationId,
        DateTimeOffset? sinceUtc,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<POSCustomer>> ListByIdsAsync(
        PosOrganizationId organizationId,
        IReadOnlyCollection<POSCustomerId> customerIds,
        CancellationToken cancellationToken = default);

    Task AddAsync(POSCustomer customer, CancellationToken cancellationToken = default);

    Task UpdateAsync(POSCustomer customer, CancellationToken cancellationToken = default);
}

public interface IPosUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<T> ExecuteInSerializableTransactionAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default);
}
