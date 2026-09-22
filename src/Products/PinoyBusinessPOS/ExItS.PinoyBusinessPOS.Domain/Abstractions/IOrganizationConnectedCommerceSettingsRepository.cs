using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Domain.Abstractions;

public interface IOrganizationConnectedCommerceSettingsRepository
{
    Task<OrganizationConnectedCommerceSettings?> GetAsync(
        PosOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        OrganizationConnectedCommerceSettings settings,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        OrganizationConnectedCommerceSettings settings,
        CancellationToken cancellationToken = default);
}
