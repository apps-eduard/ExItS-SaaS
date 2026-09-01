using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Branches;
using ExItS.PinoyBusinessPOS.Application.Commercial;

namespace ExItS.PinoyBusinessPOS.Api.Parties;

internal static class PartyBranchAccessEndpoints
{
    public static IEndpointRouteBuilder MapPartyBranchAccessEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/pos/parties/customers/{customerId:guid}/branch-access", async (
            HttpRequest request,
            Guid customerId,
            GrantPartyBranchAccessRequest body,
            PartyBranchExplicitAssignService service,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosCommercialScope.TryAuthorize(access, UtangCapability.ViewCustomersAndHistory, out problem))
            {
                return problem!;
            }

            if (!PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            var result = await service
                .GrantCustomerAsync(organizationId, customerId, body, actorId, ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(result, () => Results.NoContent());
        });

        app.MapDelete("/api/v1/pos/parties/customers/{customerId:guid}/branch-access", async (
            HttpRequest request,
            Guid customerId,
            [Microsoft.AspNetCore.Mvc.FromBody] GrantPartyBranchAccessRequest body,
            PartyBranchExplicitAssignService service,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosCommercialScope.TryAuthorize(access, UtangCapability.ViewCustomersAndHistory, out problem))
            {
                return problem!;
            }

            var result = await service
                .RevokeCustomerAsync(organizationId, customerId, body, ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(result, () => Results.NoContent());
        });

        app.MapPost("/api/v1/pos/parties/suppliers/{supplierId:guid}/branch-access", async (
            HttpRequest request,
            Guid supplierId,
            GrantPartyBranchAccessRequest body,
            PartyBranchExplicitAssignService service,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosCommercialScope.TryAuthorize(access, UtangCapability.ManageSuppliers, out problem))
            {
                return problem!;
            }

            if (!PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            var result = await service
                .GrantSupplierAsync(organizationId, supplierId, body, actorId, ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(result, () => Results.NoContent());
        });

        app.MapDelete("/api/v1/pos/parties/suppliers/{supplierId:guid}/branch-access", async (
            HttpRequest request,
            Guid supplierId,
            [Microsoft.AspNetCore.Mvc.FromBody] GrantPartyBranchAccessRequest body,
            PartyBranchExplicitAssignService service,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosCommercialScope.TryAuthorize(access, UtangCapability.ManageSuppliers, out problem))
            {
                return problem!;
            }

            var result = await service
                .RevokeSupplierAsync(organizationId, supplierId, body, ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(result, () => Results.NoContent());
        });

        return app;
    }
}
