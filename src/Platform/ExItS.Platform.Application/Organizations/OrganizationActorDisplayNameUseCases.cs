using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

/// <summary>
/// Safe internal projection for operational actor attribution (detail/history UI).
/// Does not expose email, phone, Personal profile, or cross-org memberships.
/// </summary>
public sealed record OrganizationActorDisplayNameDto(
    Guid ActorId,
    string DisplayName,
    string ActorStatus);

public sealed record ResolveOrganizationActorDisplayNamesRequest(IReadOnlyList<Guid> ActorIds);

/// <summary>
/// Org-scoped batch actor display-name resolution for POS/internal operational UI.
/// Authorization must be active org membership (or Platform view) — not ManageMemberships.
/// </summary>
public sealed class ResolveOrganizationActorDisplayNames
{
    public const int MaxActorIds = 100;
    public const string ActorStatusActive = "Active";
    public const string ActorStatusSuspended = "Suspended";
    public const string ActorStatusFormerStaff = "FormerStaff";
    public const string ActorStatusNotAvailable = "NotAvailable";
    public const string DisplayNameNotAvailable = "Not available";

    private readonly IOrganizationMembershipRepository _memberships;
    private readonly IPlatformUserRepository _users;

    public ResolveOrganizationActorDisplayNames(
        IOrganizationMembershipRepository memberships,
        IPlatformUserRepository users)
    {
        _memberships = memberships;
        _users = users;
    }

    public async Task<ApplicationResult<IReadOnlyList<OrganizationActorDisplayNameDto>>> ExecuteAsync(
        Guid organizationId,
        ResolveOrganizationActorDisplayNamesRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var distinct = (request.ActorIds ?? Array.Empty<Guid>())
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (distinct.Count > MaxActorIds)
        {
            return ApplicationResult<IReadOnlyList<OrganizationActorDisplayNameDto>>.Failure(
                ApplicationErrorCodes.DomainViolation,
                $"At most {MaxActorIds} actor ids may be resolved per request.");
        }

        if (distinct.Count == 0)
        {
            return ApplicationResult<IReadOnlyList<OrganizationActorDisplayNameDto>>.Success(
                Array.Empty<OrganizationActorDisplayNameDto>());
        }

        var orgId = PlatformOrganizationId.From(organizationId);
        var userIds = distinct.Select(PlatformUserId.From).ToList();

        var memberships = await _memberships
            .ListByOrganizationAndUserIdsAsync(orgId, userIds, cancellationToken)
            .ConfigureAwait(false);

        // Prefer latest membership row per user (repository orders by UpdatedAtUtc desc).
        var membershipByUser = new Dictionary<Guid, OrganizationMembership>();
        foreach (var membership in memberships)
        {
            membershipByUser.TryAdd(membership.UserId.Value, membership);
        }

        var memberUserIds = membershipByUser.Keys.Select(PlatformUserId.From).ToList();
        var users = memberUserIds.Count == 0
            ? Array.Empty<PlatformUser>()
            : await _users.ListByIdsAsync(memberUserIds, cancellationToken).ConfigureAwait(false);
        var userById = users.ToDictionary(u => u.Id.Value);

        var results = new List<OrganizationActorDisplayNameDto>(distinct.Count);
        foreach (var actorId in distinct)
        {
            if (!membershipByUser.TryGetValue(actorId, out var membership))
            {
                // Cross-org / unknown actor: do not leak global identity.
                results.Add(new OrganizationActorDisplayNameDto(
                    actorId,
                    DisplayNameNotAvailable,
                    ActorStatusNotAvailable));
                continue;
            }

            userById.TryGetValue(actorId, out var user);
            var displayName = string.IsNullOrWhiteSpace(user?.DisplayName)
                ? DisplayNameNotAvailable
                : user!.DisplayName.Trim();

            var status = membership.Status switch
            {
                MembershipStatus.Active => ActorStatusActive,
                MembershipStatus.Suspended => ActorStatusSuspended,
                MembershipStatus.Removed => ActorStatusFormerStaff,
                _ => ActorStatusNotAvailable,
            };

            results.Add(new OrganizationActorDisplayNameDto(actorId, displayName, status));
        }

        return ApplicationResult<IReadOnlyList<OrganizationActorDisplayNameDto>>.Success(results);
    }
}
