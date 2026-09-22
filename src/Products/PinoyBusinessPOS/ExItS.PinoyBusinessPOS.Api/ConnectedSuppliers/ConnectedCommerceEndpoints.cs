using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

namespace ExItS.PinoyBusinessPOS.Api.ConnectedSuppliers;

internal static class ConnectedCommerceEndpoints
{
    public static IEndpointRouteBuilder MapConnectedCommerceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/pos/connected-commerce");

        group.MapGet("/settings", async (
            HttpRequest req,
            GetOrganizationConnectedCommerceSettings use,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!Authorize(req, access, UtangCapability.ViewSuppliers, out var org, out var problem))
            {
                return problem!;
            }

            return PosApiResults.FromResult(await use.ExecuteAsync(org, ct), Results.Ok);
        });

        group.MapPut("/settings", async (
            HttpRequest req,
            UpdateOrganizationConnectedCommerceSettingsRequest body,
            UpdateOrganizationConnectedCommerceSettings use,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!Authorize(req, access, UtangCapability.ManageSuppliers, out var org, out var problem))
            {
                return problem!;
            }

            return PosApiResults.FromResult(await use.ExecuteAsync(org, body, ct), Results.Ok);
        });

        group.MapGet("/overview", async (
            HttpRequest req,
            GetConnectedCommerceOverview use,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!Authorize(req, access, UtangCapability.ViewSuppliers, out var org, out var problem))
            {
                return problem!;
            }

            return PosApiResults.FromResult(await use.ExecuteAsync(org, ct), Results.Ok);
        });

        group.MapGet("/business-customers/{connectionId:guid}/payment-timing", async (
            HttpRequest req,
            Guid connectionId,
            GetBusinessCustomerPaymentTimingOverride use,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!Authorize(req, access, UtangCapability.ViewSuppliers, out var org, out var problem))
            {
                return problem!;
            }

            return PosApiResults.FromResult(await use.ExecuteAsync(org, connectionId, ct), Results.Ok);
        });

        group.MapPut("/business-customers/{connectionId:guid}/payment-timing", async (
            HttpRequest req,
            Guid connectionId,
            UpdateBusinessCustomerPaymentTimingOverrideRequest body,
            UpdateBusinessCustomerPaymentTimingOverride use,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!Authorize(req, access, UtangCapability.ManageSuppliers, out var org, out var problem))
            {
                return problem!;
            }

            return PosApiResults.FromResult(await use.ExecuteAsync(org, connectionId, body, ct), Results.Ok);
        });

        group.MapGet("/business-customers/{connectionId:guid}/pricing", async (
            HttpRequest req,
            Guid connectionId,
            GetBusinessCustomerPricingOverrides use,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!Authorize(req, access, UtangCapability.ViewSuppliers, out var org, out var problem))
            {
                return problem!;
            }

            return PosApiResults.FromResult(await use.ExecuteAsync(org, connectionId, ct), Results.Ok);
        });

        group.MapPut("/business-customers/{connectionId:guid}/pricing", async (
            HttpRequest req,
            Guid connectionId,
            UpdateBusinessCustomerPricingOverridesRequest body,
            UpdateBusinessCustomerPricingOverrides use,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!Authorize(req, access, UtangCapability.ManageSuppliers, out var org, out var problem))
            {
                return problem!;
            }

            return PosApiResults.FromResult(await use.ExecuteAsync(org, connectionId, body, ct), Results.Ok);
        });

        return app;
    }

    private static bool Authorize(
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
