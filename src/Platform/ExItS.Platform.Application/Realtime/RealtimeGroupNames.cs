namespace ExItS.Platform.Application.Realtime;

/// <summary>Server-controlled SignalR group names. Clients never invent group strings.</summary>
public static class RealtimeGroupNames
{
    public static string PersonalUser(Guid userId) => $"rt:personal-user:{userId:D}";

    public static string Organization(Guid organizationId) => $"rt:organization:{organizationId:D}";

    public static string Branch(Guid organizationId, Guid branchId) =>
        $"rt:branch:{organizationId:D}:{branchId:D}";

    public static string StorefrontSeller(Guid organizationId) =>
        $"rt:storefront-seller:{organizationId:D}";

    /// <summary>Legacy PersonalProfileHub group — kept for dual-publish during migration.</summary>
    public static string LegacyPersonalProfile(Guid userId) => $"personal-profile:{userId:D}";

    /// <summary>Legacy CustomerStorefrontHub group — kept for dual-publish during migration.</summary>
    public static string LegacyStorefrontSeller(Guid organizationId) =>
        $"customer-storefront:seller:{organizationId:D}";
}
