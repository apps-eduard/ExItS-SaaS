namespace ExItS.Platform.Domain.Authorization;

/// <summary>
/// Platform system operational permission codes. Platform grants product access; these permissions
/// govern Platform Admin operations only and never grant product-local (clinical/POS) permissions.
/// See docs/engineering/authorization-matrix.md.
/// </summary>
public static class PlatformPermission
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
        PublishCatalogTemplates
    ];
}
