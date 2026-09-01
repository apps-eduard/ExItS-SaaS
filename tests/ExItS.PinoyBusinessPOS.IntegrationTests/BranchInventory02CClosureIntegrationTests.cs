using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using static ExItS.PinoyBusinessPOS.IntegrationTests.PosInventoryOpsIntegrationSupport;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

/// <summary>MB2-02C inventory gap closure: audit, org summary, BWRITE, concurrency, CustomerOrder restart.</summary>
[Collection(PosPostgreSqlCollection.Name)]
public sealed class BranchInventory02CClosureIntegrationTests(PosPostgreSqlFixture fixture)
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-01T16:00:00Z");
    private static readonly Guid Main = BranchA;
    private static readonly Guid Remote = BranchB;
    private static readonly Guid MicaA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid MicaB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid Actor = OwnerActor;

    private const string Sales = "/api/v1/pos/sales";

    [Fact]
    public async Task Physical_audit_clean_after_balanced_branches()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Audit Coke");
        await EnableTrackedAsync(client, org, product.ProductId, 100m, branchId: Main);
        await SeedBranchStockAsync(client, org, product.ProductId, Remote, 25m);

        using var audit = Scoped(HttpMethod.Get, $"{Inventory}/physical-audit", org);
        using var response = await client.SendAsync(audit);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<InventoryPhysicalAuditResult>(JsonOptions);
        Assert.NotNull(result);
        Assert.True(result!.IsClean);
        Assert.Equal(0, result.OrgOnHandMismatchCount);
        Assert.Equal(0, result.OrgReservedMismatchCount);
    }

    [Fact]
    public async Task Organization_summary_returns_branch_breakdown()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Org View Coke");
        await EnableTrackedAsync(client, org, product.ProductId, 70m, branchId: Main);
        await SeedBranchStockAsync(client, org, product.ProductId, MicaA, 5m);
        await SeedBranchStockAsync(client, org, product.ProductId, MicaB, 10m);

        using var get = Scoped(HttpMethod.Get, $"{Inventory}/{product.ProductId:D}/organization-summary", org);
        using var response = await client.SendAsync(get);
        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<PosOrganizationInventoryProductDto>(JsonOptions);
        Assert.NotNull(summary);
        Assert.Equal(85m, summary!.OrganizationOnHandQuantity);
        Assert.Equal(3, summary.Branches.Count);
        Assert.Equal(5m, summary.Branches.Single(b => b.BranchId == MicaA).OnHandQuantity);
    }

    [Fact]
    public async Task BWRITE_SALE_01_remote_sale_deducts_remote_only()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Remote Sale");
        await EnableTrackedAsync(client, org, product.ProductId, 100m, branchId: Main);
        await SeedBranchStockAsync(client, org, product.ProductId, Remote, 25m);

        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, Actor);
        using var checkout = Scoped(HttpMethod.Post, Sales, org, branchId: Remote);
        checkout.Content = JsonContent.Create(
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 5m)],
                "Cash",
                AmountTendered: 200m),
            options: JsonOptions);
        using var checkoutResponse = await client.SendAsync(checkout);
        checkoutResponse.EnsureSuccessStatusCode();

        Assert.Equal(120m, await ReadOrgOnHandAsync(org, product.ProductId));
        Assert.Equal(100m, await OnHandAtBranchAsync(client, org, product.ProductId, Main));
        Assert.Equal(20m, await OnHandAtBranchAsync(client, org, product.ProductId, Remote));
    }

    [Fact]
    public async Task BWRITE_SALE_02_remote_cannot_sell_main_stock()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Main Only");
        await EnableTrackedAsync(client, org, product.ProductId, 100m, branchId: Main);

        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, Actor);
        using var checkout = Scoped(HttpMethod.Post, Sales, org, branchId: Remote);
        checkout.Content = JsonContent.Create(
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 5m)],
                "Cash",
                AmountTendered: 200m),
            options: JsonOptions);
        using var response = await client.SendAsync(checkout);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(ApplicationErrorCodes.InsufficientStock, await ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task BWRITE_CONC_01_concurrent_deducts_do_not_oversell()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(factory.CreateClient(), org, "Conc Adjust");
        await EnableTrackedAsync(factory.CreateClient(), org, product.ProductId, 0m, branchId: Main);
        await SeedBranchStockAsync(factory.CreateClient(), org, product.ProductId, Remote, 5m);

        var clientA = factory.CreateClient();
        var clientB = factory.CreateClient();
        var taskA = Task.Run(async () =>
        {
            using var adjust = Scoped(
                HttpMethod.Post,
                $"{Inventory}/{product.ProductId:D}/adjustments",
                org,
                branchId: Remote);
            adjust.Content = JsonContent.Create(
                new AdjustInventoryRequest("Out", 4m, "Concurrent deduct A"),
                options: JsonOptions);
            return await clientA.SendAsync(adjust);
        });
        var taskB = Task.Run(async () =>
        {
            using var adjust = Scoped(
                HttpMethod.Post,
                $"{Inventory}/{product.ProductId:D}/adjustments",
                org,
                branchId: Remote);
            adjust.Content = JsonContent.Create(
                new AdjustInventoryRequest("Out", 4m, "Concurrent deduct B"),
                options: JsonOptions);
            return await clientB.SendAsync(adjust);
        });

        var responses = await Task.WhenAll(taskA, taskB);
        var successes = responses.Count(r => r.IsSuccessStatusCode);
        Assert.Equal(1, successes);

        var final = await ReadBranchOnHandAsync(org, Remote, product.ProductId);
        Assert.True(final >= 0m);
        Assert.Equal(1m, final);
    }

    [Fact]
    public async Task BWRITE_SEC_01_forged_body_branch_cannot_mutate_other_branch()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "SEC Body Branch");
        await EnableTrackedAsync(client, org, product.ProductId, 10m, branchId: Main);
        await SeedBranchStockAsync(client, org, product.ProductId, Remote, 10m);

        using var create = Scoped(HttpMethod.Post, StockUses, org, branchId: Main);
        create.Content = JsonContent.Create(
            new CreateStockUseRequest(
                "InternalOperations",
                [new CreateStockUseLineRequest(product.ProductId, 1m)],
                BranchId: Remote),
            options: JsonOptions);
        using var response = await client.SendAsync(create);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(ApplicationErrorCodes.InventoryBranchAuthorityMismatch, await ReadErrorCodeAsync(response));
        Assert.Equal(10m, await OnHandAtBranchAsync(client, org, product.ProductId, Main));
        Assert.Equal(10m, await OnHandAtBranchAsync(client, org, product.ProductId, Remote));
    }

    [Fact]
    public async Task BWRITE_CONC_05_reservation_vs_immediate_sale_cannot_over_reserve()
    {
        var options = Options();
        await MigrateAsync(options);
        var (org, productId, _) = await SeedTrackedAsync(options, 5m);
        await UpsertBalanceAsync(org, Main, productId.Value, 5m, 0m);
        var product = CatalogProduct.Create(PosOrganizationId.From(org), "Conc Reserve Sale", UnitOfMeasure.Piece, 10m, Now);
        var reserveSale = AwaitingSale(org, productId, Main, 4m);
        var paidSale = PaidSale(org, productId, Main, 4m);
        var products = new Dictionary<Guid, CatalogProduct> { [productId.Value] = product };

        var reserveTask = Task.Run(async () =>
        {
            await using var db = new PosDbContext(options);
            try
            {
                await CreateSaleStock(db).ReserveForAwaitingPaymentAsync(reserveSale, Actor, Now, branchId: Main);
                await db.SaveChangesAsync();
                return true;
            }
            catch (DomainException)
            {
                return false;
            }
            catch (DbUpdateException)
            {
                return false;
            }
        });
        var saleTask = Task.Run(async () =>
        {
            await using var db = new PosDbContext(options);
            try
            {
                await CreateSaleStock(db).DeductForSaleAsync(
                    PosOrganizationId.From(org),
                    paidSale,
                    products,
                    Actor,
                    Now,
                    branchId: Main);
                await db.SaveChangesAsync();
                return true;
            }
            catch (DomainException)
            {
                return false;
            }
            catch (DbUpdateException)
            {
                return false;
            }
        });

        var outcomes = await Task.WhenAll(reserveTask, saleTask);
        Assert.Equal(1, outcomes.Count(x => x));

        var balance = await ReloadBalanceAsync(options, org, Main, productId);
        Assert.True(balance.ReservedQuantity <= balance.OnHandQuantity);
        Assert.True(balance.OnHandQuantity >= 0m);
    }

    [Fact]
    public async Task BWRITE_CONC_06_two_concurrent_reservations_cannot_exceed_available()
    {
        var options = Options();
        await MigrateAsync(options);
        var (org, productId, _) = await SeedTrackedAsync(options, 5m);
        await UpsertBalanceAsync(org, Main, productId.Value, 5m, 0m);
        var saleA = AwaitingSale(org, productId, Main, 4m);
        var saleB = AwaitingSale(org, productId, Main, 4m);

        var taskA = Task.Run(async () =>
        {
            await using var db = new PosDbContext(options);
            try
            {
                await CreateSaleStock(db).ReserveForAwaitingPaymentAsync(saleA, Actor, Now, branchId: Main);
                await db.SaveChangesAsync();
                return true;
            }
            catch (DomainException)
            {
                return false;
            }
        });
        var taskB = Task.Run(async () =>
        {
            await using var db = new PosDbContext(options);
            try
            {
                await CreateSaleStock(db).ReserveForAwaitingPaymentAsync(saleB, Actor, Now, branchId: Main);
                await db.SaveChangesAsync();
                return true;
            }
            catch (DomainException)
            {
                return false;
            }
        });

        var results = await Task.WhenAll(taskA, taskB);
        Assert.Equal(1, results.Count(x => x));

        var balance = await ReloadBalanceAsync(options, org, Main, productId);
        Assert.Equal(4m, balance.ReservedQuantity);
        Assert.Equal(5m, balance.OnHandQuantity);
        Assert.True(balance.ReservedQuantity <= balance.OnHandQuantity);
    }

    [Fact]
    public async Task CO_E2E_01_customer_order_complete_after_restart_consumes_once()
    {
        var options = Options();
        await MigrateAsync(options);
        var (org, productId, _) = await SeedTrackedAsync(options, 20m);
        await UpsertBalanceAsync(org, MicaA, productId.Value, 20m, 0m);

        var order = AcceptedOrder(org, productId, MicaA, 8m);
        await using (var db = new PosDbContext(options))
        {
            await CreateOrderStock(db).ReserveForAcceptAsync(order, Actor, Now);
            await db.SaveChangesAsync();
        }

        await using (var db = new PosDbContext(options))
        {
            var mid = await new InventoryBranchBalanceRepository(db)
                .GetAsync(PosOrganizationId.From(org), PosBranchId.From(MicaA), productId);
            Assert.Equal(8m, mid!.ReservedQuantity);
        }

        var products = new Dictionary<Guid, CatalogProduct>
        {
            [productId.Value] = CatalogProduct.Create(
                PosOrganizationId.From(org),
                "Order Item",
                UnitOfMeasure.Piece,
                10m,
                Now)
        };

        await using (var db = new PosDbContext(options))
        {
            await CreateOrderStock(db).ConsumeOnCompleteAsync(order, products, Actor, Now);
            await db.SaveChangesAsync();
        }

        await using (var db = new PosDbContext(options))
        {
            await CreateOrderStock(db).ConsumeOnCompleteAsync(order, products, Actor, Now.AddMinutes(1));
            await db.SaveChangesAsync();
        }

        var end = await ReloadBalanceAsync(options, org, MicaA, productId);
        Assert.Equal(12m, end.OnHandQuantity);
        Assert.Equal(0m, end.ReservedQuantity);
        await using var verify = new PosDbContext(options);
        Assert.Equal(1, await verify.StockMovements.CountAsync(m =>
            m.OrganizationId == org && m.SourceType == nameof(StockMovementSourceType.CustomerOrder)));
    }

    [Fact]
    public async Task MICA_02C_reservation_lifecycle_and_audit_clean()
    {
        var options = Options();
        await MigrateAsync(options);
        var (org, productId, _) = await SeedTrackedAsync(options, 100m);
        await UpsertBalanceAsync(org, Main, productId.Value, 100m, 0m);

        await using (var db = new PosDbContext(options))
        {
            var repo = new InventoryBranchBalanceRepository(db);
            var mutations = new BranchInventoryMutationService();
            await mutations.ApplyBranchDeltaAsync(
                repo,
                PosOrganizationId.From(org),
                PosBranchId.From(Main),
                Main,
                productId,
                100m,
                -10m,
                Now);
            await mutations.ApplyBranchDeltaAsync(
                repo,
                PosOrganizationId.From(org),
                PosBranchId.From(MicaA),
                Main,
                productId,
                100m,
                10m,
                Now);
            await mutations.ApplyBranchDeltaAsync(
                repo,
                PosOrganizationId.From(org),
                PosBranchId.From(Main),
                Main,
                productId,
                90m,
                -20m,
                Now);
            await mutations.ApplyBranchDeltaAsync(
                repo,
                PosOrganizationId.From(org),
                PosBranchId.From(MicaB),
                Main,
                productId,
                70m,
                20m,
                Now);
            await db.SaveChangesAsync();
        }

        var order = AcceptedOrder(org, productId, MicaA, 4m);
        await using (var db = new PosDbContext(options))
        {
            await CreateOrderStock(db).ReserveForAcceptAsync(order, Actor, Now);
            await PersistReservedOrderAsync(db, order);
            await db.SaveChangesAsync();
        }

        await using (var db = new PosDbContext(options))
        {
            var micaA = await ReloadBalanceAsync(options, org, MicaA, productId);
            Assert.Equal(4m, micaA.ReservedQuantity);
        }

        await using (var db = new PosDbContext(options))
        {
            await CreateOrderStock(db).ReleaseIfReservedAsync(order, Now);
            await db.SaveChangesAsync();
        }
        await SetOrderReservationStateAsync(options, order.Id.Value, nameof(CustomerOrderStockReservationState.Released));

        var consumeOrder = AcceptedOrder(org, productId, MicaA, 4m);
        await using (var db = new PosDbContext(options))
        {
            await CreateOrderStock(db).ReserveForAcceptAsync(consumeOrder, Actor, Now);
            await PersistReservedOrderAsync(db, consumeOrder);
            await db.SaveChangesAsync();
        }

        await using (var db = new PosDbContext(options))
        {
            var products = new Dictionary<Guid, CatalogProduct>
            {
                [productId.Value] = CatalogProduct.Create(
                    PosOrganizationId.From(org),
                    "Coke",
                    UnitOfMeasure.Piece,
                    10m,
                    Now)
            };
            await CreateOrderStock(db).ConsumeOnCompleteAsync(consumeOrder, products, Actor, Now);
            await db.SaveChangesAsync();
        }
        await SetOrderReservationStateAsync(options, consumeOrder.Id.Value, nameof(CustomerOrderStockReservationState.Consumed));

        var finalMain = await ReloadBalanceAsync(options, org, Main, productId);
        var finalA = await ReloadBalanceAsync(options, org, MicaA, productId);
        var finalB = await ReloadBalanceAsync(options, org, MicaB, productId);
        Assert.Equal(70m, finalMain.OnHandQuantity);
        Assert.Equal(6m, finalA.OnHandQuantity);
        Assert.Equal(20m, finalB.OnHandQuantity);
        Assert.Equal(96m, finalMain.OnHandQuantity + finalA.OnHandQuantity + finalB.OnHandQuantity);

        await using (var db = new PosDbContext(options))
        {
            var audit = await new InventoryPhysicalAuditService(db).AuditAsync(org);
            Assert.True(audit.IsClean);
        }
    }

    private static Task PersistReservedOrderAsync(PosDbContext db, CustomerOrder order)
    {
        db.CustomerOrders.Add(new CustomerOrderRecord
        {
            Id = order.Id.Value,
            SellerOrganizationId = order.SellerOrganizationId.Value,
            OrderNumber = order.OrderNumber,
            Status = nameof(CustomerOrderStatus.Accepted),
            FulfillmentStatus = "Pending",
            PaymentStatus = "Unpaid",
            PaymentMethod = nameof(CustomerOrderPaymentMethod.Cash),
            FulfillmentType = nameof(CustomerOrderFulfillmentType.Pickup),
            FulfillmentBranchId = order.FulfillmentBranchId,
            BranchNameSnapshot = order.BranchNameSnapshot,
            CustomerPartyType = "Personal",
            CustomerDisplayNameSnapshot = "Buyer",
            CustomerPlatformUserId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            MerchandiseSubtotal = order.MerchandiseSubtotal,
            DeliveryFee = order.DeliveryFee,
            Total = order.Total,
            StockReservationState = nameof(CustomerOrderStockReservationState.Reserved),
            CreatedAtUtc = order.CreatedAtUtc,
            SubmittedAtUtc = order.SubmittedAtUtc ?? Now,
            SubmittedBy = order.SubmittedBy ?? Actor,
            AcceptedAtUtc = order.AcceptedAtUtc ?? Now,
            AcceptedBy = order.AcceptedBy ?? Actor,
            UpdatedAtUtc = Now
        });
        foreach (var line in order.Lines)
        {
            db.CustomerOrderLines.Add(new CustomerOrderLineRecord
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id.Value,
                SellerOrganizationId = order.SellerOrganizationId.Value,
                ProductId = line.ProductId.Value,
                LineNumber = line.LineNumber,
                NameSnapshot = line.NameSnapshot,
                UnitSnapshot = line.UnitSnapshot.ToString(),
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                Discount = line.Discount,
                LineTotal = line.LineTotal
            });
        }

        return Task.CompletedTask;
    }

    private static async Task SetOrderReservationStateAsync(
        DbContextOptions<PosDbContext> options,
        Guid orderId,
        string state)
    {
        await using var db = new PosDbContext(options);
        var row = await db.CustomerOrders.SingleAsync(o => o.Id == orderId);
        row.StockReservationState = state;
        row.UpdatedAtUtc = Now;
        await db.SaveChangesAsync();
    }

    private DbContextOptions<PosDbContext> Options() =>
        new DbContextOptionsBuilder<PosDbContext>().UseNpgsql(fixture.ConnectionString).Options;

    private static async Task MigrateAsync(DbContextOptions<PosDbContext> options)
    {
        await using var db = new PosDbContext(options);
        await db.Database.MigrateAsync();
    }

    private CustomerOrderStockService CreateOrderStock(PosDbContext db) =>
        new(
            new InventoryRepository(db),
            new InventoryBranchBalanceRepository(db),
            new FixedPrimaryDirectory(Main),
            new InventoryLotStockService(new InventoryLotRepository(db)));

    private SaleStockService CreateSaleStock(PosDbContext db) =>
        new(
            new InventoryRepository(db),
            new InventoryLotStockService(new InventoryLotRepository(db)),
            new InventoryBranchBalanceRepository(db),
            new FixedPrimaryDirectory(Main));

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

    private static Sale PaidSale(Guid org, CatalogProductId productId, Guid branchId, decimal qty)
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
            SaleStatus.Completed,
            SalePaymentMethod.Cash,
            line.LineTotal,
            line.LineTotal,
            0m,
            null,
            null,
            null,
            Now,
            Actor,
            Now,
            Actor,
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

    private async Task<(Guid Org, CatalogProductId ProductId, Guid AccountId)> SeedTrackedAsync(
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

    private async Task UpsertBalanceAsync(
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

    private static async Task<InventoryBranchBalance> ReloadBalanceAsync(
        DbContextOptions<PosDbContext> options,
        Guid org,
        Guid branchId,
        CatalogProductId productId)
    {
        await using var db = new PosDbContext(options);
        var balance = await new InventoryBranchBalanceRepository(db)
            .GetAsync(PosOrganizationId.From(org), PosBranchId.From(branchId), productId);
        return balance!;
    }

    private async Task<decimal> ReadOrgOnHandAsync(Guid org, Guid productId)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "SELECT on_hand_quantity FROM pos.inventory_accounts WHERE organization_id = @org AND product_id = @product",
            connection);
        cmd.Parameters.AddWithValue("org", org);
        cmd.Parameters.AddWithValue("product", productId);
        return Convert.ToDecimal(await cmd.ExecuteScalarAsync());
    }

    private async Task<decimal> ReadBranchOnHandAsync(Guid org, Guid branchId, Guid productId)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            """
            SELECT on_hand_quantity FROM pos.inventory_branch_balances
            WHERE organization_id = @org AND branch_id = @branch AND product_id = @product
            """,
            connection);
        cmd.Parameters.AddWithValue("org", org);
        cmd.Parameters.AddWithValue("branch", branchId);
        cmd.Parameters.AddWithValue("product", productId);
        return Convert.ToDecimal(await cmd.ExecuteScalarAsync());
    }

    private static async Task<decimal> OnHandAtBranchAsync(
        HttpClient client,
        Guid org,
        Guid productId,
        Guid branchId)
    {
        using var get = Scoped(HttpMethod.Get, $"{Inventory}/{productId:D}", org, branchId: branchId);
        using var response = await client.SendAsync(get);
        response.EnsureSuccessStatusCode();
        var account = await response.Content.ReadFromJsonAsync<PosInventoryAccountDto>(JsonOptions);
        return account!.OnHandQuantity;
    }

    private static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.TryGetProperty("errorCode", out var code) ? code.GetString() : null;
    }

    private sealed class FixedPrimaryDirectory(Guid primary) : IOrganizationBranchDirectory
    {
        public Task<bool> ExistsInOrganizationAsync(Guid organizationId, Guid branchId, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(
            Guid organizationId,
            IReadOnlyCollection<Guid> branchIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());

        public Task<Guid?> GetPrimaryBranchIdAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Guid?>(primary);
    }
}
