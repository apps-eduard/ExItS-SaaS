using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Permissions;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Permissions;

namespace ExItS.PinoyBusinessPOS.Api.Common;

/// <summary>
/// Resolves the active POS role for the request actor.
/// WP09: Platform-mapped product-local roles sync into the POS DB when present on bearer introspection.
/// Development/Testing Owner auto-bootstrap remains when no Platform mapped role is available (R-091).
/// Organization management authority (Owner/Administrator without POS checkout role) is preserved
/// without inventing a product-local Owner assignment.
/// </summary>
internal sealed class PosRoleResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        IPosRoleAssignmentRepository roles,
        IPosUnitOfWork unitOfWork,
        IClock clock,
        IHostEnvironment environment)
    {
        PosRoleRequestContext.Clear();
        Guid? organizationIdForLog = null;

        try
        {
            if (!PosOrganizationScope.TryGetOrganizationId(context.Request, out var organizationId, out _)
                || !PosOrganizationScope.TryGetActorId(context.Request, out var actorId, out _))
            {
                await next(context).ConfigureAwait(false);
                return;
            }

            organizationIdForLog = organizationId;
            PosRoleRequestContext.HasActorHeader = true;

            if (context.Items.TryGetValue(PosAuthItems.OrganizationManagementAuthority, out var mgmt)
                && mgmt is true)
            {
                PosRoleRequestContext.OrganizationManagementAuthority = true;
                if (context.Items.TryGetValue(PosAuthItems.MembershipRole, out var roleRaw)
                    && roleRaw is string membershipRole
                    && string.Equals(membershipRole, "OrganizationOwner", StringComparison.OrdinalIgnoreCase))
                {
                    PosRoleRequestContext.OrganizationManagementIsExactOwner = true;
                }
            }

            var org = PosOrganizationId.From(organizationId);
            var active = await roles.GetActiveForActorAsync(org, actorId, context.RequestAborted).ConfigureAwait(false);
            if (active is not null)
            {
                PosRoleRequestContext.CurrentRole = active.Role;
            }
            else if (TryGetMappedPlatformRole(context, out var platformRole))
            {
                try
                {
                    var assignment = PosRoleAssignment.Assign(
                        org,
                        actorId,
                        platformRole,
                        actorId,
                        clock.UtcNow);
                    await roles.AddAsync(assignment, context.RequestAborted).ConfigureAwait(false);
                    await unitOfWork.SaveChangesAsync(context.RequestAborted).ConfigureAwait(false);
                    PosRoleRequestContext.CurrentRole = platformRole;
                }
                catch
                {
                    active = await roles.GetActiveForActorAsync(org, actorId, context.RequestAborted)
                        .ConfigureAwait(false);
                    PosRoleRequestContext.CurrentRole = active?.Role ?? platformRole;
                }
            }
            else if (!PosRoleRequestContext.OrganizationManagementAuthority
                     && PosDevelopmentEnvironment.IsApprovedDevelopmentEnvironment(environment))
            {
                // Owner auto-bootstrap remains Development/Testing only (not for management-authority actors).
                var ownerCount = await roles.CountActiveOwnersAsync(org, context.RequestAborted).ConfigureAwait(false);
                if (ownerCount == 0)
                {
                    try
                    {
                        var assignment = PosRoleAssignment.Assign(
                            org,
                            actorId,
                            PosRole.Owner,
                            actorId,
                            clock.UtcNow);
                        await roles.AddAsync(assignment, context.RequestAborted).ConfigureAwait(false);
                        await unitOfWork.SaveChangesAsync(context.RequestAborted).ConfigureAwait(false);
                        PosRoleRequestContext.CurrentRole = PosRole.Owner;
                    }
                    catch
                    {
                        active = await roles.GetActiveForActorAsync(org, actorId, context.RequestAborted)
                            .ConfigureAwait(false);
                        PosRoleRequestContext.CurrentRole = active?.Role;
                    }
                }
                else
                {
                    // Trusted Dev/Testing aid for shared-org fixtures: unassigned actors act as Owner.
                    PosRoleRequestContext.CurrentRole = PosRole.Owner;
                }
            }

            await next(context).ConfigureAwait(false);
            LogDenialIfNeeded(context, environment, organizationIdForLog);
        }
        finally
        {
            PosRoleRequestContext.Clear();
        }
    }

    private static void LogDenialIfNeeded(
        HttpContext context,
        IHostEnvironment environment,
        Guid? organizationId)
    {
        if (!(environment.IsDevelopment() || environment.IsEnvironment("Testing")))
        {
            return;
        }

        if (context.Response.StatusCode is not (StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden))
        {
            return;
        }

        var hint = PosAuthorizationDiagnostics.ConsumeLast();
        if (string.IsNullOrWhiteSpace(hint))
        {
            return;
        }

        var logger = context.RequestServices.GetService<ILoggerFactory>()
            ?.CreateLogger("ExItS.Pos.Authorization");
        logger?.LogInformation(
            "POS authorization denied. platformUserId={UserId}; organizationId={OrganizationId}; membershipRole={MembershipRole}; ownerManagement={OwnerMgmt}; {Hint}",
            context.Items.TryGetValue(PosAuthItems.UserId, out var uid) ? uid : null,
            organizationId,
            context.Items.TryGetValue(PosAuthItems.MembershipRole, out var mr) ? mr : null,
            context.Items.TryGetValue(PosAuthItems.OrganizationManagementAuthority, out var om) && om is true,
            hint);
    }

    private static bool TryGetMappedPlatformRole(HttpContext context, out PosRole role)
    {
        role = default;
        if (!context.Items.TryGetValue(PosAuthItems.MappedPosRoleCode, out var raw)
            || raw is not string code
            || string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        return PosRoleCodes.TryParse(code, out role);
    }
}
