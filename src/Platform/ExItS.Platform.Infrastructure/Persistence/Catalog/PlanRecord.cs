namespace ExItS.Platform.Infrastructure.Persistence.Catalog;

internal sealed class PlanRecord
{
    public Guid Id { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
