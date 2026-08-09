using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.PrivacyCompliance;

/// <summary>Reference-only evidence link. Not automatic legal proof.</summary>
public sealed class ComplianceEvidenceReference
{
    public Guid Id { get; }
    public Guid RequirementId { get; }
    public ComplianceEvidenceKind Kind { get; }
    public string Label { get; }
    public string ReferencePath { get; }
    public string? Notes { get; }
    public DateTimeOffset CreatedAtUtc { get; }

    private ComplianceEvidenceReference(
        Guid id,
        Guid requirementId,
        ComplianceEvidenceKind kind,
        string label,
        string referencePath,
        string? notes,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        RequirementId = requirementId;
        Kind = kind;
        Label = label;
        ReferencePath = referencePath;
        Notes = notes;
        CreatedAtUtc = createdAtUtc;
    }

    public static ComplianceEvidenceReference Create(
        Guid requirementId,
        ComplianceEvidenceKind kind,
        string label,
        string referencePath,
        DateTimeOffset utcNow,
        string? notes = null,
        Guid? id = null)
    {
        DomainTime.EnsureUtc(utcNow);
        if (requirementId == Guid.Empty)
        {
            throw new DomainException(DomainErrorCodes.InvalidComplianceEvidence, "Requirement id is required.");
        }

        label = Require(label, 200);
        referencePath = Require(referencePath, 500);
        notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        if (notes is { Length: > 1000 })
        {
            throw new DomainException(DomainErrorCodes.InvalidComplianceEvidence, "Notes are too long.");
        }

        return new ComplianceEvidenceReference(
            id ?? Guid.NewGuid(),
            requirementId,
            kind,
            label,
            referencePath,
            notes,
            utcNow);
    }

    public static ComplianceEvidenceReference Rehydrate(
        Guid id,
        Guid requirementId,
        ComplianceEvidenceKind kind,
        string label,
        string referencePath,
        string? notes,
        DateTimeOffset createdAtUtc) =>
        new(id, requirementId, kind, label, referencePath, notes, createdAtUtc);

    private static string Require(string value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(DomainErrorCodes.InvalidComplianceEvidence, "Evidence fields are required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > max)
        {
            throw new DomainException(DomainErrorCodes.InvalidComplianceEvidence, "Evidence field is too long.");
        }

        return trimmed;
    }
}
