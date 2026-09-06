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
public sealed class ReplenishmentCatalogPersistenceTests(PosPostgreSqlFixture fixture)
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-06T18:00:00Z");
    private static readonly Guid Warehouse = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Retail = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task REPL_01_search_by_name_and_sku_and_warehouse_stock_on_page()
    {
        var options = CreateOptions();
        await MigrateAsync(options);

        var org = Guid.NewGuid();
        var rice = CatalogProduct.Create(
            PosOrganizationId.From(org),
            "Jasmine Rice 5kg",
            UnitOfMeasure.Piece,
            50m,
            Now,
            sku: "SKU-RICE-5");
        var soap = CatalogProduct.Create(
            PosOrganizationId.From(org),
            "Bath Soap",
            UnitOfMeasure.Piece,
            20m,
            Now,
            sku: "SKU-SOAP");
        await SaveProductAsync(options, rice);
        await SaveProductAsync(options, soap);
        await SeedTrackedAccountAsync(fixture.ConnectionString, org, rice.Id.Value, onHand: 100m);
        await SeedTrackedAccountAsync(fixture.ConnectionString, org, soap.Id.Value, onHand: 30m);
        await UpsertBranchBalanceAsync(fixture.ConnectionString, org, Retail, rice.Id.Value, onHand: 3m, reserved: 0m);
        await UpsertBranchBalanceAsync(fixture.ConnectionString, org, Warehouse, rice.Id.Value, onHand: 55m, reserved: 5m);
        await UpsertBranchBalanceAsync(fixture.ConnectionString, org, Retail, soap.Id.Value, onHand: 1m, reserved: 0m);
        await UpsertBranchBalanceAsync(fixture.ConnectionString, org, Warehouse, soap.Id.Value, onHand: 12m, reserved: 0m);

        await using var db = new PosDbContext(options);
        var repo = new BranchInventoryQueryRepository(db);
        var context = new BranchInventoryContext(org, Retail, Warehouse, OrganizationGovernance: true);

        var (byName, nameTotal) = await repo.ListReplenishmentCatalogAsync(
            context,
            new ReplenishmentCatalogFilter(Warehouse, Search: "Jasmine"),
            skip: 0,
            take: 40);
        Assert.Equal(1, nameTotal);
        Assert.Single(byName);
        Assert.Equal(rice.Id.Value, byName[0].ProductId);
        Assert.Equal(3m, byName[0].BranchOnHandQuantity);
        Assert.Equal(50m, byName[0].WarehouseAvailableQuantity);

        var (bySku, skuTotal) = await repo.ListReplenishmentCatalogAsync(
            context,
            new ReplenishmentCatalogFilter(Warehouse, Search: "SKU-SOAP"),
            skip: 0,
            take: 40);
        Assert.Equal(1, skuTotal);
        Assert.Single(bySku);
        Assert.Equal(soap.Id.Value, bySku[0].ProductId);
        Assert.Equal(12m, bySku[0].WarehouseAvailableQuantity);
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

    private static async Task UpsertBranchBalanceAsync(
        string connectionString,
        Guid org,
        Guid branchId,
        Guid productId,
        decimal onHand,
        decimal reserved)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            """
            INSERT INTO pos.inventory_branch_balances
                (organization_id, branch_id, product_id, on_hand_quantity, reserved_quantity, updated_at_utc)
            VALUES (@org, @branch, @product, @onHand, @reserved, NOW() AT TIME ZONE 'UTC')
            ON CONFLICT (organization_id, branch_id, product_id)
            DO UPDATE SET on_hand_quantity = EXCLUDED.on_hand_quantity,
                          reserved_quantity = EXCLUDED.reserved_quantity
            """,
            connection);
        cmd.Parameters.AddWithValue("org", org);
        cmd.Parameters.AddWithValue("branch", branchId);
        cmd.Parameters.AddWithValue("product", productId);
        cmd.Parameters.AddWithValue("onHand", onHand);
        cmd.Parameters.AddWithValue("reserved", reserved);
        await cmd.ExecuteNonQueryAsync();
    }
}
