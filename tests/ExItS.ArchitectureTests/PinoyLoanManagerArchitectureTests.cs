namespace ExItS.ArchitectureTests;

public sealed class PinoyLoanManagerArchitectureTests
{
    [Fact]
    public void Domain_application_apiclient_and_web_do_not_reference_infrastructure_ef_or_pos()
    {
        var root = FindRepositoryRoot();
        var projects = new[]
        {
            Path.Combine(root, "src", "Products", "PinoyLoanManager", "ExItS.PinoyLoanManager.Domain",
                "ExItS.PinoyLoanManager.Domain.csproj"),
            Path.Combine(root, "src", "Products", "PinoyLoanManager", "ExItS.PinoyLoanManager.Application",
                "ExItS.PinoyLoanManager.Application.csproj"),
            Path.Combine(root, "src", "Products", "PinoyLoanManager", "ExItS.PinoyLoanManager.ApiClient",
                "ExItS.PinoyLoanManager.ApiClient.csproj"),
            Path.Combine(root, "src", "Products", "PinoyLoanManager", "ExItS.PinoyLoanManager.Web",
                "ExItS.PinoyLoanManager.Web.csproj")
        };

        foreach (var project in projects)
        {
            Assert.True(File.Exists(project), project);
            var text = File.ReadAllText(project);
            Assert.DoesNotContain("PinoyLoanManager.Infrastructure.csproj", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("EntityFrameworkCore", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Npgsql", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PinoyBusinessPOS", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ExItS.Platform.Infrastructure", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Infrastructure_and_api_do_not_reference_pos_or_platform_infrastructure()
    {
        var root = FindRepositoryRoot();
        foreach (var relative in new[]
                 {
                     Path.Combine("src", "Products", "PinoyLoanManager", "ExItS.PinoyLoanManager.Infrastructure",
                         "ExItS.PinoyLoanManager.Infrastructure.csproj"),
                     Path.Combine("src", "Products", "PinoyLoanManager", "ExItS.PinoyLoanManager.Api",
                         "ExItS.PinoyLoanManager.Api.csproj")
                 })
        {
            var path = Path.Combine(root, relative);
            Assert.True(File.Exists(path), path);
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("PinoyBusinessPOS", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ExItS.Platform.Infrastructure", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("EntityFrameworkCore", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Npgsql", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void No_pinoy_loan_manager_project_references_pinoy_business_pos()
    {
        var root = FindRepositoryRoot();
        var productRoot = Path.Combine(root, "src", "Products", "PinoyLoanManager");
        var testsRoot = Path.Combine(root, "tests", "ExItS.PinoyLoanManager.UnitTests");
        var projects = Directory.GetFiles(productRoot, "*.csproj", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(testsRoot, "*.csproj", SearchOption.AllDirectories))
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                        && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));

        foreach (var project in projects)
        {
            var text = File.ReadAllText(project);
            Assert.DoesNotContain("PinoyBusinessPOS", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Solution_registers_plm_scaffold_projects()
    {
        var slnx = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "ExItS.slnx"));
        Assert.Contains("ExItS.PinoyLoanManager.Domain.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("ExItS.PinoyLoanManager.Application.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("ExItS.PinoyLoanManager.Infrastructure.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("ExItS.PinoyLoanManager.Api.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("ExItS.PinoyLoanManager.ApiClient.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("ExItS.PinoyLoanManager.Web.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("ExItS.PinoyLoanManager.UnitTests.csproj", slnx, StringComparison.Ordinal);
        Assert.DoesNotContain("ExItS.PinoyLoanManager.Maui.csproj", slnx, StringComparison.Ordinal);
        Assert.DoesNotContain("ExItS.PinoyLoanManager.LocalStore.csproj", slnx, StringComparison.Ordinal);
    }

    [Fact]
    public void Platform_product_code_registers_pinoy_loan_manager_independently_of_pos()
    {
        var root = FindRepositoryRoot();
        var productCode = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Domain", "Products", "ProductCode.cs"));
        Assert.Contains("PinoyBusinessPos = \"pinoy-business-pos\"", productCode, StringComparison.Ordinal);
        Assert.Contains("PinoyLoanManager = \"pinoy-loan-manager\"", productCode, StringComparison.Ordinal);

        var auth = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Api", "Identity", "AuthEndpoints.cs"));
        Assert.Contains("/api/v1/platform/auth/product-access/effective", auth, StringComparison.Ordinal);
        Assert.Contains("EvaluateCurrentSessionProductAccess", auth, StringComparison.Ordinal);

        var productRoot = Path.Combine(root, "src", "Products", "PinoyLoanManager");
        Assert.Empty(Directory.GetFiles(productRoot, "*Migration*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)));
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
