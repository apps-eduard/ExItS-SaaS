namespace ExItS.ArchitectureTests;

/// <summary>
/// Guards the Product-Based Utang / simple-sales boundary: sales may record Cash, ManualGCash,
/// Card, GCash, and online Utang checkouts — but must not host payment-gateway types inside the
/// Sales slice (those live under Payments/). Still forbids suppliers, warehouses, costing, offline
/// sale capture, tax, discounts, refunds, split tender, and real GCash APIs in Sales.
/// </summary>
public sealed class PosSalesScopeArchitectureTests
{
    private static readonly string[] OutOfScopeConcepts =
    [
        "DiscountAmount",
        "DiscountRule",
        "RefundId",
        "SaleRefund",
        "SaleReturn",
        "SplitTender",
        "PaymentGateway",
        "PaymentIntent",
        "GCashApi",
        "PurchaseOrder",
        "SupplierId",
        "WarehouseId",
        "CostPrice",
        "AverageCost",
        "NegativeStockOverride"
    ];

    [Fact]
    public void Sales_slice_declares_no_tax_discount_refund_gateway_or_supplier_concepts()
    {
        foreach (var file in SalesSourceFiles())
        {
            var text = File.ReadAllText(file);
            foreach (var concept in OutOfScopeConcepts)
            {
                Assert.DoesNotContain(concept, text, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void Sales_domain_stays_persistence_and_http_independent()
    {
        var domain = Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Domain"), "Sales");
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
    public void Sales_persistence_keeps_sale_tables_without_supplier_or_warehouse_tables()
    {
        var context = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Infrastructure"), "Persistence", "PosDbContext.cs"));

        Assert.Contains("\"sales\"", context, StringComparison.Ordinal);
        Assert.Contains("\"sale_lines\"", context, StringComparison.Ordinal);
        Assert.Contains("\"sale_number_sequences\"", context, StringComparison.Ordinal);
        Assert.Contains("\"payment_attempts\"", context, StringComparison.Ordinal);

        foreach (var table in new[]
                 {
                     "\"carts\"", "\"taxes\"", "\"discounts\"", "\"sale_refunds\"", "\"sale_payments\"",
                     "\"warehouses\""
                 })
        {
            Assert.DoesNotContain(table, context, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Payments_slice_owns_provider_neutral_gateway_abstraction()
    {
        var appPayments = Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Application"), "Payments");
        Assert.True(Directory.Exists(appPayments), appPayments);
        var gateway = File.ReadAllText(Path.Combine(appPayments, "IPaymentGateway.cs"));
        var fake = File.ReadAllText(Path.Combine(appPayments, "FakePaymentGateway.cs"));
        Assert.Contains("interface IPaymentGateway", gateway, StringComparison.Ordinal);
        Assert.Contains("PaymentWebhookEvent", gateway, StringComparison.Ordinal);
        Assert.Contains("class FakePaymentGateway", fake, StringComparison.Ordinal);
        Assert.DoesNotContain("Stripe", fake, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PayMongo", fake, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Checkout_utang_path_may_use_credit_customer_and_inventory_stock_services()
    {
        var useCases = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Application"), "Sales", "SaleUseCases.cs"));

        Assert.Contains("ICreditEntryRepository", useCases, StringComparison.Ordinal);
        Assert.Contains("IPOSCustomerRepository", useCases, StringComparison.Ordinal);
        Assert.Contains("ISaleStockService", useCases, StringComparison.Ordinal);
        Assert.Contains("SalePaymentMethod.Utang", useCases, StringComparison.Ordinal);

        Assert.DoesNotContain("IOfflineOperationDispatcher", useCases, StringComparison.Ordinal);
        Assert.DoesNotContain("IRepaymentRepository", useCases, StringComparison.Ordinal);
    }

    [Fact]
    public void Sales_have_no_offline_queue_dispatcher_or_local_projection()
    {
        foreach (var file in SalesSourceFiles())
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("IOfflineOperationDispatcher", text, StringComparison.Ordinal);
            Assert.DoesNotContain("IOfflineOperationQueue", text, StringComparison.Ordinal);
            Assert.DoesNotContain("LocalStore", text, StringComparison.Ordinal);
            Assert.DoesNotContain("SQLite", text, StringComparison.OrdinalIgnoreCase);
        }

        var localStore = PosProject("ExItS.PinoyBusinessPOS.LocalStore");
        Assert.True(Directory.Exists(localStore), localStore);
        foreach (var file in Directory.EnumerateFiles(localStore, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            foreach (var forbidden in new[]
                     {
                         "sale.checkout", "SaleCheckout", "LocalSale", "sale_lines", "local_sales", "SaleRecord"
                     })
            {
                Assert.DoesNotContain(forbidden, text, StringComparison.OrdinalIgnoreCase);
            }
        }

        var mauiProgram = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Maui"), "MauiProgram.cs"));
        Assert.DoesNotContain("SaleCheckoutOfflineDispatcher", mauiProgram, StringComparison.Ordinal);
        Assert.DoesNotContain("SaleOfflineDispatcher", mauiProgram, StringComparison.Ordinal);
    }

    [Fact]
    public void Product_based_utang_has_no_offline_queue_path()
    {
        var apiClient = PosProject("ExItS.PinoyBusinessPOS.ApiClient");
        foreach (var file in Directory.EnumerateFiles(apiClient, "*Offline*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("UtangPaymentMethod", text, StringComparison.Ordinal);
            Assert.DoesNotContain("SalePaymentMethod.Utang", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ProductBasedUtang", text, StringComparison.Ordinal);
        }

        var checkout = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Maui"),
            "Components",
            "Pages",
            "Sales",
            "SaleCheckout.razor"));
        Assert.Contains("PosSaleOptions.UtangPaymentMethod", checkout, StringComparison.Ordinal);
        Assert.DoesNotContain("IOfflineOperationQueue", checkout, StringComparison.Ordinal);
        Assert.DoesNotContain("Enqueue", checkout, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("online-only", checkout, StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> SalesSourceFiles()
    {
        var roots = new[]
        {
            Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Domain"), "Sales"),
            Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Application"), "Sales"),
            Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Infrastructure"), "Persistence", "Sales"),
            Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Api"), "Sales")
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
