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
    [InlineData(OrganizationRole.OrganizationOwner, true)]
    [InlineData(OrganizationRole.OrganizationAdministrator, true)]
    [InlineData(OrganizationRole.OrganizationMember, false)]
    public void Qualifies_from_membership_alone(OrganizationRole role, bool expected) =>
        Assert.Equal(expected, OrganizationManagementAuthority.Qualifies(role));

    [Fact]
    public void Qualifies_ignores_entitlement_flag_for_owners()
    {
        Assert.True(OrganizationManagementAuthority.Qualifies(OrganizationRole.OrganizationOwner, entitlementAllowed: false));
        Assert.True(OrganizationManagementAuthority.Qualifies(OrganizationRole.OrganizationAdministrator, entitlementAllowed: false));
        Assert.False(OrganizationManagementAuthority.Qualifies(OrganizationRole.OrganizationMember, entitlementAllowed: true));
    }

    [Fact]
    public void Exact_owner_is_distinct_from_administrator()
    {
        Assert.True(OrganizationManagementAuthority.IsExactOwner(OrganizationRole.OrganizationOwner));
        Assert.False(OrganizationManagementAuthority.IsExactOwner(OrganizationRole.OrganizationAdministrator));
    }
}
