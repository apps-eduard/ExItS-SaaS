namespace ExItS.Platform.Infrastructure.Persistence.Entitlements;

internal sealed class FeatureOverrideRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string FeatureCode { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public int? NumericLimit { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset EffectiveFromUtc { get; set; }
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public Guid? RevokedByUserId { get; set; }
    public string? RevocationReason { get; set; }
    public uint Xmin { get; set; }
}
