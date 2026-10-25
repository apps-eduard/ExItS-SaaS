using ExItS.PinoyBusinessPOS.Application.Realtime;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;

namespace ExItS.PinoyBusinessPOS.Application.CustomerOrdering;

internal static class PosRealtimeCustomerOrderPublish
{
    public static Task PublishAsync(
        IPosRealtimeChangePublisher publisher,
        CustomerOrder order,
        string eventType,
        CancellationToken cancellationToken = default)
    {
        var sellerOrgId = order.SellerOrganizationId.Value;
        var orderId = order.Id.Value;

        var groups = new List<string>
        {
            PosRealtimeGroupNames.Organization(sellerOrgId),
            PosRealtimeGroupNames.Order(sellerOrgId, orderId),
        };

        if (order.CustomerParty.PartyType == CustomerPartyType.Personal
            && order.CustomerParty.PlatformUserId is Guid personalUserId
            && personalUserId != Guid.Empty)
        {
            groups.Add(PosRealtimeGroupNames.PersonalBuyer(personalUserId));
        }

        var data = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["sellerOrganizationId"] = sellerOrgId.ToString("D"),
            ["orderId"] = orderId.ToString("D"),
            ["orderNumber"] = order.OrderNumber,
            ["status"] = order.Status.ToString(),
            ["paymentStatus"] = order.PaymentStatus.ToString(),
            ["fulfillmentType"] = order.FulfillmentType.ToString(),
        };

        var envelope = PosRealtimeEventEnvelope.Create(
            eventType,
            PosRealtimeResourceTypes.CustomerOrder,
            orderId.ToString("D"),
            PosRealtimeScopeTypes.Organization,
            sellerOrgId.ToString("D"),
            data: data);

        return publisher.PublishToGroupsAsync(groups, envelope, cancellationToken);
    }
}
