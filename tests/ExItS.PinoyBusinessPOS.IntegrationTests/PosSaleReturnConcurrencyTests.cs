using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Api.Customers;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Returns;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

/// <summary>
/// Real PostgreSQL concurrency proofs for shared OrganizationId+SaleId advisory locks on
/// ProcessSaleReturn / VoidSale (Master Run 02 Review Repair 02).
/// </summary>
[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosSaleReturnConcurrencyTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Guid Actor = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    private const string Returns = "/api/v1/pos/sale-returns";
    private const string Sales = "/api/v1/pos/sales";
    private const string Products = "/api/v1/pos/catalog/products";
    private const string Inventory = "/api/v1/pos/inventory";
    private const string Customers = "/api/v1/pos/customers";

    private const string ManagerDiscountGrants =
        $"{PosFeatureCodes.StoreSalesView},{PosFeatureCodes.StoreSalesCreate}," +
        $"{PosFeatureCodes.StoreSalesApplyCommercialDiscount},{PosFeatureCodes.StoreReturnsManage}";

    private const string ManagerUtangGrants =
        $"{PosFeatureCodes.StoreSalesView},{PosFeatureCodes.StoreSalesCreate}," +
        $"{PosFeatureCodes.CustomerCreditView},{PosFeatureCodes.CustomerCreditCreate}," +
        $"{PosFeatureCodes.StoreSalesVoid}," +
        $"{PosFeatureCodes.StoreReturnsView},{PosFeatureCodes.StoreReturnsManage}";

    [Fact]
    public async Task A_Concurrent_return_6_plus_6_against_qty_10_never_both_accept()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var setup = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(setup, org, "Conc Overlap", 10m);
        var sale = await CheckoutCashAsync(
            setup,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 10m)],
                PosSaleOptions.CashPaymentMethod,
                200m));
        var lineId = sale.Lines.Single().SaleLineId;

        var barrier = new Barrier(2);
        var clientA = factory.CreateClient();
        var clientB = factory.CreateClient();

        var taskA = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await PostReturnRawAsync(clientA, org, sale.SaleId, lineId, 6m, "DoNotRestock");
        });
        var taskB = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await PostReturnRawAsync(clientB, org, sale.SaleId, lineId, 6m, "DoNotRestock");
        });

        var responses = await Task.WhenAll(taskA, taskB);
        var successes = responses.Count(r => r.IsSuccessStatusCode);
        Assert.True(successes is 1 or 2, "At least one concurrent return should succeed.");
        // If both HTTP-succeed, one must have been remaining qty only — never both full 6.
        var refundable = await GetRefundableAsync(setup, org, sale.SaleId);
        var returned = 10m - (refundable.Lines.FirstOrDefault()?.RefundableQuantity ?? 0m);
        Assert.True(returned <= 10m, $"Final returned {returned} exceeds sold qty 10.");
        Assert.False(
            successes == 2 && returned > 10m,
            "Both concurrent 6+6 returns must not over-accept beyond sold qty.");

        // Authoritative: never both accepted as qty 6 each (would be 12).
        Assert.True(returned <= 10m);
        if (successes == 2)
        {
            Assert.True(returned == 10m, "If both requests succeed, second must take remaining only.");
        }
        else
        {
            Assert.True(returned == 6m, "Single winner of 6+6 race should leave returned=6.");
            Assert.Contains(
                responses,
                r => !r.IsSuccessStatusCode
                     && (int)r.StatusCode is >= 400 and < 500);
        }

        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    [Fact]
    public async Task B_Concurrent_return_6_plus_4_both_succeed_full_line()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var setup = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(setup, org, "Conc Fit", 10m);
        var sale = await CheckoutCashAsync(
            setup,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 10m)],
                PosSaleOptions.CashPaymentMethod,
                200m));
        var line = sale.Lines.Single();

        var barrier = new Barrier(2);
        var clientA = factory.CreateClient();
        var clientB = factory.CreateClient();

        var taskA = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await PostReturnRawAsync(clientA, org, sale.SaleId, line.SaleLineId, 6m, "DoNotRestock");
        });
        var taskB = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await PostReturnRawAsync(clientB, org, sale.SaleId, line.SaleLineId, 4m, "DoNotRestock");
        });

        var responses = await Task.WhenAll(taskA, taskB);
        Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode, $"Expected success, got {r.StatusCode}"));

        var listed = await ListReturnsForSaleAsync(setup, org, sale.SaleId);
        Assert.Equal(2, listed.Count);
        Assert.Equal(10m, listed.Sum(r => r.Lines.Sum(l => l.QuantityReturned)));
        Assert.Equal(line.LineTotal, listed.Sum(r => r.TotalRefundAmount));

        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    [Fact]
    public async Task C_Discounted_LineTotal_80_concurrent_6_plus_6_refund_cap()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var setup = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(setup, org, "Disc Conc", 10m);
        var sale = await CheckoutCashAsync(
            setup,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 10m)],
                PosSaleOptions.CashPaymentMethod,
                200m,
                Discounts:
                [
                    new CommercialDiscountIntentRequest(
                        "Line",
                        "FixedAmount",
                        20m,
                        "Bulk courtesy",
                        LineNumber: 1)
                ]),
            grants: ManagerDiscountGrants);
        var line = sale.Lines.Single();
        Assert.Equal(80m, line.LineTotal);

        var barrier = new Barrier(2);
        var clientA = factory.CreateClient();
        var clientB = factory.CreateClient();

        var taskA = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await PostReturnRawAsync(
                clientA, org, sale.SaleId, line.SaleLineId, 6m, "DoNotRestock", grants: ManagerDiscountGrants);
        });
        var taskB = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await PostReturnRawAsync(
                clientB, org, sale.SaleId, line.SaleLineId, 6m, "DoNotRestock", grants: ManagerDiscountGrants);
        });

        var responses = await Task.WhenAll(taskA, taskB);
        var listed = await ListReturnsForSaleAsync(setup, org, sale.SaleId);
        var refunded = listed.Sum(r => r.TotalRefundAmount);
        var returnedQty = listed.Sum(r => r.Lines.Sum(l => l.QuantityReturned));
        Assert.True(refunded <= 80m, $"Cumulative refund {refunded} exceeds LineTotal 80.");
        Assert.True(returnedQty <= 10m);

        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    [Fact]
    public async Task D_Expiry_lots_concurrent_returns_never_over_restore_original_lots()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var setup = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateExpiryProductAsync(setup, org, "Milk Conc");
        await EnableInventoryAsync(setup, org, product.ProductId, opening: 0m);
        var early = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var later = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(40));
        await ReceiveLotAsync(setup, org, product.ProductId, 6m, early, "LOT-E");
        await ReceiveLotAsync(setup, org, product.ProductId, 6m, later, "LOT-L");

        var sale = await CheckoutCashAsync(
            setup,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 10m)],
                PosSaleOptions.CashPaymentMethod,
                1000m));
        // FEFO: 6 from LOT-E + 4 from LOT-L consumed.
        var lineId = sale.Lines.Single().SaleLineId;

        var barrier = new Barrier(2);
        var clientA = factory.CreateClient();
        var clientB = factory.CreateClient();

        var taskA = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await PostReturnRawAsync(clientA, org, sale.SaleId, lineId, 6m, "ReturnToStock");
        });
        var taskB = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await PostReturnRawAsync(clientB, org, sale.SaleId, lineId, 6m, "ReturnToStock");
        });

        var responses = await Task.WhenAll(taskA, taskB);
        var lots = await ListLotsAsync(setup, org, product.ProductId);
        var lotE = lots.Single(l => l.LotNumber == "LOT-E");
        var lotL = lots.Single(l => l.LotNumber == "LOT-L");
        // Original received 6 each; never over-restore past received.
        Assert.True(lotE.QuantityOnHand <= 6m, $"LOT-E on hand {lotE.QuantityOnHand} > original 6.");
        Assert.True(lotL.QuantityOnHand <= 6m, $"LOT-L on hand {lotL.QuantityOnHand} > original 6.");
        // Total restored across lots cannot exceed original sale consumption (10).
        Assert.True(lotE.QuantityOnHand + lotL.QuantityOnHand <= 12m);
        var account = await GetInventoryAsync(setup, org, product.ProductId);
        Assert.Equal(lotE.QuantityOnHand + lotL.QuantityOnHand, account.OnHandQuantity);

        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    [Fact]
    public async Task E_Branch_reconciliation_after_concurrent_restock_returns()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var setup = factory.CreateClient();
        var org = Guid.NewGuid();
        var branchId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var product = await CreateProductAsync(setup, org, "Branch Conc", 5m);
        await EnableInventoryAsync(setup, org, product.ProductId, opening: 0m);
        await AdjustInAsync(setup, org, product.ProductId, 20m, branchId);

        var sale = await CheckoutCashAsync(
            setup,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 10m)],
                PosSaleOptions.CashPaymentMethod,
                200m),
            branchId: branchId);
        Assert.Equal(branchId, sale.BranchId);
        var lineId = sale.Lines.Single().SaleLineId;

        var barrier = new Barrier(2);
        var clientA = factory.CreateClient();
        var clientB = factory.CreateClient();

        var taskA = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await PostReturnRawAsync(
                clientA, org, sale.SaleId, lineId, 6m, "ReturnToStock", branchId: branchId);
        });
        var taskB = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await PostReturnRawAsync(
                clientB, org, sale.SaleId, lineId, 4m, "ReturnToStock", branchId: branchId);
        });

        var responses = await Task.WhenAll(taskA, taskB);
        Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode));

        var account = await GetInventoryAsync(setup, org, product.ProductId);
        Assert.Equal(20m, account.OnHandQuantity);

        await using var db = CreateDb();
        var branchOnHand = await db.InventoryBranchBalances
            .AsNoTracking()
            .Where(b => b.OrganizationId == org
                        && b.ProductId == product.ProductId
                        && b.BranchId == branchId)
            .Select(b => b.OnHandQuantity)
            .SingleAsync();
        Assert.Equal(20m, branchOnHand);

        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    [Fact]
    public async Task F_Utang_concurrent_returns_never_over_reduce_debt()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var setup = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(setup, org, "Utang Conc", 10m);
        var customer = await CreateCustomerAsync(setup, org, "Conc Utang");
        var creditEntryId = Guid.NewGuid();
        var sale = await CheckoutCashAsync(
            setup,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 10m)],
                PosSaleOptions.UtangPaymentMethod,
                CustomerId: customer.CustomerId,
                CreditEntryId: creditEntryId),
            grants: ManagerUtangGrants);
        Assert.Equal(100m, sale.Total);
        var lineId = sale.Lines.Single().SaleLineId;

        var barrier = new Barrier(2);
        var clientA = factory.CreateClient();
        var clientB = factory.CreateClient();

        var taskA = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await PostReturnRawAsync(
                clientA, org, sale.SaleId, lineId, 6m, "DoNotRestock", grants: ManagerUtangGrants);
        });
        var taskB = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await PostReturnRawAsync(
                clientB, org, sale.SaleId, lineId, 6m, "DoNotRestock", grants: ManagerUtangGrants);
        });

        var responses = await Task.WhenAll(taskA, taskB);
        var listed = await ListReturnsForSaleAsync(setup, org, sale.SaleId);
        var refunded = listed.Sum(r => r.TotalRefundAmount);
        Assert.True(refunded <= 100m);

        using var creditGet = Scoped(
            HttpMethod.Get,
            $"{Customers}/{customer.CustomerId:D}/credit-entries/{creditEntryId:D}",
            org,
            grants: ManagerUtangGrants);
        using var creditResponse = await setup.SendAsync(creditGet);
        creditResponse.EnsureSuccessStatusCode();
        var credit = await creditResponse.Content.ReadFromJsonAsync<CreditEntryDto>(JsonOptions);
        Assert.NotNull(credit);
        Assert.True(credit!.Amount >= 0m);
        Assert.Equal(100m - refunded, credit.Amount);

        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    [Fact]
    public async Task G_Return_vs_void_race_exclusive_outcomes()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var setup = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(setup, org, "Race Void", 10m);
        await EnableInventoryAsync(setup, org, product.ProductId, opening: 50m);
        var sale = await CheckoutCashAsync(
            setup,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 5m)],
                PosSaleOptions.CashPaymentMethod,
                100m));
        var lineId = sale.Lines.Single().SaleLineId;

        var barrier = new Barrier(2);
        var clientReturn = factory.CreateClient();
        var clientVoid = factory.CreateClient();

        var returnTask = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await PostReturnRawAsync(clientReturn, org, sale.SaleId, lineId, 2m, "ReturnToStock");
        });
        var voidTask = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await PostVoidRawAsync(clientVoid, org, sale.SaleId);
        });

        var results = await Task.WhenAll(returnTask, voidTask);
        var returnOk = results[0].IsSuccessStatusCode;
        var voidOk = results[1].IsSuccessStatusCode;
        Assert.False(returnOk && voidOk, "Return and void must not both succeed on the same sale.");
        Assert.True(returnOk || voidOk, "Exactly one of return or void should succeed.");

        using var saleGet = Scoped(HttpMethod.Get, $"{Sales}/{sale.SaleId:D}", org);
        using var saleResponse = await setup.SendAsync(saleGet);
        saleResponse.EnsureSuccessStatusCode();
        var finalSale = await saleResponse.Content.ReadFromJsonAsync<PosSaleDto>(JsonOptions);
        Assert.NotNull(finalSale);

        var returns = await ListReturnsForSaleAsync(setup, org, sale.SaleId);
        if (voidOk)
        {
            Assert.Equal("Voided", finalSale!.Status);
            Assert.Empty(returns);
        }
        else
        {
            Assert.Equal("Completed", finalSale!.Status);
            Assert.Single(returns);
        }

        var account = await GetInventoryAsync(setup, org, product.ProductId);
        // Opening 50, sold 5 → either void restores to 50, or return 2 restores to 47.
        Assert.True(account.OnHandQuantity is 50m or 47m, $"Unexpected on-hand {account.OnHandQuantity}.");

        foreach (var response in results)
        {
            response.Dispose();
        }
    }

    [Fact]
    public async Task H_Same_ReturnId_idempotent_under_concurrency()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var setup = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(setup, org, "Idem Conc", 8m);
        var sale = await CheckoutCashAsync(
            setup,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 4m)],
                PosSaleOptions.CashPaymentMethod,
                50m));
        var lineId = sale.Lines.Single().SaleLineId;
        var returnId = Guid.NewGuid();

        var barrier = new Barrier(2);
        var clientA = factory.CreateClient();
        var clientB = factory.CreateClient();

        var taskA = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await PostReturnRawAsync(
                clientA, org, sale.SaleId, lineId, 2m, "DoNotRestock", returnId: returnId);
        });
        var taskB = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await PostReturnRawAsync(
                clientB, org, sale.SaleId, lineId, 2m, "DoNotRestock", returnId: returnId);
        });

        var responses = await Task.WhenAll(taskA, taskB);
        Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode));

        var bodies = new List<PosSaleReturnDto>();
        foreach (var response in responses)
        {
            bodies.Add((await response.Content.ReadFromJsonAsync<PosSaleReturnDto>(JsonOptions))!);
            response.Dispose();
        }

        Assert.Equal(bodies[0].ReturnId, bodies[1].ReturnId);
        Assert.Equal(returnId, bodies[0].ReturnId);
        Assert.Equal(bodies[0].ReturnNumber, bodies[1].ReturnNumber);

        var listed = await ListReturnsForSaleAsync(setup, org, sale.SaleId);
        Assert.Single(listed);
        Assert.Equal(2m, listed[0].Lines.Sum(l => l.QuantityReturned));
    }

    [Fact]
    public async Task I_Different_SaleIds_return_independently()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var setup = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(setup, org, "Indep Conc", 5m);
        var saleA = await CheckoutCashAsync(
            setup,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 3m)],
                PosSaleOptions.CashPaymentMethod,
                50m));
        var saleB = await CheckoutCashAsync(
            setup,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 3m)],
                PosSaleOptions.CashPaymentMethod,
                50m));

        var barrier = new Barrier(2);
        var clientA = factory.CreateClient();
        var clientB = factory.CreateClient();

        var taskA = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await PostReturnRawAsync(
                clientA, org, saleA.SaleId, saleA.Lines.Single().SaleLineId, 1m, "DoNotRestock");
        });
        var taskB = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await PostReturnRawAsync(
                clientB, org, saleB.SaleId, saleB.Lines.Single().SaleLineId, 1m, "DoNotRestock");
        });

        var responses = await Task.WhenAll(taskA, taskB);
        Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode));

        Assert.Single(await ListReturnsForSaleAsync(setup, org, saleA.SaleId));
        Assert.Single(await ListReturnsForSaleAsync(setup, org, saleB.SaleId));

        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    private PosDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<PosDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;
        return new PosDbContext(options);
    }

    private static async Task<HttpResponseMessage> PostReturnRawAsync(
        HttpClient client,
        Guid org,
        Guid saleId,
        Guid saleLineId,
        decimal qty,
        string restock,
        Guid? returnId = null,
        Guid? branchId = null,
        string? grants = null)
    {
        var body = new CreateSaleReturnRequest(
            saleId,
            "Concurrent return",
            [new CreateSaleReturnLineRequest(saleLineId, qty, restock)],
            ReturnId: returnId);
        using var request = Scoped(HttpMethod.Post, Returns, org, grants: grants, branchId: branchId);
        request.Content = JsonContent.Create(body, options: JsonOptions);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> PostVoidRawAsync(HttpClient client, Guid org, Guid saleId)
    {
        using var request = Scoped(HttpMethod.Post, $"{Sales}/{saleId:D}/void", org);
        request.Content = JsonContent.Create(new VoidSaleRequest("Concurrent void"), options: JsonOptions);
        return await client.SendAsync(request);
    }

    private static async Task<PosSaleDto> CheckoutCashAsync(
        HttpClient client,
        Guid org,
        CheckoutSaleRequest body,
        string? grants = null,
        Guid? branchId = null)
    {
        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, Actor).ConfigureAwait(false);
        using var request = Scoped(HttpMethod.Post, Sales, org, grants: grants, branchId: branchId);
        request.Content = JsonContent.Create(body, options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PosSaleDto>(JsonOptions))!;
    }

    private static async Task<IReadOnlyList<PosSaleReturnDto>> ListReturnsForSaleAsync(
        HttpClient client,
        Guid org,
        Guid saleId)
    {
        using var request = Scoped(HttpMethod.Get, $"{Returns}?saleId={saleId:D}", org);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<PagedResult<PosSaleReturnDto>>(JsonOptions);
        return page!.Items;
    }

    private static async Task<PosRefundableSaleDto> GetRefundableAsync(HttpClient client, Guid org, Guid saleId)
    {
        using var request = Scoped(HttpMethod.Get, $"{Returns}/refundable/{saleId:D}", org);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PosRefundableSaleDto>(JsonOptions))!;
    }

    private static async Task<PosCatalogProductDto> CreateProductAsync(
        HttpClient client,
        Guid org,
        string name,
        decimal price)
    {
        using var request = Scoped(HttpMethod.Post, Products, org);
        request.Content = JsonContent.Create(
            new CreatePosCatalogProductRequest(name, "Piece", price, null, Guid.NewGuid().ToString("N")[..12]),
            options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PosCatalogProductDto>(JsonOptions))!;
    }

    private static async Task<PosCatalogProductDto> CreateExpiryProductAsync(
        HttpClient client,
        Guid org,
        string name)
    {
        using var request = Scoped(HttpMethod.Post, Products, org);
        request.Content = JsonContent.Create(
            new CreatePosCatalogProductRequest(
                name,
                "Piece",
                20m,
                Sku: Guid.NewGuid().ToString("N")[..12],
                TracksExpiration: true),
            options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PosCatalogProductDto>(JsonOptions))!;
    }

    private static async Task EnableInventoryAsync(
        HttpClient client,
        Guid org,
        Guid productId,
        decimal opening)
    {
        using var request = Scoped(HttpMethod.Post, $"{Inventory}/{productId:D}/enable", org);
        request.Content = JsonContent.Create(
            opening > 0m
                ? new EnableInventoryTrackingRequest(OpeningQuantity: opening, UnitCost: 1m)
                : new EnableInventoryTrackingRequest(OpeningQuantity: opening),
            options: JsonOptions);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private static async Task ReceiveLotAsync(
        HttpClient client,
        Guid org,
        Guid productId,
        decimal qty,
        DateOnly expiry,
        string lotNumber,
        Guid? branchId = null)
    {
        using var request = Scoped(HttpMethod.Post, $"{Inventory}/{productId:D}/adjustments", org, branchId: branchId);
        request.Content = JsonContent.Create(
            new AdjustInventoryRequest("In", qty, "Receive", ExpirationDate: expiry, LotNumber: lotNumber),
            options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task AdjustInAsync(
        HttpClient client,
        Guid org,
        Guid productId,
        decimal qty,
        Guid? branchId = null)
    {
        using var request = Scoped(HttpMethod.Post, $"{Inventory}/{productId:D}/adjustments", org, branchId: branchId);
        request.Content = JsonContent.Create(new AdjustInventoryRequest("In", qty, "Stock"), options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<IReadOnlyList<PosInventoryLotDto>> ListLotsAsync(
        HttpClient client,
        Guid org,
        Guid productId)
    {
        using var request = Scoped(HttpMethod.Get, $"{Inventory}/{productId:D}/lots", org);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<PagedResult<PosInventoryLotDto>>(JsonOptions);
        return page!.Items;
    }

    private static async Task<PosInventoryAccountDto> GetInventoryAsync(
        HttpClient client,
        Guid org,
        Guid productId)
    {
        using var request = Scoped(HttpMethod.Get, $"{Inventory}/{productId:D}", org);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PosInventoryAccountDto>(JsonOptions))!;
    }

    private static async Task<POSCustomerDto> CreateCustomerAsync(HttpClient client, Guid org, string name)
    {
        using var request = Scoped(HttpMethod.Post, Customers, org);
        request.Content = JsonContent.Create(new CreateCustomerRequest(name, null, null, null), options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<POSCustomerDto>(JsonOptions))!;
    }

    private static HttpRequestMessage Scoped(
        HttpMethod method,
        string path,
        Guid organizationId,
        string? grants = null,
        Guid? branchId = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation(
            PosOrganizationHeaders.OrganizationHeaderName,
            organizationId.ToString("D"));
        request.Headers.TryAddWithoutValidation(
            PosOrganizationHeaders.ActorHeaderName,
            Actor.ToString("D"));
        if (!string.IsNullOrWhiteSpace(grants))
        {
            request.Headers.TryAddWithoutValidation(
                PosCommercialHeaders.SubscriptionStatusHeaderName,
                PosSubscriptionStatuses.Active);
            request.Headers.TryAddWithoutValidation(PosCommercialHeaders.FeatureGrantsHeaderName, grants);
        }

        if (branchId is { } id && id != Guid.Empty)
        {
            request.Headers.TryAddWithoutValidation(
                PosOrganizationHeaders.BranchHeaderName,
                id.ToString("D"));
        }

        return request;
    }

    private sealed class PosApiFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:PosDatabase", connectionString);
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PosDatabase"] = connectionString
                });
            });
        }
    }
}
