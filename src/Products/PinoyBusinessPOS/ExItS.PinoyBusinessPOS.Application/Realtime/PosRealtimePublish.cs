namespace ExItS.PinoyBusinessPOS.Application.Realtime;

public static class PosRealtimePublish
{
    public static Task OrganizationSettingsChangedAsync(
        IPosRealtimeChangePublisher publisher,
        Guid organizationId,
        string eventType,
        CancellationToken cancellationToken = default)
    {
        var envelope = PosRealtimeEventEnvelope.Create(
            eventType,
            PosRealtimeResourceTypes.OrganizationSettings,
            organizationId.ToString("D"),
            PosRealtimeScopeTypes.Organization,
            organizationId.ToString("D"),
            data: new Dictionary<string, string>
            {
                ["organizationId"] = organizationId.ToString("D")
            });
        return publisher.PublishToGroupsAsync(
            [PosRealtimeGroupNames.Organization(organizationId)],
            envelope,
            cancellationToken);
    }

    public static Task InventoryOrPricingChangedAsync(
        IPosRealtimeChangePublisher publisher,
        Guid organizationId,
        string eventType,
        Guid? branchId = null,
        Guid? productId = null,
        string? kind = null,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, string>
        {
            ["organizationId"] = organizationId.ToString("D")
        };
        if (branchId is Guid b)
        {
            data["branchId"] = b.ToString("D");
        }

        if (productId is Guid p)
        {
            data["productId"] = p.ToString("D");
        }

        if (!string.IsNullOrWhiteSpace(kind))
        {
            data["kind"] = kind;
        }

        var envelope = PosRealtimeEventEnvelope.Create(
            eventType,
            eventType == PosRealtimeEventTypes.CatalogPricingChanged
                ? PosRealtimeResourceTypes.Catalog
                : PosRealtimeResourceTypes.Inventory,
            productId?.ToString("D") ?? organizationId.ToString("D"),
            PosRealtimeScopeTypes.Organization,
            organizationId.ToString("D"),
            data: data);
        return publisher.PublishToGroupsAsync(
            [PosRealtimeGroupNames.Organization(organizationId)],
            envelope,
            cancellationToken);
    }

    /// <summary>Post-commit stock notification. Swallows transport failures so commits stay durable.</summary>
    public static async Task TryInventoryStockChangedAsync(
        IPosRealtimeChangePublisher publisher,
        Guid organizationId,
        string kind,
        Guid? branchId = null,
        Guid? productId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await InventoryOrPricingChangedAsync(
                    publisher,
                    organizationId,
                    PosRealtimeEventTypes.InventoryStockChanged,
                    branchId,
                    productId,
                    kind,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            /* SignalR is notification-only; never fail the mutation path. */
        }
    }
}
