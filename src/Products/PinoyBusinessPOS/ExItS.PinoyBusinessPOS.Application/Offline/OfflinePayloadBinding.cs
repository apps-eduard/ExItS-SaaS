namespace ExItS.PinoyBusinessPOS.Application.Offline;

/// <summary>Deterministic associated-data binding for AES-GCM offline payloads.</summary>
public static class OfflinePayloadBinding
{
    public static string BuildAssociatedData(string contextHash, Guid operationId, string operationType) =>
        $"{contextHash}|{operationId:D}|{operationType}";

    public static string BuildCustomerAssociatedData(string contextHash, Guid customerId) =>
        $"local-customer|{contextHash}|{customerId:D}";

    public static string BuildCreditAssociatedData(string contextHash, Guid creditEntryId) =>
        $"local-credit|{contextHash}|{creditEntryId:D}";

    public static string BuildBalanceAssociatedData(string contextHash, Guid customerId) =>
        $"local-balance|{contextHash}|{customerId:D}";
}
