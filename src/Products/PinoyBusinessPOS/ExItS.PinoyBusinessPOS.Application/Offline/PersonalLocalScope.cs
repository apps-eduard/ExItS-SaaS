namespace ExItS.PinoyBusinessPOS.Application.Offline;

/// <summary>
/// Isolation marker for Personal-scope local databases. Never a real organization id —
/// used only as the path-hash org slot so Personal SQLite files stay separate from POS org DBs.
/// </summary>
public static class PersonalLocalScope
{
    /// <summary>Stable sentinel stored in local_context_info.organization_id / outbox organization_id for personal ops.</summary>
    public static readonly Guid PathIsolationMarker = Guid.Parse("a11ce000-0ff1-4e11-9e50-000000000001");

    public const string ProductCode = "exits.personal.utang";

    public const string DisplayName = "Personal";

    public static bool IsPersonalContext(Guid organizationId, string productCode) =>
        organizationId == PathIsolationMarker
        && string.Equals(productCode, ProductCode, StringComparison.Ordinal);

    public static bool IsPersonalOperationType(string? operationType) =>
        !string.IsNullOrWhiteSpace(operationType)
        && operationType.StartsWith("personal.", StringComparison.OrdinalIgnoreCase);
}
