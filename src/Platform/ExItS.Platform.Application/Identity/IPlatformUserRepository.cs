using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Identity;

public interface IPlatformUserRepository
{
    Task<PlatformUser?> GetByIdAsync(PlatformUserId id, CancellationToken cancellationToken = default);

    /// <summary>Batch load by id. Missing ids are omitted from the result.</summary>
    Task<IReadOnlyList<PlatformUser>> ListByIdsAsync(
        IReadOnlyCollection<PlatformUserId> ids,
        CancellationToken cancellationToken = default);

    Task<PlatformUser?> GetByPublicUserIdAsync(string publicUserId, CancellationToken cancellationToken = default);

    Task<PlatformUser?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);

    Task<PlatformUser?> GetByNormalizedUsernameAsync(string normalizedUsername, CancellationToken cancellationToken = default);

    /// <summary>
    /// Active organization-scoped staff with the given home org and contact email.
    /// </summary>
    Task<PlatformUser?> FindActiveStaffByHomeOrgAndContactEmailAsync(
        PlatformOrganizationId homeOrganizationId,
        string normalizedContactEmail,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Active organization-scoped staff for the home org whose LinkedPersonalUserId matches the Personal identity.
    /// </summary>
    Task<PlatformUser?> FindActiveStaffByHomeOrgAndLinkedPersonalUserIdAsync(
        PlatformOrganizationId homeOrganizationId,
        PlatformUserId linkedPersonalUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Users whose NormalizedContactEmail matches (staff identities). Contact email is not unique.
    /// </summary>
    Task<IReadOnlyList<PlatformUser>> ListByNormalizedContactEmailAsync(
        string normalizedContactEmail,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<PlatformUser> Items, int TotalCount)> ListAsync(
        AccountStatus? status,
        string? search,
        UserDirectoryFilter? directoryFilter,
        string? sortBy,
        bool sortDesc,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, PlatformUserDirectoryExtras>> GetDirectoryExtrasAsync(
        IReadOnlyList<Guid> userIds,
        CancellationToken cancellationToken = default);

    Task AddAsync(PlatformUser user, CancellationToken cancellationToken = default);

    Task UpdateAsync(PlatformUser user, CancellationToken cancellationToken = default);
}
