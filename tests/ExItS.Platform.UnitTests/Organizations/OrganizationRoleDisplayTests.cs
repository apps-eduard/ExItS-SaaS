using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class OrganizationRoleDisplayTests
{
    [Theory]
    [InlineData(OrganizationRole.OrganizationOwner, "Owner")]
    [InlineData(OrganizationRole.OrganizationMember, "Staff")]
    [InlineData(OrganizationRole.OrganizationAdministrator, "Administrator")]
    public void Maps_persisted_roles_to_business_labels(OrganizationRole role, string expected) =>
        Assert.Equal(expected, OrganizationRoleDisplay.ToDisplayLabel(role));

    [Theory]
    [InlineData("OrganizationOwner", "Owner")]
    [InlineData("OrganizationMember", "Staff")]
    [InlineData("organizationmember", "Staff")]
    public void Maps_role_codes_to_business_labels(string code, string expected) =>
        Assert.Equal(expected, OrganizationRoleDisplay.ToDisplayLabel(code));

    [Fact]
    public void Assignable_staff_roles_are_owner_and_staff_only()
    {
        Assert.True(OrganizationRoleDisplay.IsAssignableOrganizationStaffRole(OrganizationRole.OrganizationOwner));
        Assert.True(OrganizationRoleDisplay.IsAssignableOrganizationStaffRole(OrganizationRole.OrganizationMember));
        Assert.False(OrganizationRoleDisplay.IsAssignableOrganizationStaffRole(OrganizationRole.OrganizationAdministrator));
    }
}
