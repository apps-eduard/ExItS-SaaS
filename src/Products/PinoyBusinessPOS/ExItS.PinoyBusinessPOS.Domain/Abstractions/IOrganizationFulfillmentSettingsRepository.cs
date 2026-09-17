using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Domain.Abstractions;

public interface IOrganizationFulfillmentSettingsRepository
{
    Task<OrganizationFulfillmentSettings?> GetAsync(
        PosOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        OrganizationFulfillmentSettings settings,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        OrganizationFulfillmentSettings settings,
        CancellationToken cancellationToken = default);
}
