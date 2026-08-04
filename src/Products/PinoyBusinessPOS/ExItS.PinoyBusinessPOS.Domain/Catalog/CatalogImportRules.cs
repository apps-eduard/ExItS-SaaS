namespace ExItS.PinoyBusinessPOS.Domain.Catalog;

public static class CatalogImportRules
{
    public const int MaxItemsPerJob = 500;
    public const int ProcessingChunkSize = 50;
    public const int HeartbeatStaleSeconds = 120;
    public const int SnapshotVersion = 1;
    public const int IdempotencyKeyMaxLength = 128;
    public const int ErrorMessageMaxLength = 512;
    public const int CategoryNameMaxLength = ProductCategory.NameMaxLength;

    public static string? NormalizeOptionalIdempotencyKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var trimmed = key.Trim();
        return trimmed.Length <= IdempotencyKeyMaxLength
            ? trimmed
            : trimmed[..IdempotencyKeyMaxLength];
    }

    public static string? NormalizeOptionalError(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return null;
        }

        var trimmed = message.Trim();
        return trimmed.Length <= ErrorMessageMaxLength
            ? trimmed
            : trimmed[..ErrorMessageMaxLength];
    }
}
