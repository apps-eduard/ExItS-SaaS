namespace ExItS.PinoyBusinessPOS.Application.Offline;

/// <summary>Deterministic associated-data binding for AES-GCM offline payloads.</summary>
public static class OfflinePayloadBinding
{
    public static string BuildAssociatedData(string contextHash, Guid operationId, string operationType) =>
        $"{contextHash}|{operationId:D}|{operationType}";
}
