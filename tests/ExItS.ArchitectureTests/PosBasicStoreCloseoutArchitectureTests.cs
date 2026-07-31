namespace ExItS.ArchitectureTests;

/// <summary>
/// P8-WP07 closeout: cross-cutting Basic Store deferred-scope and startup safety guards.
/// </summary>
public sealed class PosBasicStoreCloseoutArchitectureTests
{
    private static readonly string[] DeferredConcepts =
    [
        "PurchaseOrderService",
        "SupplierCatalog",
        "WarehouseTransfer",
        "CostOfGoodsSoldCalculator",
        "ProfitAndLossStatement",
        "TaxInvoiceIssuer",
        "PaymentGatewayClient",
        "GCashVerificationClient",
        "OfflineSaleQueue",
        "OfflineCatalogCache",
        "OfflineInventorySync",
        "AuthoritativeOfflineReport",
        "StoreReportsExport"
    ];

    [Fact]
    public void Pos_program_does_not_call_database_migrate()
    {
        var program = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Api"), "Program.cs"));
        Assert.DoesNotContain(".Migrate(", program, StringComparison.Ordinal);
        Assert.DoesNotContain(".MigrateAsync(", program, StringComparison.Ordinal);
        Assert.Contains("P10-WP05-returns-refunds", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Basic_store_application_and_api_declare_no_deferred_capability_concepts()
    {
        foreach (var root in new[]
                 {
                     Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Application")),
                     Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Api")),
                     Path.Combine(PosProject("ExItS.PinoyBusinessPOS.ApiClient")),
                     Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Domain"))
                 })
        {
            foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                var text = File.ReadAllText(file);
                foreach (var concept in DeferredConcepts)
                {
                    Assert.DoesNotContain(concept, text, StringComparison.OrdinalIgnoreCase);
                }
            }
        }
    }

    [Fact]
    public void Pos_dbcontext_has_no_deferred_or_report_cache_tables()
    {
        var context = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Infrastructure"), "Persistence", "PosDbContext.cs"));

        foreach (var table in new[]
                 {
                     "\"warehouses\"", "\"tax_invoices\"",
                     "\"dashboard_totals\"", "\"report_snapshots\"", "\"daily_aggregates\"",
                     "\"general_ledger\"", "\"journal_entries\"", "\"supplier_invoices\"",
                     "\"accounts_payable\"", "\"supplier_payments\""
                 })
        {
            Assert.DoesNotContain(table, context, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("\"suppliers\"", context, StringComparison.Ordinal);
        Assert.Contains("\"supplier_code_sequences\"", context, StringComparison.Ordinal);
    }

    [Fact]
    public void Feature_codes_do_not_declare_store_reports_export()
    {
        var platform = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(), "src", "Platform", "ExItS.Platform.Domain", "Catalog", "FeatureCode.cs"));
        var pos = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Application"), "Commercial", "UtangCapabilityPolicy.cs"));

        Assert.DoesNotContain("store-reports-export", platform, StringComparison.Ordinal);
        Assert.DoesNotContain("store-reports-export", pos, StringComparison.Ordinal);
        Assert.Contains("store-dashboard-view", platform, StringComparison.Ordinal);
        Assert.Contains("store-reports-view", platform, StringComparison.Ordinal);
    }

    [Fact]
    public void Platform_and_pos_store_feature_code_literals_match()
    {
        var platform = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(), "src", "Platform", "ExItS.Platform.Domain", "Catalog", "FeatureCode.cs"));
        var pos = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Application"), "Commercial", "UtangCapabilityPolicy.cs"));

        foreach (var code in new[]
                 {
                     "store-catalog-view",
                     "store-catalog-manage",
                     "store-sales-view",
                     "store-sales-create",
                     "store-sales-void",
                     "store-inventory-view",
                     "store-inventory-manage",
                     "store-expenses-view",
                     "store-expenses-manage",
                     "store-dashboard-view",
                     "store-reports-view"
                 })
        {
            Assert.Contains($"\"{code}\"", platform, StringComparison.Ordinal);
            Assert.Contains($"\"{code}\"", pos, StringComparison.Ordinal);
        }
    }

    private static string PosProject(string name)
    {
        var root = FindRepositoryRoot();
        return Path.Combine(root, "src", "Products", "PinoyBusinessPOS", name);
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

        throw new InvalidOperationException("Repository root not found.");
    }
}
