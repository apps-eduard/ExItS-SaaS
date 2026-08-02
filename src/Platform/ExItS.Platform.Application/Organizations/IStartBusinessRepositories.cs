using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public interface IBusinessCreditOpeningBalanceRepository
{
    Task<BusinessCreditOpeningBalance?> GetByIdAsync(
        BusinessCreditOpeningBalanceId id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BusinessCreditOpeningBalance>> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(BusinessCreditOpeningBalance balance, CancellationToken cancellationToken = default);
}

public interface IProductLocalRoleGrantRepository
{
    Task<ProductLocalRoleGrant?> FindAsync(
        PlatformOrganizationId organizationId,
        PlatformUserId userIdentityId,
        string productCode,
        string roleCode,
        CancellationToken cancellationToken = default);

    Task AddAsync(ProductLocalRoleGrant grant, CancellationToken cancellationToken = default);
}
