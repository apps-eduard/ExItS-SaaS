using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Privacy;
using ExItS.PinoyBusinessPOS.Domain.Permissions;

namespace ExItS.PinoyBusinessPOS.Api.Privacy;

internal static class PrivacyReadinessEndpoints
{
    public static IEndpointRouteBuilder MapPrivacyReadinessEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/pos/privacy-readiness");

        group.MapGet("/", async (
            HttpRequest request,
            GetOrganizationPrivacyReadiness useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
            {
                return problem!;
            }

            // Align with Organization Web Settings visibility: Owner/Manager operational viewers.
            if (!PosCommercialScope.TryAuthorize(access, UtangCapability.ViewOperationalSetup, out problem))
            {
                return problem!;
            }

            var dto = await useCase.ExecuteAsync(organizationId, ct).ConfigureAwait(false);
            return Results.Ok(dto);
        });

        return app;
    }
}
