using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.Credit;

public interface IBusinessCustomerCreditPolicyRepository
{
    Task<BusinessCustomerCreditPolicy?> GetBySellerAndBuyerAsync(
        PosOrganizationId sellerOrganizationId,
        PosOrganizationId buyerOrganizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch load seller-owned B2B credit policies for checkout directory projection (no N+1).
    /// </summary>
    Task<IReadOnlyList<BusinessCustomerCreditPolicy>> ListBySellerAndBuyerIdsAsync(
        PosOrganizationId sellerOrganizationId,
        IReadOnlyCollection<Guid> buyerOrganizationIds,
        CancellationToken cancellationToken = default);

    Task AddAsync(BusinessCustomerCreditPolicy policy, CancellationToken cancellationToken = default);

    Task UpdateAsync(BusinessCustomerCreditPolicy policy, CancellationToken cancellationToken = default);

    Task AddChangeAsync(BusinessCustomerCreditPolicyChange change, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<BusinessCustomerCreditPolicyChange> Items, int TotalCount)> ListChangesAsync(
        PosOrganizationId sellerOrganizationId,
        PosOrganizationId buyerOrganizationId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Transaction-scoped advisory lock for B2B credit mutations (seller + buyer).
    /// </summary>
    Task AcquireBusinessCustomerCreditLockAsync(
        PosOrganizationId sellerOrganizationId,
        PosOrganizationId buyerOrganizationId,
        CancellationToken cancellationToken = default);
}
