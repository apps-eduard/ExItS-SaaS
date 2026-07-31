using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class AddPosRegistersMigrationTests(PosPostgreSqlFixture fixture)
{
    private const string TargetMigration = "AddPosRegisters";
    private const string PreviousMigration = "20260731061054_AddPosOperationalRoles";

    [Fact]
    public async Task AddPosRegisters_applies_rolls_back_and_reapplies()
    {
        var options = new DbContextOptionsBuilder<PosDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync();
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.Contains(applied, m => m.Contains(TargetMigration, StringComparison.Ordinal));
        }

        var tables = await QueryPosTablesAsync();
        Assert.Contains("registers", tables);
        Assert.Contains("register_code_sequences", tables);

        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync(PreviousMigration);
        }

        tables = await QueryPosTablesAsync();
        Assert.DoesNotContain("registers", tables);
        Assert.DoesNotContain("register_code_sequences", tables);

        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync();
        }

        tables = await QueryPosTablesAsync();
        Assert.Contains("registers", tables);
        Assert.Contains("register_code_sequences", tables);
    }

    private async Task<IReadOnlyList<string>> QueryPosTablesAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'pos'
            """,
            connection);
        await using var reader = await command.ExecuteReaderAsync();
        var names = new List<string>();
        while (await reader.ReadAsync())
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }
}
