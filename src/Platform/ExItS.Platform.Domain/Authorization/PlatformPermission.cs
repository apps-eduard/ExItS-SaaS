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
    public const string ManagePlatformUsers = "platform.permission.manage_platform_users";
    public const string ManageMemberships = "platform.permission.manage_memberships";
    public const string ManageProductAccess = "platform.permission.manage_product_access";
    public const string ManageSubscriptions = "platform.permission.manage_subscriptions";
    public const string ManageManualPayments = "platform.permission.manage_manual_payments";
    public const string ManageEntitlementOverrides = "platform.permission.manage_entitlement_overrides";
    public const string ViewAuditRecords = "platform.permission.view_audit_records";

    public static readonly IReadOnlyList<string> All =
    [
        ViewPortfolio,
        ManageOrganizations,
        ManagePlatformUsers,
        ManageMemberships,
        ManageProductAccess,
        ManageSubscriptions,
        ManageManualPayments,
        ManageEntitlementOverrides,
        ViewAuditRecords
    ];
}
