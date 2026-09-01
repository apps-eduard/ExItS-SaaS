using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Branches;
using ExItS.PinoyBusinessPOS.Application.Commercial;

namespace ExItS.PinoyBusinessPOS.Api.Branches;

internal static class BranchReadinessEndpoints
{
    public static IEndpointRouteBuilder MapBranchReadinessEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/pos/branches/{branchId:guid}/readiness", async (
            HttpRequest request,
            Guid branchId,
            BranchReadinessQueryService query,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosCommercialScope.TryAuthorize(access, UtangCapability.ViewCatalog, out problem))
            {
                return problem!;
            }

            var result = await query.GetAsync(organizationId, branchId, ct).ConfigureAwait(false);
            return PosApiResults.FromResult(result, Results.Ok);
        });

        app.MapPut("/api/v1/pos/branches/{branchId:guid}/setup-progress", async (
            HttpRequest request,
            Guid branchId,
            UpsertBranchSetupProgressRequest body,
            BranchSetupProgressService service,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosCommercialScope.TryAuthorize(access, UtangCapability.ViewCatalog, out problem))
            {
                return problem!;
            }

            var result = await service.UpsertAsync(organizationId, branchId, body, ct).ConfigureAwait(false);
            return PosApiResults.FromResult(result, Results.Ok);
        });

        return app;
    }
}
