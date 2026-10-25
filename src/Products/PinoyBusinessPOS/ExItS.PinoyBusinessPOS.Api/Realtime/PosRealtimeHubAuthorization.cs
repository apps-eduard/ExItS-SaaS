using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Api.Realtime;

internal static class PosRealtimeHubAuthorization
{
    public static bool TryAuthorizeSellerOrganization(
        HttpContext? httpContext,
        IPosCommercialAccessAccessor access,
        Guid organizationId)
    {
        if (httpContext is null || organizationId == Guid.Empty)
        {
            return false;
        }

        if (!TryGetUserId(httpContext, out _))
        {
            return false;
        }

        if (!TryOrganizationScopeMatches(httpContext.Request, organizationId))
        {
            return false;
        }

        return PosCommercialScope.TryAuthorize(access, UtangCapability.ViewCustomerOrders, out _);
    }

    public static async Task<bool> TryAuthorizeOrderWatchAsync(
        HttpContext? httpContext,
        IPosCommercialAccessAccessor access,
        ICustomerOrderRepository orders,
        Guid organizationId,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        if (httpContext is null || organizationId == Guid.Empty || orderId == Guid.Empty)
        {
            return false;
        }

        if (!TryGetUserId(httpContext, out var userId))
        {
            return false;
        }

        if (TryOrganizationScopeMatches(httpContext.Request, organizationId)
            && PosCommercialScope.TryAuthorize(access, UtangCapability.ViewCustomerOrders, out _))
        {
            return true;
        }

        var order = await orders
            .GetByIdAsync(
                PosOrganizationId.From(organizationId),
                CustomerOrderId.From(orderId),
                cancellationToken)
            .ConfigureAwait(false);
        if (order is null)
        {
            return false;
        }

        if (order.CustomerParty.PartyType == CustomerPartyType.Personal
            && order.CustomerParty.PlatformUserId is Guid buyerUserId
            && buyerUserId == userId)
        {
            return true;
        }

        if (order.CustomerParty.PartyType == CustomerPartyType.Organization
            && order.CustomerParty.BuyerOrganizationId is Guid buyerOrgId
            && buyerOrgId != Guid.Empty
            && TryGetBearerOrganizationId(httpContext, out var sessionOrg)
            && sessionOrg == buyerOrgId)
        {
            return true;
        }

        return false;
    }

    private static bool TryOrganizationScopeMatches(HttpRequest request, Guid organizationId)
    {
        if (TryGetBearerOrganizationId(request.HttpContext, out var sessionOrg) && sessionOrg == organizationId)
        {
            return true;
        }

        return PosOrganizationScope.TryGetOrganizationId(request, out var headerOrg, out _)
               && headerOrg == organizationId;
    }

    private static bool TryGetUserId(HttpContext context, out Guid userId)
    {
        userId = default;
        var raw = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(raw, out userId) && userId != Guid.Empty)
        {
            return true;
        }

        if (context.Items.TryGetValue(PosAuthItems.UserId, out var value)
            && value is Guid id
            && id != Guid.Empty)
        {
            userId = id;
            return true;
        }

        return false;
    }

    private static bool TryGetBearerOrganizationId(HttpContext context, out Guid organizationId)
    {
        organizationId = default;
        if (context.Items.TryGetValue(PosAuthItems.OrganizationId, out var value)
            && value is Guid id
            && id != Guid.Empty)
        {
            organizationId = id;
            return true;
        }

        return false;
    }
}
