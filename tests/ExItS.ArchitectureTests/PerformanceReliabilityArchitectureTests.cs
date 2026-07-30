namespace ExItS.ArchitectureTests;

/// <summary>P9-WP02: performance and reliability architecture guards.</summary>
public sealed class PerformanceReliabilityArchitectureTests
{
    [Fact]
    public void Pos_and_platform_expose_liveness_and_readiness_without_startup_migrate()
    {
        var root = FindRepositoryRoot();
        var pos = File.ReadAllText(Path.Combine(root,
            "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Api", "Program.cs"));
        var platform = File.ReadAllText(Path.Combine(root,
            "src", "Platform", "ExItS.Platform.Api", "Program.cs"));
        var posHealth = File.ReadAllText(Path.Combine(root,
            "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Api", "Common", "PosHealthEndpoints.cs"));
        var platformHealth = File.ReadAllText(Path.Combine(root,
            "src", "Platform", "ExItS.Platform.Api", "Common", "PlatformHealthEndpoints.cs"));
        var posReady = File.ReadAllText(Path.Combine(root,
            "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Infrastructure", "Health", "PosDatabaseReadyHealthCheck.cs"));
        var platformReady = File.ReadAllText(Path.Combine(root,
            "src", "Platform", "ExItS.Platform.Infrastructure", "Health", "PlatformDatabaseReadyHealthCheck.cs"));

        Assert.Contains("P9-WP05-pilot-and-deployment", pos, StringComparison.Ordinal);
        Assert.Contains("P9-WP05-pilot-and-deployment", platform, StringComparison.Ordinal);
        Assert.Contains("MapPosHealthEndpoints", pos, StringComparison.Ordinal);
        Assert.Contains("MapPlatformHealthEndpoints", platform, StringComparison.Ordinal);
        Assert.Contains("/health/ready", posHealth, StringComparison.Ordinal);
        Assert.Contains("/health/ready", platformHealth, StringComparison.Ordinal);
        Assert.Contains("Predicate = _ => false", posHealth, StringComparison.Ordinal);
        Assert.Contains("Predicate = _ => false", platformHealth, StringComparison.Ordinal);
        Assert.Contains("CanConnectAsync", posReady, StringComparison.Ordinal);
        Assert.Contains("CanConnectAsync", platformReady, StringComparison.Ordinal);
        Assert.DoesNotContain(".Migrate(", pos, StringComparison.Ordinal);
        Assert.DoesNotContain(".Migrate(", platform, StringComparison.Ordinal);
    }

    [Fact]
    public void Sale_and_expense_clients_attach_idempotency_headers_when_entity_id_present()
    {
        var root = FindRepositoryRoot();
        var sale = File.ReadAllText(Path.Combine(root,
            "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.ApiClient", "PosSaleClient.cs"));
        var expense = File.ReadAllText(Path.Combine(root,
            "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.ApiClient", "PosExpenseClient.cs"));

        Assert.Contains("PosMutationIdempotencyHelper.BuildHeaders", sale, StringComparison.Ordinal);
        Assert.Contains("PosMutationIdempotencyHelper.BuildHeaders", expense, StringComparison.Ordinal);
        Assert.Contains("OfflineOperationTypes.SaleCheckout", sale, StringComparison.Ordinal);
        Assert.Contains("OfflineOperationTypes.ExpenseCreate", expense, StringComparison.Ordinal);
    }

    [Fact]
    public void Reporting_batches_repayment_and_category_lookups()
    {
        var root = FindRepositoryRoot();
        var reporting = File.ReadAllText(Path.Combine(root,
            "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Application",
            "Reporting", "ReportingServices.cs"));

        Assert.Contains("SumActiveAmountsByOrganizationAsync", reporting, StringComparison.Ordinal);
        Assert.Contains("ListByIdsAsync", reporting, StringComparison.Ordinal);
        Assert.DoesNotContain("SumActiveAmountAsync(orgId, custId", reporting, StringComparison.Ordinal);
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

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
