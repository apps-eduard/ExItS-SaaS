using ExItS.Platform.Application.Access;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.UnitTests.Access;

public sealed class OrganizationManagementAuthorityTests
{
    [Theory]
    [InlineData(OrganizationRole.OrganizationOwner, true)]
    [InlineData(OrganizationRole.OrganizationAdministrator, true)]
    [InlineData(OrganizationRole.OrganizationMember, false)]
    public void Management_membership_roles(OrganizationRole role, bool expected) =>
        Assert.Equal(expected, OrganizationManagementAuthority.IsManagementMembership(role));

    [Theory]
    [InlineData(OrganizationRole.OrganizationOwner, true, true)]
    [InlineData(OrganizationRole.OrganizationOwner, false, false)]
    [InlineData(OrganizationRole.OrganizationAdministrator, true, true)]
    [InlineData(OrganizationRole.OrganizationMember, true, false)]
    public void Qualifies_requires_entitlement(
        OrganizationRole role,
        bool entitlementAllowed,
        bool expected) =>
        Assert.Equal(expected, OrganizationManagementAuthority.Qualifies(role, entitlementAllowed));

    [Fact]
    public void Exact_owner_is_distinct_from_administrator()
    {
        Assert.True(OrganizationManagementAuthority.IsExactOwner(OrganizationRole.OrganizationOwner));
        Assert.False(OrganizationManagementAuthority.IsExactOwner(OrganizationRole.OrganizationAdministrator));
    }
}
