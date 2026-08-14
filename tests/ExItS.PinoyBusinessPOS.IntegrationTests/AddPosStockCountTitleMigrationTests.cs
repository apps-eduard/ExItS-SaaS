using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class AddPosStockCountTitleMigrationTests(PosPostgreSqlFixture fixture)
{
    private const string TargetMigration = "AddPosStockCountTitle";
    private const string PreviousMigration = "AddPosCashDenominationsAndRequiredDefault";

    [Fact]
    public async Task AddPosStockCountTitle_backfills_historical_title_and_rolls_back()
    {
        var options = new DbContextOptionsBuilder<PosDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        await using (var context = new PosDbContext(options))
        {
            var previous = (await context.Database.GetAppliedMigrationsAsync())
                .SingleOrDefault(m => m.Contains(PreviousMigration, StringComparison.Ordinal));
            if (previous is null)
            {
                await context.Database.MigrateAsync();
                previous = (await context.Database.GetAppliedMigrationsAsync())
                    .Single(m => m.Contains(PreviousMigration, StringComparison.Ordinal));
            }

            await context.Database.MigrateAsync(previous);
        }

        var historicalId = Guid.NewGuid();
        await ExecuteAsync(
            """
            INSERT INTO pos.stock_counts (
                id, organization_id, status, count_date, notes, created_at_utc, updated_at_utc)
            VALUES (
                @id, @org, 'Completed', DATE '2026-08-01', NULL, TIMESTAMPTZ '2026-08-01 04:00:00Z', TIMESTAMPTZ '2026-08-01 04:36:00Z');
            """,
            ("id", historicalId),
            ("org", Guid.NewGuid()));

        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync();
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.Contains(applied, m => m.Contains(TargetMigration, StringComparison.Ordinal));
        }

        var title = await QueryScalarAsync(
            "SELECT title FROM pos.stock_counts WHERE id = @id",
            ("id", historicalId));
        Assert.Equal("Stock count", title);

        var nullable = await QueryScalarAsync(
            """
            SELECT is_nullable
            FROM information_schema.columns
            WHERE table_schema = 'pos' AND table_name = 'stock_counts' AND column_name = 'title'
            """);
        Assert.Equal("NO", nullable);

        await using (var context = new PosDbContext(options))
        {
            var previous = (await context.Database.GetAppliedMigrationsAsync())
                .Single(m => m.Contains(PreviousMigration, StringComparison.Ordinal));
            await context.Database.MigrateAsync(previous);
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.DoesNotContain(applied, m => m.Contains(TargetMigration, StringComparison.Ordinal));
        }

        var afterRollback = await QueryNamesAsync(
            """
            SELECT column_name
            FROM information_schema.columns
            WHERE table_schema = 'pos' AND table_name = 'stock_counts'
            """);
        Assert.DoesNotContain("title", afterRollback);

        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync();
        }
    }

    private async Task ExecuteAsync(string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        await command.ExecuteNonQueryAsync();
    }

    private async Task<HashSet<string>> QueryNamesAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }

    private async Task<string?> QueryScalarAsync(string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        return (await command.ExecuteScalarAsync())?.ToString();
    }
}
