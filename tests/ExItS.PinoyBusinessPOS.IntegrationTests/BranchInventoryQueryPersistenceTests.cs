using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class BranchInventoryQueryPersistenceTests(PosPostgreSqlFixture fixture)
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-01T06:00:00Z");
    private static readonly Guid MainBranch = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid RemoteBranch = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task BINV_PAGE_01_remote_list_returns_branch_quantity_without_materializing_balance()
    {
        var options = CreateOptions();
        await MigrateAsync(options);

        var org = Guid.NewGuid();
        var product = CatalogProduct.Create(
            PosOrganizationId.From(org),
            "Paged Coke",
            UnitOfMeasure.Piece,
            25m,
            Now);
        await SaveProductAsync(options, product);
        await SeedTrackedAccountAsync(fixture.ConnectionString, org, product.Id.Value, 50m);

        await using (var db = new PosDbContext(options))
        {
            var repo = new BranchInventoryQueryRepository(db);
            var context = new BranchInventoryContext(org, RemoteBranch, MainBranch, OrganizationGovernance: true);
            var (items, total) = await repo.ListAsync(context, new BranchInventoryListFilter(), skip: 0, take: 50);
            Assert.Equal(1, total);
            Assert.Single(items);
            Assert.Equal(0m, items[0].BranchOnHand);
        }

        await using var verify = new PosDbContext(options);
        var balanceCount = await verify.InventoryBranchBalances
            .CountAsync(b => b.OrganizationId == org && b.BranchId == RemoteBranch && b.ProductId == product.Id.Value);
        Assert.Equal(0, balanceCount);
    }

    [Fact]
    public async Task BINV_PAGE_02_remote_low_stock_pagination_before_skip_take()
    {
        var options = CreateOptions();
        await MigrateAsync(options);

        var org = Guid.NewGuid();
        for (var i = 1; i <= 37; i++)
        {
            var product = CatalogProduct.Create(
                PosOrganizationId.From(org),
                $"Low{i:D2}",
                UnitOfMeasure.Piece,
                10m,
                Now);
            await SaveProductAsync(options, product);
            await SeedTrackedAccountAsync(fixture.ConnectionString, org, product.Id.Value, onHand: 5m);
            await UpsertBranchBalanceAsync(fixture.ConnectionString, org, RemoteBranch, product.Id.Value, onHand: 5m);
            await UpsertBranchReorderAsync(fixture.ConnectionString, org, RemoteBranch, product.Id.Value, reorderLevel: 10m);
        }

        await using var db = new PosDbContext(options);
        var repo = new BranchInventoryQueryRepository(db);
        var context = new BranchInventoryContext(org, RemoteBranch, MainBranch, OrganizationGovernance: true);
        var filter = new BranchInventoryListFilter(TrackedOnly: true, LowStockOnly: true);

        var (page1, total) = await repo.ListAsync(context, filter, skip: 0, take: 20);
        var (page2, total2) = await repo.ListAsync(context, filter, skip: 20, take: 20);

        Assert.Equal(37, total);
        Assert.Equal(37, total2);
        Assert.Equal(20, page1.Count);
        Assert.Equal(17, page2.Count);
        Assert.All(page1.Concat(page2), row => Assert.True(row.IsLowStock));
    }

    private static async Task UpsertBranchBalanceAsync(
        string connectionString,
        Guid org,
        Guid branchId,
        Guid productId,
        decimal onHand)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            """
            INSERT INTO pos.inventory_branch_balances
                (organization_id, branch_id, product_id, on_hand_quantity, updated_at_utc)
            VALUES (@org, @branch, @product, @onHand, NOW() AT TIME ZONE 'UTC')
            ON CONFLICT (organization_id, branch_id, product_id)
            DO UPDATE SET on_hand_quantity = EXCLUDED.on_hand_quantity
            """,
            connection);
        cmd.Parameters.AddWithValue("org", org);
        cmd.Parameters.AddWithValue("branch", branchId);
        cmd.Parameters.AddWithValue("product", productId);
        cmd.Parameters.AddWithValue("onHand", onHand);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task UpsertBranchReorderAsync(
        string connectionString,
        Guid org,
        Guid branchId,
        Guid productId,
        decimal reorderLevel)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            """
            INSERT INTO pos.inventory_branch_reorder_settings
                (organization_id, branch_id, product_id, reorder_level, reorder_quantity, updated_at_utc, updated_by)
            VALUES (@org, @branch, @product, @level, NULL, NOW() AT TIME ZONE 'UTC', @actor)
            ON CONFLICT (organization_id, branch_id, product_id)
            DO UPDATE SET reorder_level = EXCLUDED.reorder_level
            """,
            connection);
        cmd.Parameters.AddWithValue("org", org);
        cmd.Parameters.AddWithValue("branch", branchId);
        cmd.Parameters.AddWithValue("product", productId);
        cmd.Parameters.AddWithValue("level", reorderLevel);
        cmd.Parameters.AddWithValue("actor", Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"));
        await cmd.ExecuteNonQueryAsync();
    }

    private DbContextOptions<PosDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<PosDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

    private static async Task MigrateAsync(DbContextOptions<PosDbContext> options)
    {
        await using var db = new PosDbContext(options);
        await db.Database.MigrateAsync();
    }

    private static async Task SaveProductAsync(DbContextOptions<PosDbContext> options, CatalogProduct product)
    {
        await using var db = new PosDbContext(options);
        await new CatalogProductRepository(db).AddAsync(product);
        await db.SaveChangesAsync();
    }

    private static async Task SeedTrackedAccountAsync(
        string connectionString,
        Guid org,
        Guid productId,
        decimal onHand)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            """
            INSERT INTO pos.inventory_accounts
                (id, organization_id, product_id, is_tracked, reorder_level, reorder_quantity, on_hand_quantity, reserved_quantity, created_at_utc, updated_at_utc)
            VALUES (@id, @org, @product, TRUE, NULL, NULL, @onHand, 0, NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC')
            """,
            connection);
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("org", org);
        cmd.Parameters.AddWithValue("product", productId);
        cmd.Parameters.AddWithValue("onHand", onHand);
        await cmd.ExecuteNonQueryAsync();
    }
}
