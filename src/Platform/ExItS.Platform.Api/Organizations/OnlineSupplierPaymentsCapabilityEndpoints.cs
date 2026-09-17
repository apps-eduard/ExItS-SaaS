using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Api.Organizations;

internal static class OnlineSupplierPaymentsCapabilityEndpoints
{
    public static IEndpointRouteBuilder MapOnlineSupplierPaymentsCapabilityEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/api/v1/platform/organizations/{organizationId:guid}/online-supplier-payments",
            async (
                Guid organizationId,
                GetOrganizationOnlineSupplierPaymentsCapability useCase,
                PlatformOrganizationAuthz orgAuthz,
                PlatformMembershipAuthz membershipAuthz,
                CancellationToken ct) =>
            {
                var viewDenied = await orgAuthz
                    .EnsureCanViewOrganizationAsync(organizationId, ct)
                    .ConfigureAwait(false);
                if (viewDenied is not null)
                {
                    var memberDenied = await membershipAuthz.EnsureActiveOrganizationMemberAsync(
                        PlatformAuditActions.PlatformAccessChecked,
                        nameof(OrganizationOnlineSupplierPaymentsCapability),
                        organizationId.ToString("D"),
                        organizationId,
                        summary: "Read organization online supplier payments capability.",
                        cancellationToken: ct).ConfigureAwait(false);
                    if (memberDenied is not null)
                    {
                        return viewDenied;
                    }
                }

                var result = await useCase
                    .ExecuteAsync(PlatformOrganizationId.From(organizationId), ct)
                    .ConfigureAwait(false);
                return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
            });

        app.MapPost(
            "/api/v1/platform/organizations/{organizationId:guid}/online-supplier-payments/transition",
            async (
                Guid organizationId,
                OnlineSupplierPaymentsTransitionRequest body,
                SetOrganizationOnlineSupplierPaymentsCapability useCase,
                PlatformOrganizationAuthz orgAuthz,
                CancellationToken ct) =>
            {
                var denied = await orgAuthz
                    .EnsureCanManageOrganizationLifecycleAsync(
                        organizationId,
                        PlatformAuditActions.OrganizationOnlineSupplierPaymentsEnabled,
                        ct)
                    .ConfigureAwait(false);
                if (denied is not null)
                {
                    return denied;
                }

                var actor = orgAuthz.Inner.CurrentActor;
                if (actor.PlatformUserId is null)
                {
                    return Results.Unauthorized();
                }

                var result = await useCase
                    .ExecuteAsync(
                        PlatformOrganizationId.From(organizationId),
                        body.Status,
                        actorReference: actor.PlatformUserId.Value.ToString("D"),
                        reason: body.Reason,
                        ct)
                    .ConfigureAwait(false);
                return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
            });

        return app;
    }

    private sealed record OnlineSupplierPaymentsTransitionRequest(string Status, string? Reason);
}
