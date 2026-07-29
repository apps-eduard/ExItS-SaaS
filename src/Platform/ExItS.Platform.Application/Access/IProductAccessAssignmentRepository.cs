using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Application.Access;

public interface IProductAccessAssignmentRepository
{
    Task<ProductAccessAssignment?> GetByIdAsync(
        ProductAccessAssignmentId id,
        CancellationToken cancellationToken = default);

    Task<ProductAccessAssignment?> FindActiveByUserOrganizationProductAsync(
        PlatformUserId userId,
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<ProductAccessAssignment> Items, int TotalCount)> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        ProductAccessStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<ProductAccessAssignment> Items, int TotalCount)> ListByUserAsync(
        PlatformUserId userId,
        ProductAccessStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<ProductAccessAssignment> Items, int TotalCount)> ListByProductAsync(
        ProductCode productCode,
        ProductAccessStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductAccessAssignment>> ListActiveByMembershipAsync(
        OrganizationMembershipId membershipId,
        CancellationToken cancellationToken = default);

    Task AddAsync(ProductAccessAssignment assignment, CancellationToken cancellationToken = default);

    Task UpdateAsync(ProductAccessAssignment assignment, CancellationToken cancellationToken = default);
}
