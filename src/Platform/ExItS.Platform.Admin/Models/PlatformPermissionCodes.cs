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
