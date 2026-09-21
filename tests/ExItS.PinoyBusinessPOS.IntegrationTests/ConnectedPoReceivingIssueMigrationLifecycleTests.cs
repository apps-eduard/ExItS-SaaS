using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

/// <summary>
/// Migration lifecycle for <c>AddConnectedPoReceivingIssues</c>:
/// Apply <c>20260919220000</c> → Apply <c>20260921120000</c> → rollback → re-apply.
/// </summary>
public sealed class ConnectedPoReceivingIssueMigrationLifecycleTests : IAsyncLifetime
{
    private const string BeforeReceivingIssues = "20260919220000_AddBuyerPrepaymentProof";
    private const string ReceivingIssues = "20260921120000_AddConnectedPoReceivingIssues";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:18")
        .Build();

    private string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync() => await _container.StartAsync().ConfigureAwait(false);

    public async Task DisposeAsync() => await _container.DisposeAsync().ConfigureAwait(false);

    [Fact]
    public async Task Receiving_issue_migration_apply_rollback_and_reapply()
    {
        var options = new DbContextOptionsBuilder<PosDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync(BeforeReceivingIssues);
            var applied = (await context.Database.GetAppliedMigrationsAsync()).ToList();
            Assert.Contains(applied, m => m == BeforeReceivingIssues);
            Assert.DoesNotContain(applied, m => m.Contains("AddConnectedPoReceivingIssues", StringComparison.Ordinal));
            Assert.False(await TableExistsAsync(context, "connected_po_receiving_issues"));
        }

        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync(ReceivingIssues);
            var applied = (await context.Database.GetAppliedMigrationsAsync()).ToList();
            Assert.Contains(applied, m => m == ReceivingIssues);
            Assert.True(await TableExistsAsync(context, "connected_po_receiving_issues"));
            Assert.True(await TableExistsAsync(context, "connected_po_receiving_issue_lines"));
        }

        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync(BeforeReceivingIssues);
            var applied = (await context.Database.GetAppliedMigrationsAsync()).ToList();
            Assert.Contains(applied, m => m == BeforeReceivingIssues);
            Assert.DoesNotContain(applied, m => m.Contains("AddConnectedPoReceivingIssues", StringComparison.Ordinal));
            Assert.False(await TableExistsAsync(context, "connected_po_receiving_issues"));
        }

        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync(ReceivingIssues);
            var applied = (await context.Database.GetAppliedMigrationsAsync()).ToList();
            Assert.Contains(applied, m => m == ReceivingIssues);
            Assert.True(await TableExistsAsync(context, "connected_po_receiving_issues"));
        }
    }

    private static async Task<bool> TableExistsAsync(PosDbContext context, string tableName)
    {
        await context.Database.OpenConnectionAsync();
        await using var cmd = context.Database.GetDbConnection().CreateCommand();
        cmd.CommandText =
            "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'pos' AND table_name = @name";
        var p = cmd.CreateParameter();
        p.ParameterName = "name";
        p.Value = tableName;
        cmd.Parameters.Add(p);
        var count = Convert.ToInt64(await cmd.ExecuteScalarAsync());
        return count == 1;
    }
}
