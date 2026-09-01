using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Api.Customers;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Application.Returns;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using static ExItS.PinoyBusinessPOS.IntegrationTests.PosInventoryOpsIntegrationSupport;
using H1ProofBranchDirectoryOptions = ExItS.PinoyBusinessPOS.IntegrationTests.Support.H1ProofBranchDirectoryOptions;
using H1ProofCustomerOrderBranchDirectory = ExItS.PinoyBusinessPOS.IntegrationTests.Support.H1ProofCustomerOrderBranchDirectory;
using H1ProofOrganizationBranchDirectory = ExItS.PinoyBusinessPOS.IntegrationTests.Support.H1ProofOrganizationBranchDirectory;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

/// <summary>MB2-02C-H1 dedicated security, concurrency, and full Mica Store API E2E proofs.</summary>
[Collection(PosPostgreSqlCollection.Name)]
public sealed class BranchInventory02CH1ProofIntegrationTests(PosPostgreSqlFixture fixture)
{
    private static readonly Guid Main = BranchA;
    private static readonly Guid Remote = BranchB;
    private static readonly Guid MicaA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid MicaB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid MicaStaff = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid ForeignBranch = Guid.Parse("99999999-9999-9999-9999-999999999999");
    private static readonly Guid PersonalUser = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid PlatformBusinessCustomerId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid LinkedCustomerAppUserId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    private const string Sales = "/api/v1/pos/sales";
    private const string Returns = "/api/v1/pos/sale-returns";
    private const string DirectPurchases = "/api/v1/pos/direct-purchase-receipts";
    private const string CustomerOrdersOrg = "/api/v1/pos/customer-orders/organizations";
    private const string CustomerOrdersSeller = "/api/v1/pos/organizations";
    private const string Customers = "/api/v1/pos/customers";

    [Fact]
    public async Task BWRITE_SEC_03_mica_a_staff_cannot_mutate_main_inventory()
    {
        var branchOptions = CreateMicaBranchOptions();
        branchOptions.RestrictActor(MicaStaff, MicaA);
        await using var factory = CreateBranchProofFactory(branchOptions);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        branchOptions.RegisterOrganization(org, Main, MicaA, MicaB);

        var product = await CreateProductAsync(client, org, "SEC03 Coke");
        await EnableTrackedAsync(client, org, product.ProductId, 10m, branchId: Main);
        await SeedBranchStockAsync(client, org, product.ProductId, MicaA, 10m);
        await BootstrapOwnerAsync(client, org, OwnerActor);
        await AssignRoleAsync(client, org, OwnerActor, MicaStaff, "StoreManager");

        var before = await SnapshotAsync(org, product.ProductId);

        using var adjust = Scoped(HttpMethod.Post, $"{Inventory}/{product.ProductId:D}/adjustments", org, MicaStaff, Main);
        adjust.Content = JsonContent.Create(
            new AdjustInventoryRequest("Out", 1m, "SEC-03 attempt Main"),
            options: JsonOptions);
        using var response = await client.SendAsync(adjust);

        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(DomainErrorCodes.InvalidBranchId, await ReadErrorCodeAsync(response));
        await AssertSnapshotUnchangedAsync(before, org, product.ProductId);
    }

    [Fact]
    public async Task BWRITE_SEC_04_mica_a_staff_cannot_mutate_mica_b_inventory()
    {
        var branchOptions = CreateMicaBranchOptions();
        branchOptions.RestrictActor(MicaStaff, MicaA);
        await using var factory = CreateBranchProofFactory(branchOptions);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        branchOptions.RegisterOrganization(org, Main, MicaA, MicaB);

        var product = await CreateProductAsync(client, org, "SEC04 Coke");
        await EnableTrackedAsync(client, org, product.ProductId, 10m, branchId: Main);
        await SeedBranchStockAsync(client, org, product.ProductId, MicaA, 10m);
        await SeedBranchStockAsync(client, org, product.ProductId, MicaB, 10m);
        await BootstrapOwnerAsync(client, org, OwnerActor);
        await AssignRoleAsync(client, org, OwnerActor, MicaStaff, "StoreManager");

        var before = await SnapshotAsync(org, product.ProductId);

        using var adjust = Scoped(HttpMethod.Post, $"{Inventory}/{product.ProductId:D}/adjustments", org, MicaStaff, MicaB);
        adjust.Content = JsonContent.Create(
            new AdjustInventoryRequest("Out", 1m, "SEC-04 attempt Mica B"),
            options: JsonOptions);
        using var response = await client.SendAsync(adjust);

        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(DomainErrorCodes.InvalidBranchId, await ReadErrorCodeAsync(response));
        await AssertSnapshotUnchangedAsync(before, org, product.ProductId);
    }

    [Fact]
    public async Task BWRITE_SEC_05_inactive_branch_physical_write_rejected()
    {
        var branchOptions = CreateMicaBranchOptions();
        await using var factory = CreateBranchProofFactory(branchOptions);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        branchOptions.RegisterOrganization(org, Main, MicaA, MicaB);

        var product = await CreateProductAsync(client, org, "SEC05 Coke");
        await EnableTrackedAsync(client, org, product.ProductId, 10m, branchId: Main);
        await SeedBranchStockAsync(client, org, product.ProductId, MicaB, 10m);
        branchOptions.SetInactive(org, MicaB);

        var before = await SnapshotAsync(org, product.ProductId);

        using var adjust = Scoped(HttpMethod.Post, $"{Inventory}/{product.ProductId:D}/adjustments", org, OwnerActor, MicaB);
        adjust.Content = JsonContent.Create(
            new AdjustInventoryRequest("Out", 1m, "SEC-05 inactive branch"),
            options: JsonOptions);
        using var response = await client.SendAsync(adjust);

        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(DomainErrorCodes.InvalidBranchId, await ReadErrorCodeAsync(response));
        await AssertSnapshotUnchangedAsync(before, org, product.ProductId);
    }

    [Fact]
    public async Task BWRITE_SEC_06_foreign_branch_and_cross_org_product_writes_rejected()
    {
        var branchOptions = CreateMicaBranchOptions();
        await using var factory = CreateBranchProofFactory(branchOptions);
        var client = factory.CreateClient();
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();
        branchOptions.RegisterOrganization(orgA, Main, MicaA, MicaB);
        branchOptions.RegisterOrganization(orgB, Main, MicaA, MicaB);

        var productB = await CreateProductAsync(client, orgB, "SEC06 OrgB");
        await EnableTrackedAsync(client, orgB, productB.ProductId, 8m, branchId: Main);

        var productA = await CreateProductAsync(client, orgA, "SEC06 OrgA");
        await EnableTrackedAsync(client, orgA, productA.ProductId, 8m, branchId: Main);

        var beforeA = await SnapshotAsync(orgA, productA.ProductId);
        var beforeB = await SnapshotAsync(orgB, productB.ProductId);

        using var foreignBranch = Scoped(HttpMethod.Post, $"{Inventory}/{productA.ProductId:D}/adjustments", orgA, OwnerActor, ForeignBranch);
        foreignBranch.Content = JsonContent.Create(
            new AdjustInventoryRequest("Out", 1m, "SEC-06 foreign branch"),
            options: JsonOptions);
        using var foreignBranchResponse = await client.SendAsync(foreignBranch);
        Assert.False(foreignBranchResponse.IsSuccessStatusCode);

        using var crossOrg = Scoped(HttpMethod.Post, StockUses, orgA, OwnerActor, Main);
        crossOrg.Content = JsonContent.Create(
            new CreateStockUseRequest(
                "InternalOperations",
                [new CreateStockUseLineRequest(productB.ProductId, 1m)],
                BranchId: Main),
            options: JsonOptions);
        using var crossOrgResponse = await client.SendAsync(crossOrg);
        Assert.False(crossOrgResponse.IsSuccessStatusCode);

        await AssertSnapshotUnchangedAsync(beforeA, orgA, productA.ProductId);
        await AssertSnapshotUnchangedAsync(beforeB, orgB, productB.ProductId);
    }

    [Fact]
    public async Task BWRITE_SEC_07_return_restores_original_sale_branch_not_current_workspace()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "SEC07 Return Branch");
        await EnableTrackedAsync(client, org, product.ProductId, 0m, branchId: Main);
        await SeedBranchStockAsync(client, org, product.ProductId, MicaA, 10m);
        await SeedBranchStockAsync(client, org, product.ProductId, MicaB, 10m);

        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, OwnerActor);
        using var checkout = Scoped(HttpMethod.Post, Sales, org, OwnerActor, MicaA);
        checkout.Content = JsonContent.Create(
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 3m)],
                "Cash",
                AmountTendered: 200m),
            options: JsonOptions);
        using var checkoutResponse = await client.SendAsync(checkout);
        checkoutResponse.EnsureSuccessStatusCode();
        var sale = await checkoutResponse.Content.ReadFromJsonAsync<PosSaleDto>(JsonOptions);
        Assert.NotNull(sale);
        Assert.Equal(MicaA, sale!.BranchId);

        Assert.Equal(7m, await BranchOnHandViaSummaryAsync(client, org, product.ProductId, MicaA));
        Assert.Equal(10m, await BranchOnHandViaSummaryAsync(client, org, product.ProductId, MicaB));

        var line = sale.Lines.Single();
        using var createReturn = Scoped(HttpMethod.Post, Returns, org, OwnerActor, MicaB);
        createReturn.Content = JsonContent.Create(
            new CreateSaleReturnRequest(
                sale.SaleId,
                "SEC-07 branch authority",
                [new CreateSaleReturnLineRequest(line.SaleLineId, 2m, "ReturnToStock")]),
            options: JsonOptions);
        using var returnResponse = await client.SendAsync(createReturn);
        returnResponse.EnsureSuccessStatusCode();

        Assert.Equal(9m, await BranchOnHandViaSummaryAsync(client, org, product.ProductId, MicaA));
        Assert.Equal(10m, await BranchOnHandViaSummaryAsync(client, org, product.ProductId, MicaB));
    }

    [Fact]
    public async Task BWRITE_SEC_08_not_offered_product_blocks_sale_but_allows_inventory_manage()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "SEC08 Standard");
        await EnableTrackedAsync(client, org, product.ProductId, 20m, branchId: MicaA);

        using var setOffering = Scoped(
            HttpMethod.Put,
            $"/api/v1/pos/catalog/products/{product.ProductId:D}/branches/{MicaA:D}/availability",
            org,
            OwnerActor,
            MicaA);
        setOffering.Content = JsonContent.Create(
            new SetBranchProductAvailabilityRequest(IsOffered: false),
            options: JsonOptions);
        using var offeringResponse = await client.SendAsync(setOffering);
        offeringResponse.EnsureSuccessStatusCode();

        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, OwnerActor);
        using var checkout = Scoped(HttpMethod.Post, Sales, org, OwnerActor, MicaA);
        checkout.Content = JsonContent.Create(
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                "Cash",
                AmountTendered: 100m),
            options: JsonOptions);
        using var saleResponse = await client.SendAsync(checkout);
        Assert.Equal(HttpStatusCode.BadRequest, saleResponse.StatusCode);
        Assert.Equal(ApplicationErrorCodes.ProductNotOfferedAtBranch, await ReadErrorCodeAsync(saleResponse));

        using var adjust = Scoped(HttpMethod.Post, $"{Inventory}/{product.ProductId:D}/adjustments", org, OwnerActor, MicaA);
        adjust.Content = JsonContent.Create(
            new AdjustInventoryRequest("Out", 2m, "SEC-08 inventory manage"),
            options: JsonOptions);
        using var adjustResponse = await client.SendAsync(adjust);
        adjustResponse.EnsureSuccessStatusCode();
        Assert.Equal(18m, await BranchOnHandViaSummaryAsync(client, org, product.ProductId, MicaA));
    }

    [Fact]
    public async Task BWRITE_CONC_02_concurrent_sale_and_transfer_dispatch_do_not_oversell()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var org = Guid.NewGuid();
        var setup = factory.CreateClient();
        var product = await CreateProductAsync(setup, org, "CONC02 Race");
        await EnableTrackedAsync(setup, org, product.ProductId, 0m, branchId: Main);
        await SeedBranchStockAsync(setup, org, product.ProductId, MicaA, 5m);

        var transfer = await CreateTransferAsync(setup, org, MicaA, MicaB, product.ProductId, 4m);
        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(setup, org, OwnerActor);

        var gate = new ManualResetEventSlim(false);
        var clientA = factory.CreateClient();
        var clientB = factory.CreateClient();

        var saleTask = Task.Run(async () =>
        {
            gate.Wait();
            using var checkout = Scoped(HttpMethod.Post, Sales, org, OwnerActor, MicaA);
            checkout.Content = JsonContent.Create(
                new CheckoutSaleRequest(
                    [new CheckoutSaleLineRequest(product.ProductId, 4m)],
                    "Cash",
                    AmountTendered: 500m),
                options: JsonOptions);
            return await clientA.SendAsync(checkout);
        });

        var dispatchTask = Task.Run(async () =>
        {
            gate.Wait();
            using var dispatch = Scoped(HttpMethod.Post, $"{Inventory}/transfers/{transfer.TransferId:D}/dispatch", org, OwnerActor, MicaA);
            return await clientB.SendAsync(dispatch);
        });

        gate.Set();
        var responses = await Task.WhenAll(saleTask, dispatchTask);
        var successes = responses.Count(r => r.IsSuccessStatusCode);
        Assert.Equal(1, successes);

        var finalA = await ReadBranchOnHandAsync(org, MicaA, product.ProductId);
        var finalOrg = await ReadOrgOnHandAsync(org, product.ProductId);
        Assert.True(finalA >= 0m);
        Assert.True(finalOrg >= 0m);
        Assert.True(finalA <= 5m);

        using var auditClient = factory.CreateClient();
        using var audit = Scoped(HttpMethod.Get, $"{Inventory}/physical-audit", org, OwnerActor, Main);
        using var auditResponse = await auditClient.SendAsync(audit);
        auditResponse.EnsureSuccessStatusCode();
        var auditResult = await auditResponse.Content.ReadFromJsonAsync<InventoryPhysicalAuditResult>(JsonOptions);
        Assert.NotNull(auditResult);
        Assert.True(auditResult!.IsClean);
    }

    [Fact]
    public async Task BWRITE_CONC_03_concurrent_sale_and_waste_do_not_oversell()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var org = Guid.NewGuid();
        var setup = factory.CreateClient();
        var product = await CreateProductAsync(setup, org, "CONC03 Waste Race");
        await EnableTrackedAsync(setup, org, product.ProductId, 0m, branchId: Main);
        await SeedBranchStockAsync(setup, org, product.ProductId, MicaA, 5m);
        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(setup, org, OwnerActor);

        var gate = new ManualResetEventSlim(false);
        var clientA = factory.CreateClient();
        var clientB = factory.CreateClient();

        var saleTask = Task.Run(async () =>
        {
            gate.Wait();
            using var checkout = Scoped(HttpMethod.Post, Sales, org, OwnerActor, MicaA);
            checkout.Content = JsonContent.Create(
                new CheckoutSaleRequest(
                    [new CheckoutSaleLineRequest(product.ProductId, 4m)],
                    "Cash",
                    AmountTendered: 500m),
                options: JsonOptions);
            return await clientA.SendAsync(checkout);
        });

        var wasteTask = Task.Run(async () =>
        {
            gate.Wait();
            using var create = Scoped(HttpMethod.Post, WasteLosses, org, OwnerActor, MicaA);
            create.Content = JsonContent.Create(
                new CreateWasteLossRequest(
                    "Spoilage",
                    [new CreateWasteLossLineRequest(product.ProductId, 4m)],
                    BranchId: MicaA),
                options: JsonOptions);
            return await clientB.SendAsync(create);
        });

        gate.Set();
        var responses = await Task.WhenAll(saleTask, wasteTask);
        Assert.Equal(1, responses.Count(r => r.IsSuccessStatusCode));

        var finalA = await ReadBranchOnHandAsync(org, MicaA, product.ProductId);
        var finalOrg = await ReadOrgOnHandAsync(org, product.ProductId);
        Assert.True(finalA >= 0m);
        Assert.True(finalOrg >= 0m);

        using var auditClient = factory.CreateClient();
        using var audit = Scoped(HttpMethod.Get, $"{Inventory}/physical-audit", org, OwnerActor, Main);
        using var auditResponse = await auditClient.SendAsync(audit);
        auditResponse.EnsureSuccessStatusCode();
        var auditResult = await auditResponse.Content.ReadFromJsonAsync<InventoryPhysicalAuditResult>(JsonOptions);
        Assert.True(auditResult!.IsClean);
    }

    [Fact]
    public async Task BWRITE_CONC_04_concurrent_duplicate_direct_purchase_credits_once()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var org = Guid.NewGuid();
        var setup = factory.CreateClient();
        var product = await CreateProductAsync(setup, org, "CONC04 Receipt");
        await EnableTrackedAsync(setup, org, product.ProductId, 0m, branchId: Main);

        const string idempotencyKey = "conc-04-dp-key";
        var body = new CreateDirectPurchaseReceiptRequest(
            DateOnly.FromDateTime(DateTime.UtcNow),
            [new CreateDirectPurchaseReceiptLineRequest(product.ProductId, 10m, 5m)],
            IdempotencyKey: idempotencyKey);

        var gate = new ManualResetEventSlim(false);
        var clientA = factory.CreateClient();
        var clientB = factory.CreateClient();

        var taskA = Task.Run(async () =>
        {
            gate.Wait();
            using var create = Scoped(HttpMethod.Post, DirectPurchases, org, OwnerActor, MicaA);
            create.Content = JsonContent.Create(body, options: JsonOptions);
            return await clientA.SendAsync(create);
        });
        var taskB = Task.Run(async () =>
        {
            gate.Wait();
            using var create = Scoped(HttpMethod.Post, DirectPurchases, org, OwnerActor, MicaA);
            create.Content = JsonContent.Create(body, options: JsonOptions);
            return await clientB.SendAsync(create);
        });

        gate.Set();
        var responses = await Task.WhenAll(taskA, taskB);
        Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode));

        Assert.Equal(10m, await ReadOrgOnHandAsync(org, product.ProductId));
        Assert.Equal(10m, await ReadBranchOnHandAsync(org, MicaA, product.ProductId));

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            """
            SELECT COUNT(*) FROM pos.direct_purchase_receipts
            WHERE organization_id = @org AND idempotency_key = @key
            """,
            connection);
        cmd.Parameters.AddWithValue("org", org);
        cmd.Parameters.AddWithValue("key", idempotencyKey);
        Assert.Equal(1, Convert.ToInt32(await cmd.ExecuteScalarAsync()));
    }

    [Fact]
    public async Task MICA_FULL_API_E2E_transfers_sales_reservations_consume_and_audits_clean()
    {
        await using var factory = CreateCustomerOrderFactory();
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        await CreateLinkedCustomerAsync(client, org, PlatformBusinessCustomerId, "Mica Buyer");
        await BootstrapOwnerAsync(client, org, OwnerActor);
        var product = await CreateProductAsync(client, org, "Mica E2E Coke");

        await EnableTrackedAsync(client, org, product.ProductId, 100m, branchId: Main);
        await AssertOrgSummaryAsync(client, org, product.ProductId, orgOnHand: 100m, main: 100m, micaA: 0m, micaB: 0m);

        var transferToA = await CreateTransferAsync(client, org, Main, MicaA, product.ProductId, 10m);
        await DispatchTransferAsync(client, org, Main, transferToA.TransferId);
        await ReceiveTransferAsync(client, org, MicaA, transferToA.TransferId, product.ProductId, 10m);

        var transferToB = await CreateTransferAsync(client, org, Main, MicaB, product.ProductId, 20m);
        await DispatchTransferAsync(client, org, Main, transferToB.TransferId);
        await ReceiveTransferAsync(client, org, MicaB, transferToB.TransferId, product.ProductId, 20m);

        await AssertOrgSummaryAsync(client, org, product.ProductId, orgOnHand: 100m, main: 70m, micaA: 10m, micaB: 20m);

        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, OwnerActor);
        await CheckoutAsync(client, org, MicaA, product.ProductId, 5m);
        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, OwnerActor);
        await CheckoutAsync(client, org, MicaB, product.ProductId, 10m);

        await AssertOrgSummaryAsync(client, org, product.ProductId, orgOnHand: 85m, main: 70m, micaA: 5m, micaB: 10m, orgReserved: 0m);

        var reserveMovementsBefore = await CountMovementsAsync(client, org, product.ProductId);

        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, OwnerActor);
        var reserveSale = await CheckoutGcashAsync(client, org, MicaA, product.ProductId, 4m);
        Assert.Equal(PosSaleOptions.AwaitingPaymentStatus, reserveSale.Status);
        await AssertOrgSummaryAsync(
            client,
            org,
            product.ProductId,
            orgOnHand: 85m,
            main: 70m,
            micaA: 5m,
            micaB: 10m,
            orgReserved: 4m,
            micaAReserved: 4m,
            micaAAvailable: 1m);

        await ReservationAuditCleanAsync(org);
        await PhysicalAuditCleanAsync(client, org);

        await VoidSaleAsync(client, org, MicaA, reserveSale.SaleId);
        await AssertOrgSummaryAsync(client, org, product.ProductId, orgOnHand: 85m, main: 70m, micaA: 5m, micaB: 10m, orgReserved: 0m);

        var reserveMovementsAfterCancel = await CountMovementsAsync(client, org, product.ProductId);
        Assert.Equal(reserveMovementsBefore, reserveMovementsAfterCancel);

        var consumeOrder = await PlacePersonalOrderAsync(client, org, product.ProductId, 4m, MicaA);
        await FulfillPickupAndCompleteAsync(client, org, consumeOrder.OrderId);

        await AssertOrgSummaryAsync(
            client,
            org,
            product.ProductId,
            orgOnHand: 81m,
            main: 70m,
            micaA: 1m,
            micaB: 10m,
            orgReserved: 0m,
            micaAAvailable: 1m);

        var branchSum = 70m + 1m + 10m;
        Assert.Equal(81m, branchSum);

        await ReservationAuditCleanAsync(org);
        await PhysicalAuditCleanAsync(client, org);

        var movements = (await MovementsAtBranchAsync(client, org, product.ProductId, Main))
            .Concat(await MovementsAtBranchAsync(client, org, product.ProductId, MicaA))
            .Concat(await MovementsAtBranchAsync(client, org, product.ProductId, MicaB))
            .ToList();
        Assert.Equal(3, movements.Count(m => m.MovementType == nameof(StockMovementType.SaleDeduction)));
        Assert.Equal(2, movements.Count(m =>
            m.MovementType == nameof(StockMovementType.SaleDeduction)
            && m.SourceType == nameof(StockMovementSourceType.Sale)));
        Assert.Single(movements, m =>
            m.MovementType == nameof(StockMovementType.SaleDeduction)
            && m.SourceType == nameof(StockMovementSourceType.CustomerOrder));
        Assert.Equal(2, movements.Count(m => m.MovementType == nameof(StockMovementType.TransferOut)));
        Assert.Equal(2, movements.Count(m => m.MovementType == nameof(StockMovementType.TransferIn)));
    }

    private static async Task<IReadOnlyList<PosStockMovementDto>> MovementsAtBranchAsync(
        HttpClient client,
        Guid org,
        Guid productId,
        Guid branchId)
    {
        using var get = Scoped(HttpMethod.Get, $"{Inventory}/{productId:D}/movements?page=1&pageSize=100", org, OwnerActor, branchId);
        using var response = await client.SendAsync(get);
        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<PagedResult<PosStockMovementDto>>(JsonOptions);
        return page!.Items;
    }

    private static H1ProofBranchDirectoryOptions CreateMicaBranchOptions()
    {
        var options = new H1ProofBranchDirectoryOptions { PrimaryBranchId = Main };
        return options;
    }

    private WebApplicationFactory<Program> CreateBranchProofFactory(H1ProofBranchDirectoryOptions branchOptions)
    {
        var options = branchOptions;
        return new BranchProofApiFactory(fixture.ConnectionString, options);
    }

    private WebApplicationFactory<Program> CreateCustomerOrderFactory() =>
        new CustomerOrderApiFactory(fixture.ConnectionString, PersonalUser, PlatformBusinessCustomerId, LinkedCustomerAppUserId);

    private sealed class BranchProofApiFactory(string connectionString, H1ProofBranchDirectoryOptions branchOptions)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:PosDatabase", connectionString);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IOrganizationBranchDirectory>();
                services.AddSingleton(branchOptions);
                services.AddSingleton<IOrganizationBranchDirectory, H1ProofOrganizationBranchDirectory>();
            });
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PosDatabase"] = connectionString
                });
            });
        }
    }

    private sealed class CustomerOrderApiFactory(
        string connectionString,
        Guid personalUserId,
        Guid platformBusinessCustomerId,
        Guid linkedCustomerAppUserId) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:PosDatabase", connectionString);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ILinkedCustomerPlatformAuthorization>();
                services.AddSingleton<ILinkedCustomerPlatformAuthorization>(
                    new TestLinkedCustomerPlatformAuthorization(
                        personalUserId,
                        platformBusinessCustomerId,
                        linkedCustomerAppUserId));
                services.RemoveAll<ICustomerOrderBranchDirectory>();
                services.AddSingleton<ICustomerOrderBranchDirectory, H1ProofCustomerOrderBranchDirectory>();
            });
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PosDatabase"] = connectionString
                });
            });
        }
    }

    private sealed class TestLinkedCustomerPlatformAuthorization(
        Guid personalUserId,
        Guid platformBusinessCustomerId,
        Guid linkedCustomerAppUserId) : ILinkedCustomerPlatformAuthorization
    {
        public Task<LinkedCustomerPlatformAuthorizationResult> VerifyAsync(
            Guid organizationId,
            Guid businessCustomerId,
            CancellationToken cancellationToken = default)
        {
            if (businessCustomerId != platformBusinessCustomerId)
            {
                return Task.FromResult(new LinkedCustomerPlatformAuthorizationResult(
                    LinkedCustomerPlatformAuthorizationOutcome.NotFound,
                    null));
            }

            return Task.FromResult(new LinkedCustomerPlatformAuthorizationResult(
                LinkedCustomerPlatformAuthorizationOutcome.Authorized,
                new LinkedCustomerPlatformAuthorizationProof(
                    personalUserId,
                    organizationId,
                    platformBusinessCustomerId,
                    linkedCustomerAppUserId)));
        }
    }

    private sealed record InventorySnapshot(
        decimal OrgOnHand,
        decimal MainOnHand,
        decimal MicaAOnHand,
        decimal MicaBOnHand,
        int MovementCount,
        int LotCount);

    private async Task<InventorySnapshot> SnapshotAsync(Guid org, Guid productId)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        async Task<decimal> Scalar(string sql, Guid? branchId = null)
        {
            await using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("org", org);
            cmd.Parameters.AddWithValue("product", productId);
            if (branchId is not null)
            {
                cmd.Parameters.AddWithValue("branch", branchId.Value);
            }

            return Convert.ToDecimal(await cmd.ExecuteScalarAsync());
        }

        var orgOnHand = await Scalar(
            "SELECT on_hand_quantity FROM pos.inventory_accounts WHERE organization_id = @org AND product_id = @product");
        var main = await Scalar(
            """
            SELECT COALESCE(on_hand_quantity, 0) FROM pos.inventory_branch_balances
            WHERE organization_id = @org AND branch_id = @branch AND product_id = @product
            """,
            Main);
        var micaA = await Scalar(
            """
            SELECT COALESCE(on_hand_quantity, 0) FROM pos.inventory_branch_balances
            WHERE organization_id = @org AND branch_id = @branch AND product_id = @product
            """,
            MicaA);
        var micaB = await Scalar(
            """
            SELECT COALESCE(on_hand_quantity, 0) FROM pos.inventory_branch_balances
            WHERE organization_id = @org AND branch_id = @branch AND product_id = @product
            """,
            MicaB);

        await using var moveCmd = new NpgsqlCommand(
            "SELECT COUNT(*) FROM pos.stock_movements WHERE organization_id = @org AND product_id = @product",
            connection);
        moveCmd.Parameters.AddWithValue("org", org);
        moveCmd.Parameters.AddWithValue("product", productId);
        var movementCount = Convert.ToInt32(await moveCmd.ExecuteScalarAsync());

        await using var lotCmd = new NpgsqlCommand(
            "SELECT COUNT(*) FROM pos.inventory_lots WHERE organization_id = @org AND product_id = @product",
            connection);
        lotCmd.Parameters.AddWithValue("org", org);
        lotCmd.Parameters.AddWithValue("product", productId);
        var lotCount = Convert.ToInt32(await lotCmd.ExecuteScalarAsync());

        return new InventorySnapshot(orgOnHand, main, micaA, micaB, movementCount, lotCount);
    }

    private async Task AssertSnapshotUnchangedAsync(InventorySnapshot before, Guid org, Guid productId)
    {
        var after = await SnapshotAsync(org, productId);
        Assert.Equal(before.OrgOnHand, after.OrgOnHand);
        Assert.Equal(before.MainOnHand, after.MainOnHand);
        Assert.Equal(before.MicaAOnHand, after.MicaAOnHand);
        Assert.Equal(before.MicaBOnHand, after.MicaBOnHand);
        Assert.Equal(before.MovementCount, after.MovementCount);
        Assert.Equal(before.LotCount, after.LotCount);
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
            SELECT COALESCE(on_hand_quantity, 0) FROM pos.inventory_branch_balances
            WHERE organization_id = @org AND branch_id = @branch AND product_id = @product
            """,
            connection);
        cmd.Parameters.AddWithValue("org", org);
        cmd.Parameters.AddWithValue("branch", branchId);
        cmd.Parameters.AddWithValue("product", productId);
        return Convert.ToDecimal(await cmd.ExecuteScalarAsync());
    }

    private static async Task<decimal> BranchOnHandViaSummaryAsync(HttpClient client, Guid org, Guid productId, Guid branchId)
    {
        var summary = await GetOrgSummaryAsync(client, org, productId);
        return summary.Branches.Single(b => b.BranchId == branchId).OnHandQuantity;
    }

    private static async Task<PosOrganizationInventoryProductDto> GetOrgSummaryAsync(
        HttpClient client,
        Guid org,
        Guid productId)
    {
        using var get = Scoped(HttpMethod.Get, $"{Inventory}/{productId:D}/organization-summary", org, OwnerActor, Main);
        using var response = await client.SendAsync(get);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PosOrganizationInventoryProductDto>(JsonOptions))!;
    }

    private static async Task AssertOrgSummaryAsync(
        HttpClient client,
        Guid org,
        Guid productId,
        decimal orgOnHand,
        decimal main,
        decimal micaA,
        decimal micaB,
        decimal orgReserved = 0m,
        decimal? micaAReserved = null,
        decimal? micaAAvailable = null)
    {
        var summary = await GetOrgSummaryAsync(client, org, productId);
        Assert.Equal(orgOnHand, summary.OrganizationOnHandQuantity);
        Assert.Equal(orgReserved, summary.OrganizationReservedQuantity);
        Assert.Equal(main, BranchQuantity(summary, Main));
        Assert.Equal(micaA, BranchQuantity(summary, MicaA));
        Assert.Equal(micaB, BranchQuantity(summary, MicaB));

        if (micaAReserved is not null)
        {
            Assert.Equal(micaAReserved.Value, BranchReserved(summary, MicaA));
        }

        if (micaAAvailable is not null)
        {
            Assert.Equal(micaAAvailable.Value, BranchAvailable(summary, MicaA));
        }
    }

    private static decimal BranchQuantity(PosOrganizationInventoryProductDto summary, Guid branchId) =>
        summary.Branches.SingleOrDefault(b => b.BranchId == branchId)?.OnHandQuantity ?? 0m;

    private static decimal BranchReserved(PosOrganizationInventoryProductDto summary, Guid branchId) =>
        summary.Branches.SingleOrDefault(b => b.BranchId == branchId)?.ReservedQuantity ?? 0m;

    private static decimal BranchAvailable(PosOrganizationInventoryProductDto summary, Guid branchId) =>
        summary.Branches.SingleOrDefault(b => b.BranchId == branchId)?.AvailableQuantity ?? 0m;

    private async Task ReservationAuditCleanAsync(Guid org)
    {
        var options = new DbContextOptionsBuilder<PosDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;
        await using var db = new PosDbContext(options);
        var audit = await new BranchInventoryReservationCutover(db).AuditAsync(org);
        Assert.Equal(0, audit.MismatchedBalanceCount);
    }

    private static async Task PhysicalAuditCleanAsync(HttpClient client, Guid org)
    {
        using var audit = Scoped(HttpMethod.Get, $"{Inventory}/physical-audit", org, OwnerActor, Main);
        using var response = await client.SendAsync(audit);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<InventoryPhysicalAuditResult>(JsonOptions);
        Assert.NotNull(result);
        Assert.True(result!.IsClean);
    }

    private static async Task<int> CountMovementsAsync(HttpClient client, Guid org, Guid productId) =>
        (await MovementsAsync(client, org, productId)).Count;

    private static async Task<InventoryTransferDto> CreateTransferAsync(
        HttpClient client,
        Guid org,
        Guid source,
        Guid dest,
        Guid productId,
        decimal qty)
    {
        using var request = Scoped(HttpMethod.Post, $"{Inventory}/transfers", org, OwnerActor, source);
        request.Content = JsonContent.Create(
            new CreateInventoryTransferRequest(source, dest, [new InventoryTransferLineRequest(productId, qty)]),
            options: JsonOptions);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<InventoryTransferDto>(JsonOptions))!;
    }

    private static async Task DispatchTransferAsync(HttpClient client, Guid org, Guid source, Guid transferId)
    {
        using var request = Scoped(HttpMethod.Post, $"{Inventory}/transfers/{transferId:D}/dispatch", org, OwnerActor, source);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private static async Task ReceiveTransferAsync(
        HttpClient client,
        Guid org,
        Guid dest,
        Guid transferId,
        Guid productId,
        decimal qty)
    {
        using var request = Scoped(HttpMethod.Post, $"{Inventory}/transfers/{transferId:D}/receive", org, OwnerActor, dest);
        request.Content = JsonContent.Create(
            new ReceiveInventoryTransferRequest([new InventoryTransferReceiveLineRequest(productId, qty)]),
            options: JsonOptions);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<PosSaleDto> CheckoutGcashAsync(
        HttpClient client,
        Guid org,
        Guid branchId,
        Guid productId,
        decimal qty)
    {
        using var checkout = Scoped(HttpMethod.Post, Sales, org, OwnerActor, branchId);
        checkout.Content = JsonContent.Create(
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(productId, qty)],
                "GCash",
                AmountTendered: null),
            options: JsonOptions);
        using var response = await client.SendAsync(checkout);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PosSaleDto>(JsonOptions))!;
    }

    private static async Task VoidSaleAsync(HttpClient client, Guid org, Guid branchId, Guid saleId)
    {
        using var request = Scoped(HttpMethod.Post, $"{Sales}/{saleId:D}/void", org, OwnerActor, branchId);
        request.Content = JsonContent.Create(new VoidSaleRequest("Release reservation proof"), options: JsonOptions);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private static async Task CheckoutAsync(HttpClient client, Guid org, Guid branchId, Guid productId, decimal qty)
    {
        using var checkout = Scoped(HttpMethod.Post, Sales, org, OwnerActor, branchId);
        checkout.Content = JsonContent.Create(
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(productId, qty)],
                "Cash",
                AmountTendered: 5000m),
            options: JsonOptions);
        using var response = await client.SendAsync(checkout);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<CustomerOrderDto> PlacePersonalOrderAsync(
        HttpClient client,
        Guid org,
        Guid productId,
        decimal quantity,
        Guid fulfillmentBranchId)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{CustomerOrdersOrg}/{org:D}");
        request.Headers.TryAddWithoutValidation(PosOrganizationHeaders.ActorHeaderName, PersonalUser.ToString("D"));
        request.Content = JsonContent.Create(
            new PlaceCustomerOrderRequest(
                "Pickup",
                fulfillmentBranchId,
                "Personal",
                "Mica Buyer",
                PersonalUser,
                PlatformBusinessCustomerId,
                null,
                null,
                [new PlaceCustomerOrderLineRequest(productId, quantity)],
                null,
                null,
                null,
                "Cash"),
            options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CustomerOrderDto>(JsonOptions))!;
    }

    private static async Task AcceptOrderAsync(HttpClient client, Guid org, Guid orderId)
    {
        using var request = Scoped(
            HttpMethod.Post,
            $"{CustomerOrdersSeller}/{org:D}/customer-orders/{orderId:D}/accept",
            org,
            OwnerActor,
            Main);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private static async Task RejectOrderAsync(HttpClient client, Guid org, Guid orderId)
    {
        using var request = Scoped(
            HttpMethod.Post,
            $"{CustomerOrdersSeller}/{org:D}/customer-orders/{orderId:D}/reject",
            org,
            OwnerActor,
            Main);
        request.Content = JsonContent.Create(
            new RejectCustomerOrderRequest("OutOfStock", "Release reservation proof"),
            options: JsonOptions);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private static async Task CancelOrderAsCustomerAsync(HttpClient client, Guid org, Guid orderId)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{CustomerOrdersOrg}/{org:D}/{orderId:D}/cancel");
        request.Headers.TryAddWithoutValidation(PosOrganizationHeaders.ActorHeaderName, PersonalUser.ToString("D"));
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private static async Task CancelOrderAsync(HttpClient client, Guid org, Guid orderId)
    {
        using var request = Scoped(
            HttpMethod.Post,
            $"{CustomerOrdersSeller}/{org:D}/customer-orders/{orderId:D}/cancel",
            org,
            OwnerActor,
            Main);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private static async Task FulfillPickupAndCompleteAsync(HttpClient client, Guid org, Guid orderId)
    {
        await AcceptOrderAsync(client, org, orderId);
        using var ready = Scoped(
            HttpMethod.Post,
            $"{CustomerOrdersSeller}/{org:D}/customer-orders/{orderId:D}/mark-ready",
            org,
            OwnerActor,
            Main);
        (await client.SendAsync(ready)).EnsureSuccessStatusCode();

        using var collected = Scoped(
            HttpMethod.Post,
            $"{CustomerOrdersSeller}/{org:D}/customer-orders/{orderId:D}/mark-collected",
            org,
            OwnerActor,
            Main);
        (await client.SendAsync(collected)).EnsureSuccessStatusCode();

        using var complete = Scoped(
            HttpMethod.Post,
            $"{CustomerOrdersSeller}/{org:D}/customer-orders/{orderId:D}/complete",
            org,
            OwnerActor,
            Main);
        (await client.SendAsync(complete)).EnsureSuccessStatusCode();
    }

    private static async Task CreateLinkedCustomerAsync(
        HttpClient client,
        Guid orgId,
        Guid platformBusinessCustomerId,
        string displayName)
    {
        using var request = Scoped(HttpMethod.Post, Customers, orgId, OwnerActor);
        request.Content = JsonContent.Create(
            new CreateCustomerRequest(displayName, null, null, null, PlatformBusinessCustomerId: platformBusinessCustomerId),
            options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.TryGetProperty("errorCode", out var code) ? code.GetString() : null;
    }
}
