using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Credit;
using ExItS.PinoyBusinessPOS.Api.Customers;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.CashierShifts;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Registers;
using ExItS.PinoyBusinessPOS.Application.Sales;

namespace ExItS.PinoyBusinessPOS.IntegrationTests.Support;

internal static class PosSpinePosApiHelpers
{
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    internal static HttpRequestMessage PosBearer(
        HttpMethod method,
        string path,
        string accessToken,
        object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }

        return request;
    }

    internal static async Task<PosCatalogProductDto> CreateCatalogProductAsync(
        HttpClient posClient,
        string accessToken,
        string sku = "spine-prod")
    {
        using var request = PosBearer(
            HttpMethod.Post,
            "/api/v1/pos/catalog/products",
            accessToken,
            new CreatePosCatalogProductRequest("Bigas", "Kilogram", 50m, null, sku));
        var response = await posClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var product = await response.Content.ReadFromJsonAsync<PosCatalogProductDto>(JsonOptions);
        return product ?? throw new InvalidOperationException("Catalog product response was empty.");
    }

    internal static async Task<POSCustomerDto> CreateCustomerAsync(
        HttpClient posClient,
        string accessToken,
        string name)
    {
        using var request = PosBearer(
            HttpMethod.Post,
            "/api/v1/pos/customers",
            accessToken,
            new CreateCustomerRequest(name, null, null, null));
        var response = await posClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var customer = await response.Content.ReadFromJsonAsync<POSCustomerDto>(JsonOptions);
        return customer ?? throw new InvalidOperationException("Customer response was empty.");
    }

    internal static async Task EnsureOpenShiftAsync(HttpClient posClient, string accessToken)
    {
        using var current = PosBearer(HttpMethod.Get, "/api/v1/pos/cashier-shifts/current", accessToken);
        using var currentResponse = await posClient.SendAsync(current);
        if (currentResponse.IsSuccessStatusCode)
        {
            var existing = await currentResponse.Content.ReadFromJsonAsync<PosCashierShiftDto>(JsonOptions);
            if (existing is not null)
            {
                return;
            }
        }

        using var createRegister = PosBearer(
            HttpMethod.Post,
            "/api/v1/pos/registers",
            accessToken,
            new CreateRegisterRequest($"Register {Guid.NewGuid():N}"));
        using var registerResponse = await posClient.SendAsync(createRegister);
        registerResponse.EnsureSuccessStatusCode();
        var register = await registerResponse.Content.ReadFromJsonAsync<PosRegisterDto>(JsonOptions)
            ?? throw new InvalidOperationException("Register response was empty.");

        using var open = PosBearer(
            HttpMethod.Post,
            "/api/v1/pos/cashier-shifts",
            accessToken,
            new OpenCashierShiftRequest(register.RegisterId, 0m));
        using var openResponse = await posClient.SendAsync(open);
        openResponse.EnsureSuccessStatusCode();
    }

    internal static async Task<HttpResponseMessage> CheckoutAsync(
        HttpClient posClient,
        string accessToken,
        CheckoutSaleRequest body)
    {
        var request = PosBearer(HttpMethod.Post, "/api/v1/pos/sales", accessToken, body);
        return await posClient.SendAsync(request);
    }

    internal static async Task<HttpResponseMessage> CreateCreditEntryAsync(
        HttpClient posClient,
        string accessToken,
        Guid customerId,
        decimal amount,
        string remarks)
    {
        var request = PosBearer(
            HttpMethod.Post,
            $"/api/v1/pos/customers/{customerId:D}/credit-entries",
            accessToken,
            new CreateCreditEntryRequest(amount, remarks));
        return await posClient.SendAsync(request);
    }
}
