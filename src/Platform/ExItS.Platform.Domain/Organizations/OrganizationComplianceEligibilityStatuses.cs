namespace ExItS.Platform.Domain.Organizations;

/// <summary>
/// Platform-controlled compliance review lifecycle for an organization.
/// Distinct from <see cref="OrganizationSalesDocumentCapability.TaxDocumentIssuanceEnabled"/>.
/// Education acknowledgment and subscription entitlements do not set these values.
/// </summary>
public static class OrganizationComplianceEligibilityStatuses
{
    public const string NotRequested = "NotRequested";
    public const string Requested = "Requested";
    public const string DocumentsRequired = "DocumentsRequired";
    public const string UnderReview = "UnderReview";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Suspended = "Suspended";
    public const string Revoked = "Revoked";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        NotRequested,
        Requested,
        DocumentsRequired,
        UnderReview,
        Approved,
        Rejected,
        Suspended,
        Revoked
    };

    public static bool IsKnown(string? value) =>
        !string.IsNullOrWhiteSpace(value) && All.Contains(value);
}

/// <summary>
/// Runtime gate for actual TaxDocument production. Remains false until a future
/// confirmed TaxDocument implementation is delivered and validated.
/// Enabling organization capability alone must not produce TaxDocuments.
/// </summary>
public static class TaxDocumentIssuanceRuntime
{
    /// <summary>
    /// Remains false until a future confirmed TaxDocument implementation ships.
    /// Kept as a property (not const) so gate logic remains reachable for future flip.
    /// </summary>
    public static bool ImplementationAvailable { get; } = false;
}
