using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Application.Access;

/// <summary>
/// One active organization affiliation per Platform user and product.
/// Personal workspace is not an affiliation. A different product does not conflict.
/// Removed memberships and revoked product access do not count.
/// </summary>
public sealed record UserProductAffiliation(
    Guid OrganizationId,
    bool IsOwner,
    bool CanManageBilling);

public sealed class UserProductAffiliationGuard
{
    public const string AlreadyAssociatedMessage =
        "This account is already associated with another organization for this product.";

    public const string AlreadyHaveOrganizationMessage =
        "You already have an organization for this product.";

    private readonly IProductAccessAssignmentRepository _assignments;
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly ISubscriptionRepository _subscriptions;

    public UserProductAffiliationGuard(
        IProductAccessAssignmentRepository assignments,
        IOrganizationMembershipRepository memberships,
        ISubscriptionRepository subscriptions)
    {
        _assignments = assignments;
        _memberships = memberships;
        _subscriptions = subscriptions;
    }

    public async Task<UserProductAffiliation?> FindAsync(
        PlatformUserId userId,
        ProductCode productCode,
        CancellationToken cancellationToken = default)
    {
        var access = await _assignments
            .FindActiveByUserAndProductAsync(userId, productCode, cancellationToken)
            .ConfigureAwait(false);
        if (access is not null)
        {
            var membership = await _memberships
                .FindCurrentByUserAndOrganizationAsync(userId, access.OrganizationId, cancellationToken)
                .ConfigureAwait(false);
            if (membership is null || membership.Status == MembershipStatus.Removed)
            {
                return null;
            }

            return ToAffiliation(membership);
        }

        foreach (var status in new[] { MembershipStatus.Active, MembershipStatus.Suspended })
        {
            var page = await _memberships
                .ListByUserAsync(userId, status, skip: 0, take: 50, cancellationToken)
                .ConfigureAwait(false);
            foreach (var membership in page.Items)
            {
                var subscription = await _subscriptions
                    .GetCurrentForOrganizationProductAsync(membership.OrganizationId, productCode, cancellationToken)
                    .ConfigureAwait(false);
                if (subscription is not null)
                {
                    return ToAffiliation(membership);
                }
            }
        }

        return null;
    }

    public async Task<bool> ConflictsWithOtherOrganizationAsync(
        IEnumerable<PlatformUserId> userIds,
        ProductCode productCode,
        Guid targetOrganizationId,
        CancellationToken cancellationToken = default)
    {
        foreach (var userId in userIds.Distinct())
        {
            var affiliation = await FindAsync(userId, productCode, cancellationToken).ConfigureAwait(false);
            if (affiliation is not null && affiliation.OrganizationId != targetOrganizationId)
            {
                return true;
            }
        }

        return false;
    }

    private static UserProductAffiliation ToAffiliation(OrganizationMembership membership)
    {
        var isOwner = membership.Role == OrganizationRole.OrganizationOwner;
        var canManageBilling = isOwner || membership.Role == OrganizationRole.OrganizationAdministrator;
        return new UserProductAffiliation(membership.OrganizationId.Value, isOwner, canManageBilling);
    }
}
