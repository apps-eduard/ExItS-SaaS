namespace ExItS.ArchitectureTests;

/// <summary>Guards P10-WP05 returns/refunds slice boundaries.</summary>
public sealed class PosReturnsScopeArchitectureTests
{
    private static readonly string[] OutOfScopeConcepts =
    [
        "ExchangeId",
        "StoreCredit",
        "GiftCard",
        "PaymentGateway",
        "RefundGateway"
    ];

    [Fact]
    public void Returns_slice_declares_no_exchange_or_gateway_concepts()
    {
        foreach (var file in ReturnsSourceFiles())
        {
            var text = File.ReadAllText(file);
            foreach (var concept in OutOfScopeConcepts)
            {
                Assert.DoesNotContain(concept, text, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void Returns_domain_stays_persistence_and_http_independent()
    {
        var domain = Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Domain"), "Returns");
        Assert.True(Directory.Exists(domain), domain);

        foreach (var file in Directory.EnumerateFiles(domain, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("Microsoft.EntityFrameworkCore", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Npgsql", text, StringComparison.Ordinal);
            Assert.DoesNotContain("System.Net.Http", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Returns_persistence_adds_return_tables_without_exchange_tables()
    {
        var context = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Infrastructure"), "Persistence", "PosDbContext.cs"));

        Assert.Contains("\"sale_returns\"", context, StringComparison.Ordinal);
        Assert.Contains("\"sale_return_lines\"", context, StringComparison.Ordinal);
        Assert.Contains("\"sale_return_number_sequences\"", context, StringComparison.Ordinal);
        Assert.DoesNotContain("\"sale_exchanges\"", context, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sales_slice_still_bans_sale_return_names()
    {
        var salesRoot = Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Domain"), "Sales");
        foreach (var file in Directory.EnumerateFiles(salesRoot, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("SaleReturn", text, StringComparison.Ordinal);
        }
    }

    private static IEnumerable<string> ReturnsSourceFiles()
    {
        var roots = new[]
        {
            Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Domain"), "Returns"),
            Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Application"), "Returns"),
            Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Infrastructure"), "Persistence", "Returns"),
            Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Api"), "Returns"),
            Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Maui"), "Components", "Pages", "Returns")
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

        yield return Path.Combine(PosProject("ExItS.PinoyBusinessPOS.ApiClient"), "PosSaleReturnClient.cs");
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
