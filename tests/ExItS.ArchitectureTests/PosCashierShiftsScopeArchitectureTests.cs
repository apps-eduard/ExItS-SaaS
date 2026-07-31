namespace ExItS.ArchitectureTests;

/// <summary>Guards P10-WP04 cashier shifts: shift/cash drawer scope only — no payroll, registers, or WP05+.</summary>
public sealed class PosCashierShiftsScopeArchitectureTests
{
    private static readonly string[] OutOfScopeConcepts =
    [
        "PayrollRun",
        "GeneralLedger",
        "CashRegisterDevice",
        "PurchaseReturn",
        "ExpenseLedger"
    ];

    [Fact]
    public void Cashier_shifts_declare_no_payroll_or_register_concepts()
    {
        foreach (var file in CashierShiftSourceFiles())
        {
            var text = File.ReadAllText(file);
            foreach (var concept in OutOfScopeConcepts)
            {
                Assert.DoesNotContain(concept, text, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void Cashier_shift_domain_stays_persistence_and_http_independent()
    {
        var domain = Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Domain"), "CashierShifts");
        foreach (var file in Directory.EnumerateFiles(domain, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("Microsoft.EntityFrameworkCore", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Npgsql", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Cashier_shift_persistence_adds_shift_tables_without_payroll()
    {
        var context = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Infrastructure"), "Persistence", "PosDbContext.cs"));

        Assert.Contains("\"cashier_shifts\"", context, StringComparison.Ordinal);
        Assert.Contains("\"cashier_shift_movements\"", context, StringComparison.Ordinal);
        Assert.Contains("cashier_shift_id", context, StringComparison.Ordinal);
        Assert.DoesNotContain("\"payroll_runs\"", context, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Checkout_sale_requires_shift_repository()
    {
        var useCases = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Application"), "Sales", "SaleUseCases.cs"));
        Assert.Contains("ICashierShiftRepository", useCases, StringComparison.Ordinal);
        Assert.Contains("CashierShiftNoOpenShift", useCases, StringComparison.Ordinal);
    }

    private static IEnumerable<string> CashierShiftSourceFiles()
    {
        var roots = new[]
        {
            Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Domain"), "CashierShifts"),
            Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Application"), "CashierShifts"),
            Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Infrastructure"), "Persistence", "CashierShifts"),
            Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Api"), "CashierShifts"),
            Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Maui"), "Components", "Pages", "Shifts")
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

        yield return Path.Combine(PosProject("ExItS.PinoyBusinessPOS.ApiClient"), "PosCashierShiftClient.cs");
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
