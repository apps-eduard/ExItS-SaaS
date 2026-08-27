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

    public const string SeedScopeFull = "Full";
    public const string SeedScopePlatformAdministratorsOnly = "PlatformAdministratorsOnly";

    /// <summary>Must be true for dataset seed and seed-identity discovery (non-Production only).</summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// When false, <see cref="LocalValidationHostedService"/> skips migrate/seed (integration tests).
    /// Default true when Local Validation is enabled.
    /// </summary>
    public bool RunHostedSeed { get; set; } = true;

    /// <summary>
    /// When true with <see cref="SeedScopePlatformAdministratorsOnly"/>, purge transactional rows before seed.
    /// Used by Reset after volume wipe; ordinary Start leaves app-created data intact.
    /// </summary>
    public bool PurgeTransactionalOnSeed { get; set; }

    /// <summary><see cref="SeedScopePlatformAdministratorsOnly"/> (default) or <see cref="SeedScopeFull"/>.</summary>
    public string SeedScope { get; set; } = SeedScopePlatformAdministratorsOnly;

    /// <summary>
    /// Password applied to approved Local Validation identities for normal /auth/login.
    /// Supplied only via environment/secrets; never commit a Production secret.
    /// </summary>
    public string SharedPassword { get; set; } = string.Empty;

    /// <summary>Deterministic Local Validation dataset version (logged on seed).</summary>
    public const string DatasetVersion = "2026-08-27-bnpl-ppm-local-validation-v1";

    public const string ProductPlanCode = "local-validation-pos";
    public const string ProductPlanDisplayName = "Local Validation POS Plan";
    public const string TrialDisplayName = "Local Validation POS Trial";

    /// <summary>
    /// Test-only PLM commercial fixture identifiers. Not production plan, trial, or pricing policy.
    /// </summary>
    public const string PlmLocalValidationPlanCode = "plm-local-validation";
    public const string PlmLocalValidationPlanDisplayName = "PLM Local Validation";
    public const string PlmLocalValidationTrialDisplayName = "PLM Local Validation";

    /// <summary>
    /// Test-only BNPL commercial fixture identifiers. Not production plan, trial, or pricing policy.
    /// </summary>
    public const string BnplLocalValidationPlanCode = "bnpl-local-validation";
    public const string BnplLocalValidationPlanDisplayName = "BNPL Local Validation";
    public const string BnplLocalValidationTrialDisplayName = "BNPL Local Validation";

    /// <summary>
    /// Test-only PPM commercial fixture identifiers. Not production plan, trial, pricing, or licensing policy.
    /// Does not close PPM-D-00-04–20.
    /// </summary>
    public const string PpmLocalValidationPlanCode = "ppm-local-validation";
    public const string PpmLocalValidationPlanDisplayName = "PPM Local Validation";
    public const string PpmLocalValidationTrialDisplayName = "PPM Local Validation";

    public const string Actor = "local-validation-initializer";

    /// <summary>Legacy single-org slug kept for callers that still expect the first store.</summary>
    public const string OrgSlug = LocalValidationOrganizationCatalog.AbcSariSariSlug;

    /// <summary>Legacy single-org display name kept for callers that still expect the first store.</summary>
    public const string OrgDisplayName = LocalValidationOrganizationCatalog.AbcSariSariDisplayName;

    public static IReadOnlyList<LocalValidationIdentityDefinition> IdentitiesForSeedScope(string? seedScope)
    {
        if (string.IsNullOrWhiteSpace(seedScope)
            || string.Equals(seedScope.Trim(), SeedScopePlatformAdministratorsOnly, StringComparison.OrdinalIgnoreCase))
        {
            return LocalValidationIdentityCatalog.PlatformAdministratorsOnly;
        }

        if (string.Equals(seedScope.Trim(), SeedScopeFull, StringComparison.OrdinalIgnoreCase))
        {
            return LocalValidationIdentityCatalog.All;
        }

        throw new ArgumentOutOfRangeException(
            nameof(seedScope),
            seedScope,
            $"Unknown LocalValidation seed scope. Use '{SeedScopeFull}' or '{SeedScopePlatformAdministratorsOnly}'.");
    }
}

public sealed record LocalValidationOrganizationDefinition(string Slug, string DisplayName);

public static class LocalValidationOrganizationCatalog
{
    public const string AbcSariSariSlug = "abc-sari-sari";
    public const string AbcSariSariDisplayName = "ABC Sari-Sari Store";
    public const string XyzMiniGrocerySlug = "xyz-mini-grocery";
    public const string XyzMiniGroceryDisplayName = "XYZ Mini Grocery";

    public static LocalValidationOrganizationDefinition AbcSariSari { get; } =
        new(AbcSariSariSlug, AbcSariSariDisplayName);

    public static LocalValidationOrganizationDefinition XyzMiniGrocery { get; } =
        new(XyzMiniGrocerySlug, XyzMiniGroceryDisplayName);

    public static IReadOnlyList<LocalValidationOrganizationDefinition> All { get; } =
    [
        AbcSariSari,
        XyzMiniGrocery
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
    string? PosLocalRoleCode,
    bool GrantPlmProductAccess = false,
    bool GrantBnplProductAccess = false,
    bool GrantPpmProductAccess = false);

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

/// <summary>
/// Obsolete Local Validation / Phase16 organization slugs closed during seed cleanup.
/// Does not delete manually created orgs outside this explicit list.
/// </summary>
public static class ObsoleteLocalValidationOrganizations
{
    public static IReadOnlyList<string> Slugs { get; } =
    [
        ObsoletePhase16SeedIdentities.SeedOrgSlug,
        "sampaguita-store",
        "mabuhay-mini-mart",
        "phase16-seed-org",
        "ks-store"
    ];
}

public static class LocalValidationIdentityCatalog
{
    public static IReadOnlyList<LocalValidationIdentityDefinition> PlatformAdministratorsOnly { get; } =
    [
        new(
            Key: "olivia-mendoza",
            Username: "olivia.mendoza",
            DisplayName: "Olivia Mendoza",
            Email: "olivia.mendoza@exits.local",
            Summary: "Primary Platform Administrator — onboarding baseline.",
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
            Summary: "Backup Platform Administrator — onboarding baseline.",
            PreferredAccountClass: AccountClass.Platform,
            AssignPlatformRole: PlatformSystemRole.PlatformAdministrator,
            HasOrganizationMembership: false,
            OrganizationSlug: null,
            OrganizationRole: null,
            GrantPosProductAccess: false,
            PosLocalRoleCode: null)
    ];

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
            Summary: "ABC Sari-Sari Store — Organization Owner with POS Owner role.",
            PreferredAccountClass: AccountClass.Organization,
            AssignPlatformRole: null,
            HasOrganizationMembership: true,
            OrganizationSlug: LocalValidationOrganizationCatalog.AbcSariSariSlug,
            OrganizationRole: OrganizationMembershipValidationRole.OrganizationOwner,
            GrantPosProductAccess: true,
            PosLocalRoleCode: "Owner",
            GrantPlmProductAccess: true,
            GrantBnplProductAccess: true),
        new(
            Key: "carlo-reyes",
            Username: "carlo.reyes",
            DisplayName: "Carlo Reyes",
            Email: "carlo.reyes@exits.local",
            Summary: "ABC Sari-Sari Store — org-scoped staff (login local@ORG######; contact email for recovery) with POS Cashier role.",
            PreferredAccountClass: AccountClass.Organization,
            AssignPlatformRole: null,
            HasOrganizationMembership: true,
            OrganizationSlug: LocalValidationOrganizationCatalog.AbcSariSariSlug,
            OrganizationRole: OrganizationMembershipValidationRole.OrganizationMember,
            GrantPosProductAccess: true,
            PosLocalRoleCode: "Cashier",
            GrantPlmProductAccess: true,
            GrantBnplProductAccess: true),
        new(
            Key: "ana-cruz",
            Username: "ana.cruz",
            DisplayName: "Ana Cruz",
            Email: "ana.cruz@exits.local",
            Summary: "XYZ Mini Grocery — Organization Owner with POS Owner role.",
            PreferredAccountClass: AccountClass.Organization,
            AssignPlatformRole: null,
            HasOrganizationMembership: true,
            OrganizationSlug: LocalValidationOrganizationCatalog.XyzMiniGrocerySlug,
            OrganizationRole: OrganizationMembershipValidationRole.OrganizationOwner,
            GrantPosProductAccess: true,
            PosLocalRoleCode: "Owner",
            GrantPpmProductAccess: true),
        new(
            Key: "daniel-garcia",
            Username: "daniel.garcia",
            DisplayName: "Daniel Garcia",
            Email: "daniel.garcia@exits.local",
            Summary: "XYZ Mini Grocery — org-scoped staff (login local@ORG######; contact email for recovery) with POS Cashier role.",
            PreferredAccountClass: AccountClass.Organization,
            AssignPlatformRole: null,
            HasOrganizationMembership: true,
            OrganizationSlug: LocalValidationOrganizationCatalog.XyzMiniGrocerySlug,
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

    public static bool IsCanonicalBaselineEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var normalized = email.Trim().ToUpperInvariant();
        return PlatformAdministratorsOnly.Any(i =>
            string.Equals(i.Email.Trim().ToUpperInvariant(), normalized, StringComparison.Ordinal));
    }

    /// <summary>
    /// Full-catalog fixture identities that must not remain when SeedScope is PlatformAdministratorsOnly.
    /// Owner-created accounts (e.g. Mica) are never in this list.
    /// </summary>
    public static IReadOnlyList<LocalValidationIdentityDefinition> FullCatalogExceptBaseline { get; } =
        All.Where(i => !IsCanonicalBaselineEmail(i.Email)).ToList();
}

/// <summary>Deterministic Personal Utang seed markers for Local Validation (Luis ↔ Sofia).</summary>
public static class LocalValidationPersonalUtangSeedMarkers
{
    public const string LuisToSofiaNotes = "local-validation:luis-lends-sofia";
    public const string SofiaToLuisNotes = "local-validation:sofia-lends-luis";
    public const string LuisToSofiaPaymentNotes = "local-validation:sofia-payment-on-luis-loan";
    public const decimal LuisToSofiaLoan = 5000m;
    public const decimal LuisToSofiaPayment = 1500m;
    public const decimal SofiaToLuisLoan = 1000m;
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

/// <summary>
/// One selectable Quick Login entry bound to an account profile (and optional organization membership).
/// POS product-local roles are never listed here.
/// </summary>
public sealed record LocalValidationQuickLoginIdentityDto(
    string Key,
    string Username,
    string DisplayName,
    string Email,
    Guid UserId,
    Guid AccountProfileId,
    string AccountClass,
    Guid? OrganizationId,
    string? OrganizationName,
    string? OrganizationRole,
    string ListLabel,
    string ScopeLabel,
    bool IsCanonicalBaseline = false);
