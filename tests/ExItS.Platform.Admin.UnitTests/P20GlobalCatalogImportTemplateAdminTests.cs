namespace ExItS.Platform.Admin.UnitTests;

public sealed class P20GlobalCatalogImportTemplateAdminTests
{
    [Fact]
    public void Imports_page_exposes_download_template_and_instructions()
    {
        var root = FindRepositoryRoot();
        var page = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "GlobalCatalogImports.razor"));
        var client = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Services", "PlatformApiClient.cs"));
        var resx = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Localization", "AdminResources.resx"));

        Assert.Contains("@page \"/admin/global-catalog/imports\"", page, StringComparison.Ordinal);
        Assert.Contains("DownloadTemplateAsync", page, StringComparison.Ordinal);
        Assert.Contains("GlobalImports_DownloadTemplate", page, StringComparison.Ordinal);
        Assert.Contains("GlobalImports_TemplateInstructions", page, StringComparison.Ordinal);
        Assert.Contains("exits-global-product-import-template.csv", page, StringComparison.Ordinal);
        Assert.Contains("DownloadCatalogImportTemplateAsync", client, StringComparison.Ordinal);
        Assert.Contains("/products/imports/template.csv", client, StringComparison.Ordinal);
        Assert.Contains("GlobalImports_DownloadTemplate", resx, StringComparison.Ordinal);
        Assert.Contains("GlobalImports_TemplateInstructions", resx, StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_nav_exposes_imports_when_permitted()
    {
        var root = FindRepositoryRoot();
        var nav = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Layout", "AdminNav.razor"));
        Assert.Contains("/admin/global-catalog/imports", nav, StringComparison.Ordinal);
        Assert.Contains("ImportGlobalProducts", nav, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ExItS.slnx"))
                || Directory.Exists(Path.Combine(dir.FullName, "src", "Platform")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
