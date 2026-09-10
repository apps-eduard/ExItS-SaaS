using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.Credit;

public interface ICustomerCreditPolicyRepository
{
    Task<CustomerCreditPolicy?> GetByCustomerAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch load policies for the given customer ids (single query). Empty ids → empty list.
    /// </summary>
    Task<IReadOnlyList<CustomerCreditPolicy>> ListByCustomerIdsAsync(
        PosOrganizationId organizationId,
        IReadOnlyCollection<Guid> customerIds,
        CancellationToken cancellationToken = default);

    Task AddAsync(CustomerCreditPolicy policy, CancellationToken cancellationToken = default);

    Task UpdateAsync(CustomerCreditPolicy policy, CancellationToken cancellationToken = default);

    Task AddChangeAsync(CustomerCreditPolicyChange change, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<CustomerCreditPolicyChange> Items, int TotalCount)> ListChangesAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Transaction-scoped advisory lock for customer credit mutations (org + customer).
    /// </summary>
    Task AcquireCustomerCreditLockAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        CancellationToken cancellationToken = default);
}
