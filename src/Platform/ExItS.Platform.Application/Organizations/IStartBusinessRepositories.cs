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
    Task<ProductLocalRoleGrant?> GetByIdAsync(
        ProductLocalRoleGrantId id,
        CancellationToken cancellationToken = default);

    Task<ProductLocalRoleGrant?> FindAsync(
        PlatformOrganizationId organizationId,
        PlatformUserId userIdentityId,
        string productCode,
        string roleCode,
        CancellationToken cancellationToken = default);

    Task<ProductLocalRoleGrant?> FindActiveByUserOrganizationProductAsync(
        PlatformOrganizationId organizationId,
        PlatformUserId userIdentityId,
        string productCode,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductLocalRoleGrant>> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        ProductLocalRoleGrantStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductLocalRoleGrant>> ListActiveByUserOrganizationAsync(
        PlatformOrganizationId organizationId,
        PlatformUserId userIdentityId,
        CancellationToken cancellationToken = default);

    Task AddAsync(ProductLocalRoleGrant grant, CancellationToken cancellationToken = default);

    Task UpdateAsync(ProductLocalRoleGrant grant, CancellationToken cancellationToken = default);
}
