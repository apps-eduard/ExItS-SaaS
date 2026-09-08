using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.CashierShifts;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Registers;
using ExItS.PinoyBusinessPOS.Application.Sales;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

/// <summary>
/// Register history + sales drilldown filters, cashier actor scope, and activity totals.
/// </summary>
[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosRegisterHistoryDrilldownApiTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Guid Owner = Guid.Parse("a0a0a0a0-a0a0-a0a0-a0a0-a0a0a0a0a0a0");
    private static readonly Guid CashierA = Guid.Parse("b0b0b0b0-b0b0-b0b0-b0b0-b0b0b0b0b0b0");
    private static readonly Guid CashierB = Guid.Parse("c0c0c0c0-c0c0-c0c0-c0c0-c0c0c0c0c0c0");

    private const string Sales = "/api/v1/pos/sales";
    private const string Shifts = "/api/v1/pos/cashier-shifts";
    private const string Registers = "/api/v1/pos/registers";
    private const string Products = "/api/v1/pos/catalog/products";

    [Fact]
    public async Task Register_history_and_sales_filters_respect_scope_and_cashier_isolation()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var otherOrg = Guid.NewGuid();

        await PosInventoryOpsIntegrationSupport.AssignRoleAsync(client, org, Owner, CashierA, "Cashier");
        await PosInventoryOpsIntegrationSupport.AssignRoleAsync(client, org, Owner, CashierB, "Cashier");

        var register1 = await PosShiftIntegrationSupport.EnsureRegisterAsync(client, org, Owner, "History Reg 1");
        using var createReg2 = Scoped(HttpMethod.Post, Registers, org, Owner);
        createReg2.Content = JsonContent.Create(new CreateRegisterRequest("History Reg 2"), options: JsonOptions);
        using var createReg2Response = await client.SendAsync(createReg2);
        createReg2Response.EnsureSuccessStatusCode();
        var register2 = (await createReg2Response.Content.ReadFromJsonAsync<PosRegisterDto>(JsonOptions))!;

        var shiftA = await PosShiftIntegrationSupport.EnsureOpenShiftAsync(
            client, org, CashierA, 100m, register1.RegisterId);
        var shiftB = await PosShiftIntegrationSupport.EnsureOpenShiftAsync(
            client, org, CashierB, 100m, register2.RegisterId);

        var product = await CreateProductAsync(client, org, Owner, "History Pan", "Piece", 50m, "hist-pan");

        var saleA = await CheckoutAsync(
            client, org, CashierA,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                PosSaleOptions.CashPaymentMethod,
                50m,
                ShiftId: shiftA.ShiftId));
        var saleB = await CheckoutAsync(
            client, org, CashierB,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 2m)],
                PosSaleOptions.ManualGCashPaymentMethod,
                ShiftId: shiftB.ShiftId));

        Assert.Equal(register1.RegisterId, saleA.RegisterId);
        Assert.Equal(shiftA.ShiftId, saleA.ShiftId);
        Assert.Equal(CashierA, saleA.RecordedBy);

        // A. Shifts for selected register only
        using var shiftsReg1 = Scoped(
            HttpMethod.Get,
            $"{Shifts}?registerId={register1.RegisterId:D}&page=1&pageSize=50",
            org,
            Owner);
        using var shiftsReg1Response = await client.SendAsync(shiftsReg1);
        shiftsReg1Response.EnsureSuccessStatusCode();
        var reg1Shifts = await shiftsReg1Response.Content.ReadFromJsonAsync<PosCashierShiftPagedResult>(JsonOptions);
        Assert.Contains(reg1Shifts!.Items, s => s.ShiftId == shiftA.ShiftId);
        Assert.DoesNotContain(reg1Shifts.Items, s => s.ShiftId == shiftB.ShiftId);
        Assert.Equal(1, Assert.Single(reg1Shifts.Items, s => s.ShiftId == shiftA.ShiftId).CompletedTransactionCount);
        Assert.Equal(50m, Assert.Single(reg1Shifts.Items, s => s.ShiftId == shiftA.ShiftId).CompletedSalesTotal);

        // B. Register sales filter
        var byRegister = await ListSalesAsync(client, org, Owner, $"registerId={register1.RegisterId:D}");
        Assert.Contains(byRegister.Items, s => s.SaleId == saleA.SaleId);
        Assert.DoesNotContain(byRegister.Items, s => s.SaleId == saleB.SaleId);

        // C. Shift sales filter
        var byShift = await ListSalesAsync(client, org, Owner, $"cashierShiftId={shiftA.ShiftId:D}");
        Assert.Equal(saleA.SaleId, Assert.Single(byShift.Items).SaleId);

        // E. Cross-org concealed
        var otherOrgSales = await ListSalesAsync(client, otherOrg, Owner, $"registerId={register1.RegisterId:D}");
        Assert.Empty(otherOrgSales.Items);

        // F/G/H. Cashier own shift allowed; other actor shift denied
        using var ownShift = Scoped(HttpMethod.Get, $"{Shifts}/{shiftA.ShiftId:D}", org, CashierA);
        using var ownShiftResponse = await client.SendAsync(ownShift);
        ownShiftResponse.EnsureSuccessStatusCode();

        using var otherShift = Scoped(HttpMethod.Get, $"{Shifts}/{shiftB.ShiftId:D}", org, CashierA);
        using var otherShiftResponse = await client.SendAsync(otherShift);
        Assert.Equal(HttpStatusCode.NotFound, otherShiftResponse.StatusCode);

        // I/J. Cashier own transactions; other cashier concealed
        var cashierASales = await ListSalesAsync(client, org, CashierA, "page=1&pageSize=50");
        Assert.Contains(cashierASales.Items, s => s.SaleId == saleA.SaleId);
        Assert.DoesNotContain(cashierASales.Items, s => s.SaleId == saleB.SaleId);
        Assert.All(cashierASales.Items, s => Assert.Equal(CashierA, s.RecordedBy));

        using var getOtherSale = Scoped(HttpMethod.Get, $"{Sales}/{saleB.SaleId:D}", org, CashierA);
        using var getOtherSaleResponse = await client.SendAsync(getOtherSale);
        Assert.Equal(HttpStatusCode.NotFound, getOtherSaleResponse.StatusCode);

        // K. Voided sale remains visible
        using var voidReq = Scoped(HttpMethod.Post, $"{Sales}/{saleA.SaleId:D}/void", org, Owner);
        voidReq.Content = JsonContent.Create(new VoidSaleRequest("History void"), options: JsonOptions);
        using var voidResponse = await client.SendAsync(voidReq);
        voidResponse.EnsureSuccessStatusCode();

        var voidedList = await ListSalesAsync(
            client, org, Owner, $"registerId={register1.RegisterId:D}&status=Voided");
        Assert.Contains(voidedList.Items, s => s.SaleId == saleA.SaleId);

        // M/N/O/P/Q/R. Paging, date, actor, payment, status, sale-number
        var paged = await ListSalesAsync(client, org, Owner, "page=1&pageSize=1");
        Assert.Single(paged.Items);
        Assert.True(paged.TotalCount >= 2);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dated = await ListSalesAsync(
            client, org, Owner, $"fromDate={today:yyyy-MM-dd}&toDate={today:yyyy-MM-dd}");
        Assert.NotEmpty(dated.Items);

        var byActor = await ListSalesAsync(client, org, Owner, $"actorId={CashierB:D}");
        Assert.Contains(byActor.Items, s => s.SaleId == saleB.SaleId);
        Assert.DoesNotContain(byActor.Items, s => s.SaleId == saleA.SaleId);

        var byPayment = await ListSalesAsync(
            client, org, Owner, $"paymentMethod={PosSaleOptions.ManualGCashPaymentMethod}");
        Assert.Contains(byPayment.Items, s => s.SaleId == saleB.SaleId);

        var byNumber = await ListSalesAsync(client, org, Owner, $"saleNumber={saleB.SaleNumber}");
        Assert.Equal(saleB.SaleId, Assert.Single(byNumber.Items).SaleId);

        // S. Register activity totals match authoritative sales (register2 still has completed saleB)
        using var activity = Scoped(
            HttpMethod.Get,
            $"{Registers}/{register2.RegisterId:D}/activity",
            org,
            Owner);
        using var activityResponse = await client.SendAsync(activity);
        activityResponse.EnsureSuccessStatusCode();
        var totals = await activityResponse.Content.ReadFromJsonAsync<PosRegisterActivityDto>(JsonOptions);
        Assert.Equal(1, totals!.CompletedSaleCount);
        Assert.Equal(100m, totals.GrossSalesTotal);
        Assert.Equal(100m, totals.ManualGCashSalesTotal);

        // Cashier cannot list other cashiers' shifts even when requesting register2
        using var cashierList = Scoped(
            HttpMethod.Get,
            $"{Shifts}?registerId={register2.RegisterId:D}&page=1&pageSize=50",
            org,
            CashierA);
        using var cashierListResponse = await client.SendAsync(cashierList);
        cashierListResponse.EnsureSuccessStatusCode();
        var cashierPage = await cashierListResponse.Content.ReadFromJsonAsync<PosCashierShiftPagedResult>(JsonOptions);
        Assert.Empty(cashierPage!.Items);
    }

    private static async Task<PagedResult<PosSaleDto>> ListSalesAsync(
        HttpClient client,
        Guid org,
        Guid actor,
        string query)
    {
        using var request = Scoped(HttpMethod.Get, $"{Sales}?{query}", org, actor);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<PagedResult<PosSaleDto>>(JsonOptions);
        Assert.NotNull(page);
        return page!;
    }

    private static async Task<PosSaleDto> CheckoutAsync(
        HttpClient client,
        Guid org,
        Guid actor,
        CheckoutSaleRequest body)
    {
        using var request = Scoped(HttpMethod.Post, Sales, org, actor);
        request.Content = JsonContent.Create(body, options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var sale = await response.Content.ReadFromJsonAsync<PosSaleDto>(JsonOptions);
        Assert.NotNull(sale);
        return sale!;
    }

    private static async Task<PosCatalogProductDto> CreateProductAsync(
        HttpClient client,
        Guid org,
        Guid actor,
        string name,
        string unitOfMeasure,
        decimal sellingPrice,
        string? sku = null)
    {
        using var request = Scoped(HttpMethod.Post, Products, org, actor);
        request.Content = JsonContent.Create(
            new CreatePosCatalogProductRequest(name, unitOfMeasure, sellingPrice, null, sku),
            options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<PosCatalogProductDto>(JsonOptions);
        Assert.NotNull(product);
        return product!;
    }

    private static HttpRequestMessage Scoped(
        HttpMethod method,
        string path,
        Guid organizationId,
        Guid actorId)
    {
        return PosIntegrationRequest.Scoped(method, path, organizationId, actorId);
    }

    private sealed class PosApiFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:PosDatabase", connectionString);
            builder.UseSetting("LocalValidation:Enabled", "false");
            builder.UseSetting("PosDeviceAuthorization:EnforcementEnabled", "false");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PosDatabase"] = connectionString,
                    ["LocalValidation:Enabled"] = "false",
                    ["PosDeviceAuthorization:EnforcementEnabled"] = "false"
                });
            });
        }
    }
}
