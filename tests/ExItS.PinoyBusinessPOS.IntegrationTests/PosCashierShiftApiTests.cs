using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.CashierShifts;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Domain.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosCashierShiftApiTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Guid Actor = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    private const string Shifts = "/api/v1/pos/cashier-shifts";
    private const string Sales = "/api/v1/pos/sales";
    private const string Products = "/api/v1/pos/catalog/products";

    [Fact]
    public async Task Open_movement_sale_close_lifecycle_with_variance()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var register = await PosShiftIntegrationSupport.EnsureRegisterAsync(client, org, Actor);
        using var open = Scoped(HttpMethod.Post, Shifts, org);
        open.Content = JsonContent.Create(new OpenCashierShiftRequest(register.RegisterId, 100m), options: JsonOptions);
        using var openResponse = await client.SendAsync(open);
        openResponse.EnsureSuccessStatusCode();
        var shift = await openResponse.Content.ReadFromJsonAsync<PosCashierShiftDto>(JsonOptions);
        Assert.StartsWith("SHIFT-", shift!.ShiftNumber, StringComparison.Ordinal);
        Assert.Equal(register.RegisterId, shift.RegisterId);

        using var duplicateOpen = Scoped(HttpMethod.Post, Shifts, org);
        duplicateOpen.Content = JsonContent.Create(new OpenCashierShiftRequest(register.RegisterId, 50m), options: JsonOptions);
        using var duplicateResponse = await client.SendAsync(duplicateOpen);
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
        Assert.Equal(ApplicationErrorCodes.CashierShiftOpenConflict, await ReadErrorCodeAsync(duplicateResponse));

        var product = await CreateProductAsync(client, org, "Bread", "Piece", 25m, "shift-bread");
        using var saleReq = Scoped(HttpMethod.Post, Sales, org);
        saleReq.Content = JsonContent.Create(
            new CheckoutSaleRequest([new CheckoutSaleLineRequest(product.ProductId, 2m)], "Cash", AmountTendered: 100m),
            options: JsonOptions);
        using var saleResponse = await client.SendAsync(saleReq);
        saleResponse.EnsureSuccessStatusCode();
        var sale = await saleResponse.Content.ReadFromJsonAsync<PosSaleDto>(JsonOptions);
        Assert.Equal(shift.ShiftId, sale!.ShiftId);

        using var movement = Scoped(HttpMethod.Post, $"{Shifts}/{shift.ShiftId:D}/movements", org);
        movement.Content = JsonContent.Create(
            new RecordCashierShiftMovementRequest("CashIn", 10m, "Float top-up"),
            options: JsonOptions);
        (await client.SendAsync(movement)).EnsureSuccessStatusCode();

        using var summaryBefore = Scoped(HttpMethod.Get, $"{Shifts}/{shift.ShiftId:D}/summary", org);
        using var summaryResponse = await client.SendAsync(summaryBefore);
        summaryResponse.EnsureSuccessStatusCode();
        var summary = await summaryResponse.Content.ReadFromJsonAsync<PosCashierShiftSummaryDto>(JsonOptions);
        Assert.Equal(160m, summary!.ExpectedCashAmount);

        using var close = Scoped(HttpMethod.Post, $"{Shifts}/{shift.ShiftId:D}/close", org);
        close.Content = JsonContent.Create(new CloseCashierShiftRequest(165m, "Short counted"), options: JsonOptions);
        using var closeResponse = await client.SendAsync(close);
        closeResponse.EnsureSuccessStatusCode();
        var closed = await closeResponse.Content.ReadFromJsonAsync<PosCashierShiftDto>(JsonOptions);
        Assert.Equal("Closed", closed!.Status);
        Assert.Equal(5m, closed!.CashVarianceAmount);
    }

    [Fact]
    public async Task Checkout_without_open_shift_fails_with_stable_code()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Juice", "Piece", 15m, "shift-juice");

        using var checkout = Scoped(HttpMethod.Post, Sales, org);
        checkout.Content = JsonContent.Create(
            new CheckoutSaleRequest([new CheckoutSaleLineRequest(product.ProductId, 1m)], "Cash", AmountTendered: 20m),
            options: JsonOptions);
        using var response = await client.SendAsync(checkout);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(ApplicationErrorCodes.CashierShiftNoOpenShift, await ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task Cancel_with_sale_is_denied()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var shift = await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, Actor, 0m);
        var product = await CreateProductAsync(client, org, "Milk", "Piece", 20m, "shift-milk");

        using var saleReq = Scoped(HttpMethod.Post, Sales, org);
        saleReq.Content = JsonContent.Create(
            new CheckoutSaleRequest([new CheckoutSaleLineRequest(product.ProductId, 1m)], "Cash", AmountTendered: 50m),
            options: JsonOptions);
        (await client.SendAsync(saleReq)).EnsureSuccessStatusCode();

        using var cancel = Scoped(HttpMethod.Post, $"{Shifts}/{shift.ShiftId:D}/cancel", org);
        using var cancelResponse = await client.SendAsync(cancel);
        Assert.Equal(HttpStatusCode.Conflict, cancelResponse.StatusCode);
        Assert.Equal(DomainErrorCodes.CashierShiftCancelBlockedByActivity, await ReadErrorCodeAsync(cancelResponse));
    }

    private static async Task<PosCatalogProductDto> CreateProductAsync(
        HttpClient client,
        Guid org,
        string name,
        string uom,
        decimal price,
        string sku)
    {
        using var request = Scoped(HttpMethod.Post, Products, org);
        request.Content = JsonContent.Create(
            new CreatePosCatalogProductRequest(name, uom, price, null, sku),
            options: JsonOptions);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PosCatalogProductDto>(JsonOptions))!;
    }

    private static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return problem.TryGetProperty("errorCode", out var code) ? code.GetString() : null;
    }

    private static HttpRequestMessage Scoped(HttpMethod method, string path, Guid organizationId)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation(
            ExItS.PinoyBusinessPOS.Api.Common.PosOrganizationHeaders.OrganizationHeaderName,
            organizationId.ToString("D"));
        request.Headers.TryAddWithoutValidation(
            ExItS.PinoyBusinessPOS.Api.Common.PosOrganizationHeaders.ActorHeaderName,
            Actor.ToString("D"));
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
