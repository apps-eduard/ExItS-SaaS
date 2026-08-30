namespace ExItS.ArchitectureTests;

/// <summary>
/// Guards P10-WP02 purchasing: PO/GRN/receipt scope only — no AP, payments, offline queues, or WP03+.
/// </summary>
public sealed class PosPurchasingScopeArchitectureTests
{
    private static readonly string[] OutOfScopeConcepts =
    [
        "AccountsPayable",
        "SupplierInvoice",
        "CostHistory",
        "PurchaseReturn",
        "AccountsPayableLedger"
    ];

    [Fact]
    public void Purchasing_slice_declares_no_ap_payments_or_returns()
    {
        foreach (var file in PurchasingSourceFiles())
        {
            var text = File.ReadAllText(file);
            foreach (var concept in OutOfScopeConcepts)
            {
                Assert.DoesNotContain(concept, text, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void Purchasing_domain_stays_persistence_and_http_independent()
    {
        var domain = Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Domain"), "Purchasing");
        foreach (var file in Directory.EnumerateFiles(domain, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("Microsoft.EntityFrameworkCore", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Npgsql", text, StringComparison.Ordinal);
            Assert.DoesNotContain("System.Net.Http", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Purchasing_persistence_adds_po_and_grn_tables_without_ap()
    {
        var context = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Infrastructure"), "Persistence", "PosDbContext.cs"));

        Assert.Contains("\"purchase_orders\"", context, StringComparison.Ordinal);
        Assert.Contains("\"goods_receipts\"", context, StringComparison.Ordinal);
        Assert.Contains("PurchaseReceipt", context, StringComparison.Ordinal);

        foreach (var table in new[]
                 {
                     "\"accounts_payable\"", "\"supplier_invoices\"", "\"supplier_payments\"",
                     "\"cost_history\"", "\"purchase_returns\""
                 })
        {
            Assert.DoesNotContain(table, context, StringComparison.OrdinalIgnoreCase);
        }

        // ADR-023 authorizes organization supplier payables (distinct from legacy AP table names).
        Assert.Contains("\"supplier_payables\"", context, StringComparison.Ordinal);
        Assert.Contains("\"supplier_payable_payments\"", context, StringComparison.Ordinal);
    }

    [Fact]
    public void Purchasing_api_and_client_expose_no_offline_queue_surface()
    {
        var endpoints = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Api"), "Purchasing", "PurchaseOrderEndpoints.cs"));
        var client = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.ApiClient"), "PosPurchaseOrderClient.cs"));

        foreach (var text in new[] { endpoints, client })
        {
            Assert.DoesNotContain("IOfflineOperationDispatcher", text, StringComparison.Ordinal);
            Assert.DoesNotContain("IOfflineOperationQueue", text, StringComparison.Ordinal);
            Assert.DoesNotContain("LocalStore", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Offline_processor_maps_no_purchasing_operations()
    {
        var processor = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Application"), "Offline", "OfflineQueueProcessor.cs"));

        var mapStart = processor.IndexOf("private static bool TryMapCapability", StringComparison.Ordinal);
        var typesStart = processor.IndexOf("public static class OfflineOperationTypes", StringComparison.Ordinal);
        Assert.True(mapStart > 0 && typesStart > mapStart);
        var mapSection = processor[mapStart..typesStart];
        Assert.DoesNotContain("PurchaseOrderSubmit", mapSection, StringComparison.Ordinal);
        Assert.DoesNotContain("PurchaseOrderReceive", mapSection, StringComparison.Ordinal);
        Assert.DoesNotContain("purchase_order", mapSection, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Purchasing_idempotency_constants_exist_without_offline_dispatch()
    {
        var processor = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Application"), "Offline", "OfflineQueueProcessor.cs"));
        var typesStart = processor.IndexOf("public static class OfflineOperationTypes", StringComparison.Ordinal);
        Assert.True(typesStart >= 0);
        var typesSection = processor[typesStart..];

        Assert.Contains("purchase_order.submit", typesSection, StringComparison.Ordinal);
        Assert.Contains("purchase_order.receive", typesSection, StringComparison.Ordinal);
    }

    private static IEnumerable<string> PurchasingSourceFiles()
    {
        var roots = new[]
        {
            Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Domain"), "Purchasing"),
            Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Application"), "Purchasing"),
            Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Infrastructure"), "Persistence", "Purchasing"),
            Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Api"), "Purchasing"),
            Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Maui"), "Components", "Pages", "Purchasing")
        };

        foreach (var root in roots)
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
            {
                if (file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
                {
                    yield return file;
                }
            }
        }

        yield return Path.Combine(PosProject("ExItS.PinoyBusinessPOS.ApiClient"), "PosPurchaseOrderClient.cs");
        yield return Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Application"), "Abstractions", "IPosPurchaseOrderClient.cs");
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
