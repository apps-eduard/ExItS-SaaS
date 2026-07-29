namespace ExItS.Platform.Infrastructure.Persistence.Entitlements;

internal sealed class EntitlementSnapshotRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public Guid SubscriptionId { get; set; }
    public string PlanCode { get; set; } = string.Empty;
    public int PlanVersionNumber { get; set; }
    public int SnapshotVersion { get; set; }
    public int SchemaVersion { get; set; }
    public string SubscriptionStatus { get; set; } = string.Empty;
    public bool InGracePeriod { get; set; }
    public DateTimeOffset GeneratedAtUtc { get; set; }
    public DateTimeOffset EffectiveAtUtc { get; set; }
    public DateTimeOffset RefreshByUtc { get; set; }
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public int SourceAggregateVersion { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public List<EntitlementSnapshotGrantRecord> Grants { get; set; } = [];
}

internal sealed class EntitlementSnapshotGrantRecord
{
    public Guid SnapshotId { get; set; }
    public string FeatureCode { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public int? NumericLimit { get; set; }
    public string Source { get; set; } = string.Empty;
    public DateTimeOffset EffectiveAtUtc { get; set; }
    public DateTimeOffset? ExpiresAtUtc { get; set; }

    public EntitlementSnapshotRecord Snapshot { get; set; } = null!;
}
