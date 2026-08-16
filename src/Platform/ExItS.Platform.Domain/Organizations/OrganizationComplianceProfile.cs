using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Organizations;

/// <summary>
/// Organization-scoped compliance profile. Holds confirmed BIR registration readiness fields
/// (registered taxpayer name, TIN digits, setup status). Does not invent invoice layout or numbering.
/// Machine Identification Number (MIN) association is a FUTURE residual and is not modeled here.
/// </summary>
public sealed class OrganizationComplianceProfile
{
    public const int RegisteredTaxpayerNameMaxLength = 200;

    public PlatformOrganizationId OrganizationId { get; }
    public string? RegisteredTaxpayerName { get; private set; }
    /// <summary>Digits-only TIN (exactly 9 when set). Never expose on public contracts.</summary>
    public string? TinNormalized { get; private set; }
    public string SetupStatus { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public string? UpdatedByActorReference { get; private set; }

    private OrganizationComplianceProfile(
        PlatformOrganizationId organizationId,
        string? registeredTaxpayerName,
        string? tinNormalized,
        string setupStatus,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        string? updatedByActorReference)
    {
        OrganizationId = organizationId;
        RegisteredTaxpayerName = registeredTaxpayerName;
        TinNormalized = tinNormalized;
        SetupStatus = setupStatus;
        CreatedAtUtc = EnsureUtc(createdAtUtc);
        UpdatedAtUtc = EnsureUtc(updatedAtUtc);
        UpdatedByActorReference = updatedByActorReference;
    }

    public static OrganizationComplianceProfile Create(
        PlatformOrganizationId organizationId,
        DateTimeOffset utcNow,
        string? actorReference = null) =>
        new(
            organizationId,
            registeredTaxpayerName: null,
            tinNormalized: null,
            ComplianceSetupStatuses.NotConfigured,
            utcNow,
            utcNow,
            actorReference);

    public static OrganizationComplianceProfile Rehydrate(
        PlatformOrganizationId organizationId,
        string? registeredTaxpayerName,
        string? tinNormalized,
        string setupStatus,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        string? updatedByActorReference) =>
        new(
            organizationId,
            registeredTaxpayerName,
            tinNormalized,
            setupStatus,
            createdAtUtc,
            updatedAtUtc,
            updatedByActorReference);

    public void Touch(string actorReference, DateTimeOffset utcNow)
    {
        if (string.IsNullOrWhiteSpace(actorReference))
        {
            throw new ArgumentException("Actor reference is required.", nameof(actorReference));
        }

        UpdatedAtUtc = EnsureUtc(utcNow);
        UpdatedByActorReference = actorReference.Trim();
    }

    public void UpdateRegisteredTaxpayerInfo(
        string? registeredTaxpayerName,
        string? tin,
        string actorReference,
        DateTimeOffset utcNow)
    {
        RequireActor(actorReference);
        var name = NormalizeOptionalName(registeredTaxpayerName);
        var tinNormalized = TinMask.NormalizeOrThrow(tin);

        RegisteredTaxpayerName = name;
        TinNormalized = tinNormalized;
        Touch(actorReference, utcNow);

        if (SetupStatus is ComplianceSetupStatuses.NotConfigured
            && (name is not null || tinNormalized is not null))
        {
            SetupStatus = ComplianceSetupStatuses.SetupInProgress;
        }
    }

    public void SetSetupStatus(string setupStatus, string actorReference, DateTimeOffset utcNow)
    {
        RequireActor(actorReference);
        if (!ComplianceSetupStatuses.IsKnownOrganizationStatus(setupStatus))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidComplianceSetupStatus,
                $"Unknown compliance setup status '{setupStatus}'.");
        }

        SetupStatus = setupStatus;
        Touch(actorReference, utcNow);
    }

    public string? MaskedTin => TinMask.Mask(TinNormalized);

    private static string? NormalizeOptionalName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > RegisteredTaxpayerNameMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidRegisteredTaxpayerName,
                $"Registered taxpayer name must be at most {RegisteredTaxpayerNameMaxLength} characters.");
        }

        return trimmed;
    }

    private static void RequireActor(string actorReference)
    {
        if (string.IsNullOrWhiteSpace(actorReference))
        {
            throw new ArgumentException("Actor reference is required.", nameof(actorReference));
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
