using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Api.Organizations;

internal static class SalesDocumentCapabilityEndpoints
{
    public static IEndpointRouteBuilder MapSalesDocumentCapabilityEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/api/v1/platform/organizations/{organizationId:guid}/sales-document-capability",
            async (
                Guid organizationId,
                GetOrganizationSalesDocumentCapability useCase,
                PlatformMembershipAuthz membershipAuthz,
                CancellationToken ct) =>
            {
                var denied = await membershipAuthz.EnsureActiveOrganizationMemberAsync(
                    PlatformAuditActions.PlatformAccessChecked,
                    nameof(OrganizationSalesDocumentCapability),
                    organizationId.ToString("D"),
                    organizationId,
                    summary: "Read organization sales-document capability.",
                    cancellationToken: ct).ConfigureAwait(false);
                if (denied is not null)
                {
                    return denied;
                }

                var result = await useCase
                    .ExecuteAsync(PlatformOrganizationId.From(organizationId), ct)
                    .ConfigureAwait(false);
                return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
            });

        return app;
    }
}
