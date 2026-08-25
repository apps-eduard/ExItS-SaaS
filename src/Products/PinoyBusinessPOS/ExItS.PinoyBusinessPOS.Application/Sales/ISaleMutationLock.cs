using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Application.Sales;

/// <summary>
/// Transaction-scoped exclusive lock for mutations that race on the same sale
/// (returns and voids). Must be acquired inside an ambient DB transaction.
/// </summary>
public interface ISaleMutationLock
{
    Task AcquireAsync(
        PosOrganizationId organizationId,
        SaleId saleId,
        CancellationToken cancellationToken = default);
}
