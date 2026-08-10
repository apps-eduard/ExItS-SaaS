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
    public void Imports_page_exposes_template_aware_confirm_destination()
    {
        var root = FindRepositoryRoot();
        var page = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "GlobalCatalogImports.razor"));
        var client = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Services", "PlatformApiClient.cs"));
        var iface = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Services", "IPlatformApiClient.cs"));
        var dtos = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Models", "PlatformDtos.cs"));
        var resx = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Localization", "AdminResources.resx"));

        Assert.Contains("GlobalImports_Destination", page, StringComparison.Ordinal);
        Assert.Contains("GlobalImports_GlobalOnly", page, StringComparison.Ordinal);
        Assert.Contains("GlobalImports_GlobalPlusTemplate", page, StringComparison.Ordinal);
        Assert.Contains("GlobalImports_ConfirmPreview", page, StringComparison.Ordinal);
        Assert.Contains("GetCatalogTemplatesAsync", page, StringComparison.Ordinal);
        Assert.Contains("ConfirmCatalogImportAsync(Id.Value, targetTemplateId)", page, StringComparison.Ordinal);
        Assert.Contains("ConfirmDisabled", page, StringComparison.Ordinal);

        Assert.Contains("Guid? targetTemplateId = null", iface, StringComparison.Ordinal);
        Assert.Contains("Guid? targetTemplateId = null", client, StringComparison.Ordinal);
        Assert.Contains("new { targetTemplateId }", client, StringComparison.Ordinal);

        Assert.Contains("TargetTemplateId", dtos, StringComparison.Ordinal);
        Assert.Contains("TargetTemplateName", dtos, StringComparison.Ordinal);
        Assert.Contains("EstimatedTemplateLinks", dtos, StringComparison.Ordinal);
        Assert.Contains("ProductsAlreadyInTemplate", dtos, StringComparison.Ordinal);
        Assert.Contains("ConfirmCatalogImportRequest", dtos, StringComparison.Ordinal);

        Assert.Contains("GlobalImports_Destination", resx, StringComparison.Ordinal);
        Assert.Contains("GlobalImports_GlobalOnly", resx, StringComparison.Ordinal);
        Assert.Contains("GlobalImports_GlobalPlusTemplate", resx, StringComparison.Ordinal);
        Assert.Contains("GlobalImports_Template", resx, StringComparison.Ordinal);
        Assert.Contains("GlobalImports_ConfirmPreview", resx, StringComparison.Ordinal);
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
