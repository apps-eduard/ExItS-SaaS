namespace ExItS.PinoyPawnManager.UnitTests;

/// <summary>
/// PPM-01 must not introduce pawn operational domain types yet.
/// </summary>
public sealed class DomainSafetyGuardTests
{
    private static readonly string[] ForbiddenTypeNameFragments =
    [
        "PawnTransaction",
        "PledgedItem",
        "Appraisal",
        "PawnAgreement",
        "PawnTicket",
        "PawnPayment",
        "CustodyMovement",
        "CustodyLocation",
        "Renewal",
        "Redemption",
        "Disposition"
    ];

    [Fact]
    public void Domain_and_application_assemblies_contain_no_pawn_operational_types()
    {
        var assemblies = new[]
        {
            typeof(Domain.DomainAssembly).Assembly,
            typeof(Application.ApplicationAssembly).Assembly
        };

        foreach (var assembly in assemblies)
        {
            var typeNames = assembly.GetTypes().Select(t => t.FullName ?? t.Name).ToArray();
            foreach (var fragment in ForbiddenTypeNameFragments)
            {
                Assert.DoesNotContain(
                    typeNames,
                    name => name.Contains(fragment, StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    [Fact]
    public void Product_tree_has_no_ef_migrations_or_dbcontext()
    {
        var root = FindRepositoryRoot();
        var productRoot = Path.Combine(root, "src", "Products", "PinoyPawnManager");
        Assert.True(Directory.Exists(productRoot), productRoot);

        var sources = Directory.GetFiles(productRoot, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                        && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                        && !p.Contains($"{Path.DirectorySeparatorChar}Docs{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));

        foreach (var file in sources)
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Migration", text, StringComparison.Ordinal);
            Assert.DoesNotContain("EntityFrameworkCore", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Npgsql", text, StringComparison.Ordinal);
        }

        Assert.Empty(Directory.GetFiles(productRoot, "*Migration*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                        && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                        && !p.Contains($"{Path.DirectorySeparatorChar}Docs{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)));
    }

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
