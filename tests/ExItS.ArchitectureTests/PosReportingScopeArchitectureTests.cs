namespace ExItS.ArchitectureTests;

/// <summary>
/// Guards P8-WP06 dashboard/reports: read-only projections only — no P&amp;L, valuation, accounting,
/// forecasting, or offline authoritative report caches.
/// </summary>
public sealed class PosReportingScopeArchitectureTests
{
    private static readonly string[] OutOfScopeConcepts =
    [
        "ProfitAndLoss",
        "GrossMargin",
        "CostOfGoodsSold",
        "BalanceSheet",
        "CashFlowStatement",
        "TaxReport",
        "InventoryValuation",
        "ForecastRecommendation",
        "ScheduledReportJob",
        "OfflineReportCache",
        "AuthoritativeOfflineReport",
        "CustomReportBuilder"
    ];

    [Fact]
    public void Reporting_slice_declares_no_pnl_valuation_accounting_or_offline_cache_concepts()
    {
        foreach (var file in ReportingSourceFiles())
        {
            var text = File.ReadAllText(file);
            foreach (var concept in OutOfScopeConcepts)
            {
                Assert.DoesNotContain(concept, text, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void Reporting_api_is_get_only_and_does_not_persist_totals()
    {
        var endpoints = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Api"), "Reporting", "ReportingEndpoints.cs"));

        Assert.Contains("MapGet", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPut", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChanges", endpoints, StringComparison.Ordinal);
    }

    [Fact]
    public void No_report_snapshot_tables_are_added()
    {
        var context = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Infrastructure"), "Persistence", "PosDbContext.cs"));

        foreach (var table in new[]
                 {
                     "\"dashboard_totals\"", "\"report_snapshots\"", "\"daily_aggregates\"",
                     "\"report_caches\""
                 })
        {
            Assert.DoesNotContain(table, context, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static IEnumerable<string> ReportingSourceFiles()
    {
        foreach (var project in new[]
                 {
                     "ExItS.PinoyBusinessPOS.Application",
                     "ExItS.PinoyBusinessPOS.Api",
                     "ExItS.PinoyBusinessPOS.ApiClient",
                     "ExItS.PinoyBusinessPOS.Maui"
                 })
        {
            var root = PosProject(project);
            foreach (var dir in new[] { "Reporting", "Components/Pages/Reporting" })
            {
                var path = Path.Combine(root, dir);
                if (!Directory.Exists(path))
                {
                    continue;
                }

                foreach (var file in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
                {
                    if (file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                        || file.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
                    {
                        yield return file;
                    }
                }
            }
        }
    }

    private static string PosProject(string name)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Products", "PinoyBusinessPOS", name);
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException($"Project root not found for {name}.");
    }
}
