using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

/// <summary>
/// P10-WP08 closeout: Phase 10 migration chain apply → stepwise rollback to last pre-Phase-10
/// migration (<c>AddPosPerformanceIndexes</c>) → re-apply to latest.
/// </summary>
[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosPhase10MigrationChainTests(PosPostgreSqlFixture fixture)
{
    private const string PrePhase10Migration = "20260730212431_AddPosPerformanceIndexes";

    private static readonly string[] Phase10MigrationMarkers =
    [
        "AddPosSuppliers",
        "AddPosPurchasing",
        "EnrichPosGoodsReceiptFields",
        "AddPosAdvancedInventory",
        "EnrichPosStockCountDate",
        "AddPosCashierShifts",
        "AddPosSaleReturns",
        "AddPosOperationalRoles",
        "AddPosRegisters"
    ];

    private static readonly string[] Phase10Tables =
    [
        "suppliers",
        "supplier_code_sequences",
        "purchase_orders",
        "purchase_order_lines",
        "goods_receipts",
        "goods_receipt_lines",
        "stock_counts",
        "stock_count_lines",
        "cashier_shifts",
        "cashier_shift_movements",
        "sale_returns",
        "sale_return_lines",
        "pos_role_assignments",
        "registers",
        "register_code_sequences"
    ];

    private static readonly string[] ForbiddenTables =
    [
        "warehouses",
        "branches",
        "cash_drawers",
        "payroll",
        "general_ledger",
        "journal_entries",
        "accounts_payable",
        "supplier_payments",
        "tax_invoices",
        "platform_users",
        "patients",
        "phi_records"
    ];

    [Fact]
    public async Task Phase10_migration_chain_applies_rolls_back_stepwise_and_reapplies()
    {
        var options = new DbContextOptionsBuilder<PosDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync();
            var applied = (await context.Database.GetAppliedMigrationsAsync()).ToList();
            foreach (var marker in Phase10MigrationMarkers)
            {
                Assert.Contains(applied, m => m.Contains(marker, StringComparison.Ordinal));
            }
        }

        var tables = await QueryPosTablesAsync();
        foreach (var table in Phase10Tables)
        {
            Assert.Contains(table, tables);
        }

        foreach (var table in ForbiddenTables)
        {
            Assert.DoesNotContain(table, tables);
        }

        // Newest → older Phase 10 checkpoints → pre-Phase-10.
        await MigrateToAsync(options, "20260731061054_AddPosOperationalRoles");
        tables = await QueryPosTablesAsync();
        Assert.DoesNotContain("registers", tables);
        Assert.DoesNotContain("register_code_sequences", tables);
        Assert.Contains("pos_role_assignments", tables);

        await MigrateToAsync(options, "20260731052329_AddPosSaleReturns");
        tables = await QueryPosTablesAsync();
        Assert.DoesNotContain("pos_role_assignments", tables);
        Assert.Contains("sale_returns", tables);

        await MigrateToAsync(options, "20260731035548_AddPosCashierShifts");
        tables = await QueryPosTablesAsync();
        Assert.DoesNotContain("sale_returns", tables);
        Assert.Contains("cashier_shifts", tables);

        await MigrateToAsync(options, "20260730235210_EnrichPosStockCountDate");
        tables = await QueryPosTablesAsync();
        Assert.DoesNotContain("cashier_shifts", tables);
        Assert.Contains("stock_counts", tables);

        await MigrateToAsync(options, "20260730232853_EnrichPosGoodsReceiptFields");
        tables = await QueryPosTablesAsync();
        Assert.DoesNotContain("stock_counts", tables);
        Assert.Contains("goods_receipts", tables);
        Assert.Contains("purchase_orders", tables);

        await MigrateToAsync(options, "20260730224635_AddPosSuppliers");
        tables = await QueryPosTablesAsync();
        Assert.DoesNotContain("purchase_orders", tables);
        Assert.DoesNotContain("goods_receipts", tables);
        Assert.Contains("suppliers", tables);

        await MigrateToAsync(options, PrePhase10Migration);
        tables = await QueryPosTablesAsync();
        Assert.DoesNotContain("suppliers", tables);
        Assert.DoesNotContain("supplier_code_sequences", tables);
        Assert.Contains("products", tables);
        Assert.Contains("sales", tables);

        foreach (var table in ForbiddenTables)
        {
            Assert.DoesNotContain(table, tables);
        }

        // Re-apply to latest Phase 10 tip.
        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync();
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.Contains(applied, m => m.Contains("AddPosRegisters", StringComparison.Ordinal));
        }

        tables = await QueryPosTablesAsync();
        foreach (var table in Phase10Tables)
        {
            Assert.Contains(table, tables);
        }

        foreach (var table in ForbiddenTables)
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
