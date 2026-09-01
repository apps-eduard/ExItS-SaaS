using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Sales;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

/// <summary>MB2-02B-H2: H1 column migration + H2 cutover apply / rollback / re-apply.</summary>
public sealed class ReconcileBranchInventoryReservationsMigrationTests : IAsyncLifetime
{
    private const string H1 = "20260901120000_AddBranchInventoryReservations";
    private const string H2 = "20260901133000_ReconcileBranchInventoryReservations";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:18")
        .Build();

    private string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync() => await _container.StartAsync().ConfigureAwait(false);

    public async Task DisposeAsync() => await _container.DisposeAsync().ConfigureAwait(false);

    [Fact]
    public async Task H2_migration_apply_cutover_rollback_zeros_reserved_reapply_and_fail_closed()
    {
        var options = new DbContextOptionsBuilder<PosDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        await using (var db = new PosDbContext(options))
        {
            await db.Database.MigrateAsync(H1);
            var applied = await db.Database.GetAppliedMigrationsAsync();
            Assert.Contains(applied, m => m == H1);
            Assert.DoesNotContain(applied, m => m == H2);
        }

        var org = Guid.NewGuid();
        var productId = await CreateProductAsync(options, org);
        var branch = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var actor = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        await SeedAccountAndBalanceAsync(org, productId, branch, onHand: 100m, orgReserved: 10m);
        await SeedReservedSaleAsync(org, productId, branch, 10m, actor);

        await using (var db = new PosDbContext(options))
        {
            await db.Database.MigrateAsync(H2);
            Assert.Contains(await db.Database.GetAppliedMigrationsAsync(), m => m == H2);
        }

        Assert.Equal(10m, await ReadBranchReservedAsync(org, branch, productId));
        Assert.Equal(100m, await ReadBranchOnHandAsync(org, branch, productId));

        await using (var db = new PosDbContext(options))
        {
            await db.Database.MigrateAsync(H1);
            var applied = await db.Database.GetAppliedMigrationsAsync();
            Assert.Contains(applied, m => m == H1);
            Assert.DoesNotContain(applied, m => m == H2);
        }

        Assert.Equal(0m, await ReadBranchReservedAsync(org, branch, productId));
        Assert.Equal(100m, await ReadBranchOnHandAsync(org, branch, productId));

        await using (var db = new PosDbContext(options))
        {
            await db.Database.MigrateAsync(H2);
        }

        Assert.Equal(10m, await ReadBranchReservedAsync(org, branch, productId));
        Assert.Equal(100m, await ReadBranchOnHandAsync(org, branch, productId));

        var constraints = await QueryNamesAsync(
            """
            SELECT conname
            FROM pg_constraint
            WHERE conname IN (
              'ck_inventory_branch_balances_reserved_non_negative',
              'ck_inventory_branch_balances_reserved_not_over_on_hand')
            """);
        Assert.Contains("ck_inventory_branch_balances_reserved_non_negative", constraints);
        Assert.Contains("ck_inventory_branch_balances_reserved_not_over_on_hand", constraints);

        // Fail-closed unresolved branch: roll back H2, seed bad sale, H2 must refuse.
        await using (var db = new PosDbContext(options))
        {
            await db.Database.MigrateAsync(H1);
        }

        Assert.Equal(0m, await ReadBranchReservedAsync(org, branch, productId));

        var badOrg = Guid.NewGuid();
        var badProduct = await CreateProductAsync(options, badOrg);
        await SeedAccountAndBalanceAsync(badOrg, badProduct, branch, onHand: 100m, orgReserved: 5m);
        await SeedReservedSaleAsync(badOrg, badProduct, branchId: null, qty: 5m, actor);

        await using var dbFail = new PosDbContext(options);
        var ex = await Assert.ThrowsAnyAsync<Exception>(() => dbFail.Database.MigrateAsync(H2));
        Assert.Contains("unresolved_sale_branch", ex.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<Guid> CreateProductAsync(DbContextOptions<PosDbContext> options, Guid org)
    {
        var product = CatalogProduct.Create(
            PosOrganizationId.From(org),
            $"Cutover-{Guid.NewGuid():N}"[..20],
            UnitOfMeasure.Piece,
            10m,
            DateTimeOffset.UtcNow);
        await using var db = new PosDbContext(options);
        await new CatalogProductRepository(db).AddAsync(product);
        await db.SaveChangesAsync();
        return product.Id.Value;
    }

    private async Task SeedAccountAndBalanceAsync(
        Guid org,
        Guid product,
        Guid branch,
        decimal onHand,
        decimal orgReserved)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var accountCmd = new NpgsqlCommand(
            """
            INSERT INTO pos.inventory_accounts
                (id, organization_id, product_id, is_tracked, reorder_level, reorder_quantity,
                 on_hand_quantity, reserved_quantity, created_at_utc, updated_at_utc)
            VALUES (@id, @org, @product, TRUE, NULL, NULL, @onHand, @reserved, NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC')
            """,
            connection);
        accountCmd.Parameters.AddWithValue("id", Guid.NewGuid());
        accountCmd.Parameters.AddWithValue("org", org);
        accountCmd.Parameters.AddWithValue("product", product);
        accountCmd.Parameters.AddWithValue("onHand", onHand);
        accountCmd.Parameters.AddWithValue("reserved", orgReserved);
        await accountCmd.ExecuteNonQueryAsync();

        await using var balanceCmd = new NpgsqlCommand(
            """
            INSERT INTO pos.inventory_branch_balances
                (organization_id, branch_id, product_id, on_hand_quantity, reserved_quantity, updated_at_utc)
            VALUES (@org, @branch, @product, @onHand, 0, NOW() AT TIME ZONE 'UTC')
            """,
            connection);
        balanceCmd.Parameters.AddWithValue("org", org);
        balanceCmd.Parameters.AddWithValue("branch", branch);
        balanceCmd.Parameters.AddWithValue("product", product);
        balanceCmd.Parameters.AddWithValue("onHand", onHand);
        await balanceCmd.ExecuteNonQueryAsync();
    }

    private async Task SeedReservedSaleAsync(Guid org, Guid product, Guid? branchId, decimal qty, Guid actor)
    {
        await using var db = new PosDbContext(new DbContextOptionsBuilder<PosDbContext>().UseNpgsql(ConnectionString).Options);
        var saleId = Guid.NewGuid();
        db.Sales.Add(new SaleRecord
        {
            Id = saleId,
            OrganizationId = org,
            SaleNumber = $"MIG-{saleId:N}"[..18],
            Status = "AwaitingPayment",
            PaymentMethod = "GCash",
            Subtotal = qty * 10m,
            Total = qty * 10m,
            TaxAmount = 0m,
            GrossSubtotal = qty * 10m,
            LineDiscountTotal = 0m,
            SaleDiscountTotal = 0m,
            DiscountTotal = 0m,
            BuyerPartyKind = "WalkIn",
            BranchId = branchId,
            StockReservationState = "Reserved",
            RecordedAtUtc = DateTimeOffset.UtcNow,
            RecordedBy = actor,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        });
        db.SaleLines.Add(new SaleLineRecord
        {
            Id = Guid.NewGuid(),
            SaleId = saleId,
            OrganizationId = org,
            ProductId = product,
            LineNumber = 1,
            NameSnapshot = "Item",
            UnitOfMeasureSnapshot = "Piece",
            SellingModeSnapshot = "PerItem",
            UnitPrice = 10m,
            Quantity = qty,
            LineTotal = qty * 10m,
            GrossLineTotal = qty * 10m,
            LineDiscountAmount = 0m,
            SaleDiscountAllocatedAmount = 0m
        });
        await db.SaveChangesAsync();
    }

    private async Task<decimal> ReadBranchReservedAsync(Guid org, Guid branch, Guid product)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            """
            SELECT reserved_quantity
            FROM pos.inventory_branch_balances
            WHERE organization_id = @org AND branch_id = @branch AND product_id = @product
            """,
            connection);
        cmd.Parameters.AddWithValue("org", org);
        cmd.Parameters.AddWithValue("branch", branch);
        cmd.Parameters.AddWithValue("product", product);
        return (decimal)(await cmd.ExecuteScalarAsync())!;
    }

    private async Task<decimal> ReadBranchOnHandAsync(Guid org, Guid branch, Guid product)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            """
            SELECT on_hand_quantity
            FROM pos.inventory_branch_balances
            WHERE organization_id = @org AND branch_id = @branch AND product_id = @product
            """,
            connection);
        cmd.Parameters.AddWithValue("org", org);
        cmd.Parameters.AddWithValue("branch", branch);
        cmd.Parameters.AddWithValue("product", product);
        return (decimal)(await cmd.ExecuteScalarAsync())!;
    }

    private async Task<IReadOnlyList<string>> QueryNamesAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var names = new List<string>();
        while (await reader.ReadAsync())
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }
}
