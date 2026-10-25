namespace ExItS.PinoyBusinessPOS.Application.Realtime;

/// <summary>Server-controlled POS SignalR group names. Clients never invent group strings.</summary>
public static class PosRealtimeGroupNames
{
    public static string Organization(Guid organizationId) => $"pos-rt:organization:{organizationId:D}";

    public static string Order(Guid organizationId, Guid orderId) =>
        $"pos-rt:order:{organizationId:D}:{orderId:D}";

    public static string PersonalBuyer(Guid userId) => $"pos-rt:personal-buyer:{userId:D}";
}
