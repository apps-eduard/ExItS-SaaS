using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

/// <summary>
/// P29-WP11: POS migration apply / rollback / re-apply for
/// <c>StrengthenCustomerOrderTenantAndMoneyIntegrity</c> and
/// <c>StrengthenCustomerOrderLineTenantForeignKeys</c>.
/// </summary>
public sealed class P29Wp11PosMigrationLifecycleTests : IAsyncLifetime
{
    private const string BeforeStrengthen = "20260816104401_AddCustomerOrdersAndInventoryReservation";
    private const string StrengthenMoney = "20260816115556_StrengthenCustomerOrderTenantAndMoneyIntegrity";
    private const string StrengthenLineFks = "20260816121841_StrengthenCustomerOrderLineTenantForeignKeys";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:18")
        .Build();

    private string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync() => await _container.StartAsync().ConfigureAwait(false);

    public async Task DisposeAsync() => await _container.DisposeAsync().ConfigureAwait(false);

    [Fact]
    public async Task Pos_P29_migrations_clean_apply_rollback_and_reapply()
    {
        var options = new DbContextOptionsBuilder<PosDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        // Clean → latest
        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync();
            var applied = (await context.Database.GetAppliedMigrationsAsync()).ToList();
            Assert.Contains(applied, m => m == StrengthenMoney);
            Assert.Contains(applied, m => m == StrengthenLineFks);
        }

        // Migrate to before money strengthen
        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync(BeforeStrengthen);
            var applied = (await context.Database.GetAppliedMigrationsAsync()).ToList();
            Assert.Contains(applied, m => m == BeforeStrengthen);
            Assert.DoesNotContain(applied, m => m.Contains("StrengthenCustomerOrderTenantAndMoneyIntegrity", StringComparison.Ordinal));
            Assert.DoesNotContain(applied, m => m.Contains("StrengthenCustomerOrderLineTenantForeignKeys", StringComparison.Ordinal));
        }

        // Upgrade through money integrity
        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync(StrengthenMoney);
            var applied = (await context.Database.GetAppliedMigrationsAsync()).ToList();
            Assert.Contains(applied, m => m == StrengthenMoney);
            Assert.DoesNotContain(applied, m => m.Contains("StrengthenCustomerOrderLineTenantForeignKeys", StringComparison.Ordinal));
        }

        // Upgrade to line-tenant FKs (latest)
        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync();
            var applied = (await context.Database.GetAppliedMigrationsAsync()).ToList();
            Assert.Contains(applied, m => m == StrengthenLineFks);
        }

        // Rollback to before money strengthen
        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync(BeforeStrengthen);
            var applied = (await context.Database.GetAppliedMigrationsAsync()).ToList();
            Assert.DoesNotContain(applied, m => m.Contains("StrengthenCustomerOrderTenantAndMoneyIntegrity", StringComparison.Ordinal));
            Assert.DoesNotContain(applied, m => m.Contains("StrengthenCustomerOrderLineTenantForeignKeys", StringComparison.Ordinal));
        }

        // Re-apply to latest
        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync();
            var applied = (await context.Database.GetAppliedMigrationsAsync()).ToList();
            Assert.Contains(applied, m => m == StrengthenMoney);
            Assert.Contains(applied, m => m == StrengthenLineFks);
        }
    }
}
