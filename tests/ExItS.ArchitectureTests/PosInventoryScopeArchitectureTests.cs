namespace ExItS.ArchitectureTests;

/// <summary>
/// Guards P8-WP04 basic inventory: online-only stock accounts and movements may exist, but
/// suppliers, warehouses, costing, offline inventory queues, and negative-stock overrides must not.
/// </summary>
public sealed class PosInventoryScopeArchitectureTests
{
    private static readonly string[] OutOfScopeConcepts =
    [
        "SupplierId",
        "PurchaseOrder",
        "WarehouseId",
        "WarehouseBin",
        "SerialNumber",
        "ExpiryDate",
        "CostPrice",
        "AverageCost",
        "ValuationMethod",
        "NegativeStockOverride",
        "IOfflineInventory",
        "InventoryOfflineDispatcher",
        "LocalInventory"
    ];

    [Fact]
    public void Inventory_slice_declares_no_supplier_warehouse_costing_or_offline_queue_concepts()
    {
        foreach (var file in InventorySourceFiles())
        {
            var text = File.ReadAllText(file);
            foreach (var concept in OutOfScopeConcepts)
            {
                Assert.DoesNotContain(concept, text, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void Inventory_domain_stays_persistence_and_http_independent()
    {
        var domain = Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Domain"), "Inventory");
        Assert.True(Directory.Exists(domain), domain);

        foreach (var file in Directory.EnumerateFiles(domain, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("Microsoft.EntityFrameworkCore", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Npgsql", text, StringComparison.Ordinal);
            Assert.DoesNotContain("System.Net.Http", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Infrastructure", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Inventory_persistence_adds_account_movement_and_advanced_tables()
    {
        var context = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Infrastructure"), "Persistence", "PosDbContext.cs"));

        Assert.Contains("\"inventory_accounts\"", context, StringComparison.Ordinal);
        Assert.Contains("\"stock_movements\"", context, StringComparison.Ordinal);
        Assert.Contains("\"inventory_reorder_changes\"", context, StringComparison.Ordinal);
        Assert.Contains("\"stock_counts\"", context, StringComparison.Ordinal);
        Assert.Contains("\"stock_count_lines\"", context, StringComparison.Ordinal);
        Assert.Contains("\"inventory_transfers\"", context, StringComparison.Ordinal);
        Assert.Contains("\"inventory_transfer_lines\"", context, StringComparison.Ordinal);
        Assert.Contains("\"inventory_branch_balances\"", context, StringComparison.Ordinal);
        Assert.Contains("\"inventory_lots\"", context, StringComparison.Ordinal);
        Assert.Contains("\"inventory_lot_movements\"", context, StringComparison.Ordinal);

        foreach (var table in new[]
                 {
                     "\"warehouses\"", "\"stock_transfers\"",
                     "\"inventory_costs\"", "\"serials\""
                 })
        {
            Assert.DoesNotContain(table, context, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Inventory_api_and_client_expose_no_offline_queue_surface()
    {
        var endpoints = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Api"), "Inventory", "InventoryEndpoints.cs"));
        var client = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.ApiClient"), "PosInventoryClient.cs"));

        foreach (var text in new[] { endpoints, client })
        {
            Assert.DoesNotContain("IOfflineOperationDispatcher", text, StringComparison.Ordinal);
            Assert.DoesNotContain("IOfflineOperationQueue", text, StringComparison.Ordinal);
            Assert.DoesNotContain("LocalStore", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Enqueue", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// AREA02-17: areas group, navigate and report only. Stock authority stays on branch balances and
    /// the organization inventory account, so no persisted area-stock type or table may appear.
    /// </summary>
    [Fact]
    public void Areas_hold_no_inventory_write_authority()
    {
        foreach (var concept in new[]
                 {
                     "AreaInventoryBalance", "InventoryAreaBalance", "AreaStockBalance",
                     "AreaInventoryAccount", "AreaStockLevel"
                 })
        {
            foreach (var file in InventorySourceFiles())
            {
                Assert.DoesNotContain(concept, File.ReadAllText(file), StringComparison.OrdinalIgnoreCase);
            }
        }

        var domain = Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Domain"), "Inventory");
        foreach (var file in Directory.EnumerateFiles(domain, "*.cs", SearchOption.AllDirectories))
        {
            Assert.DoesNotContain("Area", File.ReadAllText(file), StringComparison.Ordinal);
        }

        var context = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Infrastructure"), "Persistence", "PosDbContext.cs"));
        foreach (var table in new[] { "area_inventory", "inventory_area", "area_stock", "area_balances" })
        {
            Assert.DoesNotContain(table, context, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// AREA02-17: the hierarchical rollup is a read projection. Area subtotals are summed per request
    /// and never written back through a repository.
    /// </summary>
    [Fact]
    public void Area_stock_rollup_stays_a_read_projection()
    {
        var rollup = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Application"), "Inventory", "InventoryStockRollupQuery.cs"));

        Assert.Contains("ListByProductIdsAsync", rollup, StringComparison.Ordinal);
        Assert.DoesNotContain("UpsertAsync", rollup, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChanges", rollup, StringComparison.Ordinal);
        Assert.DoesNotContain("AddAsync", rollup, StringComparison.Ordinal);
        Assert.DoesNotContain("UpdateAsync", rollup, StringComparison.Ordinal);
    }

    [Fact]
    public void Sale_checkout_may_collaborate_with_sale_stock_service()
    {
        var useCases = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Application"), "Sales", "SaleUseCases.cs"));

        Assert.Contains("ISaleStockService", useCases, StringComparison.Ordinal);
        Assert.Contains("DeductForSaleAsync", useCases, StringComparison.Ordinal);
        Assert.Contains("RestoreForSaleVoidAsync", useCases, StringComparison.Ordinal);
        Assert.DoesNotContain("IOfflineOperationDispatcher", useCases, StringComparison.Ordinal);
        Assert.DoesNotContain("SupplierId", useCases, StringComparison.Ordinal);
        Assert.DoesNotContain("WarehouseId", useCases, StringComparison.Ordinal);
        Assert.DoesNotContain("CostPrice", useCases, StringComparison.Ordinal);
    }

    private static IEnumerable<string> InventorySourceFiles()
    {
        var roots = new[]
        {
            Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Domain"), "Inventory"),
            Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Application"), "Inventory"),
            Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Infrastructure"), "Persistence", "Inventory"),
            Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Api"), "Inventory")
        };

        foreach (var root in roots)
        {
            Assert.True(Directory.Exists(root), root);
            foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                yield return file;
            }
        }
    }

    private static string PosProject(string projectName) => Path.Combine(
        FindRepositoryRoot(), "src", "Products", "PinoyBusinessPOS", projectName);

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
