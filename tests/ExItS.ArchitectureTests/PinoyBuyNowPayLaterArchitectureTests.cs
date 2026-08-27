namespace ExItS.ArchitectureTests;

public sealed class PinoyBuyNowPayLaterArchitectureTests
{
    [Fact]
    public void Domain_and_application_do_not_reference_infrastructure_ef_or_other_products()
    {
        var root = FindRepositoryRoot();
        var projects = new[]
        {
            Path.Combine(root, "src", "Products", "PinoyBuyNowPayLater", "ExItS.PinoyBuyNowPayLater.Domain",
                "ExItS.PinoyBuyNowPayLater.Domain.csproj"),
            Path.Combine(root, "src", "Products", "PinoyBuyNowPayLater", "ExItS.PinoyBuyNowPayLater.Application",
                "ExItS.PinoyBuyNowPayLater.Application.csproj")
        };

        foreach (var project in projects)
        {
            Assert.True(File.Exists(project), project);
            var text = File.ReadAllText(project);
            Assert.DoesNotContain("PinoyBuyNowPayLater.Infrastructure.csproj", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("EntityFrameworkCore", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Npgsql", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PinoyBusinessPOS", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PinoyLoanManager", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PinoyServicePro", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ExItS.Platform.Infrastructure", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Infrastructure_may_use_ef_but_api_does_not_reference_ef_or_other_products()
    {
        var root = FindRepositoryRoot();
        var infrastructure = Path.Combine(root, "src", "Products", "PinoyBuyNowPayLater",
            "ExItS.PinoyBuyNowPayLater.Infrastructure", "ExItS.PinoyBuyNowPayLater.Infrastructure.csproj");
        var api = Path.Combine(root, "src", "Products", "PinoyBuyNowPayLater",
            "ExItS.PinoyBuyNowPayLater.Api", "ExItS.PinoyBuyNowPayLater.Api.csproj");

        Assert.True(File.Exists(infrastructure), infrastructure);
        Assert.True(File.Exists(api), api);

        var infraText = File.ReadAllText(infrastructure);
        Assert.Contains("EntityFrameworkCore", infraText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Npgsql", infraText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PinoyBusinessPOS", infraText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PinoyLoanManager", infraText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PinoyServicePro", infraText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ExItS.Platform.Infrastructure", infraText, StringComparison.OrdinalIgnoreCase);

        var apiText = File.ReadAllText(api);
        Assert.DoesNotContain("EntityFrameworkCore", apiText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Npgsql", apiText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PinoyBusinessPOS", apiText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PinoyLoanManager", apiText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PinoyServicePro", apiText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ExItS.Platform.Infrastructure", apiText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void No_bnpl_project_references_pos_plm_or_psp_operational_projects()
    {
        var root = FindRepositoryRoot();
        var productRoot = Path.Combine(root, "src", "Products", "PinoyBuyNowPayLater");
        var testsRoot = Path.Combine(root, "tests", "ExItS.PinoyBuyNowPayLater.UnitTests");
        var projects = Directory.GetFiles(productRoot, "*.csproj", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(testsRoot, "*.csproj", SearchOption.AllDirectories))
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                        && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));

        foreach (var project in projects)
        {
            var text = File.ReadAllText(project);
            Assert.DoesNotContain("PinoyBusinessPOS", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PinoyLoanManager", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PinoyServicePro", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Solution_registers_bnpl_scaffold_projects()
    {
        var slnx = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "ExItS.slnx"));
        Assert.Contains("ExItS.PinoyBuyNowPayLater.Domain.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("ExItS.PinoyBuyNowPayLater.Application.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("ExItS.PinoyBuyNowPayLater.Infrastructure.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("ExItS.PinoyBuyNowPayLater.Api.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("ExItS.PinoyBuyNowPayLater.UnitTests.csproj", slnx, StringComparison.Ordinal);
    }

    [Fact]
    public void Platform_product_code_registers_bnpl_and_migrations_exclude_installments_repayments()
    {
        var root = FindRepositoryRoot();
        var productCode = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Domain", "Products", "ProductCode.cs"));
        Assert.Contains("PinoyBuyNowPayLater = \"pinoy-buy-now-pay-later\"", productCode, StringComparison.Ordinal);

        var migrationsDir = Path.Combine(
            root,
            "src",
            "Products",
            "PinoyBuyNowPayLater",
            "ExItS.PinoyBuyNowPayLater.Infrastructure",
            "Persistence",
            "Migrations");
        Assert.True(Directory.Exists(migrationsDir), migrationsDir);
        var migrations = Directory.GetFiles(migrationsDir, "*.cs")
            .Where(p => !p.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase)
                        && !p.EndsWith("ModelSnapshot.cs", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.Contains(migrations, p => p.Contains("InitialBnplCustomerFoundation", StringComparison.Ordinal));
        Assert.Contains(migrations, p => p.Contains("AddBnplFinancingApplicationLifecycle", StringComparison.Ordinal));
        Assert.Contains(migrations, p => p.Contains("AddBnplInstallmentPlanFoundation", StringComparison.Ordinal));
        foreach (var migration in migrations)
        {
            var text = File.ReadAllText(migration);
            Assert.DoesNotContain("name: \"repayments\"", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("name: \"settlements\"", text, StringComparison.OrdinalIgnoreCase);
        }

        var financingMigration = migrations.Single(p => p.Contains("AddBnplFinancingApplicationLifecycle", StringComparison.Ordinal));
        var financingText = File.ReadAllText(financingMigration);
        Assert.Contains("financing_applications", financingText, StringComparison.Ordinal);
        Assert.Contains("financing_offers", financingText, StringComparison.Ordinal);
        Assert.Contains("financing_decisions", financingText, StringComparison.Ordinal);
        Assert.DoesNotContain("'Active'", financingText, StringComparison.Ordinal);

        var planMigration = migrations.Single(p => p.Contains("AddBnplInstallmentPlanFoundation", StringComparison.Ordinal));
        var planText = File.ReadAllText(planMigration);
        Assert.Contains("installment_plans", planText, StringComparison.Ordinal);
        Assert.Contains("installment_plan_items", planText, StringComparison.Ordinal);
        Assert.DoesNotContain("name: \"repayments\"", planText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Domain_sources_do_not_reference_platform_other_products_or_aspnet_transport()
    {
        AssertSourceFilesAvoid(
            Path.Combine(FindRepositoryRoot(), "src", "Products", "PinoyBuyNowPayLater", "ExItS.PinoyBuyNowPayLater.Domain"),
            "ExItS.Platform",
            "PinoyBusinessPOS",
            "PinoyLoanManager",
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
            Path.Combine(FindRepositoryRoot(), "src", "Products", "PinoyBuyNowPayLater", "ExItS.PinoyBuyNowPayLater.Application"),
            "ExItS.Platform.Infrastructure",
            "PinoyBusinessPOS",
            "PinoyLoanManager",
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
            Path.Combine(FindRepositoryRoot(), "src", "Products", "PinoyBuyNowPayLater", "ExItS.PinoyBuyNowPayLater.Api"),
            "PinoyBusinessPOS",
            "PinoyLoanManager",
            "PinoyServicePro",
            "ExItS.Platform.Infrastructure",
            "Microsoft.EntityFrameworkCore",
            "Npgsql",
            "DbContext");
    }

    [Fact]
    public void Bnpl_product_identity_matches_catalog_code_without_platform_domain_reference()
    {
        var root = FindRepositoryRoot();
        var identityPath = Path.Combine(
            root,
            "src",
            "Products",
            "PinoyBuyNowPayLater",
            "ExItS.PinoyBuyNowPayLater.Domain",
            "Access",
            "BnplProductIdentity.cs");
        Assert.True(File.Exists(identityPath), identityPath);
        var text = File.ReadAllText(identityPath);
        Assert.Contains("pinoy-buy-now-pay-later", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ExItS.Platform", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ProductCode.PinoyBuyNowPayLater", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Sources_do_not_define_financing_entities_and_dbcontext_stays_in_infrastructure()
    {
        var root = Path.Combine(FindRepositoryRoot(), "src", "Products", "PinoyBuyNowPayLater");
        var files = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                        && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));

        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("class FinancingPlan", text, StringComparison.Ordinal);
            Assert.DoesNotContain("class Installment", text, StringComparison.Ordinal);
            Assert.DoesNotContain("class Repayment", text, StringComparison.Ordinal);
            Assert.DoesNotContain("class Settlement", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MarkActive(", text, StringComparison.Ordinal);
            Assert.DoesNotContain("enum BnplFinancingApplicationStatus\n{\n    Active", text, StringComparison.Ordinal);

            var declaresDbContext =
                text.Contains("class BnplDbContext :", StringComparison.Ordinal)
                || text.Contains(": DbContext", StringComparison.Ordinal);
            if (declaresDbContext)
            {
                var normalized = file.Replace('/', Path.DirectorySeparatorChar);
                Assert.True(
                    normalized.Contains(".Infrastructure", StringComparison.OrdinalIgnoreCase)
                    || normalized.Contains(
                        $"{Path.DirectorySeparatorChar}Infrastructure{Path.DirectorySeparatorChar}",
                        StringComparison.OrdinalIgnoreCase),
                    $"DbContext types must live under Infrastructure. Offending file: {file}");
            }
        }
    }

    [Fact]
    public void Bnpl_dbcontext_model_does_not_reference_other_product_entity_types()
    {
        var path = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "Products",
            "PinoyBuyNowPayLater",
            "ExItS.PinoyBuyNowPayLater.Infrastructure",
            "Persistence",
            "BnplDbContext.cs");
        Assert.True(File.Exists(path), path);
        var text = File.ReadAllText(path);
        Assert.Contains("BnplCustomerRecord", text, StringComparison.Ordinal);
        Assert.Contains("BnplInstallmentPlanRecord", text, StringComparison.Ordinal);
        Assert.DoesNotContain("POSCustomer", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PlatformUser", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PlmBorrower", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DbSet<Sale", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PinoyBusinessPOS", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PinoyLoanManager", text, StringComparison.OrdinalIgnoreCase);
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
