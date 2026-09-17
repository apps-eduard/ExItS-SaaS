using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Payments;

namespace ExItS.PinoyBusinessPOS.Domain.Abstractions;

public interface IOrganizationPaymentMethodSettingRepository
{
    Task<IReadOnlyList<OrganizationPaymentMethodSetting>> ListByOrganizationAsync(
        PosOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task<OrganizationPaymentMethodSetting?> GetAsync(
        PosOrganizationId organizationId,
        string methodCode,
        CancellationToken cancellationToken = default);

    Task AddAsync(OrganizationPaymentMethodSetting setting, CancellationToken cancellationToken = default);

    Task UpdateAsync(OrganizationPaymentMethodSetting setting, CancellationToken cancellationToken = default);
}
