namespace ExItS.Platform.Infrastructure.Persistence.Catalog;

internal sealed class PlanVersionRecord
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public int VersionNumber { get; set; }
    public DateTimeOffset EffectiveFromUtc { get; set; }
    public DateTimeOffset? EffectiveToUtc { get; set; }
    public string BillingPeriod { get; set; } = string.Empty;
    public bool TrialEligible { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? PublishedAtUtc { get; set; }

    public PlanRecord Plan { get; set; } = null!;
    public ICollection<PlanVersionFeatureGrantRecord> FeatureGrants { get; set; } = [];
    public ICollection<PlanVersionBusinessTypeGrantRecord> BusinessTypeGrants { get; set; } = [];
}
