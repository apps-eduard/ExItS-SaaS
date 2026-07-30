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
        "StockTransfer",
        "LotNumber",
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
    public void Inventory_persistence_adds_account_and_movement_tables_only()
    {
        var context = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Infrastructure"), "Persistence", "PosDbContext.cs"));

        Assert.Contains("\"inventory_accounts\"", context, StringComparison.Ordinal);
        Assert.Contains("\"stock_movements\"", context, StringComparison.Ordinal);

        foreach (var table in new[]
                 {
                     "\"suppliers\"", "\"warehouses\"", "\"purchase_orders\"", "\"stock_transfers\"",
                     "\"inventory_costs\"", "\"lots\"", "\"serials\""
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
