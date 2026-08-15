using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Access;

/// <summary>
/// Organization Web / business-management authority derived from Platform membership,
/// independent of product-local POS checkout roles and independent of commercial entitlement
/// for core organization administration.
/// </summary>
public static class OrganizationManagementAuthority
{
    public const string ReasonCode = "organization_management_authority";

    public static bool IsManagementMembership(OrganizationRole role) =>
        role is OrganizationRole.OrganizationOwner or OrganizationRole.OrganizationAdministrator;

    /// <summary>
    /// Organization Owner/Administrator may administer Organization Web for the selected org
    /// without a product-local selling role. Commercial entitlement is a separate dimension
    /// (paid product features), not a gate for core management membership.
    /// </summary>
    public static bool Qualifies(OrganizationRole membershipRole) =>
        IsManagementMembership(membershipRole);

    /// <summary>Backward-compatible overload; entitlement is ignored for membership qualification.</summary>
    public static bool Qualifies(OrganizationRole membershipRole, bool entitlementAllowed) =>
        Qualifies(membershipRole);

    public static bool IsExactOwner(OrganizationRole membershipRole) =>
        membershipRole is OrganizationRole.OrganizationOwner;
}
