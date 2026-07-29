namespace ExItS.Platform.Infrastructure.Persistence.Catalog;

internal sealed class FeatureDefinitionRecord
{
    public string ProductCode { get; set; } = string.Empty;
    public string FeatureCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ValueType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
