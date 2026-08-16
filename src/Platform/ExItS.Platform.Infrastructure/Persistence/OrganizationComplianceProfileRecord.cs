namespace ExItS.Platform.Infrastructure.Persistence;

internal sealed class OrganizationComplianceProfileRecord
{
    public Guid OrganizationId { get; set; }
    public string? RegisteredTaxpayerName { get; set; }
    public string? TinNormalized { get; set; }
    public string SetupStatus { get; set; } = "NotConfigured";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string? UpdatedByActorReference { get; set; }
}

internal sealed class BranchComplianceProfileRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid OrganizationBranchId { get; set; }
    public string? BirBranchCode { get; set; }
    public string SetupStatus { get; set; } = "NotConfigured";
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string? UpdatedByActorReference { get; set; }
}

internal sealed class ComplianceRegistrationRecordEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? OrganizationBranchId { get; set; }
    public string RegistrationType { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? EvidenceReference { get; set; }
    public string? DocumentType { get; set; }
    public DateOnly? IssuedAt { get; set; }
    public DateOnly? EffectiveAt { get; set; }
    public DateOnly? ExpiresAt { get; set; }
    public DateTimeOffset RecordedAtUtc { get; set; }
    public string RecordedBy { get; set; } = string.Empty;
    public DateTimeOffset? ReviewedAtUtc { get; set; }
    public string? ReviewedBy { get; set; }
    public string? ReviewNotes { get; set; }
}
