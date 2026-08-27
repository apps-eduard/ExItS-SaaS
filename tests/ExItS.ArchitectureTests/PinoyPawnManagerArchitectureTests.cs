namespace ExItS.ArchitectureTests;

public sealed class PinoyPawnManagerArchitectureTests
{
    [Fact]
    public void Domain_and_application_do_not_reference_infrastructure_ef_or_other_products()
    {
        var root = FindRepositoryRoot();
        var projects = new[]
        {
            Path.Combine(root, "src", "Products", "PinoyPawnManager", "ExItS.PinoyPawnManager.Domain",
                "ExItS.PinoyPawnManager.Domain.csproj"),
            Path.Combine(root, "src", "Products", "PinoyPawnManager", "ExItS.PinoyPawnManager.Application",
                "ExItS.PinoyPawnManager.Application.csproj")
        };

        foreach (var project in projects)
        {
            Assert.True(File.Exists(project), project);
            var text = File.ReadAllText(project);
            Assert.DoesNotContain("PinoyPawnManager.Infrastructure.csproj", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("EntityFrameworkCore", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Npgsql", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PinoyBusinessPOS", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PinoyLoanManager", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PinoyBuyNowPayLater", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PinoyServicePro", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ExItS.Platform.Infrastructure", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Infrastructure_and_api_do_not_reference_other_products_or_platform_infrastructure()
    {
        var root = FindRepositoryRoot();
        foreach (var relative in new[]
                 {
                     Path.Combine("src", "Products", "PinoyPawnManager", "ExItS.PinoyPawnManager.Infrastructure",
                         "ExItS.PinoyPawnManager.Infrastructure.csproj"),
                     Path.Combine("src", "Products", "PinoyPawnManager", "ExItS.PinoyPawnManager.Api",
                         "ExItS.PinoyPawnManager.Api.csproj")
                 })
        {
            var path = Path.Combine(root, relative);
            Assert.True(File.Exists(path), path);
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("PinoyBusinessPOS", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PinoyLoanManager", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PinoyBuyNowPayLater", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PinoyServicePro", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ExItS.Platform.Infrastructure", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("EntityFrameworkCore", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Npgsql", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void No_pinoy_pawn_manager_project_references_sibling_products()
    {
        var root = FindRepositoryRoot();
        var productRoot = Path.Combine(root, "src", "Products", "PinoyPawnManager");
        var testsRoot = Path.Combine(root, "tests", "ExItS.PinoyPawnManager.UnitTests");
        var projects = Directory.GetFiles(productRoot, "*.csproj", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(testsRoot, "*.csproj", SearchOption.AllDirectories))
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                        && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                        && !p.Contains($"{Path.DirectorySeparatorChar}Docs{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));

        foreach (var project in projects)
        {
            var text = File.ReadAllText(project);
            Assert.DoesNotContain("PinoyBusinessPOS", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PinoyLoanManager", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PinoyBuyNowPayLater", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PinoyServicePro", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Solution_registers_ppm_scaffold_projects()
    {
        var slnx = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "ExItS.slnx"));
        Assert.Contains("ExItS.PinoyPawnManager.Domain.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("ExItS.PinoyPawnManager.Application.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("ExItS.PinoyPawnManager.Infrastructure.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("ExItS.PinoyPawnManager.Api.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("ExItS.PinoyPawnManager.UnitTests.csproj", slnx, StringComparison.Ordinal);
        Assert.DoesNotContain("ExItS.PinoyPawnManager.Maui.csproj", slnx, StringComparison.Ordinal);
        Assert.DoesNotContain("ExItS.PinoyPawnManager.LocalStore.csproj", slnx, StringComparison.Ordinal);
    }

    [Fact]
    public void Platform_product_code_registers_pinoy_pawn_manager_independently()
    {
        var root = FindRepositoryRoot();
        var productCode = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Domain", "Products", "ProductCode.cs"));
        Assert.Contains("PinoyBusinessPos = \"pinoy-business-pos\"", productCode, StringComparison.Ordinal);
        Assert.Contains("PinoyLoanManager = \"pinoy-loan-manager\"", productCode, StringComparison.Ordinal);
        Assert.Contains("PinoyPawnManager = \"pinoy-pawn-manager\"", productCode, StringComparison.Ordinal);

        var productRoot = Path.Combine(root, "src", "Products", "PinoyPawnManager");
        Assert.Empty(Directory.GetFiles(productRoot, "*Migration*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}Docs{Path.DirectorySeparatorChar}", StringComparison.Ordinal)));
    }

    [Fact]
    public void Domain_sources_do_not_reference_platform_other_products_or_aspnet_transport()
    {
        AssertSourceFilesAvoid(
            Path.Combine(FindRepositoryRoot(), "src", "Products", "PinoyPawnManager", "ExItS.PinoyPawnManager.Domain"),
            "ExItS.Platform",
            "PinoyBusinessPOS",
            "PinoyLoanManager",
            "PinoyBuyNowPayLater",
            "PinoyServicePro",
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore",
            "Npgsql",
            "HttpContext",
            "DbContext");
    }

    [Fact]
    public void Application_sources_do_not_reference_platform_infrastructure_other_products_or_aspnet()
    {
        AssertSourceFilesAvoid(
            Path.Combine(FindRepositoryRoot(), "src", "Products", "PinoyPawnManager", "ExItS.PinoyPawnManager.Application"),
            "ExItS.Platform.Infrastructure",
            "PinoyBusinessPOS",
            "PinoyLoanManager",
            "PinoyBuyNowPayLater",
            "PinoyServicePro",
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore",
            "Npgsql",
            "HttpContext",
            "DbContext");
    }

    [Fact]
    public void Api_sources_do_not_reference_other_products_platform_infrastructure_or_ef()
    {
        AssertSourceFilesAvoid(
            Path.Combine(FindRepositoryRoot(), "src", "Products", "PinoyPawnManager", "ExItS.PinoyPawnManager.Api"),
            "PinoyBusinessPOS",
            "PinoyLoanManager",
            "PinoyBuyNowPayLater",
            "PinoyServicePro",
            "ExItS.Platform.Infrastructure",
            "Microsoft.EntityFrameworkCore",
            "Npgsql",
            "DbContext");
    }

    [Fact]
    public void Ppm_product_identity_matches_catalog_code_without_platform_domain_reference()
    {
        var root = FindRepositoryRoot();
        var identityPath = Path.Combine(
            root,
            "src",
            "Products",
            "PinoyPawnManager",
            "ExItS.PinoyPawnManager.Domain",
            "Access",
            "PpmProductIdentity.cs");
        Assert.True(File.Exists(identityPath), identityPath);
        var text = File.ReadAllText(identityPath);
        Assert.Contains("pinoy-pawn-manager", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ExItS.Platform", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ProductCode.PinoyPawnManager", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Ppm_01_source_contains_no_pawn_operational_entities()
    {
        var root = Path.Combine(FindRepositoryRoot(), "src", "Products", "PinoyPawnManager");
        var forbidden = new[]
        {
            "PawnTransaction",
            "PledgedItem",
            "Appraisal",
            "PawnAgreement",
            "PawnPayment",
            "CustodyMovement",
            "Renewal",
            "Redemption",
            "Disposition"
        };

        var files = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                        && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                        && !p.Contains($"{Path.DirectorySeparatorChar}Docs{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));

        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            foreach (var name in forbidden)
            {
                Assert.DoesNotContain($"class {name}", text, StringComparison.Ordinal);
                Assert.DoesNotContain($"record {name}", text, StringComparison.Ordinal);
                Assert.DoesNotContain($"enum {name}", text, StringComparison.Ordinal);
            }
        }
    }

    private static void AssertSourceFilesAvoid(string projectRoot, params string[] forbidden)
    {
        Assert.True(Directory.Exists(projectRoot), projectRoot);
        var files = Directory.GetFiles(projectRoot, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                        && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));

        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            foreach (var token in forbidden)
            {
                Assert.DoesNotContain(token, text, StringComparison.OrdinalIgnoreCase);
            }
        }
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
