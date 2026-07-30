namespace ExItS.ArchitectureTests;

/// <summary>
/// Guards the P8-WP01 boundary: the catalog slice may identify and price products, but must not
/// introduce inventory, sales, tax, discount, or supplier concepts reserved for P8-WP02+.
/// </summary>
public sealed class PosCatalogScopeArchitectureTests
{
    private static readonly string[] OutOfScopeConcepts =
    [
        "StockOnHand",
        "QuantityOnHand",
        "ReorderLevel",
        "StockMovement",
        "InventoryAdjustment",
        "SaleLine",
        "SaleOrder",
        "CartItem",
        "Checkout",
        "TaxRate",
        "VatRate",
        "DiscountRule",
        "PriceTier",
        "SupplierId",
        "PurchaseOrder"
    ];

    [Fact]
    public void Catalog_slice_declares_no_inventory_sales_tax_or_discount_concepts()
    {
        foreach (var file in CatalogSourceFiles())
        {
            var text = File.ReadAllText(file);
            foreach (var concept in OutOfScopeConcepts)
            {
                Assert.DoesNotContain(concept, text, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void Catalog_domain_stays_persistence_and_http_independent()
    {
        var domain = Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Domain"), "Catalog");
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
    public void Catalog_persistence_adds_no_stock_tax_discount_or_supplier_tables()
    {
        var context = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Infrastructure"), "Persistence", "PosDbContext.cs"));

        Assert.Contains("\"product_categories\"", context, StringComparison.Ordinal);
        Assert.Contains("\"products\"", context, StringComparison.Ordinal);

        // Sales tables are owned by the P8-WP02 slice; stock, cart, tax, discount, and supplier
        // persistence remains out of scope for both slices.
        foreach (var table in new[]
                 {
                     "\"stock\"", "\"stock_levels\"", "\"inventory\"",
                     "\"carts\"", "\"taxes\"", "\"discounts\"", "\"product_barcodes\"", "\"suppliers\""
                 })
        {
            Assert.DoesNotContain(table, context, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Catalog_api_and_client_expose_no_offline_queue_or_idempotency_surface()
    {
        var endpoints = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Api"), "Catalog", "CatalogEndpoints.cs"));
        var client = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.ApiClient"), "PosCatalogClient.cs"));

        foreach (var text in new[] { endpoints, client })
        {
            Assert.DoesNotContain("Idempotency", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("OfflineOperation", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("LocalStore", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static IEnumerable<string> CatalogSourceFiles()
    {
        var roots = new[]
        {
            Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Domain"), "Catalog"),
            Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Application"), "Catalog"),
            Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Infrastructure"), "Persistence", "Catalog"),
            Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Api"), "Catalog")
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
