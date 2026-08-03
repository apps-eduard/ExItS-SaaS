using ExItS.Platform.Admin.Models;
using ExItS.Platform.Admin.Services;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Admin.UnitTests;

public sealed class Wp11SharedStaffFieldsAndInvitationsRouteTests
{
    [Fact]
    public void Platform_and_organization_forms_share_staff_person_fields_component()
    {
        var root = FindRepositoryRoot();
        var sharedForm = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Shared", "StaffPersonFieldsForm.razor"));
        var users = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "Users.razor"));
        var invitations = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "OrganizationInvitations.razor"));
        var members = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "OrganizationMembers.razor"));

        Assert.Contains("StaffPersonFieldsModel", sharedForm, StringComparison.Ordinal);
        Assert.Contains("FirstName", sharedForm, StringComparison.Ordinal);
        Assert.Contains("LastName", sharedForm, StringComparison.Ordinal);
        Assert.Contains("DisplayName", sharedForm, StringComparison.Ordinal);
        Assert.Contains("EmployeeCode", sharedForm, StringComparison.Ordinal);
        Assert.Contains("RequireEmailVerification", sharedForm, StringComparison.Ordinal);
        Assert.Contains("<StaffPersonFieldsForm", users, StringComparison.Ordinal);
        Assert.Contains("<StaffPersonFieldsForm", invitations, StringComparison.Ordinal);
        Assert.Contains("Users_PlatformRole", users, StringComparison.Ordinal);
        Assert.Contains("OrgMembers_OrgRole", invitations, StringComparison.Ordinal);
        Assert.DoesNotContain("@page \"/admin/organizations/{OrganizationId:guid}/invitations\"", members, StringComparison.Ordinal);
        Assert.Contains("@page \"/admin/organizations/{OrganizationId:guid}/invitations\"", invitations, StringComparison.Ordinal);
        Assert.Contains("Invitation status", invitations, StringComparison.Ordinal);
        Assert.DoesNotContain("Account status", invitations, StringComparison.Ordinal);
    }

    [Fact]
    public void Staff_and_invitations_nav_use_separate_routes()
    {
        var orgId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var items = AdminAccountUserNav.OrganizationPeople(true, orgId);
        Assert.Equal($"/admin/organizations/{orgId}/members", items.Single(i => i.Key == "org-staff").Route);
        Assert.Equal($"/admin/organizations/{orgId}/invitations", items.Single(i => i.Key == "org-invitations").Route);
        Assert.DoesNotContain("tab=invitations", items.Single(i => i.Key == "org-invitations").Route);
    }

    [Fact]
    public void Invitations_page_reloads_when_organization_context_changes()
    {
        var invitations = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "OrganizationInvitations.razor"));
        Assert.Contains("_loadedOrganizationId != OrganizationId", invitations, StringComparison.Ordinal);
        Assert.Contains("OnParametersSetAsync", invitations, StringComparison.Ordinal);
        Assert.Contains("ReloadInvitationsAsync", invitations, StringComparison.Ordinal);
    }

    [Fact]
    public void Shared_staff_person_model_requires_core_fields_and_platform_role_is_separate()
    {
        var model = new StaffPersonFieldsModel();
        var result = model.Validate();
        Assert.False(result.IsValid);
        Assert.NotNull(result.Errors.FirstName);
        Assert.NotNull(result.Errors.LastName);
        Assert.NotNull(result.Errors.Email);

        var users = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "Users.razor"));
        Assert.Contains("Users_PlatformRoleRequired", users, StringComparison.Ordinal);
    }

    [Fact]
    public void Shared_staff_person_model_defaults_display_name_and_allows_optional_phone_and_employee_code()
    {
        var model = new StaffPersonFieldsModel
        {
            FirstName = "Maria",
            LastName = "Santos",
            Email = "maria@example.com",
            Phone = "",
            EmployeeCode = ""
        };
        var result = model.Validate();
        Assert.True(result.IsValid);
        Assert.Equal("Maria Santos", model.DisplayName);
    }

    [Fact]
    public void Shared_staff_person_model_rejects_overlong_phone()
    {
        var model = new StaffPersonFieldsModel
        {
            FirstName = "Maria",
            LastName = "Santos",
            Email = "maria@example.com",
            Phone = new string('1', 33)
        };
        var result = model.Validate();
        Assert.False(result.IsValid);
        Assert.NotNull(result.Errors.Phone);
    }

    [Fact]
    public void Organization_role_and_product_role_remain_separate_and_never_Member()
    {
        Assert.Equal("Staff", OrganizationRoleDisplay.ToDisplayLabel(OrganizationRole.OrganizationMember));
        Assert.DoesNotContain("Member", OrganizationRoleDisplay.ToDisplayLabel(OrganizationRole.OrganizationMember));

        var invitations = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "OrganizationInvitations.razor"));
        Assert.Contains("OrgMembers_OrgRole", invitations, StringComparison.Ordinal);
        Assert.Contains("Product role", invitations, StringComparison.Ordinal);
        Assert.Contains("OrganizationMember", invitations, StringComparison.Ordinal);
        Assert.Contains("OrganizationOwner", invitations, StringComparison.Ordinal);
        Assert.DoesNotContain("Account status", invitations, StringComparison.Ordinal);
    }

    [Fact]
    public void Organization_staff_table_shows_org_role_once_and_product_role_separately()
    {
        var members = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "OrganizationMembers.razor"));
        Assert.Contains("OrgMembers_OrgRole", members, StringComparison.Ordinal);
        Assert.Contains("Product role", members, StringComparison.Ordinal);
        Assert.Contains("Account status", members, StringComparison.Ordinal);
        Assert.Contains("Membership status", members, StringComparison.Ordinal);
        Assert.DoesNotContain("Member Member", members, StringComparison.Ordinal);
        Assert.DoesNotContain("@page \"/admin/organizations/{OrganizationId:guid}/invitations\"", members, StringComparison.Ordinal);
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
