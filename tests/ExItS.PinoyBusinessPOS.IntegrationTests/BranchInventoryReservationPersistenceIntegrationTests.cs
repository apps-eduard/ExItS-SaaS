using ExItS.PinoyBusinessPOS.Application.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Sales;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

/// <summary>MB2-02B-H2: branch ReservedQuantity persistence round-trips and cutover.</summary>
[Collection(PosPostgreSqlCollection.Name)]
public sealed class BranchInventoryReservationPersistenceIntegrationTests(PosPostgreSqlFixture fixture)
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-01T12:00:00Z");
    private static readonly Guid Main = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Remote = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Actor = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task H2_PERSIST_01_reserve_survives_fresh_dbcontext()
    {
        var options = CreateOptions();
        await MigrateAsync(options);
        var (org, productId) = await SeedProductAndBalancesAsync(options, onHand: 100m, reserved: 0m);

        await using (var db = new PosDbContext(options))
        {
            var repo = new InventoryBranchBalanceRepository(db);
            var balance = await repo.GetAsync(PosOrganizationId.From(org), PosBranchId.From(Main), productId);
            Assert.NotNull(balance);
            balance!.Reserve(10m, Now);
            await repo.UpsertAsync(balance);
            await db.SaveChangesAsync();
        }

        await using var reload = new PosDbContext(options);
        var reloaded = await new InventoryBranchBalanceRepository(reload)
            .GetAsync(PosOrganizationId.From(org), PosBranchId.From(Main), productId);
        Assert.NotNull(reloaded);
        Assert.Equal(100m, reloaded!.OnHandQuantity);
        Assert.Equal(10m, reloaded.ReservedQuantity);
        Assert.Equal(90m, reloaded.AvailableQuantity);
    }

    [Fact]
    public async Task H2_PERSIST_02_release_survives_fresh_dbcontext()
    {
        var options = CreateOptions();
        await MigrateAsync(options);
        var (org, productId) = await SeedProductAndBalancesAsync(options, onHand: 100m, reserved: 10m);

        await using (var db = new PosDbContext(options))
        {
            var repo = new InventoryBranchBalanceRepository(db);
            var balance = await repo.GetAsync(PosOrganizationId.From(org), PosBranchId.From(Main), productId);
            Assert.NotNull(balance);
            balance!.Release(4m, Now);
            await repo.UpsertAsync(balance);
            await db.SaveChangesAsync();
        }

        var reloaded = await ReloadAsync(options, org, Main, productId);
        Assert.Equal(100m, reloaded.OnHandQuantity);
        Assert.Equal(6m, reloaded.ReservedQuantity);
        Assert.Equal(94m, reloaded.AvailableQuantity);
    }

    [Fact]
    public async Task H2_PERSIST_03_consume_survives_fresh_dbcontext()
    {
        var options = CreateOptions();
        await MigrateAsync(options);
        var (org, productId) = await SeedProductAndBalancesAsync(options, onHand: 100m, reserved: 10m);

        await using (var db = new PosDbContext(options))
        {
            var repo = new InventoryBranchBalanceRepository(db);
            var balance = await repo.GetAsync(PosOrganizationId.From(org), PosBranchId.From(Main), productId);
            Assert.NotNull(balance);
            balance!.ConsumeReservation(10m, Now);
            await repo.UpsertAsync(balance);
            await db.SaveChangesAsync();
        }

        var reloaded = await ReloadAsync(options, org, Main, productId);
        Assert.Equal(90m, reloaded.OnHandQuantity);
        Assert.Equal(0m, reloaded.ReservedQuantity);
        Assert.Equal(90m, reloaded.AvailableQuantity);
    }

    [Fact]
    public async Task H2_PERSIST_04_repeated_reserve_updates_existing_row()
    {
        var options = CreateOptions();
        await MigrateAsync(options);
        var (org, productId) = await SeedProductAndBalancesAsync(options, onHand: 100m, reserved: 10m);

        await using (var db = new PosDbContext(options))
        {
            var repo = new InventoryBranchBalanceRepository(db);
            var balance = await repo.GetAsync(PosOrganizationId.From(org), PosBranchId.From(Main), productId);
            Assert.NotNull(balance);
            balance!.Reserve(5m, Now);
            await repo.UpsertAsync(balance);
            await db.SaveChangesAsync();
        }

        var reloaded = await ReloadAsync(options, org, Main, productId);
        Assert.Equal(100m, reloaded.OnHandQuantity);
        Assert.Equal(15m, reloaded.ReservedQuantity);
        Assert.Equal(85m, reloaded.AvailableQuantity);
    }

    [Fact]
    public async Task H2_PERSIST_05A_existing_row_and_05B_new_row_both_persist_reserved()
    {
        var options = CreateOptions();
        await MigrateAsync(options);
        var (org, productId) = await SeedProductAndBalancesAsync(options, onHand: 100m, reserved: 0m, seedRemote: false);

        await using (var db = new PosDbContext(options))
        {
            var repo = new InventoryBranchBalanceRepository(db);
            var existing = await repo.GetAsync(PosOrganizationId.From(org), PosBranchId.From(Main), productId);
            Assert.NotNull(existing);
            existing!.Reserve(7m, Now);
            await repo.UpsertAsync(existing);

            var created = InventoryBranchBalance.Create(
                PosOrganizationId.From(org),
                PosBranchId.From(Remote),
                productId,
                20m,
                Now);
            created.Reserve(8m, Now);
            await repo.UpsertAsync(created);
            await db.SaveChangesAsync();
        }

        var main = await ReloadAsync(options, org, Main, productId);
        var remote = await ReloadAsync(options, org, Remote, productId);
        Assert.Equal(7m, main.ReservedQuantity);
        Assert.Equal(8m, remote.ReservedQuantity);
        Assert.Equal(20m, remote.OnHandQuantity);
    }

    [Fact]
    public async Task H2_PERSIST_E2E_01_sale_reserve_affects_available_after_restart()
    {
        var options = CreateOptions();
        await MigrateAsync(options);
        var (org, productId, accountId) = await SeedTrackedWithAccountIdAsync(options, 100m);
        await UpsertBranchBalanceSqlAsync(org, Main, productId.Value, 100m, 0m);

        var sale = AwaitingSale(org, productId, Main, 10m);
        await using (var db = new PosDbContext(options))
        {
            var stock = CreateSaleStock(db, Main);
            await stock.ReserveForAwaitingPaymentAsync(sale, Actor, Now, branchId: Main);
            await db.SaveChangesAsync();
        }

        await using (var db = new PosDbContext(options))
        {
            var balance = await new InventoryBranchBalanceRepository(db)
                .GetAsync(PosOrganizationId.From(org), PosBranchId.From(Main), productId);
            Assert.NotNull(balance);
            Assert.Equal(100m, balance!.OnHandQuantity);
            Assert.Equal(10m, balance.ReservedQuantity);
            Assert.Equal(90m, balance.AvailableQuantity);

            var account = await new InventoryRepository(db)
                .GetByProductIdAsync(PosOrganizationId.From(org), productId);
            Assert.NotNull(account);
            Assert.Equal(10m, account!.ReservedQuantity);
            Assert.Equal(accountId, account.Id.Value);
        }
    }

    [Fact]
    public async Task H2_PERSIST_E2E_02_payment_after_restart_consumes_once()
    {
        var options = CreateOptions();
        await MigrateAsync(options);
        var (org, productId, _) = await SeedTrackedWithAccountIdAsync(options, 100m);
        await UpsertBranchBalanceSqlAsync(org, Main, productId.Value, 100m, 0m);
        var product = await LoadProductAsync(options, org, productId);

        var sale = AwaitingSale(org, productId, Main, 10m);
        await using (var db = new PosDbContext(options))
        {
            await CreateSaleStock(db, Main).ReserveForAwaitingPaymentAsync(sale, Actor, Now, branchId: Main);
            await db.SaveChangesAsync();
        }

        await using (var db = new PosDbContext(options))
        {
            await CreateSaleStock(db, Main)
                .ConsumeReservedForPaidAsync(sale, new Dictionary<Guid, CatalogProduct> { [productId.Value] = product }, Actor, Now, branchId: Main);
            await db.SaveChangesAsync();
        }

        var balance = await ReloadAsync(options, org, Main, productId);
        Assert.Equal(90m, balance.OnHandQuantity);
        Assert.Equal(0m, balance.ReservedQuantity);

        await using var verify = new PosDbContext(options);
        var account = await new InventoryRepository(verify).GetByProductIdAsync(PosOrganizationId.From(org), productId);
        Assert.Equal(90m, account!.OnHandQuantity);
        Assert.Equal(0m, account.ReservedQuantity);
        var deductions = await verify.StockMovements.CountAsync(m =>
            m.OrganizationId == org
            && m.ProductId == productId.Value
            && m.MovementType == "SaleDeduction");
        Assert.Equal(1, deductions);
    }

    [Fact]
    public async Task H2_PERSIST_E2E_03_cancel_after_restart_releases_only()
    {
        var options = CreateOptions();
        await MigrateAsync(options);
        var (org, productId, _) = await SeedTrackedWithAccountIdAsync(options, 100m);
        await UpsertBranchBalanceSqlAsync(org, Main, productId.Value, 100m, 0m);

        var sale = AwaitingSale(org, productId, Main, 10m);
        await using (var db = new PosDbContext(options))
        {
            await CreateSaleStock(db, Main).ReserveForAwaitingPaymentAsync(sale, Actor, Now, branchId: Main);
            await db.SaveChangesAsync();
        }

        await using (var db = new PosDbContext(options))
        {
            await CreateSaleStock(db, Main).ReleaseIfReservedAsync(sale, Now, branchId: Main);
            await db.SaveChangesAsync();
        }

        var balance = await ReloadAsync(options, org, Main, productId);
        Assert.Equal(100m, balance.OnHandQuantity);
        Assert.Equal(0m, balance.ReservedQuantity);
        await using var verify = new PosDbContext(options);
        Assert.Equal(0, await verify.StockMovements.CountAsync(m => m.OrganizationId == org));
    }

    [Fact]
    public async Task H2_PERSIST_E2E_04_customer_order_cancel_after_restart()
    {
        var options = CreateOptions();
        await MigrateAsync(options);
        var (org, productId, _) = await SeedTrackedWithAccountIdAsync(options, 20m);
        await UpsertBranchBalanceSqlAsync(org, Remote, productId.Value, 20m, 0m);

        var order = AcceptedOrder(org, productId, Remote, 8m);
        await using (var db = new PosDbContext(options))
        {
            await CreateOrderStock(db, Main).ReserveForAcceptAsync(order, Actor, Now);
            await db.SaveChangesAsync();
        }

        var mid = await ReloadAsync(options, org, Remote, productId);
        Assert.Equal(20m, mid.OnHandQuantity);
        Assert.Equal(8m, mid.ReservedQuantity);

        await using (var db = new PosDbContext(options))
        {
            await CreateOrderStock(db, Main).ReleaseIfReservedAsync(order, Now);
            await db.SaveChangesAsync();
        }

        var end = await ReloadAsync(options, org, Remote, productId);
        Assert.Equal(20m, end.OnHandQuantity);
        Assert.Equal(0m, end.ReservedQuantity);
    }

    [Fact]
    public async Task H2_CUTOVER_01_no_active_reservations_leaves_zero()
    {
        var options = CreateOptions();
        await MigrateAsync(options);
        var (org, productId) = await SeedProductAndBalancesAsync(options, 100m, 0m);
        await SetOrgReservedAsync(org, productId.Value, 0m);

        await using var db = new PosDbContext(options);
        var result = await new BranchInventoryReservationCutover(db).ReconcileAsync(org);
        Assert.Equal(0, result.BalancesUpdated);
        var balance = await ReloadAsync(options, org, Main, productId);
        Assert.Equal(0m, balance.ReservedQuantity);
        Assert.Equal(100m, balance.OnHandQuantity);
    }

    [Fact]
    public async Task H2_CUTOVER_02_active_sale_sets_branch_reserved()
    {
        var options = CreateOptions();
        await MigrateAsync(options);
        var (org, productId) = await SeedProductAndBalancesAsync(options, 100m, 0m);
        await SetOrgReservedAsync(org, productId.Value, 10m);
        await SeedReservedSaleAsync(options, org, productId.Value, Main, 10m);

        await using (var db = new PosDbContext(options))
        {
            await new BranchInventoryReservationCutover(db).ReconcileAsync(org);
        }

        var balance = await ReloadAsync(options, org, Main, productId);
        Assert.Equal(100m, balance.OnHandQuantity);
        Assert.Equal(10m, balance.ReservedQuantity);
        Assert.Equal(90m, balance.AvailableQuantity);
        await AssertOrgAsync(options, org, productId, onHand: 100m, reserved: 10m);
        await AssertNoMovementsAsync(options, org);
    }

    [Fact]
    public async Task H2_CUTOVER_03_active_customer_order_sets_remote_reserved()
    {
        var options = CreateOptions();
        await MigrateAsync(options);
        var (org, productId) = await SeedProductAndBalancesAsync(options, onHand: 20m, reserved: 0m, mainOnHand: 0m, remoteOnHand: 20m);
        await SetOrgReservedAsync(org, productId.Value, 8m);
        await SeedReservedOrderAsync(options, org, productId.Value, Remote, 8m);

        await using (var db = new PosDbContext(options))
        {
            await new BranchInventoryReservationCutover(db).ReconcileAsync(org);
        }

        var remote = await ReloadAsync(options, org, Remote, productId);
        Assert.Equal(20m, remote.OnHandQuantity);
        Assert.Equal(8m, remote.ReservedQuantity);
        Assert.Equal(12m, remote.AvailableQuantity);
        await AssertNoMovementsAsync(options, org);
    }

    [Fact]
    public async Task H2_CUTOVER_04_multiple_documents_same_branch_aggregate()
    {
        var options = CreateOptions();
        await MigrateAsync(options);
        var (org, productId) = await SeedProductAndBalancesAsync(options, 50m, 0m, mainOnHand: 50m, remoteOnHand: 0m);
        await SetOrgReservedAsync(org, productId.Value, 23m);
        await SeedReservedSaleAsync(options, org, productId.Value, Main, 10m, saleNumber: "H2-A");
        await SeedReservedSaleAsync(options, org, productId.Value, Main, 5m, saleNumber: "H2-B");
        await SeedReservedOrderAsync(options, org, productId.Value, Main, 8m, orderNumber: "SO-000023");

        await using (var db = new PosDbContext(options))
        {
            await new BranchInventoryReservationCutover(db).ReconcileAsync(org);
        }

        var balance = await ReloadAsync(options, org, Main, productId);
        Assert.Equal(23m, balance.ReservedQuantity);
        Assert.Equal(27m, balance.AvailableQuantity);
    }

    [Fact]
    public async Task H2_CUTOVER_05_cross_branch_no_leakage()
    {
        var options = CreateOptions();
        await MigrateAsync(options);
        var (org, productId) = await SeedProductAndBalancesAsync(options, 100m, 0m, mainOnHand: 50m, remoteOnHand: 50m);
        await SetOrgReservedAsync(org, productId.Value, 30m);
        await SeedReservedSaleAsync(options, org, productId.Value, Main, 20m);
        await SeedReservedOrderAsync(options, org, productId.Value, Remote, 10m);

        await using (var db = new PosDbContext(options))
        {
            await new BranchInventoryReservationCutover(db).ReconcileAsync(org);
        }

        Assert.Equal(20m, (await ReloadAsync(options, org, Main, productId)).ReservedQuantity);
        Assert.Equal(10m, (await ReloadAsync(options, org, Remote, productId)).ReservedQuantity);
    }

    [Fact]
    public async Task H2_CUTOVER_06_unknown_branch_fails_closed()
    {
        var options = CreateOptions();
        await MigrateAsync(options);
        var (org, productId) = await SeedProductAndBalancesAsync(options, 100m, 0m);
        await SetOrgReservedAsync(org, productId.Value, 5m);
        await SeedReservedSaleAsync(options, org, productId.Value, branchId: null, qty: 5m);

        await using var db = new PosDbContext(options);
        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            new BranchInventoryReservationCutover(db).ReconcileAsync(org));
        Assert.Equal(DomainErrorCodes.InventoryBranchReservationCutoverUnresolvedSaleBranch, ex.ErrorCode);
        Assert.Equal(0m, (await ReloadAsync(options, org, Main, productId)).ReservedQuantity);
    }

    [Fact]
    public async Task H2_CUTOVER_07_over_reserved_fails_closed()
    {
        var options = CreateOptions();
        await MigrateAsync(options);
        // Org on-hand can hold the reservation; branch on-hand is the over-reserve subject.
        var (org, productId) = await SeedProductAndBalancesAsync(options, onHand: 10m, reserved: 0m, mainOnHand: 5m);
        await SetOrgReservedAsync(org, productId.Value, 10m);
        await SeedReservedSaleAsync(options, org, productId.Value, Main, 10m);

        await using var db = new PosDbContext(options);
        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            new BranchInventoryReservationCutover(db).ReconcileAsync(org));
        Assert.Equal(DomainErrorCodes.InventoryBranchReservationCutoverOverReserved, ex.ErrorCode);
        Assert.Equal(0m, (await ReloadAsync(options, org, Main, productId)).ReservedQuantity);
    }

    [Fact]
    public async Task H2_CUTOVER_DOUBLE_01_consumed_order_not_double_counted_with_none_sale()
    {
        var options = CreateOptions();
        await MigrateAsync(options);
        var (org, productId) = await SeedProductAndBalancesAsync(options, 100m, 0m);
        await SetOrgReservedAsync(org, productId.Value, 0m);
        await SeedOrderWithStateAsync(options, org, productId.Value, Main, 10m, "Consumed");

        await using (var db = new PosDbContext(options))
        {
            var result = await new BranchInventoryReservationCutover(db).ReconcileAsync(org);
            Assert.Equal(0, result.BranchProductGroups);
        }

        Assert.Equal(0m, (await ReloadAsync(options, org, Main, productId)).ReservedQuantity);
    }

    [Fact]
    public async Task H2_constraints_still_reject_reserved_over_on_hand()
    {
        var options = CreateOptions();
        await MigrateAsync(options);
        var (org, productId) = await SeedProductAndBalancesAsync(options, 5m, 0m, mainOnHand: 5m);

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            """
            UPDATE pos.inventory_branch_balances
            SET reserved_quantity = 10
            WHERE organization_id = @org AND branch_id = @branch AND product_id = @product
            """,
            connection);
        cmd.Parameters.AddWithValue("org", org);
        cmd.Parameters.AddWithValue("branch", Main);
        cmd.Parameters.AddWithValue("product", productId.Value);
        await Assert.ThrowsAsync<PostgresException>(async () => await cmd.ExecuteNonQueryAsync());
    }

    private SaleStockService CreateSaleStock(PosDbContext db, Guid primary) =>
        new(
            new InventoryRepository(db),
            new InventoryLotStockService(new InventoryLotRepository(db)),
            new InventoryBranchBalanceRepository(db),
            new FixedPrimaryDirectory(primary));

    private CustomerOrderStockService CreateOrderStock(PosDbContext db, Guid primary) =>
        new(
            new InventoryRepository(db),
            new InventoryBranchBalanceRepository(db),
            new FixedPrimaryDirectory(primary));

    private static Sale AwaitingSale(Guid org, CatalogProductId productId, Guid branchId, decimal qty)
    {
        var saleId = SaleId.New();
        var line = SaleLine.Create(
            saleId,
            PosOrganizationId.From(org),
            1,
            new SaleLineDraft(productId, "Item", "SKU", null, UnitOfMeasure.Piece, 10m, qty));
        return Sale.Rehydrate(
            saleId,
            PosOrganizationId.From(org),
            $"S-{Guid.NewGuid():N}"[..20],
            SaleStatus.AwaitingPayment,
            SalePaymentMethod.GCash,
            line.LineTotal,
            line.LineTotal,
            0m,
            null,
            null,
            null,
            Now,
            Actor,
            null,
            null,
            null,
            Now,
            [line],
            stockReservationState: SaleStockReservationState.None,
            branchId: PosBranchId.From(branchId));
    }

    private static CustomerOrder AcceptedOrder(Guid org, CatalogProductId productId, Guid branchId, decimal qty)
    {
        var order = CustomerOrder.CreateSubmitted(
            PosOrganizationId.From(org),
            $"SO-{Random.Shared.Next(1, 999999):D6}",
            CustomerOrderParty.Personal(Guid.Parse("44444444-4444-4444-4444-444444444444"), "Buyer"),
            CustomerOrderFulfillmentType.Pickup,
            branchId,
            "Branch",
            [new CustomerOrderLineDraft(productId, "Item", "SKU", UnitOfMeasure.Piece, qty, 25m)],
            Actor,
            Now);
        order.Accept(Actor, Now);
        return order;
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

    private static async Task<InventoryBranchBalance> ReloadAsync(
        DbContextOptions<PosDbContext> options,
        Guid org,
        Guid branchId,
        CatalogProductId productId)
    {
        await using var db = new PosDbContext(options);
        var balance = await new InventoryBranchBalanceRepository(db)
            .GetAsync(PosOrganizationId.From(org), PosBranchId.From(branchId), productId);
        Assert.NotNull(balance);
        return balance!;
    }

    private async Task<(Guid Org, CatalogProductId ProductId)> SeedProductAndBalancesAsync(
        DbContextOptions<PosDbContext> options,
        decimal onHand,
        decimal reserved,
        bool seedRemote = true,
        decimal? mainOnHand = null,
        decimal? remoteOnHand = null)
    {
        var (org, productId, _) = await SeedTrackedWithAccountIdAsync(options, onHand);
        await UpsertBranchBalanceSqlAsync(org, Main, productId.Value, mainOnHand ?? onHand, reserved);
        if (seedRemote)
        {
            await UpsertBranchBalanceSqlAsync(org, Remote, productId.Value, remoteOnHand ?? 0m, 0m);
        }

        return (org, productId);
    }

    private async Task<(Guid Org, CatalogProductId ProductId, Guid AccountId)> SeedTrackedWithAccountIdAsync(
        DbContextOptions<PosDbContext> options,
        decimal onHand)
    {
        var org = Guid.NewGuid();
        var product = CatalogProduct.Create(PosOrganizationId.From(org), $"P-{org:N}"[..20], UnitOfMeasure.Piece, 10m, Now);
        await using (var db = new PosDbContext(options))
        {
            await new CatalogProductRepository(db).AddAsync(product);
            await db.SaveChangesAsync();
        }

        var accountId = Guid.NewGuid();
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            """
            INSERT INTO pos.inventory_accounts
                (id, organization_id, product_id, is_tracked, reorder_level, reorder_quantity,
                 on_hand_quantity, reserved_quantity, created_at_utc, updated_at_utc)
            VALUES (@id, @org, @product, TRUE, NULL, NULL, @onHand, 0, NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC')
            """,
            connection);
        cmd.Parameters.AddWithValue("id", accountId);
        cmd.Parameters.AddWithValue("org", org);
        cmd.Parameters.AddWithValue("product", product.Id.Value);
        cmd.Parameters.AddWithValue("onHand", onHand);
        await cmd.ExecuteNonQueryAsync();
        return (org, product.Id, accountId);
    }

    private async Task UpsertBranchBalanceSqlAsync(
        Guid org,
        Guid branchId,
        Guid productId,
        decimal onHand,
        decimal reserved)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            """
            INSERT INTO pos.inventory_branch_balances
                (organization_id, branch_id, product_id, on_hand_quantity, reserved_quantity, updated_at_utc)
            VALUES (@org, @branch, @product, @onHand, @reserved, NOW() AT TIME ZONE 'UTC')
            ON CONFLICT (organization_id, branch_id, product_id)
            DO UPDATE SET
                on_hand_quantity = EXCLUDED.on_hand_quantity,
                reserved_quantity = EXCLUDED.reserved_quantity,
                updated_at_utc = EXCLUDED.updated_at_utc
            """,
            connection);
        cmd.Parameters.AddWithValue("org", org);
        cmd.Parameters.AddWithValue("branch", branchId);
        cmd.Parameters.AddWithValue("product", productId);
        cmd.Parameters.AddWithValue("onHand", onHand);
        cmd.Parameters.AddWithValue("reserved", reserved);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task SetOrgReservedAsync(Guid org, Guid productId, decimal reserved)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            """
            UPDATE pos.inventory_accounts
            SET reserved_quantity = @reserved
            WHERE organization_id = @org AND product_id = @product
            """,
            connection);
        cmd.Parameters.AddWithValue("org", org);
        cmd.Parameters.AddWithValue("product", productId);
        cmd.Parameters.AddWithValue("reserved", reserved);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task SeedReservedSaleAsync(
        DbContextOptions<PosDbContext> options,
        Guid org,
        Guid productId,
        Guid? branchId,
        decimal qty,
        string? saleNumber = null)
    {
        await using var db = new PosDbContext(options);
        var saleId = Guid.NewGuid();
        var number = saleNumber ?? $"H2S-{saleId:N}"[..18];
        db.Sales.Add(new SaleRecord
        {
            Id = saleId,
            OrganizationId = org,
            SaleNumber = number,
            Status = nameof(SaleStatus.AwaitingPayment),
            PaymentMethod = nameof(SalePaymentMethod.GCash),
            Subtotal = qty * 10m,
            Total = qty * 10m,
            TaxAmount = 0m,
            GrossSubtotal = qty * 10m,
            LineDiscountTotal = 0m,
            SaleDiscountTotal = 0m,
            DiscountTotal = 0m,
            BuyerPartyKind = "WalkIn",
            BranchId = branchId,
            StockReservationState = nameof(SaleStockReservationState.Reserved),
            RecordedAtUtc = Now,
            RecordedBy = Actor,
            UpdatedAtUtc = Now
        });
        db.SaleLines.Add(new SaleLineRecord
        {
            Id = Guid.NewGuid(),
            SaleId = saleId,
            OrganizationId = org,
            ProductId = productId,
            LineNumber = 1,
            NameSnapshot = "Item",
            UnitOfMeasureSnapshot = nameof(UnitOfMeasure.Piece),
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

    private static async Task SeedReservedOrderAsync(
        DbContextOptions<PosDbContext> options,
        Guid org,
        Guid productId,
        Guid branchId,
        decimal qty,
        string? orderNumber = null) =>
        await SeedOrderWithStateAsync(options, org, productId, branchId, qty, "Reserved", orderNumber);

    private static async Task SeedOrderWithStateAsync(
        DbContextOptions<PosDbContext> options,
        Guid org,
        Guid productId,
        Guid branchId,
        decimal qty,
        string reservationState,
        string? orderNumber = null)
    {
        await using var db = new PosDbContext(options);
        var orderId = Guid.NewGuid();
        db.CustomerOrders.Add(new CustomerOrderRecord
        {
            Id = orderId,
            SellerOrganizationId = org,
            OrderNumber = orderNumber ?? $"SO-{Random.Shared.Next(1, 999999):D6}",
            Status = nameof(CustomerOrderStatus.Accepted),
            FulfillmentStatus = "Pending",
            PaymentStatus = "Unpaid",
            PaymentMethod = nameof(CustomerOrderPaymentMethod.Cash),
            FulfillmentType = nameof(CustomerOrderFulfillmentType.Pickup),
            FulfillmentBranchId = branchId,
            BranchNameSnapshot = "Branch",
            CustomerPartyType = "Personal",
            CustomerDisplayNameSnapshot = "Buyer",
            CustomerPlatformUserId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            MerchandiseSubtotal = qty * 25m,
            DeliveryFee = 0m,
            Total = qty * 25m,
            StockReservationState = reservationState,
            CreatedAtUtc = Now,
            SubmittedAtUtc = Now,
            SubmittedBy = Actor,
            AcceptedAtUtc = Now,
            AcceptedBy = Actor,
            UpdatedAtUtc = Now
        });
        db.CustomerOrderLines.Add(new CustomerOrderLineRecord
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            SellerOrganizationId = org,
            ProductId = productId,
            LineNumber = 1,
            NameSnapshot = "Item",
            UnitSnapshot = nameof(UnitOfMeasure.Piece),
            Quantity = qty,
            UnitPrice = 25m,
            Discount = 0m,
            LineTotal = qty * 25m
        });
        await db.SaveChangesAsync();
    }

    private static async Task<CatalogProduct> LoadProductAsync(
        DbContextOptions<PosDbContext> options,
        Guid org,
        CatalogProductId productId)
    {
        await using var db = new PosDbContext(options);
        var product = await new CatalogProductRepository(db).GetByIdAsync(PosOrganizationId.From(org), productId);
        Assert.NotNull(product);
        return product!;
    }

    private static async Task AssertOrgAsync(
        DbContextOptions<PosDbContext> options,
        Guid org,
        CatalogProductId productId,
        decimal onHand,
        decimal reserved)
    {
        await using var db = new PosDbContext(options);
        var account = await new InventoryRepository(db).GetByProductIdAsync(PosOrganizationId.From(org), productId);
        Assert.NotNull(account);
        Assert.Equal(onHand, account!.OnHandQuantity);
        Assert.Equal(reserved, account.ReservedQuantity);
    }

    private static async Task AssertNoMovementsAsync(DbContextOptions<PosDbContext> options, Guid org)
    {
        await using var db = new PosDbContext(options);
        Assert.Equal(0, await db.StockMovements.CountAsync(m => m.OrganizationId == org));
    }

    private sealed class FixedPrimaryDirectory(Guid primaryId) : IOrganizationBranchDirectory
    {
        public Task<bool> ExistsInOrganizationAsync(Guid organizationId, Guid branchId, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(
            Guid organizationId,
            IReadOnlyCollection<Guid> branchIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());

        public Task<Guid?> GetPrimaryBranchIdAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Guid?>(primaryId);
    }
}
