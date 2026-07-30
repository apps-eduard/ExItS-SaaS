namespace ExItS.ArchitectureTests;

/// <summary>
/// Guards P8-WP05 expenses: online-only expense recording may exist, but suppliers/AP, payroll,
/// GL, OCR, and offline expense queues must not.
/// </summary>
public sealed class PosExpensesScopeArchitectureTests
{
    private static readonly string[] OutOfScopeConcepts =
    [
        "SupplierId",
        "PurchaseOrder",
        "AccountsPayableLedger",
        "PayrollRun",
        "GeneralLedgerAccount",
        "JournalEntry",
        "OcrReceipt",
        "ExpenseAttachment",
        "IOfflineExpense",
        "ExpenseOfflineDispatcher",
        "LocalExpense",
        "BudgetApproval",
        "RecurringExpense"
    ];

    [Fact]
    public void Expense_slice_declares_no_supplier_payroll_gl_ocr_or_offline_queue_concepts()
    {
        foreach (var file in ExpenseSourceFiles())
        {
            var text = File.ReadAllText(file);
            foreach (var concept in OutOfScopeConcepts)
            {
                Assert.DoesNotContain(concept, text, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void Expense_domain_stays_persistence_and_http_independent()
    {
        var domain = Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Domain"), "Expenses");
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
    public void Expense_persistence_adds_category_expense_and_sequence_tables_only()
    {
        var context = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Infrastructure"), "Persistence", "PosDbContext.cs"));

        Assert.Contains("\"expense_categories\"", context, StringComparison.Ordinal);
        Assert.Contains("\"expenses\"", context, StringComparison.Ordinal);
        Assert.Contains("\"expense_number_sequences\"", context, StringComparison.Ordinal);

        foreach (var table in new[]
                 {
                     "\"payroll\"", "\"general_ledger\"", "\"journal_entries\"",
                     "\"expense_attachments\"", "\"expense_budgets\"",
                     "\"accounts_payable\""
                 })
        {
            Assert.DoesNotContain(table, context, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Expense_api_and_client_expose_no_offline_queue_surface()
    {
        var endpoints = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Api"), "Expenses", "ExpenseEndpoints.cs"));
        var client = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.ApiClient"), "PosExpenseClient.cs"));

        foreach (var text in new[] { endpoints, client })
        {
            Assert.DoesNotContain("IOfflineOperationDispatcher", text, StringComparison.Ordinal);
            Assert.DoesNotContain("IOfflineOperationQueue", text, StringComparison.Ordinal);
            Assert.DoesNotContain("LocalStore", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Enqueue", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Local_store_and_offline_processor_do_not_queue_or_dispatch_expenses()
    {
        var processor = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Application"), "Offline", "OfflineQueueProcessor.cs"));
        Assert.Contains("public const string ExpenseCreate = \"expense.create\"", processor, StringComparison.Ordinal);

        // Constant exists for server idempotency headers only — no capability map arm.
        var mapStart = processor.IndexOf("private static bool TryMapCapability", StringComparison.Ordinal);
        var typesStart = processor.IndexOf("public static class OfflineOperationTypes", StringComparison.Ordinal);
        Assert.True(mapStart > 0 && typesStart > mapStart);
        var mapSection = processor[mapStart..typesStart];
        Assert.DoesNotContain("ExpenseCreate", mapSection, StringComparison.Ordinal);

        var localStore = PosProject("ExItS.PinoyBusinessPOS.LocalStore");
        foreach (var file in Directory.EnumerateFiles(localStore, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            foreach (var banned in new[] { "expense.create", "ExpenseCreate", "LocalExpense", "ExpenseRecord" })
            {
                Assert.DoesNotContain(banned, text, StringComparison.Ordinal);
            }
        }
    }

    private static IEnumerable<string> ExpenseSourceFiles()
    {
        var roots = new[]
        {
            Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Domain"), "Expenses"),
            Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Application"), "Expenses"),
            Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Infrastructure"), "Persistence", "Expenses"),
            Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Api"), "Expenses")
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
