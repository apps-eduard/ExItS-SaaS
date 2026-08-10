namespace ExItS.Platform.Admin.UnitTests;

/// <summary>Guards for Phase 20 Organization Catalog Visibility Admin page.</summary>
public sealed class OrganizationCatalogVisibilityUiTests
{
    [Fact]
    public void Catalog_page_route_exists_and_gates_manage_organizations()
    {
        var text = ReadAdminPage("OrganizationCatalog.razor");
        Assert.Contains("@page \"/admin/organizations/{OrganizationId:guid}/catalog\"", text, StringComparison.Ordinal);
        Assert.Contains("ManageOrganizations", text, StringComparison.Ordinal);
        Assert.Contains("UnauthorizedPanel", text, StringComparison.Ordinal);
        Assert.Contains("GetOrganizationCatalogAsync", text, StringComparison.Ordinal);
        Assert.Contains("SourceBreakdown", text, StringComparison.Ordinal);
        Assert.Contains("SourceType", text, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CreateCatalogProduct", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Organizations_detail_links_to_catalog_page()
    {
        var text = ReadAdminPage("Organizations.razor");
        Assert.Contains("/catalog", text, StringComparison.Ordinal);
        Assert.Contains("Organizations_OpenCatalog", text, StringComparison.Ordinal);
        Assert.Contains("Organizations_TabCatalog", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Catalog_resx_keys_exist_in_en_and_fil()
    {
        var root = FindRepositoryRoot();
        var en = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Localization", "AdminResources.resx"));
        var fil = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Localization", "AdminResources.fil-PH.resx"));
        Assert.Contains("OrgCatalog_Title", en, StringComparison.Ordinal);
        Assert.Contains("OrgCatalog_Title", fil, StringComparison.Ordinal);
        Assert.Contains("Organizations_OpenCatalog", en, StringComparison.Ordinal);
        Assert.Contains("Organizations_OpenCatalog", fil, StringComparison.Ordinal);
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
