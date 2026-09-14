using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class B2bBusinessContactPrivacyTests
{
    [Fact]
    public void Owner_create_defaults_IsBusinessContact_true()
    {
        var membership = OrganizationMembership.Create(
            PlatformOrganizationId.New(),
            PlatformUserId.New(),
            OrganizationRole.OrganizationOwner,
            DateTimeOffset.UtcNow);

        Assert.True(membership.IsBusinessContact);
    }

    [Fact]
    public void Staff_create_defaults_IsBusinessContact_false()
    {
        var membership = OrganizationMembership.Create(
            PlatformOrganizationId.New(),
            PlatformUserId.New(),
            OrganizationRole.OrganizationMember,
            DateTimeOffset.UtcNow);

        Assert.False(membership.IsBusinessContact);
    }

    [Fact]
    public void Managed_business_profile_stores_work_contact_not_user_identity()
    {
        var membership = OrganizationMembership.Create(
            PlatformOrganizationId.New(),
            PlatformUserId.New(),
            OrganizationRole.OrganizationMember,
            DateTimeOffset.UtcNow);

        membership.UpdateManagedBusinessProfile(
            department: "Purchasing",
            jobTitle: "Purchasing Manager",
            workPhone: "+63 917 123 4567",
            workEmail: "maria@paulcoffee.example",
            isBusinessContact: true,
            utcNow: DateTimeOffset.UtcNow);

        Assert.Equal("Purchasing", membership.Department);
        Assert.Equal("Purchasing Manager", membership.JobTitle);
        Assert.Equal("+63 917 123 4567", membership.WorkPhone);
        Assert.Equal("maria@paulcoffee.example", membership.WorkEmail);
        Assert.True(membership.IsBusinessContact);
    }

    [Fact]
    public void Own_business_contact_cannot_change_department_or_title()
    {
        var membership = OrganizationMembership.Create(
            PlatformOrganizationId.New(),
            PlatformUserId.New(),
            OrganizationRole.OrganizationMember,
            DateTimeOffset.UtcNow);

        membership.UpdateManagedBusinessProfile(
            "Purchasing",
            "Buyer",
            null,
            null,
            false,
            DateTimeOffset.UtcNow);

        membership.UpdateOwnBusinessContact(
            workPhone: "09171234567",
            workEmail: "work@example.com",
            isBusinessContact: true,
            utcNow: DateTimeOffset.UtcNow);

        Assert.Equal("Purchasing", membership.Department);
        Assert.Equal("Buyer", membership.JobTitle);
        Assert.Equal("09171234567", membership.WorkPhone);
        Assert.Equal("work@example.com", membership.WorkEmail);
        Assert.True(membership.IsBusinessContact);
    }

    [Fact]
    public void ChangeRole_does_not_clear_business_profile()
    {
        var membership = OrganizationMembership.Create(
            PlatformOrganizationId.New(),
            PlatformUserId.New(),
            OrganizationRole.OrganizationMember,
            DateTimeOffset.UtcNow);
        membership.UpdateManagedBusinessProfile(
            "Ops",
            "Lead",
            "09171111111",
            "ops@example.com",
            true,
            DateTimeOffset.UtcNow);

        membership.ChangeRole(OrganizationRole.OrganizationAdministrator, DateTimeOffset.UtcNow);

        Assert.Equal(OrganizationRole.OrganizationAdministrator, membership.Role);
        Assert.Equal("Ops", membership.Department);
        Assert.Equal("Lead", membership.JobTitle);
        Assert.True(membership.IsBusinessContact);
    }
}
