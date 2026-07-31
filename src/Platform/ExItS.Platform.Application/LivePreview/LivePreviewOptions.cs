namespace ExItS.Platform.Application.LivePreview;

/// <summary>
/// Personal live-preview only (exits-live-preview Compose). Never enable in Production.
/// </summary>
public sealed class LivePreviewOptions
{
    public const string SectionName = "LivePreview";

    /// <summary>Must be true for initializer, identities API, and quick-login.</summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Shared local password for preview identities. Supplied via environment; never commit a real Production secret.
    /// </summary>
    public string SharedPassword { get; set; } = string.Empty;

    public const string OrgSlug = "preview-organization-a";
    public const string OrgDisplayName = "Preview Organization A";
    public const string ProductPlanCode = "preview-pos";
    public const string ProductPlanDisplayName = "Preview POS Plan";
    public const string TrialDisplayName = "Preview Trial";

    public const string Actor = "live-preview-initializer";
}

public sealed record LivePreviewIdentityDefinition(
    string Key,
    string Username,
    string DisplayName,
    string Email,
    string Summary,
    bool AssignPlatformAdministrator,
    bool HasOrganizationMembership,
    OrganizationMembershipPreviewRole? OrganizationRole,
    bool GrantPosProductAccess,
    string? PosLocalRoleCode);

public enum OrganizationMembershipPreviewRole
{
    OrganizationAdministrator,
    OrganizationMember
}

public static class LivePreviewIdentityCatalog
{
    public static IReadOnlyList<LivePreviewIdentityDefinition> All { get; } =
    [
        new(
            Key: "platform-admin",
            Username: "preview-platform-admin",
            DisplayName: "Preview Platform Administrator",
            Email: "preview-platform-admin@live-preview.exits.local",
            Summary: "Platform Administrator — Platform admin only; no POS-local role by default.",
            AssignPlatformAdministrator: true,
            HasOrganizationMembership: false,
            OrganizationRole: null,
            GrantPosProductAccess: false,
            PosLocalRoleCode: null),
        new(
            Key: "org-admin",
            Username: "preview-org-admin",
            DisplayName: "Preview Organization Administrator",
            Email: "preview-org-admin@live-preview.exits.local",
            Summary: "Organization Administrator with POS access and POS Owner (administrative) role.",
            AssignPlatformAdministrator: false,
            HasOrganizationMembership: true,
            OrganizationRole: OrganizationMembershipPreviewRole.OrganizationAdministrator,
            GrantPosProductAccess: true,
            PosLocalRoleCode: "Owner"),
        new(
            Key: "pos-cashier",
            Username: "preview-pos-cashier",
            DisplayName: "Preview POS Cashier",
            Email: "preview-pos-cashier@live-preview.exits.local",
            Summary: "Organization member with POS access and Cashier role only.",
            AssignPlatformAdministrator: false,
            HasOrganizationMembership: true,
            OrganizationRole: OrganizationMembershipPreviewRole.OrganizationMember,
            GrantPosProductAccess: true,
            PosLocalRoleCode: "Cashier"),
        new(
            Key: "no-pos",
            Username: "preview-no-pos",
            DisplayName: "Preview User - No POS Access",
            Email: "preview-no-pos@live-preview.exits.local",
            Summary: "Active organization membership without POS product access.",
            AssignPlatformAdministrator: false,
            HasOrganizationMembership: true,
            OrganizationRole: OrganizationMembershipPreviewRole.OrganizationMember,
            GrantPosProductAccess: false,
            PosLocalRoleCode: null),
        new(
            Key: "no-org",
            Username: "preview-no-org",
            DisplayName: "Preview User - No Organization",
            Email: "preview-no-org@live-preview.exits.local",
            Summary: "Valid Platform User with no organization memberships.",
            AssignPlatformAdministrator: false,
            HasOrganizationMembership: false,
            OrganizationRole: null,
            GrantPosProductAccess: false,
            PosLocalRoleCode: null)
    ];

    public static LivePreviewIdentityDefinition? FindByKey(string? key) =>
        All.FirstOrDefault(i => string.Equals(i.Key, key, StringComparison.OrdinalIgnoreCase));
}

public sealed record LivePreviewIdentityDto(
    string Key,
    string Username,
    string DisplayName,
    string Email,
    Guid UserId,
    Guid? OrganizationId,
    string Summary,
    string? PosLocalRoleCode);
