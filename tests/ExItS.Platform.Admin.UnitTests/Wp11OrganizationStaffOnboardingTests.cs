using ExItS.Platform.Admin.Models;

namespace ExItS.Platform.Admin.UnitTests;

public sealed class Wp11OrganizationStaffOnboardingTests
{
    [Fact]
    public void Organization_staff_page_uses_invite_form_not_guid_as_primary()
    {
        var members = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "OrganizationMembers.razor"));

        Assert.Contains("OrgMembers_InviteStaff", members, StringComparison.Ordinal);
        Assert.Contains("<StaffPersonFieldsForm", members, StringComparison.Ordinal);
        Assert.Contains("InviteStaffAsync", members, StringComparison.Ordinal);
        Assert.Contains("CreateOrganizationInvitationAsync", members, StringComparison.Ordinal);
        Assert.DoesNotContain("OrgMembers_AddMember", members, StringComparison.Ordinal);
        Assert.Contains("CanUseAdvancedIdentityLink", members, StringComparison.Ordinal);
        Assert.Contains("Shell.IsPlatformShell", members, StringComparison.Ordinal);
        Assert.Contains("LinkExistingIdentityAsync", members, StringComparison.Ordinal);
        Assert.Contains("_linkReason", members, StringComparison.Ordinal);
        Assert.Contains("OrgMembers_LinkExistingIdentity", members, StringComparison.Ordinal);
    }

    [Fact]
    public void Invite_staff_shared_fields_are_required_and_optional_fields_stay_optional()
    {
        var model = new StaffPersonFieldsModel();
        Assert.False(model.Validate().IsValid);

        model.FirstName = "Lia";
        model.LastName = "Cruz";
        model.Email = "lia.cruz@example.com";
        model.RequireEmailVerification = true;
        Assert.True(model.Validate().IsValid);
        Assert.Equal("Lia Cruz", model.DisplayName);

        model.Phone = "";
        model.EmployeeCode = "";
        Assert.True(model.Validate().IsValid);
    }

    [Fact]
    public void Advanced_guid_link_is_platform_only_and_requires_reason_in_ui()
    {
        var members = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "OrganizationMembers.razor"));
        Assert.Contains("CanUseAdvancedIdentityLink =>", members, StringComparison.Ordinal);
        Assert.Contains("ManageMemberships", members, StringComparison.Ordinal);
        Assert.Contains("OrgMembers_LinkReasonRequired", members, StringComparison.Ordinal);
        Assert.Contains("AddMemberRequest(userId, _linkRole, Reason: _linkReason.Trim())", members, StringComparison.Ordinal);
    }

    [Fact]
    public void Membership_api_denies_non_platform_guid_linking()
    {
        var endpoints = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src", "Platform", "ExItS.Platform.Api", "Organizations", "MembershipEndpoints.cs"));
        Assert.Contains("Linking an existing identity by User ID is restricted to Platform support", endpoints, StringComparison.Ordinal);
        Assert.Contains("HasPlatformManageMemberships", endpoints, StringComparison.Ordinal);
        Assert.Contains("Advanced identity link. Reason:", endpoints, StringComparison.Ordinal);
        Assert.Contains("Use Invite Staff instead", endpoints, StringComparison.Ordinal);
    }

    [Fact]
    public void Invitations_remain_on_separate_route_from_staff_table()
    {
        var root = FindRepositoryRoot();
        var members = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "OrganizationMembers.razor"));
        var invitations = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "OrganizationInvitations.razor"));

        Assert.DoesNotContain("@page \"/admin/organizations/{OrganizationId:guid}/invitations\"", members, StringComparison.Ordinal);
        Assert.Contains("@page \"/admin/organizations/{OrganizationId:guid}/invitations\"", invitations, StringComparison.Ordinal);
        Assert.Contains("Invitation status", invitations, StringComparison.Ordinal);
        Assert.Contains("/invitations", members, StringComparison.Ordinal);
        Assert.Contains("Product role", members, StringComparison.Ordinal);
        Assert.DoesNotContain("Member Member", members, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ExItS.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate ExItS.slnx.");
    }
}
