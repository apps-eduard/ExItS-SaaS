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
using Testcontainers.PostgreSql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

/// <summary>MB2-02B-H3 exact branch reservation projection and write-authority closure.</summary>
[Collection(PosPostgreSqlCollection.Name)]
public sealed class BranchInventoryReservationExactProjectionIntegrationTests(PosPostgreSqlFixture fixture)
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-01T14:00:00Z");
    private static readonly Guid Main = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid MicaA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid MicaB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid Remote = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Actor = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task H3_STALE_01_no_active_reservations_clears_stale_branch_reserved()
    {
        var options = CreateOptions();
        await MigrateAsync(options);
        var (org, productId) = await SeedProductAndBalancesAsync(options, 100m, mainReserved: 10m);
        await SetOrgReservedAsync(org, productId.Value, 0m);

        await using (var db = new PosDbContext(options))
        {
            var audit = await new BranchInventoryReservationCutover(db).AuditAsync(org);
            Assert.Equal(1, audit.MismatchedBalanceCount);
            await new BranchInventoryReservationCutover(db).ReconcileAsync(org);
        }

        var balance = await ReloadAsync(options, org, Main, productId);
        Assert.Equal(100m, balance.OnHandQuantity);
        Assert.Equal(0m, balance.ReservedQuantity);
        Assert.Equal(100m, balance.AvailableQuantity);
    }

    [Fact]
    public async Task H3_STALE_02_mixed_stale_and_active_exact_projection()
    {
        var options = CreateOptions();
        await MigrateAsync(options);
        var (org, productId) = await SeedProductAndBalancesAsync(
            options,
            150m,
            mainOnHand: 100m,
            mainReserved: 15m,
            remoteOnHand: 50m,
            remoteReserved: 10m);
        await SetOrgReservedAsync(org, productId.Value, 10m);
        await SeedReservedOrderAsync(options, org, productId.Value, Remote, 10m);

        await using (var db = new PosDbContext(options))
        {
            await new BranchInventoryReservationCutover(db).ReconcileAsync(org);
        }

        var main = await ReloadAsync(options, org, Main, productId);
        var remote = await ReloadAsync(options, org, Remote, productId);
        Assert.Equal(0m, main.ReservedQuantity);
        Assert.Equal(10m, remote.ReservedQuantity);
        Assert.Equal(40m, remote.AvailableQuantity);
    }

    [Fact]
    public async Task H3_STALE_03_stale_on_second_product_only()
    {
        var options = CreateOptions();
        await MigrateAsync(options);
        var org = Guid.NewGuid();
        var productA = await CreateProductAsync(options, org, "ProdA");
        var productB = await CreateProductAsync(options, org, "ProdB");
        await SeedAccountAsync(org, productA, onHand: 20m, reserved: 8m);
        await SeedAccountAsync(org, productB, onHand: 30m, reserved: 0m);
        await UpsertBranchBalanceSqlAsync(org, Main, productA, 20m, 8m);
        await UpsertBranchBalanceSqlAsync(org, Main, productB, 30m, 7m);
        await SeedReservedOrderAsync(options, org, productA, Main, 8m);

        await using (var db = new PosDbContext(options))
        {
            await new BranchInventoryReservationCutover(db).ReconcileAsync(org);
        }

        Assert.Equal(8m, (await ReloadAsync(options, org, Main, CatalogProductId.From(productA))).ReservedQuantity);
        Assert.Equal(0m, (await ReloadAsync(options, org, Main, CatalogProductId.From(productB))).ReservedQuantity);
    }

    [Fact]
    public async Task H3_PROJECTION_01_multi_branch_exact_projection()
    {
        var options = CreateOptions();
        await MigrateAsync(options);
        var (org, productId) = await SeedProductAndBalancesAsync(
            options,
            100m,
            seedRemote: false,
            mainOnHand: 50m,
            mainReserved: 40m);
        await UpsertBranchBalanceSqlAsync(org, MicaA, productId.Value, 30m, 1m);
        await UpsertBranchBalanceSqlAsync(org, MicaB, productId.Value, 20m, 7m);
        await SetOrgReservedAsync(org, productId.Value, 25m);
        await SeedReservedSaleAsync(options, org, productId.Value, Main, 15m);
        await SeedReservedOrderAsync(options, org, productId.Value, MicaA, 10m);

        await using (var db = new PosDbContext(options))
        {
            await new BranchInventoryReservationCutover(db).ReconcileAsync(org);
        }

        Assert.Equal(15m, (await ReloadAsync(options, org, Main, productId)).ReservedQuantity);
        Assert.Equal(35m, (await ReloadAsync(options, org, Main, productId)).AvailableQuantity);
        Assert.Equal(10m, (await ReloadAsync(options, org, MicaA, productId)).ReservedQuantity);
        Assert.Equal(20m, (await ReloadAsync(options, org, MicaA, productId)).AvailableQuantity);
        Assert.Equal(0m, (await ReloadAsync(options, org, MicaB, productId)).ReservedQuantity);
        Assert.Equal(20m, (await ReloadAsync(options, org, MicaB, productId)).AvailableQuantity);
    }

    [Fact]
    public async Task H3_SCOPE_01_org_scoped_reconcile_leaves_other_org_stale()
    {
        var options = CreateOptions();
        await MigrateAsync(options);
        var (orgA, productA) = await SeedProductAndBalancesAsync(options, 100m, mainReserved: 10m);
        var (orgB, productB) = await SeedProductAndBalancesAsync(options, 100m, mainReserved: 12m);
        await SetOrgReservedAsync(orgA, productA.Value, 0m);
        await SetOrgReservedAsync(orgB, productB.Value, 0m);

        await using (var db = new PosDbContext(options))
        {
            await new BranchInventoryReservationCutover(db).ReconcileAsync(orgA);
        }

        Assert.Equal(0m, (await ReloadAsync(options, orgA, Main, productA)).ReservedQuantity);
        Assert.Equal(12m, (await ReloadAsync(options, orgB, Main, productB)).ReservedQuantity);
    }

    [Fact]
    public async Task H3_IDEMPOTENT_01_second_reconcile_updates_nothing()
    {
        var options = CreateOptions();
        await MigrateAsync(options);
        var (org, productId) = await SeedProductAndBalancesAsync(options, 100m, mainReserved: 10m);
        await SetOrgReservedAsync(org, productId.Value, 0m);

        await using (var db = new PosDbContext(options))
        {
            var first = await new BranchInventoryReservationCutover(db).ReconcileAsync(org);
            Assert.Equal(1, first.BalancesUpdated);
            var second = await new BranchInventoryReservationCutover(db).ReconcileAsync(org);
            Assert.Equal(0, second.BalancesUpdated);
            Assert.Equal(0, second.MismatchedBalanceCount);
        }
    }

    [Fact]
    public async Task H3_ATOMIC_01_validation_failure_leaves_stale_unchanged()
    {
        var options = CreateOptions();
        await MigrateAsync(options);
        var (org, productId) = await SeedProductAndBalancesAsync(
            options,
            100m,
            mainOnHand: 100m,
            mainReserved: 10m,
            remoteOnHand: 5m,
            remoteReserved: 0m);
        await SetOrgReservedAsync(org, productId.Value, 10m);
        await SeedReservedSaleAsync(options, org, productId.Value, Remote, 10m);

        await using var db = new PosDbContext(options);
        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            new BranchInventoryReservationCutover(db).ReconcileAsync(org));
        Assert.Equal(DomainErrorCodes.InventoryBranchReservationCutoverOverReserved, ex.ErrorCode);
        Assert.Equal(10m, (await ReloadAsync(options, org, Main, productId)).ReservedQuantity);
    }

    [Fact]
    public async Task H3_LIFECYCLE_01_released_sale_clears_stale_on_reconcile()
    {
        var options = CreateOptions();
        await MigrateAsync(options);
        var (org, productId) = await SeedProductAndBalancesAsync(options, 100m, mainReserved: 10m);
        await SetOrgReservedAsync(org, productId.Value, 0m);
        await SeedOrderWithStateAsync(options, org, productId.Value, Main, 10m, "Released");

        await using (var db = new PosDbContext(options))
        {
            await new BranchInventoryReservationCutover(db).ReconcileAsync(org);
        }

        var balance = await ReloadAsync(options, org, Main, productId);
        Assert.Equal(100m, balance.OnHandQuantity);
        Assert.Equal(0m, balance.ReservedQuantity);
    }

    [Fact]
    public async Task H3_LIFECYCLE_02_consumed_reservation_stays_zero()
    {
        var options = CreateOptions();
        await MigrateAsync(options);
        var (org, productId) = await SeedProductAndBalancesAsync(options, 90m, mainReserved: 0m);
        await SetOrgReservedAsync(org, productId.Value, 0m);
        await SeedReservedSaleAsync(options, org, productId.Value, Main, 10m, state: "Consumed");

        await using (var db = new PosDbContext(options))
        {
            await new BranchInventoryReservationCutover(db).ReconcileAsync(org);
        }

        var balance = await ReloadAsync(options, org, Main, productId);
        Assert.Equal(90m, balance.OnHandQuantity);
        Assert.Equal(0m, balance.ReservedQuantity);
    }

    [Fact]
    public async Task H3_RESTART_01_stale_cleared_after_scope_restart()
    {
        var options = CreateOptions();
        await MigrateAsync(options);
        var (org, productId) = await SeedProductAndBalancesAsync(options, 100m, mainReserved: 10m);
        await SetOrgReservedAsync(org, productId.Value, 0m);

        await using (var db = new PosDbContext(options))
        {
            await new BranchInventoryReservationCutover(db).ReconcileAsync(org);
        }

        var balance = await ReloadAsync(options, org, Main, productId);
        Assert.Equal(0m, balance.ReservedQuantity);
        Assert.Equal(100m, balance.AvailableQuantity);
    }

    [Fact]
    public async Task H3_RESTART_02_active_reservation_exact_after_restart()
    {
        var options = CreateOptions();
        await MigrateAsync(options);
        var (org, productId) = await SeedProductAndBalancesAsync(options, 100m, mainReserved: 20m);
        await SetOrgReservedAsync(org, productId.Value, 10m);
        await SeedReservedSaleAsync(options, org, productId.Value, Main, 10m);

        await using (var db = new PosDbContext(options))
        {
            await new BranchInventoryReservationCutover(db).ReconcileAsync(org);
        }

        var balance = await ReloadAsync(options, org, Main, productId);
        Assert.Equal(10m, balance.ReservedQuantity);
        Assert.Equal(90m, balance.AvailableQuantity);
    }

    [Fact]
    public async Task H3_MICA_STORE_stale_reservation_cleared_when_org_zero()
    {
        var options = CreateOptions();
        await MigrateAsync(options);
        var (org, productId) = await SeedProductAndBalancesAsync(
            options,
            5m,
            mainOnHand: 5m,
            mainReserved: 4m);
        await SetOrgReservedAsync(org, productId.Value, 0m);

        await using (var db = new PosDbContext(options))
        {
            await new BranchInventoryReservationCutover(db).ReconcileAsync(org);
        }

        var micaA = await ReloadAsync(options, org, Main, productId);
        Assert.Equal(5m, micaA.OnHandQuantity);
        Assert.Equal(0m, micaA.ReservedQuantity);
        Assert.Equal(5m, micaA.AvailableQuantity);
    }

    [Fact]
    public async Task H3_MICA_STORE_reservation_lifecycle_via_services()
    {
        var options = CreateOptions();
        await MigrateAsync(options);
        var org = Guid.NewGuid();
        var product = CatalogProduct.Create(PosOrganizationId.From(org), "Coke 1L", UnitOfMeasure.Piece, 10m, Now);
        await using (var db = new PosDbContext(options))
        {
            await new CatalogProductRepository(db).AddAsync(product);
            await db.SaveChangesAsync();
        }

        await SeedAccountAsync(org, product.Id.Value, 85m, 0m);
        await UpsertBranchBalanceSqlAsync(org, Main, product.Id.Value, 70m, 0m);
        await UpsertBranchBalanceSqlAsync(org, MicaA, product.Id.Value, 5m, 0m);
        await UpsertBranchBalanceSqlAsync(org, MicaB, product.Id.Value, 10m, 0m);

        var sale = AwaitingSale(org, product.Id, MicaA, 4m);
        await using (var db = new PosDbContext(options))
        {
            var stock = CreateSaleStock(db, Main);
            await stock.ReserveForAwaitingPaymentAsync(sale, Actor, Now, branchId: MicaA);
            await db.SaveChangesAsync();
        }

        await using (var db = new PosDbContext(options))
        {
            var micaA = await ReloadAsync(options, org, MicaA, product.Id);
            Assert.Equal(5m, micaA.OnHandQuantity);
            Assert.Equal(4m, micaA.ReservedQuantity);
            Assert.Equal(1m, micaA.AvailableQuantity);
            var account = await new InventoryRepository(db).GetByProductIdAsync(PosOrganizationId.From(org), product.Id);
            Assert.Equal(4m, account!.ReservedQuantity);
        }

        await using (var db = new PosDbContext(options))
        {
            await CreateSaleStock(db, Main).ReleaseIfReservedAsync(sale, Now, branchId: MicaA);
            await db.SaveChangesAsync();
        }

        await using (var db = new PosDbContext(options))
        {
            var micaA = await ReloadAsync(options, org, MicaA, product.Id);
            Assert.Equal(0m, micaA.ReservedQuantity);
            Assert.Equal(5m, micaA.AvailableQuantity);
        }

        await using (var db = new PosDbContext(options))
        {
            await CreateSaleStock(db, Main).ReserveForAwaitingPaymentAsync(sale, Actor, Now, branchId: MicaA);
            await db.SaveChangesAsync();
        }

        await using (var db = new PosDbContext(options))
        {
            var catalog = await new CatalogProductRepository(db).GetByIdAsync(PosOrganizationId.From(org), product.Id);
            await CreateSaleStock(db, Main).ConsumeReservedForPaidAsync(
                sale,
                new Dictionary<Guid, CatalogProduct> { [product.Id.Value] = catalog! },
                Actor,
                Now,
                branchId: MicaA);
            await db.SaveChangesAsync();
        }

        var finalMain = await ReloadAsync(options, org, Main, product.Id);
        var finalA = await ReloadAsync(options, org, MicaA, product.Id);
        var finalB = await ReloadAsync(options, org, MicaB, product.Id);
        Assert.Equal(70m, finalMain.OnHandQuantity);
        Assert.Equal(1m, finalA.OnHandQuantity);
        Assert.Equal(10m, finalB.OnHandQuantity);
        Assert.Equal(0m, finalA.ReservedQuantity);
        Assert.Equal(81m, finalMain.OnHandQuantity + finalA.OnHandQuantity + finalB.OnHandQuantity);
    }

    private DbContextOptions<PosDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<PosDbContext>().UseNpgsql(fixture.ConnectionString).Options;

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
        decimal mainReserved = 0m,
        decimal? mainOnHand = null,
        decimal? remoteOnHand = null,
        decimal remoteReserved = 0m,
        bool seedRemote = true)
    {
        var org = Guid.NewGuid();
        var product = CatalogProduct.Create(PosOrganizationId.From(org), $"P-{org:N}"[..20], UnitOfMeasure.Piece, 10m, Now);
        await using (var db = new PosDbContext(options))
        {
            await new CatalogProductRepository(db).AddAsync(product);
            await db.SaveChangesAsync();
        }

        await SeedAccountAsync(org, product.Id.Value, onHand, 0m);
        await UpsertBranchBalanceSqlAsync(org, Main, product.Id.Value, mainOnHand ?? onHand, mainReserved);
        if (seedRemote)
        {
            await UpsertBranchBalanceSqlAsync(org, Remote, product.Id.Value, remoteOnHand ?? 0m, remoteReserved);
        }

        return (org, product.Id);
    }

    private async Task<Guid> CreateProductAsync(DbContextOptions<PosDbContext> options, Guid org, string name)
    {
        var product = CatalogProduct.Create(PosOrganizationId.From(org), name, UnitOfMeasure.Piece, 10m, Now);
        await using var db = new PosDbContext(options);
        await new CatalogProductRepository(db).AddAsync(product);
        await db.SaveChangesAsync();
        return product.Id.Value;
    }

    private async Task SeedAccountAsync(Guid org, Guid productId, decimal onHand, decimal reserved)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            """
            INSERT INTO pos.inventory_accounts
                (id, organization_id, product_id, is_tracked, reorder_level, reorder_quantity,
                 on_hand_quantity, reserved_quantity, created_at_utc, updated_at_utc)
            VALUES (@id, @org, @product, TRUE, NULL, NULL, @onHand, @reserved, NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC')
            """,
            connection);
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("org", org);
        cmd.Parameters.AddWithValue("product", productId);
        cmd.Parameters.AddWithValue("onHand", onHand);
        cmd.Parameters.AddWithValue("reserved", reserved);
        await cmd.ExecuteNonQueryAsync();
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
            DO UPDATE SET on_hand_quantity = EXCLUDED.on_hand_quantity,
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
        Guid branchId,
        decimal qty,
        string state = "Reserved")
    {
        await using var db = new PosDbContext(options);
        var saleId = Guid.NewGuid();
        db.Sales.Add(new SaleRecord
        {
            Id = saleId,
            OrganizationId = org,
            SaleNumber = $"H3S-{saleId:N}"[..18],
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
            StockReservationState = state,
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

    private static async Task SeedReservedOrderAsync(
        DbContextOptions<PosDbContext> options,
        Guid org,
        Guid productId,
        Guid branchId,
        decimal qty) =>
        await SeedOrderWithStateAsync(options, org, productId, branchId, qty, "Reserved");

    private static async Task SeedOrderWithStateAsync(
        DbContextOptions<PosDbContext> options,
        Guid org,
        Guid productId,
        Guid branchId,
        decimal qty,
        string reservationState)
    {
        await using var db = new PosDbContext(options);
        var orderId = Guid.NewGuid();
        db.CustomerOrders.Add(new CustomerOrderRecord
        {
            Id = orderId,
            SellerOrganizationId = org,
            OrderNumber = "SO-000100",
            Status = "Accepted",
            FulfillmentStatus = "Pending",
            PaymentStatus = "Unpaid",
            PaymentMethod = "Cash",
            FulfillmentType = "Pickup",
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
            UnitSnapshot = "Piece",
            Quantity = qty,
            UnitPrice = 25m,
            Discount = 0m,
            LineTotal = qty * 25m
        });
        await db.SaveChangesAsync();
    }

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

    private SaleStockService CreateSaleStock(PosDbContext db, Guid primary) =>
        new(
            new InventoryRepository(db),
            new InventoryLotStockService(new InventoryLotRepository(db)),
            new InventoryBranchBalanceRepository(db),
            new FixedPrimaryDirectory(primary));

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

/// <summary>H3 migration apply/rollback against isolated PostgreSQL.</summary>
public sealed class ExactProjectBranchInventoryReservationsMigrationTests : IAsyncLifetime
{
    private const string H2 = "20260901133000_ReconcileBranchInventoryReservations";
    private const string H3 = "20260901143000_ExactProjectBranchInventoryReservations";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder().WithImage("postgres:18").Build();
    private string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync() => await _container.StartAsync().ConfigureAwait(false);
    public async Task DisposeAsync() => await _container.DisposeAsync().ConfigureAwait(false);

    [Fact]
    public async Task H3_MIGRATION_01_clears_stale_zero_active()
    {
        var options = Options();
        await MigrateToH2Async(options);
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(options, org);
        await SeedAccountAsync(org, product, 100m, 0m);
        await SeedBalanceAsync(org, Main, product, 100m, 10m);

        await ApplyH3Async(options);

        Assert.Equal(0m, await ReadReservedAsync(org, Main, product));
        Assert.Equal(100m, await ReadOnHandAsync(org, Main, product));
    }

    [Fact]
    public async Task H3_MIGRATION_02_mixed_stale_and_active()
    {
        var options = Options();
        await MigrateToH2Async(options);
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(options, org);
        await SeedAccountAsync(org, product, 58m, 8m);
        await SeedBalanceAsync(org, Main, product, 50m, 20m);
        await SeedBalanceAsync(org, Remote, product, 8m, 3m);
        await SeedReservedOrderAsync(options, org, product, Remote, 8m);

        await ApplyH3Async(options);

        Assert.Equal(0m, await ReadReservedAsync(org, Main, product));
        Assert.Equal(8m, await ReadReservedAsync(org, Remote, product));
    }

    [Fact]
    public async Task H3_MIGRATION_FAIL_01_over_reserved_rolls_back_stale()
    {
        var options = Options();
        await MigrateToH2Async(options);
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(options, org);
        await SeedAccountAsync(org, product, 15m, 10m);
        await SeedBalanceAsync(org, Main, product, 100m, 10m);
        await SeedBalanceAsync(org, Remote, product, 5m, 0m);
        await SeedReservedOrderAsync(options, org, product, Remote, 10m);

        await using var db = new PosDbContext(options);
        var ex = await Assert.ThrowsAnyAsync<Exception>(() => db.Database.MigrateAsync(H3));
        Assert.Contains("over_reserved", ex.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(10m, await ReadReservedAsync(org, Main, product));
    }

    [Fact]
    public async Task H3_MIGRATION_03_down_is_noop_and_reapply_idempotent()
    {
        var options = Options();
        await MigrateToH2Async(options);
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(options, org);
        await SeedAccountAsync(org, product, 100m, 0m);
        await SeedBalanceAsync(org, Main, product, 100m, 7m);

        await ApplyH3Async(options);
        Assert.Equal(0m, await ReadReservedAsync(org, Main, product));

        await UpdateBalanceReservedAsync(org, Main, product, 7m);

        await using (var db = new PosDbContext(options))
        {
            await db.Database.MigrateAsync(H2);
        }

        Assert.Equal(7m, await ReadReservedAsync(org, Main, product));

        await ApplyH3Async(options);
        Assert.Equal(0m, await ReadReservedAsync(org, Main, product));
    }

    private static readonly Guid Main = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Remote = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Actor = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-01T14:00:00Z");

    private DbContextOptions<PosDbContext> Options() =>
        new DbContextOptionsBuilder<PosDbContext>().UseNpgsql(ConnectionString).Options;

    private static async Task MigrateToH2Async(DbContextOptions<PosDbContext> options)
    {
        await using var db = new PosDbContext(options);
        await db.Database.MigrateAsync(H2);
    }

    private static async Task ApplyH3Async(DbContextOptions<PosDbContext> options)
    {
        await using var db = new PosDbContext(options);
        await db.Database.MigrateAsync(H3);
    }

    private async Task<Guid> CreateProductAsync(DbContextOptions<PosDbContext> options, Guid org)
    {
        var product = CatalogProduct.Create(PosOrganizationId.From(org), "MigProd", UnitOfMeasure.Piece, 10m, Now);
        await using var db = new PosDbContext(options);
        await new CatalogProductRepository(db).AddAsync(product);
        await db.SaveChangesAsync();
        return product.Id.Value;
    }

    private async Task SeedAccountAsync(Guid org, Guid product, decimal onHand, decimal reserved)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            """
            INSERT INTO pos.inventory_accounts
                (id, organization_id, product_id, is_tracked, reorder_level, reorder_quantity,
                 on_hand_quantity, reserved_quantity, created_at_utc, updated_at_utc)
            VALUES (@id, @org, @product, TRUE, NULL, NULL, @onHand, @reserved, NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC')
            """,
            connection);
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("org", org);
        cmd.Parameters.AddWithValue("product", product);
        cmd.Parameters.AddWithValue("onHand", onHand);
        cmd.Parameters.AddWithValue("reserved", reserved);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task SeedBalanceAsync(Guid org, Guid branch, Guid product, decimal onHand, decimal reserved)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            """
            INSERT INTO pos.inventory_branch_balances
                (organization_id, branch_id, product_id, on_hand_quantity, reserved_quantity, updated_at_utc)
            VALUES (@org, @branch, @product, @onHand, @reserved, NOW() AT TIME ZONE 'UTC')
            """,
            connection);
        cmd.Parameters.AddWithValue("org", org);
        cmd.Parameters.AddWithValue("branch", branch);
        cmd.Parameters.AddWithValue("product", product);
        cmd.Parameters.AddWithValue("onHand", onHand);
        cmd.Parameters.AddWithValue("reserved", reserved);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task UpdateBalanceReservedAsync(Guid org, Guid branch, Guid product, decimal reserved)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            """
            UPDATE pos.inventory_branch_balances
            SET reserved_quantity = @reserved,
                updated_at_utc = NOW() AT TIME ZONE 'UTC'
            WHERE organization_id = @org
              AND branch_id = @branch
              AND product_id = @product
            """,
            connection);
        cmd.Parameters.AddWithValue("org", org);
        cmd.Parameters.AddWithValue("branch", branch);
        cmd.Parameters.AddWithValue("product", product);
        cmd.Parameters.AddWithValue("reserved", reserved);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task SeedReservedOrderAsync(
        DbContextOptions<PosDbContext> options,
        Guid org,
        Guid product,
        Guid branch,
        decimal qty)
    {
        await using var db = new PosDbContext(options);
        var orderId = Guid.NewGuid();
        db.CustomerOrders.Add(new CustomerOrderRecord
        {
            Id = orderId,
            SellerOrganizationId = org,
            OrderNumber = "SO-000200",
            Status = "Accepted",
            FulfillmentStatus = "Pending",
            PaymentStatus = "Unpaid",
            PaymentMethod = "Cash",
            FulfillmentType = "Pickup",
            FulfillmentBranchId = branch,
            BranchNameSnapshot = "Branch",
            CustomerPartyType = "Personal",
            CustomerDisplayNameSnapshot = "Buyer",
            CustomerPlatformUserId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            MerchandiseSubtotal = qty * 25m,
            DeliveryFee = 0m,
            Total = qty * 25m,
            StockReservationState = "Reserved",
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
            ProductId = product,
            LineNumber = 1,
            NameSnapshot = "Item",
            UnitSnapshot = "Piece",
            Quantity = qty,
            UnitPrice = 25m,
            Discount = 0m,
            LineTotal = qty * 25m
        });
        await db.SaveChangesAsync();
    }

    private async Task<decimal> ReadReservedAsync(Guid org, Guid branch, Guid product)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            """
            SELECT reserved_quantity FROM pos.inventory_branch_balances
            WHERE organization_id = @org AND branch_id = @branch AND product_id = @product
            """,
            connection);
        cmd.Parameters.AddWithValue("org", org);
        cmd.Parameters.AddWithValue("branch", branch);
        cmd.Parameters.AddWithValue("product", product);
        return (decimal)(await cmd.ExecuteScalarAsync())!;
    }

    private async Task<decimal> ReadOnHandAsync(Guid org, Guid branch, Guid product)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            """
            SELECT on_hand_quantity FROM pos.inventory_branch_balances
            WHERE organization_id = @org AND branch_id = @branch AND product_id = @product
            """,
            connection);
        cmd.Parameters.AddWithValue("org", org);
        cmd.Parameters.AddWithValue("branch", branch);
        cmd.Parameters.AddWithValue("product", product);
        return (decimal)(await cmd.ExecuteScalarAsync())!;
    }
}
