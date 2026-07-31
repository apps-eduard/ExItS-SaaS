namespace ExItS.ArchitectureTests;

/// <summary>Guards P10-WP06 advanced permissions / operational reports slice boundaries.</summary>
public sealed class PosPermissionsReportsScopeArchitectureTests
{
    private static readonly string[] OutOfScopeConcepts =
    [
        "MfaChallenge",
        "IdentityProvider",
        "ProfitAndLoss",
        "CsvExport",
        "PdfExport",
        "ScheduledReport"
    ];

    [Fact]
    public void Permissions_slice_declares_no_production_auth_or_export_concepts()
    {
        foreach (var file in PermissionsSourceFiles())
        {
            var text = File.ReadAllText(file);
            foreach (var concept in OutOfScopeConcepts)
            {
                Assert.DoesNotContain(concept, text, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void Permissions_domain_stays_persistence_and_http_independent()
    {
        var domain = Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Domain"), "Permissions");
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
    public void Persistence_adds_role_assignment_table()
    {
        var context = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Infrastructure"), "Persistence", "PosDbContext.cs"));

        Assert.Contains("\"pos_role_assignments\"", context, StringComparison.Ordinal);
    }

    [Fact]
    public void Application_does_not_reference_infrastructure()
    {
        var csproj = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Application"),
            "ExItS.PinoyBusinessPOS.Application.csproj"));
        Assert.DoesNotContain("ExItS.PinoyBusinessPOS.Infrastructure", csproj, StringComparison.Ordinal);
    }

    private static IEnumerable<string> PermissionsSourceFiles()
    {
        var roots = new[]
        {
            Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Domain"), "Permissions"),
            Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Application"), "Permissions"),
            Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Infrastructure"), "Persistence", "Permissions"),
            Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Api"), "Permissions"),
            Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Maui"), "Components", "Pages", "Permissions")
        };

        foreach (var root in roots)
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(root, "*.cs*", SearchOption.AllDirectories))
            {
                yield return file;
            }
        }
    }

    private static string PosProject(string projectName)
    {
        var root = FindRepoRoot();
        return Path.Combine(root, "src", "Products", "PinoyBusinessPOS", projectName);
    }

    private static string FindRepoRoot()
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
