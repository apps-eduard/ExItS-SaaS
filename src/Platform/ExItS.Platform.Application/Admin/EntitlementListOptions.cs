namespace ExItS.Platform.Application.Admin;

/// <summary>Safe server-side sort keys for latest-entitlement portfolio lists.</summary>
public enum EntitlementListSortBy
{
    GeneratedAtUtc = 0,
    OrganizationDisplayName = 1,
    ProductDisplayName = 2,
    Status = 3,
    Revision = 4
}
