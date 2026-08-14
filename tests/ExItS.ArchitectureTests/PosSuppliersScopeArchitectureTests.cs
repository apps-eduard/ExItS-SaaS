namespace ExItS.ArchitectureTests;

/// <summary>
/// Guards P10-WP01 suppliers master-data boundaries.
/// Connected ExItS Suppliers Phase 1 may navigate to purchase orders from supplier UI,
/// but supplier pages must still not embed receiving or accounts-payable concepts.
/// </summary>
public sealed class PosSuppliersScopeArchitectureTests
{
    private static readonly string[] OutOfScopeConcepts =
    [
        "GoodsReceipt",
        "AccountsPayable",
        "SupplierInvoice",
        "SupplierPayment",
        "CostHistory",
        "PurchaseReturn"
    ];

    [Fact]
    public void Suppliers_slice_declares_no_receiving_or_payables_concepts()
    {
        foreach (var file in SupplierSourceFiles())
        {
            var text = File.ReadAllText(file);
            foreach (var concept in OutOfScopeConcepts)
            {
                Assert.DoesNotContain(concept, text, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void Supplier_domain_stays_persistence_and_http_independent()
    {
        var domain = Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Domain"), "Suppliers");
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
    public void Supplier_persistence_adds_supplier_tables_without_purchasing_in_supplier_slice()
    {
        var context = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Infrastructure"), "Persistence", "PosDbContext.cs"));

        Assert.Contains("\"suppliers\"", context, StringComparison.Ordinal);
        Assert.Contains("\"supplier_code_sequences\"", context, StringComparison.Ordinal);

        var supplierPersistence = string.Join(
            '\n',
            Directory.EnumerateFiles(
                    Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Infrastructure"), "Persistence", "Suppliers"),
                    "*.cs",
                    SearchOption.AllDirectories)
                .Select(File.ReadAllText));

        foreach (var table in new[]
                 {
                     "\"purchase_orders\"", "\"goods_receipts\"", "\"receiving\"",
                     "\"accounts_payable\"", "\"supplier_invoices\"", "\"supplier_payments\"",
                     "\"cost_history\"", "\"purchase_returns\""
                 })
        {
            Assert.DoesNotContain(table, supplierPersistence, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Supplier_api_and_client_expose_no_offline_queue_surface()
    {
        var endpoints = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Api"), "Suppliers", "SupplierEndpoints.cs"));
        var client = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.ApiClient"), "PosSupplierClient.cs"));

        foreach (var text in new[] { endpoints, client })
        {
            Assert.DoesNotContain("IOfflineOperationDispatcher", text, StringComparison.Ordinal);
            Assert.DoesNotContain("IOfflineOperationQueue", text, StringComparison.Ordinal);
            Assert.DoesNotContain("LocalStore", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Enqueue", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Local_store_and_offline_processor_do_not_queue_or_dispatch_suppliers()
    {
        var processor = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Application"), "Offline", "OfflineQueueProcessor.cs"));

        foreach (var banned in new[] { "supplier.create", "SupplierCreate", "LocalSupplier", "SupplierUpdate" })
        {
            Assert.DoesNotContain(banned, processor, StringComparison.OrdinalIgnoreCase);
        }

        var localStore = PosProject("ExItS.PinoyBusinessPOS.LocalStore");
        foreach (var file in Directory.EnumerateFiles(localStore, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            foreach (var banned in new[] { "supplier.create", "SupplierCreate", "LocalSupplier", "SupplierRecord" })
            {
                Assert.DoesNotContain(banned, text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Maui_supplier_pages_do_not_surface_payables_or_receiving()
    {
        var pages = Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Maui"), "Components", "Pages", "Suppliers");
        Assert.True(Directory.Exists(pages), pages);

        foreach (var file in Directory.EnumerateFiles(pages, "*.*", SearchOption.AllDirectories))
        {
            if (!file.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)
                && !file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            // Connected-supplier Phase 1 may navigate to purchase orders / linked catalog.
            // Supplier master-data pages still must not embed receiving or accounts-payable.
            foreach (var forbidden in new[]
                     {
                         "GoodsReceipt", "Receiving", "AccountsPayable",
                         "SupplierInvoice", "SupplierPayment", "CostHistory", "PurchaseReturn"
                     })
            {
                Assert.DoesNotContain(forbidden, text, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    private static IEnumerable<string> SupplierSourceFiles()
    {
        var roots = new[]
        {
            Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Domain"), "Suppliers"),
            Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Application"), "Suppliers"),
            Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Infrastructure"), "Persistence", "Suppliers"),
            Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Api"), "Suppliers"),
            Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Maui"), "Components", "Pages", "Suppliers")
        };

        foreach (var root in roots)
        {
            Assert.True(Directory.Exists(root), root);
            foreach (var file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
            {
                if (file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
                {
                    yield return file;
                }
            }
        }

        foreach (var project in new[]
                 {
                     PosProject("ExItS.PinoyBusinessPOS.Domain"),
                     PosProject("ExItS.PinoyBusinessPOS.Application"),
                     PosProject("ExItS.PinoyBusinessPOS.Infrastructure"),
                     PosProject("ExItS.PinoyBusinessPOS.ApiClient")
                 })
        {
            foreach (var file in Directory.EnumerateFiles(project, "Supplier*.cs", SearchOption.AllDirectories))
            {
                yield return file;
            }
        }

        var clientAbstraction = Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Application"), "Abstractions", "IPosSupplierClient.cs");
        Assert.True(File.Exists(clientAbstraction), clientAbstraction);
        yield return clientAbstraction;
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
