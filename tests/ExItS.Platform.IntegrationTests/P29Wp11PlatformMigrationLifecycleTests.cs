using ExItS.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace ExItS.Platform.IntegrationTests;

/// <summary>
/// P29-WP11: Platform migration apply / rollback / re-apply for
/// <c>StrengthenBranchDeliveryPolicyTenantIntegrity</c> and
/// <c>CloseoutBranchDeliveryPolicyConstraints</c>.
/// Uses a dedicated container so shared-fixture state is not mutated.
/// </summary>
public sealed class P29Wp11PlatformMigrationLifecycleTests : IAsyncLifetime
{
    private const string BeforeStrengthen = "20260816110906_AddBirRegistrationReadinessProfiles";
    private const string StrengthenMigration = "20260816115719_StrengthenBranchDeliveryPolicyTenantIntegrity";
    private const string CloseoutMigration = "20260816121842_CloseoutBranchDeliveryPolicyConstraints";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:18")
        .Build();

    private string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync() => await _container.StartAsync().ConfigureAwait(false);

    public async Task DisposeAsync() => await _container.DisposeAsync().ConfigureAwait(false);

    [Fact]
    public async Task Platform_P29_migrations_clean_apply_rollback_and_reapply()
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        // Clean → latest
        await using (var context = new PlatformDbContext(options))
        {
            await context.Database.MigrateAsync();
            var applied = (await context.Database.GetAppliedMigrationsAsync()).ToList();
            Assert.Contains(applied, m => m == StrengthenMigration);
            Assert.Contains(applied, m => m == CloseoutMigration);
        }

        // Migrate to before Strengthen, then upgrade through Strengthen + closeout
        await using (var context = new PlatformDbContext(options))
        {
            await context.Database.MigrateAsync(BeforeStrengthen);
            var applied = (await context.Database.GetAppliedMigrationsAsync()).ToList();
            Assert.DoesNotContain(applied, m => m.Contains("StrengthenBranchDeliveryPolicyTenantIntegrity", StringComparison.Ordinal));
            Assert.DoesNotContain(applied, m => m.Contains("CloseoutBranchDeliveryPolicyConstraints", StringComparison.Ordinal));
            Assert.Contains(applied, m => m == BeforeStrengthen);
        }

        await using (var context = new PlatformDbContext(options))
        {
            await context.Database.MigrateAsync(StrengthenMigration);
            var applied = (await context.Database.GetAppliedMigrationsAsync()).ToList();
            Assert.Contains(applied, m => m == StrengthenMigration);
            Assert.DoesNotContain(applied, m => m.Contains("CloseoutBranchDeliveryPolicyConstraints", StringComparison.Ordinal));
        }

        await using (var context = new PlatformDbContext(options))
        {
            await context.Database.MigrateAsync();
            var applied = (await context.Database.GetAppliedMigrationsAsync()).ToList();
            Assert.Contains(applied, m => m == CloseoutMigration);
        }

        // Rollback to before Strengthen
        await using (var context = new PlatformDbContext(options))
        {
            await context.Database.MigrateAsync(BeforeStrengthen);
            var applied = (await context.Database.GetAppliedMigrationsAsync()).ToList();
            Assert.DoesNotContain(applied, m => m.Contains("StrengthenBranchDeliveryPolicyTenantIntegrity", StringComparison.Ordinal));
            Assert.DoesNotContain(applied, m => m.Contains("CloseoutBranchDeliveryPolicyConstraints", StringComparison.Ordinal));
        }

        // Re-apply to latest
        await using (var context = new PlatformDbContext(options))
        {
            await context.Database.MigrateAsync();
            var applied = (await context.Database.GetAppliedMigrationsAsync()).ToList();
            Assert.Contains(applied, m => m == StrengthenMigration);
            Assert.Contains(applied, m => m == CloseoutMigration);
        }
    }
}
