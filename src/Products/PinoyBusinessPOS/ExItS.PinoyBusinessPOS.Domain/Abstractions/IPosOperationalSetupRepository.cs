using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.OperationalSetup;

namespace ExItS.PinoyBusinessPOS.Domain.Abstractions;

public interface IPosOperationalSetupRepository
{
    Task<PosOperationalSetup?> GetByOrganizationIdAsync(
        PosOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(PosOperationalSetup setup, CancellationToken cancellationToken = default);

    Task UpdateAsync(PosOperationalSetup setup, CancellationToken cancellationToken = default);
}
