using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Payments;

namespace ExItS.PinoyBusinessPOS.Application.Payments;

public interface IRepaymentRepository
{
    Task<Repayment?> GetByIdAsync(
        PosOrganizationId organizationId,
        RepaymentId repaymentId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Repayment> Items, int TotalCount)> ListByCustomerAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Repayment> Items, int TotalCount)> ListCreatedSinceAsync(
        PosOrganizationId organizationId,
        DateTimeOffset? sinceUtc,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Repayments whose UTC calendar <c>CreatedAtUtc</c> falls in the inclusive range (all statuses).
    /// </summary>
    Task<IReadOnlyList<Repayment>> ListRecordedInRangeAsync(
        PosOrganizationId organizationId,
        DateOnly fromDateUtc,
        DateOnly toDateUtc,
        CancellationToken cancellationToken = default);

    Task<decimal> SumActiveAmountAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Active repayment totals keyed by customer id for one organization (reporting hot path).
    /// </summary>
    Task<IReadOnlyDictionary<Guid, decimal>> SumActiveAmountsByOrganizationAsync(
        PosOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task<int> CountActiveAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        CancellationToken cancellationToken = default);

    Task AddAsync(Repayment repayment, CancellationToken cancellationToken = default);

    Task UpdateAsync(Repayment repayment, CancellationToken cancellationToken = default);
}
