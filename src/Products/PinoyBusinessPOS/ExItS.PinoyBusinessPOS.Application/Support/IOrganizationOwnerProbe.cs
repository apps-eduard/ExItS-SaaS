using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Application.Platform;
using ExItS.PinoyBusinessPOS.Domain.Permissions;

namespace ExItS.PinoyBusinessPOS.Application.Support;

/// <summary>Resolves whether the current user is owner-class for an organization (diagnostics gate).</summary>
public interface IOrganizationOwnerProbe
{
    Task<bool> IsOwnerAsync(AuthSession session, Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// Exact Platform <c>OrganizationOwner</c> only (not Administrator). Used for sales-document education.
    /// Does not use offline POS Owner grants — Platform ownership is authoritative.
    /// </summary>
    Task<bool> IsExactOrganizationOwnerAsync(
        AuthSession session,
        Guid organizationId,
        CancellationToken ct = default);
}

/// <summary>
/// Online: Platform OrganizationOwner / OrganizationAdministrator.
/// Offline fallback: durable Organization grant with POS Owner role for the same org/user.
/// </summary>
public sealed class PlatformOrganizationOwnerProbe(
    IProductAccessResolver accessResolver,
    IPlatformAccessClient platform,
    IOfflineOperatingGrantService offlineGrant,
    TimeProvider time) : IOrganizationOwnerProbe
{
    public Task<bool> IsOwnerAsync(
        AuthSession session,
        Guid organizationId,
        CancellationToken ct = default) =>
        ResolveOnlineOwnerAsync(
            session,
            organizationId,
            OrganizationMembershipRoles.IsOwnerRole,
            allowOfflinePosOwnerFallback: true,
            ct);

    public Task<bool> IsExactOrganizationOwnerAsync(
        AuthSession session,
        Guid organizationId,
        CancellationToken ct = default) =>
        ResolveOnlineOwnerAsync(
            session,
            organizationId,
            role => string.Equals(
                role,
                OrganizationMembershipRoles.Owner,
                StringComparison.OrdinalIgnoreCase),
            allowOfflinePosOwnerFallback: false,
            ct);

    private async Task<bool> ResolveOnlineOwnerAsync(
        AuthSession session,
        Guid organizationId,
        Func<string?, bool> roleMatches,
        bool allowOfflinePosOwnerFallback,
        CancellationToken ct)
    {
        try
        {
            var eligible = await accessResolver
                .ListEligibleOrganizationsAsync(session.UserId, ct)
                .ConfigureAwait(false);
            if (eligible.Any(o =>
                    o.OrganizationId == organizationId
                    && roleMatches(o.MembershipRole)))
            {
                return true;
            }
        }
        catch
        {
            // Fall through.
        }

        try
        {
            var memberships = await platform.GetUserMembershipsAsync(session.UserId, ct)
                .ConfigureAwait(false);
            if (memberships.IsSuccess
                && memberships.Data is not null
                && memberships.Data.Items.Any(m =>
                    m.OrganizationId == organizationId
                    && string.Equals(m.Status, "Active", StringComparison.OrdinalIgnoreCase)
                    && roleMatches(m.Role)))
            {
                return true;
            }
        }
        catch
        {
            // Fall through.
        }

        if (!allowOfflinePosOwnerFallback)
        {
            return false;
        }

        var grant = session.UserId != Guid.Empty
            ? await offlineGrant.PeekStoredGrantAsync(session.UserId, ct).ConfigureAwait(false)
            : await offlineGrant.PeekStoredGrantAsync(ct).ConfigureAwait(false);
        return MatchesOfflineOwnerGrant(grant, session, organizationId, time.GetUtcNow());
    }

    /// <summary>
    /// Offline owner-class gate: durable Organization grant for the same user/org with POS Owner role.
    /// </summary>
    public static bool MatchesOfflineOwnerGrant(
        OfflineOperatingGrant? grant,
        AuthSession session,
        Guid organizationId,
        DateTimeOffset utcNow) =>
        grant is not null
        && grant.IsOrganizationScope
        && grant.OrganizationId == organizationId
        && grant.UserId == session.UserId
        && !grant.IsExpired(utcNow)
        && PosRoleCodes.TryParse(grant.RoleCode, out var role)
        && role == PosRole.Owner;
}
