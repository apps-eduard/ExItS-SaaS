namespace ExItS.Platform.Domain.Organizations;

/// <summary>
/// Platform-controlled online supplier payments capability for an organization.
/// Distinct from plan entitlements — enablement is product authorization only,
/// not payment settlement or PSP certification.
/// </summary>
public static class OrganizationOnlineSupplierPaymentsStatuses
{
    public const string Disabled = "Disabled";
    public const string Available = "Available";
    public const string Suspended = "Suspended";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Disabled,
        Available,
        Suspended
    };

    public static bool IsKnown(string? value) =>
        !string.IsNullOrWhiteSpace(value) && All.Contains(value);
}
