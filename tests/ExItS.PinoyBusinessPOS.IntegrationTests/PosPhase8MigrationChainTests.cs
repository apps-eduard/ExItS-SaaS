using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

/// <summary>
/// P8-WP07 closeout: full Phase 8 migration chain apply → stepwise rollback → re-apply
/// from the latest pre-Phase-8 migration (<c>AddPosIdempotencyRecords</c>).
/// </summary>
[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosPhase8MigrationChainTests(PosPostgreSqlFixture fixture)
{
    private static readonly string[] Phase8Migrations =
    [
        "AddPosCatalogAndBarcodes",
        "AddPosSimpleSales",
        "AddProductBasedUtang",
        "AddPosBasicInventory",
        "AddPosExpenses"
    ];

    private static readonly string[] Phase8Tables =
    [
        "product_categories",
        "products",
        "sales",
        "sale_lines",
        "sale_number_sequences",
        "inventory_accounts",
        "stock_movements",
        "expense_categories",
        "expenses",
        "expense_number_sequences"
    ];

    private static readonly string[] DeferredTables =
    [
        "warehouses",
        "payroll",
        "general_ledger",
        "journal_entries",
        "dashboard_totals",
        "report_snapshots",
        "tax_invoices",
        "platform_users",
        "patients"
    ];

    [Fact]
    public async Task Phase8_migration_chain_applies_rolls_back_stepwise_and_reapplies()
    {
        var options = new DbContextOptionsBuilder<PosDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync();
            var applied = (await context.Database.GetAppliedMigrationsAsync()).ToList();
            foreach (var marker in Phase8Migrations)
            {
                Assert.Contains(applied, m => m.Contains(marker, StringComparison.Ordinal));
            }
        }

        var tables = await QueryPosTablesAsync();
        foreach (var table in Phase8Tables)
        {
            Assert.Contains(table, tables);
        }

        foreach (var table in DeferredTables)
        {
            Assert.DoesNotContain(table, tables);
        }

        // Newest → oldest prior (Expenses → Inventory → Utang → Sales → Catalog → pre-Phase-8).
        await MigrateToAsync(options, "20260730193916_AddPosBasicInventory");
        Assert.DoesNotContain("expenses", await QueryPosTablesAsync());
        Assert.DoesNotContain("expense_categories", await QueryPosTablesAsync());
        Assert.Contains("inventory_accounts", await QueryPosTablesAsync());

        await MigrateToAsync(options, "20260730190056_AddProductBasedUtang");
        Assert.DoesNotContain("inventory_accounts", await QueryPosTablesAsync());
        Assert.DoesNotContain("stock_movements", await QueryPosTablesAsync());
        Assert.Contains("sales", await QueryPosTablesAsync());

        await MigrateToAsync(options, "20260730181424_AddPosSimpleSales");
        // Product-Based Utang columns roll back; sales tables remain.
        Assert.Contains("sales", await QueryPosTablesAsync());
        Assert.Contains("sale_lines", await QueryPosTablesAsync());

        await MigrateToAsync(options, "20260730144243_AddPosCatalogAndBarcodes");
        Assert.DoesNotContain("sales", await QueryPosTablesAsync());
        Assert.Contains("products", await QueryPosTablesAsync());
        Assert.Contains("product_categories", await QueryPosTablesAsync());

        await MigrateToAsync(options, "20260730113358_AddPosIdempotencyRecords");
        tables = await QueryPosTablesAsync();
        Assert.DoesNotContain("products", tables);
        Assert.DoesNotContain("product_categories", tables);

        // Re-apply to latest Phase 8 tip.
        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync();
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.Contains(applied, m => m.Contains("AddPosExpenses", StringComparison.Ordinal));
        }

        tables = await QueryPosTablesAsync();
        foreach (var table in Phase8Tables)
        {
            Assert.Contains(table, tables);
        }

        foreach (var table in DeferredTables)
        {
            Assert.DoesNotContain(table, tables);
        }
    }

    private static async Task MigrateToAsync(DbContextOptions<PosDbContext> options, string targetMigration)
    {
        await using var context = new PosDbContext(options);
        await context.Database.MigrateAsync(targetMigration);
    }

    private async Task<IReadOnlyList<string>> QueryPosTablesAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'pos'
            ORDER BY table_name
            """;
        var tables = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }
}
