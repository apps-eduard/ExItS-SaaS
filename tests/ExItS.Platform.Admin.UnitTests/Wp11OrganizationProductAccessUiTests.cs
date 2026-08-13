using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Admin.UnitTests;

/// <summary>
/// Source and terminology guards for Organization Product UX (P16-WP11).
/// </summary>
public sealed class Wp11OrganizationProductAccessUiTests
{
    [Fact]
    public void Enabled_products_ui_uses_cards_and_human_readable_title_fields()
    {
        var text = ReadAdminPage("OrganizationEnabledProducts.razor");
        Assert.Contains("ProductDisplayName", text, StringComparison.Ordinal);
        Assert.Contains("ProductTitle", text, StringComparison.Ordinal);
        Assert.Contains("OrgEnabledProducts_OpenProduct", text, StringComparison.Ordinal);
        Assert.Contains("OrgEnabledProducts_ManageStaffAccess", text, StringComparison.Ordinal);
        Assert.Contains("OrgEnabledProducts_SelectStaff", text, StringComparison.Ordinal);
        Assert.Contains("OrgEnabledProducts_SelectProduct", text, StringComparison.Ordinal);
        Assert.Contains("OrgEnabledProducts_SelectProductRole", text, StringComparison.Ordinal);
        Assert.Contains("POS Owner", text, StringComparison.Ordinal);
        Assert.Contains("Store Manager", text, StringComparison.Ordinal);
        Assert.Contains("Reporting User", text, StringComparison.Ordinal);
        Assert.Contains("<Card", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Placeholder=\"pinoy-business-pos\"", text, StringComparison.Ordinal);
        Assert.DoesNotContain("OrgMembers_UserId", text, StringComparison.Ordinal);
        Assert.DoesNotContain("OrgProductAccess_ProductCode", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Label=\"Owner\"", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Label=\"Manager\"", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Label=\"Viewer\"", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Label=\"POS Administrator\"", text, StringComparison.Ordinal);

        var resx = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src", "Platform", "ExItS.Platform.Admin", "Localization", "AdminResources.resx"));
        Assert.Contains("<value>Open Product</value>", resx, StringComparison.Ordinal);
        Assert.Contains("<value>Manage Staff Access</value>", resx, StringComparison.Ordinal);
        Assert.Contains("<value>Select Staff Member</value>", resx, StringComparison.Ordinal);
    }

    [Fact]
    public void Enabled_products_ui_hides_raw_guid_and_product_code_inputs()
    {
        var text = ReadAdminPage("OrganizationEnabledProducts.razor");
        Assert.DoesNotContain("_roleUserId", text, StringComparison.Ordinal);
        Assert.DoesNotContain("_roleProductCode", text, StringComparison.Ordinal);
        Assert.DoesNotContain("TryParse(_roleUserId", text, StringComparison.Ordinal);
        Assert.Contains("_selectedStaffUserId", text, StringComparison.Ordinal);
        Assert.Contains("_selectedProductKey", text, StringComparison.Ordinal);
        Assert.Contains("GetOrganizationMembersAsync", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Enabled_products_ui_shows_friendly_denial_not_raw_reason_code_as_primary()
    {
        var text = ReadAdminPage("OrganizationEnabledProducts.razor");
        Assert.Contains("DenialReasonDisplay", text, StringComparison.Ordinal);
        Assert.Contains("You do not have a role assigned for this Product.", text, StringComparison.Ordinal);
        Assert.DoesNotContain("b.AddContent(0, ctx.Item.ReasonCode)", text, StringComparison.Ordinal);
    }

    [Fact]
    public void My_products_nav_points_to_enabled_products_not_commercial_grants()
    {
        var nav = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src", "Platform", "ExItS.Platform.Admin", "Components", "Layout", "AdminNav.razor"));
        var page = ReadAdminPage("OrganizationEnabledProducts.razor");
        Assert.Contains("/admin/handoff/organization", nav, StringComparison.Ordinal);
        Assert.DoesNotContain("Nav_MyProducts", nav, StringComparison.Ordinal);
        Assert.Contains("@page \"/admin/organizations/{OrganizationId:guid}/enabled-products\"", page, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "RouterLink=\"@($\"/admin/organizations/{accessOrg}/product-access\")\"",
            nav,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Commercial_product_access_page_is_platform_permission_only()
    {
        var text = ReadAdminPage("OrganizationProductAccess.razor");
        Assert.Contains("ManageProductAccess", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IsOrganizationShell", text, StringComparison.Ordinal);
        Assert.Contains("Platform support tool", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Product_role_display_labels_remain_approved_pos_roles()
    {
        Assert.Equal("POS Owner", ProductRoleDisplay.ToDisplayLabel(ProductLocalRoleCodes.Owner));
        Assert.Equal("Store Manager", ProductRoleDisplay.ToDisplayLabel(ProductLocalRoleCodes.Manager));
        Assert.Equal("Cashier", ProductRoleDisplay.ToDisplayLabel(ProductLocalRoleCodes.Cashier));
        Assert.Equal("Reporting User", ProductRoleDisplay.ToDisplayLabel(ProductLocalRoleCodes.Viewer));
        Assert.Equal("Staff", OrganizationRoleDisplay.ToDisplayLabel(OrganizationRole.OrganizationMember));
        Assert.NotEqual(
            ProductRoleDisplay.ToDisplayLabel(ProductLocalRoleCodes.Cashier),
            OrganizationRoleDisplay.ToDisplayLabel(OrganizationRole.OrganizationMember));
        Assert.Contains("Pinoy Business POS", "Pinoy Business POS", StringComparison.Ordinal);
        Assert.Equal(ProductCode.PinoyBusinessPos, "pinoy-business-pos");
    }

    private static string ReadAdminPage(string fileName) =>
        File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", fileName));

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
