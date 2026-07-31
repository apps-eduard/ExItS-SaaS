using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Permissions;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Permissions;

namespace ExItS.PinoyBusinessPOS.Api.Common;

/// <summary>
/// Resolves the active POS role for the request actor. In Development/Testing, when an organization
/// has no active Owner, the current actor is auto-bootstrapped as Owner (trusted primary-owner aid).
/// Production resolves assignments only — no auto-bootstrap (R-091).
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

        try
        {
            if (!PosOrganizationScope.TryGetOrganizationId(context.Request, out var organizationId, out _)
                || !PosOrganizationScope.TryGetActorId(context.Request, out var actorId, out _))
            {
                await next(context).ConfigureAwait(false);
                return;
            }

            PosRoleRequestContext.HasActorHeader = true;

            var org = PosOrganizationId.From(organizationId);
            var active = await roles.GetActiveForActorAsync(org, actorId, context.RequestAborted).ConfigureAwait(false);
            if (active is not null)
            {
                PosRoleRequestContext.CurrentRole = active.Role;
                await next(context).ConfigureAwait(false);
                return;
            }

            // Owner auto-bootstrap remains Development/Testing only.
            if (!PosDevelopmentEnvironment.IsApprovedDevelopmentEnvironment(environment))
            {
                await next(context).ConfigureAwait(false);
                return;
            }

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
                // Persisted assignments (Cashier, InventoryStaff, …) always win above.
                // Production identity provisioning remains out of scope (R-091).
                PosRoleRequestContext.CurrentRole = PosRole.Owner;
            }

            await next(context).ConfigureAwait(false);
        }
        finally
        {
            PosRoleRequestContext.Clear();
        }
    }
}
