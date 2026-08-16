using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Organizations;

/// <summary>
/// Evidence metadata for secondary registration / eAccReg concepts (Permit to Use, CAS/CBA,
/// EIS certification / Permit to Transmit). Tracks reference types only — no transmission,
/// no secure file upload, no invented invoice numbering. Authority to Print may be recorded
/// as <see cref="ComplianceRegistrationTypes.Other"/> until a dedicated type is confirmed.
/// </summary>
public sealed class ComplianceRegistrationRecord
{
    public const int ReferenceNumberMaxLength = 128;
    public const int EvidenceReferenceMaxLength = 256;
    public const int DocumentTypeMaxLength = 64;
    public const int ReviewNotesMaxLength = 1000;

    public Guid Id { get; }
    public PlatformOrganizationId OrganizationId { get; }
    public OrganizationBranchId? OrganizationBranchId { get; private set; }
    public string RegistrationType { get; private set; }
    public string? ReferenceNumber { get; private set; }
    public string Status { get; private set; }
    public string? EvidenceReference { get; private set; }
    public string? DocumentType { get; private set; }
    public DateOnly? IssuedAt { get; private set; }
    public DateOnly? EffectiveAt { get; private set; }
    public DateOnly? ExpiresAt { get; private set; }
    public DateTimeOffset RecordedAtUtc { get; private set; }
    public string RecordedBy { get; private set; }
    public DateTimeOffset? ReviewedAtUtc { get; private set; }
    public string? ReviewedBy { get; private set; }
    public string? ReviewNotes { get; private set; }

    private ComplianceRegistrationRecord(
        Guid id,
        PlatformOrganizationId organizationId,
        OrganizationBranchId? organizationBranchId,
        string registrationType,
        string? referenceNumber,
        string status,
        string? evidenceReference,
        string? documentType,
        DateOnly? issuedAt,
        DateOnly? effectiveAt,
        DateOnly? expiresAt,
        DateTimeOffset recordedAtUtc,
        string recordedBy,
        DateTimeOffset? reviewedAtUtc,
        string? reviewedBy,
        string? reviewNotes)
    {
        Id = id;
        OrganizationId = organizationId;
        OrganizationBranchId = organizationBranchId;
        RegistrationType = registrationType;
        ReferenceNumber = referenceNumber;
        Status = status;
        EvidenceReference = evidenceReference;
        DocumentType = documentType;
        IssuedAt = issuedAt;
        EffectiveAt = effectiveAt;
        ExpiresAt = expiresAt;
        RecordedAtUtc = EnsureUtc(recordedAtUtc);
        RecordedBy = recordedBy;
        ReviewedAtUtc = reviewedAtUtc is null ? null : EnsureUtc(reviewedAtUtc.Value);
        ReviewedBy = reviewedBy;
        ReviewNotes = reviewNotes;
    }

    public static ComplianceRegistrationRecord Create(
        PlatformOrganizationId organizationId,
        string registrationType,
        string recordedBy,
        DateTimeOffset utcNow,
        OrganizationBranchId? organizationBranchId = null,
        string? referenceNumber = null,
        string status = ComplianceRegistrationStatuses.Provided,
        string? evidenceReference = null,
        string? documentType = null,
        DateOnly? issuedAt = null,
        DateOnly? effectiveAt = null,
        DateOnly? expiresAt = null)
    {
        if (!ComplianceRegistrationTypes.IsKnown(registrationType))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidComplianceRegistrationType,
                $"Unknown compliance registration type '{registrationType}'.");
        }

        if (!ComplianceRegistrationStatuses.IsOwnerMutable(status))
        {
            throw new DomainException(
                DomainErrorCodes.ComplianceSelfReviewUnauthorized,
                "Organization actors cannot set Platform readiness review statuses.");
        }

        RequireActor(recordedBy);

        return new(
            Guid.NewGuid(),
            organizationId,
            organizationBranchId,
            registrationType,
            NormalizeOptional(referenceNumber, ReferenceNumberMaxLength, DomainErrorCodes.InvalidComplianceRegistrationField),
            status,
            NormalizeOptional(evidenceReference, EvidenceReferenceMaxLength, DomainErrorCodes.InvalidComplianceRegistrationField),
            NormalizeOptional(documentType, DocumentTypeMaxLength, DomainErrorCodes.InvalidComplianceRegistrationField),
            issuedAt,
            effectiveAt,
            expiresAt,
            utcNow,
            recordedBy.Trim(),
            reviewedAtUtc: null,
            reviewedBy: null,
            reviewNotes: null);
    }

    public static ComplianceRegistrationRecord Rehydrate(
        Guid id,
        PlatformOrganizationId organizationId,
        OrganizationBranchId? organizationBranchId,
        string registrationType,
        string? referenceNumber,
        string status,
        string? evidenceReference,
        string? documentType,
        DateOnly? issuedAt,
        DateOnly? effectiveAt,
        DateOnly? expiresAt,
        DateTimeOffset recordedAtUtc,
        string recordedBy,
        DateTimeOffset? reviewedAtUtc,
        string? reviewedBy,
        string? reviewNotes) =>
        new(
            id,
            organizationId,
            organizationBranchId,
            registrationType,
            referenceNumber,
            status,
            evidenceReference,
            documentType,
            issuedAt,
            effectiveAt,
            expiresAt,
            recordedAtUtc,
            recordedBy,
            reviewedAtUtc,
            reviewedBy,
            reviewNotes);

    /// <summary>
    /// Owner/Manager update path. Cannot set AcceptedForReadiness / RejectedForReadiness or review fields.
    /// </summary>
    public void UpdateByOwner(
        string? registrationType,
        OrganizationBranchId? organizationBranchId,
        string? referenceNumber,
        string status,
        string? evidenceReference,
        string? documentType,
        DateOnly? issuedAt,
        DateOnly? effectiveAt,
        DateOnly? expiresAt,
        string actorReference,
        DateTimeOffset utcNow)
    {
        RequireActor(actorReference);

        if (!ComplianceRegistrationStatuses.IsOwnerMutable(status))
        {
            throw new DomainException(
                DomainErrorCodes.ComplianceSelfReviewUnauthorized,
                "Organization actors cannot accept or reject registration readiness.");
        }

        if (registrationType is not null)
        {
            if (!ComplianceRegistrationTypes.IsKnown(registrationType))
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidComplianceRegistrationType,
                    $"Unknown compliance registration type '{registrationType}'.");
            }

            RegistrationType = registrationType;
        }

        OrganizationBranchId = organizationBranchId;
        ReferenceNumber = NormalizeOptional(referenceNumber, ReferenceNumberMaxLength, DomainErrorCodes.InvalidComplianceRegistrationField);
        Status = status;
        EvidenceReference = NormalizeOptional(evidenceReference, EvidenceReferenceMaxLength, DomainErrorCodes.InvalidComplianceRegistrationField);
        DocumentType = NormalizeOptional(documentType, DocumentTypeMaxLength, DomainErrorCodes.InvalidComplianceRegistrationField);
        IssuedAt = issuedAt;
        EffectiveAt = effectiveAt;
        ExpiresAt = expiresAt;
        RecordedAtUtc = EnsureUtc(utcNow);
        RecordedBy = actorReference.Trim();
        // Clear Platform review markers when Owner mutates content.
        ReviewedAtUtc = null;
        ReviewedBy = null;
        ReviewNotes = null;
    }

    public void AcceptForReadiness(string reviewer, string? reviewNotes, DateTimeOffset utcNow)
    {
        RequireActor(reviewer);
        Status = ComplianceRegistrationStatuses.AcceptedForReadiness;
        ReviewedAtUtc = EnsureUtc(utcNow);
        ReviewedBy = reviewer.Trim();
        ReviewNotes = NormalizeOptional(reviewNotes, ReviewNotesMaxLength, DomainErrorCodes.InvalidComplianceRegistrationField);
    }

    public void RejectForReadiness(string reviewer, string? reviewNotes, DateTimeOffset utcNow)
    {
        RequireActor(reviewer);
        Status = ComplianceRegistrationStatuses.RejectedForReadiness;
        ReviewedAtUtc = EnsureUtc(utcNow);
        ReviewedBy = reviewer.Trim();
        ReviewNotes = NormalizeOptional(reviewNotes, ReviewNotesMaxLength, DomainErrorCodes.InvalidComplianceRegistrationField);
    }

    private static string? NormalizeOptional(string? value, int maxLength, string errorCode)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainException(errorCode, $"Value must be at most {maxLength} characters.");
        }

        return trimmed;
    }

    private static void RequireActor(string actor)
    {
        if (string.IsNullOrWhiteSpace(actor))
        {
            throw new ArgumentException("Actor reference is required.", nameof(actor));
        }
    }

    private static DateTimeOffset EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Timestamp must be UTC.", nameof(value));
        }

        return value;
    }
}
