using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.Credit;

public interface ICreditDueDateChangeRepository
{
    Task AddAsync(CreditDueDateChange change, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<CreditDueDateChange> Items, int TotalCount)> ListByCreditAsync(
        PosOrganizationId organizationId,
        CreditEntryId creditEntryId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
}
