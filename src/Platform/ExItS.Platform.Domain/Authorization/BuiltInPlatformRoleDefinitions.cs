namespace ExItS.Platform.Domain.Authorization;

/// <summary>
/// Stable seeded identifiers and codes for built-in platform role definitions (catalog UI).
/// Assignments for these roles continue to use <see cref="PlatformSystemRole"/> /
/// <see cref="PlatformRoleAssignment"/>.
/// </summary>
public static class BuiltInPlatformRoleDefinitions
{
    public static readonly Guid PlatformAdministratorId = Guid.Parse("11111111-1111-4111-8111-111111111101");
    public static readonly Guid BillingAdministratorId = Guid.Parse("11111111-1111-4111-8111-111111111102");
    public static readonly Guid PlatformSupportId = Guid.Parse("11111111-1111-4111-8111-111111111103");
    public static readonly Guid PlatformAuditorId = Guid.Parse("11111111-1111-4111-8111-111111111104");

    public const string PlatformAdministratorCode = "PlatformAdministrator";
    public const string BillingAdministratorCode = "BillingAdministrator";
    public const string PlatformSupportCode = "PlatformSupport";
    public const string PlatformAuditorCode = "PlatformAuditor";

    public static IReadOnlyList<(Guid Id, string Code, string Name, string Description, PlatformSystemRole SystemRole)> All { get; } =
    [
        (PlatformAdministratorId, PlatformAdministratorCode, "Platform Administrator",
            "Full Platform administration permissions.", PlatformSystemRole.PlatformAdministrator),
        (BillingAdministratorId, BillingAdministratorCode, "Billing Administrator",
            "Organizations, subscriptions, manual payments, and audit visibility.", PlatformSystemRole.BillingAdministrator),
        (PlatformSupportId, PlatformSupportCode, "Platform Support",
            "Memberships, product access, portfolio view, and audit visibility.", PlatformSystemRole.PlatformSupport),
        (PlatformAuditorId, PlatformAuditorCode, "Platform Auditor",
            "Read-only portfolio and audit visibility.", PlatformSystemRole.PlatformAuditor)
    ];
}

/// <summary>User directory taxonomy for Platform Admin filtered views (same identity may match multiple views).</summary>
public enum UserDirectoryFilter
{
    All = 0,
    Unassigned = 1,
    Organization = 2,
    PlatformStaff = 3,
    Personal = 4
}
