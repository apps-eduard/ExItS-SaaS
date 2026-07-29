namespace ExItS.Platform.Infrastructure.Persistence.Catalog;

internal sealed class TrialDefinitionRecord
{
    public Guid Id { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public Guid? PlanId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public long DurationTicks { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }

    public ICollection<TrialDefinitionFeatureGrantRecord> FeatureGrants { get; set; } = [];
}
