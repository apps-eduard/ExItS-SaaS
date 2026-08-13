using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.OperationalSetup;

namespace ExItS.PinoyBusinessPOS.Domain.Abstractions;

public interface IOrganizationCashDenominationRepository
{
    Task<IReadOnlyList<OrganizationCashDenomination>> ListAsync(
        PosOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task ReplaceAsync(
        PosOrganizationId organizationId,
        IReadOnlyList<OrganizationCashDenomination> denominations,
        CancellationToken cancellationToken = default);
}
