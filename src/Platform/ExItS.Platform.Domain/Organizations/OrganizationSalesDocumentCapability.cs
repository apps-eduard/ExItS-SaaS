namespace ExItS.Platform.Domain.Organizations;

/// <summary>
/// Platform-controlled, organization-scoped sales-document compliance authority.
/// Holds compliance eligibility review state, TaxDocument issuance capability, and
/// Tax Configuration enablement (separate from issuance and from POS tax values).
/// Tax calculation values do not grant any of these authorities.
/// Enablement is product authorization — not BIR, NPC, or other regulatory certification.
/// </summary>
public sealed class OrganizationSalesDocumentCapability
{
    public PlatformOrganizationId OrganizationId { get; }
    public string ComplianceEligibilityStatus { get; private set; }
    public bool TaxDocumentIssuanceEnabled { get; private set; }
    public bool TaxConfigurationEnabled { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public string? UpdatedByActorReference { get; private set; }

    private OrganizationSalesDocumentCapability(
        PlatformOrganizationId organizationId,
        string complianceEligibilityStatus,
        bool taxDocumentIssuanceEnabled,
        bool taxConfigurationEnabled,
        DateTimeOffset updatedAtUtc,
        string? updatedByActorReference)
    {
        OrganizationId = organizationId;
        ComplianceEligibilityStatus = NormalizeStatus(complianceEligibilityStatus);
        TaxDocumentIssuanceEnabled = taxDocumentIssuanceEnabled;
        TaxConfigurationEnabled = taxConfigurationEnabled;
        UpdatedAtUtc = EnsureUtc(updatedAtUtc);
        UpdatedByActorReference = updatedByActorReference;
    }

    public static OrganizationSalesDocumentCapability CreateDefault(
        PlatformOrganizationId organizationId,
        DateTimeOffset utcNow) =>
        new(
            organizationId,
            OrganizationComplianceEligibilityStatuses.NotRequested,
            taxDocumentIssuanceEnabled: false,
            taxConfigurationEnabled: false,
            EnsureUtc(utcNow),
            null);

    public static OrganizationSalesDocumentCapability Rehydrate(
        PlatformOrganizationId organizationId,
        string complianceEligibilityStatus,
        bool taxDocumentIssuanceEnabled,
        bool taxConfigurationEnabled,
        DateTimeOffset updatedAtUtc,
        string? updatedByActorReference) =>
        new(
            organizationId,
            complianceEligibilityStatus,
            taxDocumentIssuanceEnabled,
            taxConfigurationEnabled,
            updatedAtUtc,
            updatedByActorReference);

    public bool TransitionEligibility(
        string targetStatus,
        string actorReference,
        DateTimeOffset utcNow)
    {
        var target = NormalizeStatus(targetStatus);
        if (ComplianceEligibilityStatus == target)
        {
            Touch(actorReference, utcNow);
            return false;
        }

        if (!IsAllowedTransition(ComplianceEligibilityStatus, target))
        {
            throw new InvalidOperationException(
                $"Compliance eligibility cannot move from '{ComplianceEligibilityStatus}' to '{target}'.");
        }

        ComplianceEligibilityStatus = target;
        if (RequiresPlatformCapabilitiesDisabled(target))
        {
            TaxDocumentIssuanceEnabled = false;
            TaxConfigurationEnabled = false;
        }

        Touch(actorReference, utcNow);
        return true;
    }

    public bool SetTaxDocumentIssuanceEnabled(
        bool enabled,
        string actorReference,
        DateTimeOffset utcNow)
    {
        if (enabled)
        {
            if (ComplianceEligibilityStatus != OrganizationComplianceEligibilityStatuses.Approved)
            {
                throw new InvalidOperationException(
                    "Tax-document issuance can be enabled only when compliance eligibility is Approved.");
            }
        }

        if (TaxDocumentIssuanceEnabled == enabled)
        {
            Touch(actorReference, utcNow);
            return false;
        }

        TaxDocumentIssuanceEnabled = enabled;
        Touch(actorReference, utcNow);
        return true;
    }

    public bool SetTaxConfigurationEnabled(
        bool enabled,
        string actorReference,
        DateTimeOffset utcNow)
    {
        if (enabled)
        {
            if (ComplianceEligibilityStatus != OrganizationComplianceEligibilityStatuses.Approved)
            {
                throw new InvalidOperationException(
                    "Tax configuration can be enabled only when compliance eligibility is Approved.");
            }
        }

        if (TaxConfigurationEnabled == enabled)
        {
            Touch(actorReference, utcNow);
            return false;
        }

        TaxConfigurationEnabled = enabled;
        Touch(actorReference, utcNow);
        return true;
    }

    private void Touch(string actorReference, DateTimeOffset utcNow)
    {
        if (string.IsNullOrWhiteSpace(actorReference))
        {
            throw new ArgumentException("Actor reference is required.", nameof(actorReference));
        }

        UpdatedAtUtc = EnsureUtc(utcNow);
        UpdatedByActorReference = actorReference.Trim();
    }

    private static bool RequiresPlatformCapabilitiesDisabled(string status) =>
        status is OrganizationComplianceEligibilityStatuses.Rejected
            or OrganizationComplianceEligibilityStatuses.Suspended
            or OrganizationComplianceEligibilityStatuses.Revoked
            or OrganizationComplianceEligibilityStatuses.NotRequested
            or OrganizationComplianceEligibilityStatuses.Requested
            or OrganizationComplianceEligibilityStatuses.DocumentsRequired
            or OrganizationComplianceEligibilityStatuses.UnderReview;

    internal static bool IsAllowedTransition(string from, string to) =>
        (from, to) switch
        {
            (OrganizationComplianceEligibilityStatuses.NotRequested, OrganizationComplianceEligibilityStatuses.Requested) => true,
            (OrganizationComplianceEligibilityStatuses.NotRequested, OrganizationComplianceEligibilityStatuses.UnderReview) => true,
            (OrganizationComplianceEligibilityStatuses.Requested, OrganizationComplianceEligibilityStatuses.DocumentsRequired) => true,
            (OrganizationComplianceEligibilityStatuses.Requested, OrganizationComplianceEligibilityStatuses.UnderReview) => true,
            (OrganizationComplianceEligibilityStatuses.Requested, OrganizationComplianceEligibilityStatuses.Rejected) => true,
            (OrganizationComplianceEligibilityStatuses.DocumentsRequired, OrganizationComplianceEligibilityStatuses.UnderReview) => true,
            (OrganizationComplianceEligibilityStatuses.DocumentsRequired, OrganizationComplianceEligibilityStatuses.Rejected) => true,
            (OrganizationComplianceEligibilityStatuses.UnderReview, OrganizationComplianceEligibilityStatuses.DocumentsRequired) => true,
            (OrganizationComplianceEligibilityStatuses.UnderReview, OrganizationComplianceEligibilityStatuses.Approved) => true,
            (OrganizationComplianceEligibilityStatuses.UnderReview, OrganizationComplianceEligibilityStatuses.Rejected) => true,
            (OrganizationComplianceEligibilityStatuses.Approved, OrganizationComplianceEligibilityStatuses.Suspended) => true,
            (OrganizationComplianceEligibilityStatuses.Approved, OrganizationComplianceEligibilityStatuses.Revoked) => true,
            (OrganizationComplianceEligibilityStatuses.Approved, OrganizationComplianceEligibilityStatuses.UnderReview) => true,
            (OrganizationComplianceEligibilityStatuses.Rejected, OrganizationComplianceEligibilityStatuses.Requested) => true,
            (OrganizationComplianceEligibilityStatuses.Rejected, OrganizationComplianceEligibilityStatuses.UnderReview) => true,
            (OrganizationComplianceEligibilityStatuses.Suspended, OrganizationComplianceEligibilityStatuses.Approved) => true,
            (OrganizationComplianceEligibilityStatuses.Suspended, OrganizationComplianceEligibilityStatuses.UnderReview) => true,
            (OrganizationComplianceEligibilityStatuses.Suspended, OrganizationComplianceEligibilityStatuses.Revoked) => true,
            (OrganizationComplianceEligibilityStatuses.Revoked, OrganizationComplianceEligibilityStatuses.Requested) => true,
            (OrganizationComplianceEligibilityStatuses.Revoked, OrganizationComplianceEligibilityStatuses.UnderReview) => true,
            _ => false
        };

    private static string NormalizeStatus(string status)
    {
        if (!OrganizationComplianceEligibilityStatuses.IsKnown(status))
        {
            throw new ArgumentException($"Unknown compliance eligibility status '{status}'.", nameof(status));
        }

        return status;
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
