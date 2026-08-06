namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class CatalogImportWizardTests
{
    [Fact]
    public void SelectTemplateAsync_marshals_ui_state_without_configure_await_false()
    {
        var import = ReadCatalogPage("CatalogImport.razor");

        Assert.Contains("SelectTemplateAsync", import, StringComparison.Ordinal);
        Assert.Contains("StateHasChanged()", import, StringComparison.Ordinal);
        var selectMethod = ExtractMethod(import, "SelectTemplateAsync");
        Assert.DoesNotContain("ConfigureAwait(false)", selectMethod, StringComparison.Ordinal);
    }

    [Fact]
    public void Preview_builds_from_enriched_template_products_without_per_product_fetch()
    {
        var import = ReadCatalogPage("CatalogImport.razor");

        Assert.Contains("_previewProducts = result.Data.Products", import, StringComparison.Ordinal);
        Assert.Contains(".Where(p => p.IsFirstBatch)", import, StringComparison.Ordinal);
        Assert.DoesNotContain("GetActiveProductAsync", import, StringComparison.Ordinal);
        Assert.Contains("CostPrice", import, StringComparison.Ordinal);
        Assert.Contains("SellingPrice", import, StringComparison.Ordinal);
        Assert.Contains("Brand", import, StringComparison.Ordinal);
    }

    [Fact]
    public void Confirm_step_requires_acknowledgement_and_stable_idempotency_key()
    {
        var import = ReadCatalogPage("CatalogImport.razor");

        Assert.Contains("_confirmAcknowledged", import, StringComparison.Ordinal);
        Assert.Contains("_confirmError", import, StringComparison.Ordinal);
        Assert.Contains("Catalog_Import_ConfirmAcknowledge", import, StringComparison.Ordinal);
        Assert.Contains("Catalog_Import_ConfirmRequired", import, StringComparison.Ordinal);
        Assert.Contains("_confirmIdempotencyKey", import, StringComparison.Ordinal);
        Assert.Contains("ImportTemplateBatchAsync", import, StringComparison.Ordinal);
        Assert.Contains("IsInvalid=\"@(!string.IsNullOrEmpty(_confirmError))\"", import, StringComparison.Ordinal);
        Assert.Contains("ErrorText=\"@_confirmError\"", import, StringComparison.Ordinal);
        Assert.Contains("Disabled=\"@(_isOffline || _busy)\"", import, StringComparison.Ordinal);
        Assert.DoesNotContain("Disabled=\"@(_isOffline || _busy || !_confirmAcknowledged)\"", import, StringComparison.Ordinal);

        var confirmMethod = ExtractMethod(import, "ConfirmImportAsync");
        Assert.Contains("if (!_confirmAcknowledged)", confirmMethod, StringComparison.Ordinal);
        Assert.Contains("Catalog_Import_ConfirmRequired", confirmMethod, StringComparison.Ordinal);
        Assert.Contains("ImportTemplateBatchAsync", confirmMethod, StringComparison.Ordinal);
        Assert.Contains("finally", confirmMethod, StringComparison.Ordinal);
    }

    [Fact]
    public void Confirm_ack_toggle_clears_stale_field_error()
    {
        var import = ReadCatalogPage("CatalogImport.razor");
        var handler = ExtractMethod(import, "OnConfirmAcknowledgedChanged");
        Assert.Contains("_confirmAcknowledged = value", handler, StringComparison.Ordinal);
        Assert.Contains("_confirmError = null", handler, StringComparison.Ordinal);
    }

    [Fact]
    public void Import_pipeline_forwards_platform_session_not_bearer_to_merchant_catalog()
    {
        var root = FindRepoRoot();
        Assert.Contains("PosPlatformSessionForwardingHandler", File.ReadAllText(Path.Combine(
            root,
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.ApiClient",
            "DependencyInjection.cs")), StringComparison.Ordinal);
        Assert.Contains("PlatformSession", File.ReadAllText(Path.Combine(
            root,
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Api",
            "Catalog",
            "PlatformMerchantCatalogClient.cs")), StringComparison.Ordinal);
        Assert.Contains("ExtractPlatformSessionToken", File.ReadAllText(Path.Combine(
            root,
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Api",
            "Catalog",
            "CatalogImportEndpoints.cs")), StringComparison.Ordinal);
    }

    [Fact]
    public void Import_wizard_localization_keys_exist_in_english_and_filipino()
    {
        var root = FindRepoRoot();
        var loc = Path.Combine(root, "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "Localization");
        var en = File.ReadAllText(Path.Combine(loc, "PosResources.resx"));
        var fil = File.ReadAllText(Path.Combine(loc, "PosResources.fil-PH.resx"));

        foreach (var key in new[]
                 {
                     "Catalog_Import_ConfirmAcknowledge",
                     "Catalog_Import_ConfirmRequired",
                     "Catalog_Import_Selected",
                     "Catalog_Import_PreviewSearch",
                     "Catalog_Import_UnavailableProduct",
                     "Catalog_Import_UnavailableProductCount",
                     "Catalog_Import_GoToProducts",
                     "Catalog_Import_CostPrice",
                     "Catalog_Import_SellingPrice",
                     "Catalog_Import_Brand",
                     "Catalog_Import_Category",
                     "Catalog_Import_Unit",
                     "Catalog_Import_Description"
                 })
        {
            Assert.Contains($"name=\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"name=\"{key}\"", fil, StringComparison.Ordinal);
        }

        Assert.Contains("Confirm that you understand before starting the import.", en, StringComparison.Ordinal);
        Assert.Contains("Kumpirmahin na nauunawaan mo bago simulan ang import.", fil, StringComparison.Ordinal);
    }

    [Fact]
    public void Choose_step_marks_selected_template_and_disables_while_busy()
    {
        var import = ReadCatalogPage("CatalogImport.razor");

        Assert.Contains("_selectedTemplateId", import, StringComparison.Ordinal);
        Assert.Contains("pos-row--selected", import, StringComparison.Ordinal);
        Assert.Contains("Catalog_Import_Selected", import, StringComparison.Ordinal);
        Assert.Contains("if (_busy)", import, StringComparison.Ordinal);
        Assert.Contains("Disabled=\"@(_isOffline || _busy)\"", import, StringComparison.Ordinal);
    }

    [Fact]
    public void Import_job_page_offers_go_to_products_and_auto_navigate_on_success()
    {
        var job = ReadCatalogPage("CatalogImportJob.razor");

        Assert.Contains("Catalog_Import_GoToProducts", job, StringComparison.Ordinal);
        Assert.Contains("Nav.NavigateTo(\"/catalog\")", job, StringComparison.Ordinal);
        Assert.Contains("IsSuccessTerminal", job, StringComparison.Ordinal);
        Assert.Contains("Task.Delay(1500", job, StringComparison.Ordinal);
        Assert.Contains("GoReview", job, StringComparison.Ordinal);
    }

    [Fact]
    public void Merchant_global_product_dto_uses_cost_and_selling_price()
    {
        var root = FindRepoRoot();
        var contracts = File.ReadAllText(Path.Combine(
            root,
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Application",
            "Catalog",
            "CatalogImportContracts.cs"));

        var globalStart = contracts.IndexOf("public sealed record PlatformMerchantGlobalProductDto(", StringComparison.Ordinal);
        Assert.True(globalStart >= 0);
        var globalEnd = contracts.IndexOf("public sealed record PlatformMerchantGlobalCategoryDto(", StringComparison.Ordinal);
        var globalBlock = contracts[globalStart..globalEnd];
        Assert.Contains("CostPrice", globalBlock, StringComparison.Ordinal);
        Assert.Contains("SellingPrice", globalBlock, StringComparison.Ordinal);
        Assert.Contains("Brand", globalBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("SuggestedPrice", globalBlock, StringComparison.Ordinal);

        var templateStart = contracts.IndexOf("public sealed record PlatformMerchantCatalogTemplateProductDto(", StringComparison.Ordinal);
        var templateEnd = contracts.IndexOf("public sealed record PlatformMerchantCatalogTemplateSummaryDto(", StringComparison.Ordinal);
        var templateBlock = contracts[templateStart..templateEnd];
        Assert.Contains("ProductName", templateBlock, StringComparison.Ordinal);
        Assert.Contains("Brand", templateBlock, StringComparison.Ordinal);
        Assert.Contains("CostPrice", templateBlock, StringComparison.Ordinal);
        Assert.Contains("SellingPrice", templateBlock, StringComparison.Ordinal);
    }

    private static string ReadCatalogPage(string fileName) =>
        File.ReadAllText(Path.Combine(CatalogPagesDirectory(), fileName));

    private static string CatalogPagesDirectory() => Path.Combine(
        FindRepoRoot(),
        "src",
        "Products",
        "PinoyBusinessPOS",
        "ExItS.PinoyBusinessPOS.Maui",
        "Components",
        "Pages",
        "Catalog");

    private static string ExtractMethod(string source, string methodName)
    {
        var start = source.IndexOf($"Task {methodName}", StringComparison.Ordinal);
        if (start < 0)
        {
            start = source.IndexOf($"void {methodName}", StringComparison.Ordinal);
        }

        Assert.True(start >= 0, $"Method {methodName} not found.");
        var nextMethod = source.IndexOf("\n    private ", start + 1, StringComparison.Ordinal);
        return nextMethod < 0 ? source[start..] : source[start..nextMethod];
    }

    private static string FindRepoRoot()
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

        throw new InvalidOperationException("Repository root not found.");
    }
}
