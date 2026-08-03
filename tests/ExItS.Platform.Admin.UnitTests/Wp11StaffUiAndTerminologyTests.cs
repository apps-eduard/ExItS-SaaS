using ExItS.Platform.Admin.Services;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Admin.UnitTests;

public sealed class Wp11StaffUiAndTerminologyTests
{
    [Fact]
    public void Invitations_nav_uses_dedicated_route_not_query_tab_only()
    {
        var orgId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var items = AdminAccountUserNav.OrganizationPeople(isOrganizationOwnerOrAdmin: true, orgId);
        var invitations = Assert.Single(items, i => i.Key == "org-invitations");
        Assert.Equal($"/admin/organizations/{orgId}/invitations", invitations.Route);
        Assert.DoesNotContain("tab=invitations", invitations.Route, StringComparison.Ordinal);
        Assert.Contains(items, i => i.Key == "org-staff" && i.Route == $"/admin/organizations/{orgId}/members");
    }

    [Fact]
    public void Organization_staff_and_invitations_use_separate_page_routes()
    {
        var root = FindRepositoryRoot();
        var members = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "OrganizationMembers.razor"));
        var invitations = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "OrganizationInvitations.razor"));

        Assert.Contains("@page \"/admin/organizations/{OrganizationId:guid}/members\"", members, StringComparison.Ordinal);
        Assert.DoesNotContain("@page \"/admin/organizations/{OrganizationId:guid}/invitations\"", members, StringComparison.Ordinal);
        Assert.Contains("@page \"/admin/organizations/{OrganizationId:guid}/invitations\"", invitations, StringComparison.Ordinal);
        Assert.Contains("ProductRoles", members, StringComparison.Ordinal);
        Assert.Contains("Invitation status", invitations, StringComparison.Ordinal);
        Assert.DoesNotContain("<Tabs", members, StringComparison.Ordinal);
        Assert.DoesNotContain("Member Member", members, StringComparison.Ordinal);
        Assert.DoesNotContain("Member Member", invitations, StringComparison.Ordinal);
    }

    [Fact]
    public void Organization_role_display_never_returns_Member_label()
    {
        Assert.Equal("Staff", OrganizationRoleDisplay.ToDisplayLabel(OrganizationRole.OrganizationMember));
        Assert.Equal("Owner", OrganizationRoleDisplay.ToDisplayLabel(OrganizationRole.OrganizationOwner));
        Assert.NotEqual("Member", OrganizationRoleDisplay.ToDisplayLabel("OrganizationMember"));
        Assert.DoesNotContain("Member", OrganizationRoleDisplay.ToDisplayLabel(OrganizationRole.OrganizationMember), StringComparison.Ordinal);
    }

    [Fact]
    public void Product_role_display_is_separate_from_organization_role()
    {
        Assert.Equal("POS Owner", ProductRoleDisplay.ToDisplayLabel(ProductLocalRoleCodes.Owner));
        Assert.Equal("Store Manager", ProductRoleDisplay.ToDisplayLabel(ProductLocalRoleCodes.Manager));
        Assert.Equal("Cashier", ProductRoleDisplay.ToDisplayLabel(ProductLocalRoleCodes.Cashier));
        Assert.Equal("Reporting User", ProductRoleDisplay.ToDisplayLabel(ProductLocalRoleCodes.Viewer));
        Assert.Equal("Staff", OrganizationRoleDisplay.ToDisplayLabel(OrganizationRole.OrganizationMember));
        Assert.NotEqual(
            OrganizationRoleDisplay.ToDisplayLabel(OrganizationRole.OrganizationMember),
            ProductRoleDisplay.ToDisplayLabel(ProductLocalRoleCodes.Cashier));
    }

    [Fact]
    public void Enabled_products_ui_shows_display_name_without_raw_code_in_cell()
    {
        var path = Path.Combine(
            FindRepositoryRoot(),
            "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "OrganizationEnabledProducts.razor");
        var text = File.ReadAllText(path);
        Assert.DoesNotContain("DisplayName} ({ctx.Item.ProductCode})", text, StringComparison.Ordinal);
        Assert.Contains("Pinoy Business POS", File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src", "Platform", "ExItS.Platform.Application", "LocalValidation", "InitializeLocalValidationDataset.cs")), StringComparison.Ordinal);
        Assert.Contains("pinoy-business-pos", ProductCode.PinoyBusinessPos, StringComparison.Ordinal);
    }

    [Fact]
    public void Discover_enabled_products_deduplicates_by_product_id()
    {
        var path = Path.Combine(
            FindRepositoryRoot(),
            "src", "Platform", "ExItS.Platform.Application", "Access", "AccessUseCases.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("seenProductIds", text, StringComparison.Ordinal);
        Assert.Contains("GroupBy", text, StringComparison.Ordinal);
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
