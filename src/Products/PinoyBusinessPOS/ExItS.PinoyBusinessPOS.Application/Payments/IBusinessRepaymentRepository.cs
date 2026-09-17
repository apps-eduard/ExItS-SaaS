using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Payments;

namespace ExItS.PinoyBusinessPOS.Application.Payments;

public interface IBusinessRepaymentRepository
{
    Task<BusinessRepayment?> GetByIdAsync(
        PosOrganizationId sellerOrganizationId,
        BusinessRepaymentId repaymentId,
        CancellationToken cancellationToken = default);

    Task AddAsync(BusinessRepayment repayment, CancellationToken cancellationToken = default);

    Task UpdateAsync(BusinessRepayment repayment, CancellationToken cancellationToken = default);

    Task AddAllocationsAsync(
        IReadOnlyList<BusinessRepaymentAllocation> allocations,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BusinessRepaymentAllocation>> ListAllocationsByRepaymentAsync(
        PosOrganizationId sellerOrganizationId,
        BusinessRepaymentId repaymentId,
        CancellationToken cancellationToken = default);

    Task<decimal> SumSettledAmountAsync(
        PosOrganizationId sellerOrganizationId,
        PosOrganizationId buyerOrganizationId,
        CancellationToken cancellationToken = default);

    Task<decimal> SumPendingCheckAmountAsync(
        PosOrganizationId sellerOrganizationId,
        PosOrganizationId buyerOrganizationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BusinessRepayment>> ListByConnectionAsync(
        PosOrganizationId sellerOrganizationId,
        Guid connectionId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
}
