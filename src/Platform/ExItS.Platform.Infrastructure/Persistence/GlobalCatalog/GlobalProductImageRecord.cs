namespace ExItS.Platform.Infrastructure.Persistence.GlobalCatalog;

internal sealed class GlobalProductImageRecord
{
    public Guid Id { get; set; }
    public Guid GlobalProductId { get; set; }
    public Guid StorageKey { get; set; }
    public int Version { get; set; }
    public int ThumbWidth { get; set; }
    public int ThumbHeight { get; set; }
    public int MediumWidth { get; set; }
    public int MediumHeight { get; set; }
    public string ContentType { get; set; } = "image/webp";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
