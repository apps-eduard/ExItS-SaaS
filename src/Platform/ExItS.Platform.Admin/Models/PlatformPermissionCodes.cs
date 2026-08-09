namespace ExItS.Platform.Admin.Models;

/// <summary>
/// Mirrors <c>ExItS.Platform.Domain.Authorization.PlatformPermission</c> permission codes as plain
/// strings so the Admin UI project stays decoupled from Platform.Domain/Infrastructure and only
/// talks to the Platform API over HTTP. Values must stay in sync with the Domain source of truth.
/// UI-side permission checks are convenience only — the server remains authoritative.
/// </summary>
public static class PlatformPermissionCodes
{
    public const string ViewPortfolio = "platform.permission.view_portfolio";
    public const string ManageOrganizations = "platform.permission.manage_organizations";
    public const string ManageCatalog = "platform.permission.manage_catalog";
    public const string ManagePlatformUsers = "platform.permission.manage_platform_users";
    public const string ManageMemberships = "platform.permission.manage_memberships";
    public const string ManageProductAccess = "platform.permission.manage_product_access";
    public const string ManageSubscriptions = "platform.permission.manage_subscriptions";
    public const string ManageManualPayments = "platform.permission.manage_manual_payments";
    public const string ManageEntitlementOverrides = "platform.permission.manage_entitlement_overrides";
    public const string ViewAuditRecords = "platform.permission.view_audit_records";

    public const string ViewGlobalCatalog = "platform.permission.view_global_catalog";
    public const string ManageGlobalCategories = "platform.permission.manage_global_categories";
    public const string ManageGlobalProducts = "platform.permission.manage_global_products";
    public const string ImportGlobalProducts = "platform.permission.import_global_products";
    public const string ManageCatalogTemplates = "platform.permission.manage_catalog_templates";
    public const string PublishCatalogTemplates = "platform.permission.publish_catalog_templates";

    public const string ViewPrivacyCompliance = "platform.permission.view_privacy_compliance";
    public const string ManagePrivacyCompliance = "platform.permission.manage_privacy_compliance";

    public static readonly IReadOnlyList<string> All =
    [
        ViewPortfolio,
        ManageOrganizations,
        ManageCatalog,
        ManagePlatformUsers,
        ManageMemberships,
        ManageProductAccess,
        ManageSubscriptions,
        ManageManualPayments,
        ManageEntitlementOverrides,
        ViewAuditRecords,
        ViewGlobalCatalog,
        ManageGlobalCategories,
        ManageGlobalProducts,
        ImportGlobalProducts,
        ManageCatalogTemplates,
        PublishCatalogTemplates,
        ViewPrivacyCompliance,
        ManagePrivacyCompliance
    ];
}
