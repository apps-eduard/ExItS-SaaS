using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Access;

/// <summary>
/// Organization Web / business-management authority derived from Platform membership,
/// independent of product-local POS checkout roles.
/// </summary>
public static class OrganizationManagementAuthority
{
    public const string ReasonCode = "organization_management_authority";

    public static bool IsManagementMembership(OrganizationRole role) =>
        role is OrganizationRole.OrganizationOwner or OrganizationRole.OrganizationAdministrator;

    /// <summary>
    /// Organization Owner/Administrator with active commercial entitlement may call
    /// organization management APIs without a product-local selling role.
    /// </summary>
    public static bool Qualifies(OrganizationRole membershipRole, bool entitlementAllowed) =>
        IsManagementMembership(membershipRole) && entitlementAllowed;

    public static bool IsExactOwner(OrganizationRole membershipRole) =>
        membershipRole is OrganizationRole.OrganizationOwner;
}
