using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Authorization;

public interface IPlatformRoleDefinitionRepository
{
    Task<PlatformRoleDefinition?> GetByIdAsync(PlatformRoleDefinitionId id, CancellationToken cancellationToken = default);

    Task<PlatformRoleDefinition?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<PlatformRoleDefinition> Items, int TotalCount)> ListAsync(
        PlatformRoleKind? kind,
        PlatformRoleLifecycleStatus? status,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(PlatformRoleDefinition definition, CancellationToken cancellationToken = default);

    Task UpdateAsync(PlatformRoleDefinition definition, int? expectedVersion, CancellationToken cancellationToken = default);
}

public interface IPlatformCustomRoleAssignmentRepository
{
    Task<PlatformCustomRoleAssignment?> GetByIdAsync(
        PlatformCustomRoleAssignmentId id,
        CancellationToken cancellationToken = default);

    Task<PlatformCustomRoleAssignment?> FindActiveAsync(
        PlatformUserId userId,
        PlatformRoleDefinitionId roleDefinitionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlatformCustomRoleAssignment>> ListActiveByUserAsync(
        PlatformUserId userId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<PlatformCustomRoleAssignment> Items, int TotalCount)> ListAsync(
        PlatformUserId? userId,
        PlatformRoleDefinitionId? roleDefinitionId,
        PlatformRoleAssignmentStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(PlatformCustomRoleAssignment assignment, CancellationToken cancellationToken = default);

    Task UpdateAsync(PlatformCustomRoleAssignment assignment, CancellationToken cancellationToken = default);
}

public interface IOrganizationRoleDefinitionRepository
{
    Task<OrganizationRoleDefinition?> GetByIdAsync(
        OrganizationRoleDefinitionId id,
        CancellationToken cancellationToken = default);

    Task<OrganizationRoleDefinition?> GetByOrgAndCodeAsync(
        PlatformOrganizationId organizationId,
        string code,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<OrganizationRoleDefinition> Items, int TotalCount)> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        PlatformRoleLifecycleStatus? status,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(OrganizationRoleDefinition definition, CancellationToken cancellationToken = default);

    Task UpdateAsync(OrganizationRoleDefinition definition, int? expectedVersion, CancellationToken cancellationToken = default);
}

public interface IOrganizationCustomRoleAssignmentRepository
{
    Task<OrganizationCustomRoleAssignment?> GetByIdAsync(
        OrganizationCustomRoleAssignmentId id,
        CancellationToken cancellationToken = default);

    Task<OrganizationCustomRoleAssignment?> FindActiveAsync(
        PlatformOrganizationId organizationId,
        PlatformUserId userId,
        OrganizationRoleDefinitionId roleDefinitionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrganizationCustomRoleAssignment>> ListActiveByUserAsync(
        PlatformOrganizationId organizationId,
        PlatformUserId userId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<OrganizationCustomRoleAssignment> Items, int TotalCount)> ListAsync(
        PlatformOrganizationId organizationId,
        PlatformUserId? userId,
        OrganizationRoleDefinitionId? roleDefinitionId,
        PlatformRoleAssignmentStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(OrganizationCustomRoleAssignment assignment, CancellationToken cancellationToken = default);

    Task UpdateAsync(OrganizationCustomRoleAssignment assignment, CancellationToken cancellationToken = default);
}
