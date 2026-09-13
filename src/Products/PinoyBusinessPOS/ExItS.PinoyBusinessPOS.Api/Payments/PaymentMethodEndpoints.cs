using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Payments;

namespace ExItS.PinoyBusinessPOS.Api.Payments;

internal static class PaymentMethodEndpoints
{
    public static IEndpointRouteBuilder MapPaymentMethodEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/pos/payment-methods");

        group.MapGet("/", async (
            HttpRequest request,
            ListOrganizationPaymentMethods query,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewOperationalSetup, out var organizationId, out var problem))
            {
                return problem!;
            }

            var items = await query.ExecuteAsync(organizationId, ct).ConfigureAwait(false);
            return Results.Ok(items);
        });

        group.MapGet("/checkout", async (
            HttpRequest request,
            ResolveCheckoutPaymentMethods query,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.CreateSale, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosOrganizationScope.TryGetBranchId(request, out var branchId, out problem))
            {
                return problem!;
            }

            var items = await query.ExecuteAsync(organizationId, branchId, ct).ConfigureAwait(false);
            return Results.Ok(new { methods = items });
        });

        group.MapPut("/{methodCode}", async (
            HttpRequest request,
            string methodCode,
            UpsertPaymentMethodSettingRequest body,
            UpsertOrganizationPaymentMethodSetting useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageOperationalSetup, out var organizationId, out var problem))
            {
                return problem!;
            }

            var requestBody = body with { MethodCode = methodCode };
            var result = await useCase.ExecuteAsync(organizationId, requestBody, ct).ConfigureAwait(false);
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
