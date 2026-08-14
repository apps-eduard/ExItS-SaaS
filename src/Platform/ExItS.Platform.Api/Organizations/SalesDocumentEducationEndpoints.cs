using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Api.Organizations;

internal static class SalesDocumentEducationEndpoints
{
    public static IEndpointRouteBuilder MapSalesDocumentEducationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/api/v1/platform/organizations/{organizationId:guid}/sales-document-education",
            async (
                Guid organizationId,
                GetSalesDocumentEducationStatus useCase,
                PlatformMembershipAuthz membershipAuthz,
                CancellationToken ct) =>
            {
                var denied = await membershipAuthz.EnsureActiveOrganizationMemberAsync(
                    PlatformAuditActions.PlatformAccessChecked,
                    nameof(OrganizationSalesDocumentAcknowledgment),
                    organizationId.ToString("D"),
                    organizationId,
                    summary: "Read organization sales-document education status.",
                    cancellationToken: ct).ConfigureAwait(false);
                if (denied is not null)
                {
                    return denied;
                }

                var actorUserId = membershipAuthz.Inner.CurrentActor.PlatformUserId;
                if (actorUserId is null)
                {
                    return Results.Unauthorized();
                }

                var result = await useCase
                    .ExecuteAsync(
                        PlatformOrganizationId.From(organizationId),
                        actorUserId,
                        ct)
                    .ConfigureAwait(false);
                return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
            });

        app.MapPost(
            "/api/v1/platform/organizations/{organizationId:guid}/sales-document-education/acknowledge",
            async (
                Guid organizationId,
                AcknowledgeSalesDocumentEducation useCase,
                PlatformMembershipAuthz membershipAuthz,
                CancellationToken ct) =>
            {
                var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                    PlatformAuditActions.OrganizationSalesDocumentEducationAcknowledged,
                    nameof(OrganizationSalesDocumentAcknowledgment),
                    organizationId.ToString("D"),
                    organizationId,
                    summary: "Acknowledge organization sales-document education.",
                    cancellationToken: ct).ConfigureAwait(false);
                if (denied is not null)
                {
                    return denied;
                }

                var actor = membershipAuthz.Inner.CurrentActor;
                var authority = await membershipAuthz
                    .ResolveActorMembershipAuthorityAsync(organizationId, ct)
                    .ConfigureAwait(false);
                if (actor.PlatformUserId is null
                    || authority.ActorMembershipRole != OrganizationRole.OrganizationOwner)
                {
                    return Results.Forbid();
                }

                var result = await useCase
                    .ExecuteAsync(
                        PlatformOrganizationId.From(organizationId),
                        actor.PlatformUserId,
                        ct)
                    .ConfigureAwait(false);
                return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
            });

        return app;
    }
}
