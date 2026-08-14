using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Api.Organizations;

internal static class OrganizationComplianceProfileEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationComplianceProfileEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/api/v1/platform/organizations/{organizationId:guid}/compliance-profile",
            async (
                Guid organizationId,
                GetOrganizationComplianceProfile useCase,
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
                        nameof(OrganizationComplianceProfile),
                        organizationId.ToString("D"),
                        organizationId,
                        summary: "Read organization compliance profile.",
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
            "/api/v1/platform/organizations/{organizationId:guid}/compliance-profile/ensure",
            async (
                Guid organizationId,
                EnsureOrganizationComplianceProfile ensure,
                GetOrganizationComplianceProfile get,
                PlatformOrganizationAuthz orgAuthz,
                CancellationToken ct) =>
            {
                var denied = await orgAuthz
                    .EnsureCanManageOrganizationLifecycleAsync(
                        organizationId,
                        PlatformAuditActions.PlatformAccessChecked,
                        ct)
                    .ConfigureAwait(false);
                if (denied is not null)
                {
                    return denied;
                }

                var actor = orgAuthz.Inner.CurrentActor;
                await ensure
                    .ExecuteAsync(
                        PlatformOrganizationId.From(organizationId),
                        actor.PlatformUserId?.Value.ToString("D"),
                        ct)
                    .ConfigureAwait(false);

                var result = await get
                    .ExecuteAsync(PlatformOrganizationId.From(organizationId), ct)
                    .ConfigureAwait(false);
                return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
            });

        return app;
    }
}
