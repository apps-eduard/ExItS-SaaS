using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Application.LocalValidation;

/// <summary>
/// Production-equivalent local validation seed (exits-local-validation Compose).
/// Never enable in Production — only configuration differs from Production deployment.
/// </summary>
public sealed class LocalValidationOptions
{
    public const string SectionName = "LocalValidation";

    /// <summary>Must be true for dataset seed and seed-identity discovery (non-Production only).</summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Password applied to approved Local Validation identities for normal /auth/login.
    /// Supplied only via environment/secrets; never commit a Production secret.
    /// </summary>
    public string SharedPassword { get; set; } = string.Empty;

    public const string ProductPlanCode = "sampaguita-pos";
    public const string ProductPlanDisplayName = "Sampaguita POS Plan";
    public const string TrialDisplayName = "Sampaguita POS Trial";

    public const string Actor = "local-validation-initializer";

    /// <summary>Legacy single-org slug kept for callers that still expect the first store.</summary>
    public const string OrgSlug = LocalValidationOrganizationCatalog.SampaguitaSlug;

    /// <summary>Legacy single-org display name kept for callers that still expect the first store.</summary>
    public const string OrgDisplayName = LocalValidationOrganizationCatalog.SampaguitaDisplayName;
}

public sealed record LocalValidationOrganizationDefinition(string Slug, string DisplayName);

public static class LocalValidationOrganizationCatalog
{
    public const string SampaguitaSlug = "sampaguita-store";
    public const string SampaguitaDisplayName = "Sampaguita Neighborhood Store";
    public const string MabuhaySlug = "mabuhay-mini-mart";
    public const string MabuhayDisplayName = "Mabuhay Mini Mart";

    public static LocalValidationOrganizationDefinition Sampaguita { get; } =
        new(SampaguitaSlug, SampaguitaDisplayName);

    public static LocalValidationOrganizationDefinition Mabuhay { get; } =
        new(MabuhaySlug, MabuhayDisplayName);

    public static IReadOnlyList<LocalValidationOrganizationDefinition> All { get; } =
    [
        Sampaguita,
        Mabuhay
    ];

    public static LocalValidationOrganizationDefinition? FindBySlug(string? slug) =>
        All.FirstOrDefault(o => string.Equals(o.Slug, slug, StringComparison.OrdinalIgnoreCase));
}

public sealed record LocalValidationIdentityDefinition(
    string Key,
    string Username,
    string DisplayName,
    string Email,
    string Summary,
    AccountClass PreferredAccountClass,
    PlatformSystemRole? AssignPlatformRole,
    bool HasOrganizationMembership,
    string? OrganizationSlug,
    OrganizationMembershipValidationRole? OrganizationRole,
    bool GrantPosProductAccess,
    string? PosLocalRoleCode);

public enum OrganizationMembershipValidationRole
{
    OrganizationOwner,
    OrganizationAdministrator,
    OrganizationMember
}

/// <summary>Known obsolete Phase 16 / Development seed identities removed by Local Validation cleanup.</summary>
public static class ObsoletePhase16SeedIdentities
{
    public static IReadOnlyList<string> NormalizedEmails { get; } =
    [
        "platform.admin1@exits.test",
        "platform.admin2@exits.test",
        "org.seed.owner@exits.test",
        "personal.user1@exits.test",
        "personal.user2@exits.test"
    ];

    public static IReadOnlyList<string> NormalizedUsernames { get; } =
    [
        "platform.admin1",
        "platform.admin2",
        "org.seed.owner",
        "personal.user1",
        "personal.user2"
    ];

    public const string SeedOrgSlug = "phase16-seed-org";
}

public static class LocalValidationIdentityCatalog
{
    public static IReadOnlyList<LocalValidationIdentityDefinition> All { get; } =
    [
        new(
            Key: "olivia-mendoza",
            Username: "olivia.mendoza",
            DisplayName: "Olivia Mendoza",
            Email: "olivia.mendoza@exits.local",
            Summary: "Platform account — Platform Administrator only.",
            PreferredAccountClass: AccountClass.Platform,
            AssignPlatformRole: PlatformSystemRole.PlatformAdministrator,
            HasOrganizationMembership: false,
            OrganizationSlug: null,
            OrganizationRole: null,
            GrantPosProductAccess: false,
            PosLocalRoleCode: null),
        new(
            Key: "rafael-torres",
            Username: "rafael.torres",
            DisplayName: "Rafael Torres",
            Email: "rafael.torres@exits.local",
            Summary: "Platform account — Platform Support only.",
            PreferredAccountClass: AccountClass.Platform,
            AssignPlatformRole: PlatformSystemRole.PlatformSupport,
            HasOrganizationMembership: false,
            OrganizationSlug: null,
            OrganizationRole: null,
            GrantPosProductAccess: false,
            PosLocalRoleCode: null),
        new(
            Key: "maria-santos",
            Username: "maria.santos",
            DisplayName: "Maria Santos",
            Email: "maria.santos@exits.local",
            Summary: "Sampaguita Neighborhood Store — Organization Owner with POS Owner role.",
            PreferredAccountClass: AccountClass.Organization,
            AssignPlatformRole: null,
            HasOrganizationMembership: true,
            OrganizationSlug: LocalValidationOrganizationCatalog.SampaguitaSlug,
            OrganizationRole: OrganizationMembershipValidationRole.OrganizationOwner,
            GrantPosProductAccess: true,
            PosLocalRoleCode: "Owner"),
        new(
            Key: "carlo-reyes",
            Username: "carlo.reyes",
            DisplayName: "Carlo Reyes",
            Email: "carlo.reyes@exits.local",
            Summary: "Sampaguita Neighborhood Store — Organization Member with POS Cashier role.",
            PreferredAccountClass: AccountClass.Organization,
            AssignPlatformRole: null,
            HasOrganizationMembership: true,
            OrganizationSlug: LocalValidationOrganizationCatalog.SampaguitaSlug,
            OrganizationRole: OrganizationMembershipValidationRole.OrganizationMember,
            GrantPosProductAccess: true,
            PosLocalRoleCode: "Cashier"),
        new(
            Key: "ana-cruz",
            Username: "ana.cruz",
            DisplayName: "Ana Cruz",
            Email: "ana.cruz@exits.local",
            Summary: "Mabuhay Mini Mart — Organization Owner with POS Owner role.",
            PreferredAccountClass: AccountClass.Organization,
            AssignPlatformRole: null,
            HasOrganizationMembership: true,
            OrganizationSlug: LocalValidationOrganizationCatalog.MabuhaySlug,
            OrganizationRole: OrganizationMembershipValidationRole.OrganizationOwner,
            GrantPosProductAccess: true,
            PosLocalRoleCode: "Owner"),
        new(
            Key: "daniel-garcia",
            Username: "daniel.garcia",
            DisplayName: "Daniel Garcia",
            Email: "daniel.garcia@exits.local",
            Summary: "Mabuhay Mini Mart — Organization Member with POS Cashier role.",
            PreferredAccountClass: AccountClass.Organization,
            AssignPlatformRole: null,
            HasOrganizationMembership: true,
            OrganizationSlug: LocalValidationOrganizationCatalog.MabuhaySlug,
            OrganizationRole: OrganizationMembershipValidationRole.OrganizationMember,
            GrantPosProductAccess: true,
            PosLocalRoleCode: "Cashier"),
        new(
            Key: "luis-navarro",
            Username: "luis.navarro",
            DisplayName: "Luis Navarro",
            Email: "luis.navarro@exits.local",
            Summary: "Personal account only.",
            PreferredAccountClass: AccountClass.Personal,
            AssignPlatformRole: null,
            HasOrganizationMembership: false,
            OrganizationSlug: null,
            OrganizationRole: null,
            GrantPosProductAccess: false,
            PosLocalRoleCode: null),
        new(
            Key: "sofia-ramos",
            Username: "sofia.ramos",
            DisplayName: "Sofia Ramos",
            Email: "sofia.ramos@exits.local",
            Summary: "Personal account only.",
            PreferredAccountClass: AccountClass.Personal,
            AssignPlatformRole: null,
            HasOrganizationMembership: false,
            OrganizationSlug: null,
            OrganizationRole: null,
            GrantPosProductAccess: false,
            PosLocalRoleCode: null)
    ];

    public static LocalValidationIdentityDefinition? FindByKey(string? key) =>
        All.FirstOrDefault(i => string.Equals(i.Key, key, StringComparison.OrdinalIgnoreCase));
}

public sealed record LocalValidationIdentityDto(
    string Key,
    string Username,
    string DisplayName,
    string Email,
    Guid UserId,
    Guid? OrganizationId,
    string Summary,
    string? PosLocalRoleCode,
    string ListLabel);
