using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.Credit;

public interface ICreditEntryRepository
{
    Task<CreditEntry?> GetByIdAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        CreditEntryId entryId,
        CancellationToken cancellationToken = default);

    Task<CreditEntry?> GetByIdForOrganizationAsync(
        PosOrganizationId organizationId,
        CreditEntryId entryId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<CreditEntry> Items, int TotalCount)> ListByCustomerAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CreditEntry>> ListActiveByOrganizationAsync(
        PosOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task<decimal> SumActiveAmountAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        CancellationToken cancellationToken = default);

    Task<int> CountActiveAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        CancellationToken cancellationToken = default);

    Task AddAsync(CreditEntry entry, CancellationToken cancellationToken = default);

    Task UpdateAsync(CreditEntry entry, CancellationToken cancellationToken = default);
}
