using System.Security.Claims;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Realtime;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ExItS.Platform.Api.Realtime;

/// <summary>
/// Canonical Platform real-time hub. Clients join server-authorized groups only.
/// Legacy hubs remain mapped for dual-publish during migration.
/// </summary>
[Authorize]
public sealed class ExitsRealtimeHub(
    IOrganizationMembershipRepository memberships,
    ILinkedCustomerAppUserRepository linkedCustomers) : Hub
{
    public const string HubPath = "/hubs/exits-realtime";
    public const string ClientMethod = "RealtimeEvent";

    public override async Task OnConnectedAsync()
    {
        if (TryGetUserId(out var userId))
        {
            await Groups
                .AddToGroupAsync(Context.ConnectionId, RealtimeGroupNames.PersonalUser(userId))
                .ConfigureAwait(false);
        }

        await base.OnConnectedAsync().ConfigureAwait(false);
    }

    /// <summary>Join organization staff/owner group after membership check.</summary>
    public async Task JoinOrganization(Guid organizationId)
    {
        if (organizationId == Guid.Empty || !TryGetUserId(out var userId))
        {
            return;
        }

        var membership = await memberships
            .FindActiveByUserAndOrganizationAsync(
                PlatformUserId.From(userId),
                PlatformOrganizationId.From(organizationId),
                Context.ConnectionAborted)
            .ConfigureAwait(false);
        if (membership is null)
        {
            return;
        }

        await Groups
            .AddToGroupAsync(Context.ConnectionId, RealtimeGroupNames.Organization(organizationId))
            .ConfigureAwait(false);
    }

    public Task LeaveOrganization(Guid organizationId)
    {
        if (organizationId == Guid.Empty)
        {
            return Task.CompletedTask;
        }

        return Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            RealtimeGroupNames.Organization(organizationId));
    }

    /// <summary>
    /// Storefront availability group. Allowed when the caller is an active org member
    /// OR an actively linked Personal customer of that seller organization.
    /// Unrelated authenticated users are rejected (no group join).
    /// </summary>
    public async Task JoinStorefrontSeller(Guid organizationId)
    {
        if (organizationId == Guid.Empty || !TryGetUserId(out var userId))
        {
            return;
        }

        var orgId = PlatformOrganizationId.From(organizationId);
        var platformUserId = PlatformUserId.From(userId);

        var membership = await memberships
            .FindActiveByUserAndOrganizationAsync(platformUserId, orgId, Context.ConnectionAborted)
            .ConfigureAwait(false);
        if (membership is not null)
        {
            await Groups
                .AddToGroupAsync(Context.ConnectionId, RealtimeGroupNames.StorefrontSeller(organizationId))
                .ConfigureAwait(false);
            return;
        }

        var link = await linkedCustomers
            .FindActiveByUserAndOrganizationAsync(platformUserId, orgId, Context.ConnectionAborted)
            .ConfigureAwait(false);
        if (link is null)
        {
            return;
        }

        await Groups
            .AddToGroupAsync(Context.ConnectionId, RealtimeGroupNames.StorefrontSeller(organizationId))
            .ConfigureAwait(false);
    }

    public Task LeaveStorefrontSeller(Guid organizationId)
    {
        if (organizationId == Guid.Empty)
        {
            return Task.CompletedTask;
        }

        return Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            RealtimeGroupNames.StorefrontSeller(organizationId));
    }

    private bool TryGetUserId(out Guid userId)
    {
        var raw = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out userId);
    }
}
