using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Branches;
using ExItS.PinoyBusinessPOS.Application.Commercial;

namespace ExItS.PinoyBusinessPOS.Api.Branches;

internal static class OperationalBranchEndpoints
{
    public static IEndpointRouteBuilder MapOperationalBranchEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/v1/pos/operational-branch", async (
            HttpRequest request,
            SelectOperationalBranchRequest body,
            SelectOperationalBranch useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            // Not CreateSale/EnterPos: owners may switch management branch without selling rights.
            if (!TryAuthorize(request, access, UtangCapability.ViewCatalog, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            PosOrganizationScope.TryGetOptionalBranchId(request, out var currentSelectedBranchId);

            var result = await useCase.ExecuteAsync(
                    organizationId,
                    actorId,
                    body.BranchId,
                    currentSelectedBranchId,
                    deviceBoundBranchId: null,
                    ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(result, Results.Ok);
        });

        return app;
    }

    private static bool TryAuthorize(
        HttpRequest request,
        IPosCommercialAccessAccessor access,
        UtangCapability capability,
        out Guid organizationId,
        out IResult? problem)
    {
        if (!PosOrganizationScope.TryGetOrganizationId(request, out organizationId, out problem))
        {
            return false;
        }

        return PosCommercialScope.TryAuthorize(access, capability, out problem);
    }
}
