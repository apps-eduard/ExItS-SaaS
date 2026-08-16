using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Organizations;

/// <summary>
/// Branch-scoped BIR branch registration profile. <see cref="BirBranchCode"/> is distinct from TIN.
/// Typically 5 digits for head-office style codes (e.g. <c>00000</c>); digits only, length 1–10 when set.
/// No checksum is invented. Machine/MIN association is a FUTURE residual.
/// </summary>
public sealed class BranchComplianceProfile
{
    public const int BirBranchCodeMaxLength = 10;
    public const int NotesMaxLength = 1000;

    public Guid Id { get; }
    public PlatformOrganizationId OrganizationId { get; }
    public OrganizationBranchId OrganizationBranchId { get; }
    public string? BirBranchCode { get; private set; }
    public string SetupStatus { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public string? UpdatedByActorReference { get; private set; }

    private BranchComplianceProfile(
        Guid id,
        PlatformOrganizationId organizationId,
        OrganizationBranchId organizationBranchId,
        string? birBranchCode,
        string setupStatus,
        string? notes,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        string? updatedByActorReference)
    {
        Id = id;
        OrganizationId = organizationId;
        OrganizationBranchId = organizationBranchId;
        BirBranchCode = birBranchCode;
        SetupStatus = setupStatus;
        Notes = notes;
        CreatedAtUtc = EnsureUtc(createdAtUtc);
        UpdatedAtUtc = EnsureUtc(updatedAtUtc);
        UpdatedByActorReference = updatedByActorReference;
    }

    public static BranchComplianceProfile Create(
        PlatformOrganizationId organizationId,
        OrganizationBranchId organizationBranchId,
        DateTimeOffset utcNow,
        string? actorReference = null) =>
        new(
            Guid.NewGuid(),
            organizationId,
            organizationBranchId,
            birBranchCode: null,
            ComplianceSetupStatuses.NotConfigured,
            notes: null,
            utcNow,
            utcNow,
            actorReference);

    public static BranchComplianceProfile Rehydrate(
        Guid id,
        PlatformOrganizationId organizationId,
        OrganizationBranchId organizationBranchId,
        string? birBranchCode,
        string setupStatus,
        string? notes,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        string? updatedByActorReference) =>
        new(
            id,
            organizationId,
            organizationBranchId,
            birBranchCode,
            setupStatus,
            notes,
            createdAtUtc,
            updatedAtUtc,
            updatedByActorReference);

    public void Update(
        string? birBranchCode,
        string? setupStatus,
        string? notes,
        string actorReference,
        DateTimeOffset utcNow)
    {
        if (string.IsNullOrWhiteSpace(actorReference))
        {
            throw new ArgumentException("Actor reference is required.", nameof(actorReference));
        }

        BirBranchCode = NormalizeBranchCode(birBranchCode);
        if (setupStatus is not null)
        {
            if (!ComplianceSetupStatuses.IsKnownBranchStatus(setupStatus))
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidComplianceSetupStatus,
                    $"Unknown branch compliance setup status '{setupStatus}'.");
            }

            SetupStatus = setupStatus;
        }
        else if (SetupStatus is ComplianceSetupStatuses.NotConfigured && BirBranchCode is not null)
        {
            SetupStatus = ComplianceSetupStatuses.SetupInProgress;
        }

        Notes = NormalizeNotes(notes);
        UpdatedAtUtc = EnsureUtc(utcNow);
        UpdatedByActorReference = actorReference.Trim();
    }

    private static string? NormalizeBranchCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
        {
            return null;
        }

        if (digits.Length > BirBranchCodeMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidBirBranchCode,
                $"BIR branch code must be at most {BirBranchCodeMaxLength} digits when set.");
        }

        return digits;
    }

    private static string? NormalizeNotes(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > NotesMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidBranchComplianceNotes,
                $"Branch compliance notes must be at most {NotesMaxLength} characters.");
        }

        return trimmed;
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
