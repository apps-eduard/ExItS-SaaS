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

    Task<decimal> SumActiveAmountAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        CancellationToken cancellationToken = default);

    Task<int> CountActiveAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        CancellationToken cancellationToken = default);

    Task AddAsync(Repayment repayment, CancellationToken cancellationToken = default);

    Task UpdateAsync(Repayment repayment, CancellationToken cancellationToken = default);
}
