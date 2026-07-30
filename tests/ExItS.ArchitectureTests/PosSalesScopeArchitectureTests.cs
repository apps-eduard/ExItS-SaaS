namespace ExItS.ArchitectureTests;

/// <summary>
/// Guards the P8-WP02 boundary: simple sales may record cash and manual GCash checkouts and void
/// them, but must not introduce inventory deduction, Utang sales, offline sale capture, tax,
/// discounts, refunds, split tender, or payment gateways reserved for later work packages.
/// </summary>
public sealed class PosSalesScopeArchitectureTests
{
    private static readonly string[] OutOfScopeConcepts =
    [
        "StockOnHand",
        "QuantityOnHand",
        "StockMovement",
        "StockDeduction",
        "DeductStock",
        "InventoryAdjustment",
        "ReorderLevel",
        "TaxRate",
        "VatRate",
        "TaxAmount",
        "DiscountAmount",
        "DiscountRule",
        "RefundId",
        "SaleRefund",
        "SaleReturn",
        "SplitTender",
        "PaymentGateway",
        "PaymentIntent",
        "GCashApi",
        "CreditEntryId",
        "UtangSale",
        "PurchaseOrder",
        "SupplierId"
    ];

    [Fact]
    public void Sales_slice_declares_no_inventory_tax_discount_refund_or_gateway_concepts()
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
    public void Sales_persistence_adds_only_sale_sale_line_and_sequence_tables()
    {
        var context = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Infrastructure"), "Persistence", "PosDbContext.cs"));

        Assert.Contains("\"sales\"", context, StringComparison.Ordinal);
        Assert.Contains("\"sale_lines\"", context, StringComparison.Ordinal);
        Assert.Contains("\"sale_number_sequences\"", context, StringComparison.Ordinal);

        foreach (var table in new[]
                 {
                     "\"stock\"", "\"stock_levels\"", "\"inventory\"", "\"inventory_movements\"",
                     "\"carts\"", "\"taxes\"", "\"discounts\"", "\"sale_refunds\"", "\"sale_payments\"",
                     "\"suppliers\""
                 })
        {
            Assert.DoesNotContain(table, context, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Checkout_never_touches_credit_repayment_or_customer_repositories()
    {
        var useCases = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Application"), "Sales", "SaleUseCases.cs"));

        foreach (var forbidden in new[]
                 {
                     "ICreditEntryRepository", "IRepaymentRepository", "IPOSCustomerRepository",
                     "CreditEntry.", "Repayment.", "POSCustomer."
                 })
        {
            Assert.DoesNotContain(forbidden, useCases, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Sales_have_no_offline_queue_dispatcher_or_local_projection()
    {
        // The sale checkout operation type exists purely for server-side idempotency headers; there is
        // no offline dispatcher, local sale table, or queued sale operation.
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
