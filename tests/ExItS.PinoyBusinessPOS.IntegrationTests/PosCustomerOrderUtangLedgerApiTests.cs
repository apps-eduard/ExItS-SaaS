using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Api.Credit;
using ExItS.PinoyBusinessPOS.Api.Customers;
using ExItS.PinoyBusinessPOS.Api.Payments;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Application.Statements;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosCustomerOrderUtangLedgerApiTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Guid PersonalUser = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid SellerActor = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid PlatformBusinessCustomerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid LinkedCustomerAppUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TestBranchId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    private const string Products = "/api/v1/pos/catalog/products";
    private const string Inventory = "/api/v1/pos/inventory";
    private const string Customers = "/api/v1/pos/customers";

    [Fact]
    public async Task Personal_utang_order_posts_once_at_completion_and_projects_to_linked_statement()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var customer = await CreateLinkedCustomerAsync(client, org, PlatformBusinessCustomerId, "Ana Reyes");
        await CreateCreditAsync(client, org, customer.CustomerId, 500m, "Opening balance");

        var product = await CreateProductAsync(client, org, "Rice", "Kilogram", 400m, "co-utang-rice");
        await EnableInventoryAsync(client, org, product.ProductId, 20m);

        var order = await PlacePersonalUtangOrderAsync(client, org, product.ProductId, quantity: 2m);
        Assert.Equal("Submitted", order.Status);
        Assert.Equal("Utang", order.PaymentMethod);
        Assert.Equal("Unpaid", order.PaymentStatus);
        Assert.Equal(800m, order.Total);
        await AssertOutstandingAsync(client, org, customer.CustomerId, 500m);

        order = await AcceptOrderAsync(client, org, order.OrderId);
        Assert.Equal("Accepted", order.Status);
        await AssertOutstandingAsync(client, org, customer.CustomerId, 500m);

        await MarkReadyAsync(client, org, order.OrderId);
        await MarkCollectedAsync(client, org, order.OrderId);

        order = await CompleteOrderAsync(client, org, order.OrderId);
        Assert.Equal("Completed", order.Status);
        Assert.Equal("Unpaid", order.PaymentStatus);
        await AssertOutstandingAsync(client, org, customer.CustomerId, 1300m);

        var statement = await GetPersonalStatementAsync(client, org, PlatformBusinessCustomerId);
        Assert.Equal(1300m, statement.OutstandingBalance);

        var openDebt = await GetPersonalOpenDebtAsync(client, org, PlatformBusinessCustomerId);
        Assert.Contains(openDebt.Items, i =>
            i.Type == "UtangCharge"
            && i.ChargeAmount == 800m
            && i.ReferenceNumber.StartsWith("SO-", StringComparison.OrdinalIgnoreCase));

        var recent = await GetPersonalRecentActivityAsync(client, org, PlatformBusinessCustomerId);
        Assert.Contains(recent.Items, i =>
            i.Type == "UtangCharge" && i.ChargeAmount == 800m);

        await CreateRepaymentAsync(client, org, customer.CustomerId, 300m, "Partial after order");
        await AssertOutstandingAsync(client, org, customer.CustomerId, 1000m);

        await CompleteOrderAsync(client, org, order.OrderId);
        await AssertOutstandingAsync(client, org, customer.CustomerId, 1000m);
    }

    [Fact]
    public async Task Completed_cash_order_creates_no_utang_charge()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var customer = await CreateLinkedCustomerAsync(client, org, PlatformBusinessCustomerId, "Cash Ana");
        var product = await CreateProductAsync(client, org, "Snacks", "Piece", 50m, "co-cash-snack");
        await EnableInventoryAsync(client, org, product.ProductId, 10m);

        var order = await PlacePersonalOrderAsync(client, org, product.ProductId, 1m, "Cash");
        await FulfillPickupAndCompleteAsync(client, org, order.OrderId);
        await AssertOutstandingAsync(client, org, customer.CustomerId, 0m);
    }

    [Fact]
    public async Task Cancelled_utang_order_before_completion_creates_no_charge()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var customer = await CreateLinkedCustomerAsync(client, org, PlatformBusinessCustomerId, "Cancel Ana");
        await CreateCreditAsync(client, org, customer.CustomerId, 500m, "Opening");
        var product = await CreateProductAsync(client, org, "Bread", "Piece", 100m, "co-cancel-bread");
        await EnableInventoryAsync(client, org, product.ProductId, 10m);

        var order = await PlacePersonalUtangOrderAsync(client, org, product.ProductId, 2m);
        await CancelOrderAsCustomerAsync(client, org, order.OrderId);
        await AssertOutstandingAsync(client, org, customer.CustomerId, 500m);
    }

    [Fact]
    public async Task Concurrent_completion_does_not_duplicate_utang_charge()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var customer = await CreateLinkedCustomerAsync(client, org, PlatformBusinessCustomerId, "Race Ana");
        var product = await CreateProductAsync(client, org, "Noodles", "Pack", 100m, "co-race-noodles");
        await EnableInventoryAsync(client, org, product.ProductId, 10m);

        var order = await PlacePersonalUtangOrderAsync(client, org, product.ProductId, 3m);
        await AcceptOrderAsync(client, org, order.OrderId);
        await MarkReadyAsync(client, org, order.OrderId);
        await MarkCollectedAsync(client, org, order.OrderId);

        var tasks = Enumerable.Range(0, 5)
            .Select(_ => CompleteOrderLenientAsync(client, org, order.OrderId))
            .ToArray();
        await Task.WhenAll(tasks);

        await AssertOutstandingAsync(client, org, customer.CustomerId, 300m);
    }

    private static async Task CompleteOrderLenientAsync(HttpClient client, Guid orgId, Guid orderId)
    {
        using var request = Scoped(
            HttpMethod.Post,
            $"/api/v1/pos/organizations/{orgId:D}/customer-orders/{orderId:D}/complete",
            orgId,
            SellerActor);
        using var response = await client.SendAsync(request);
        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Conflict,
            $"Unexpected status: {response.StatusCode}");
    }

    private PosApiFactory CreateFactory() =>
        new(fixture.ConnectionString, PersonalUser, PlatformBusinessCustomerId, LinkedCustomerAppUserId);

    private static async Task AssertOutstandingAsync(HttpClient client, Guid orgId, Guid customerId, decimal expected)
    {
        using var req = Scoped(HttpMethod.Get, $"{Customers}/{customerId:D}/utang-summary", orgId);
        using var response = await client.SendAsync(req);
        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<CustomerUtangSummaryDto>(JsonOptions);
        Assert.Equal(expected, summary!.OutstandingAmount);
    }

    private static async Task<POSCustomerDto> CreateLinkedCustomerAsync(
        HttpClient client,
        Guid orgId,
        Guid platformBusinessCustomerId,
        string displayName)
    {
        using var request = Scoped(HttpMethod.Post, Customers, orgId);
        request.Content = JsonContent.Create(
            new CreateCustomerRequest(displayName, null, null, null, PlatformBusinessCustomerId: platformBusinessCustomerId),
            options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<POSCustomerDto>(JsonOptions))!;
    }

    private static async Task<CreditEntryDto> CreateCreditAsync(
        HttpClient client,
        Guid orgId,
        Guid customerId,
        decimal amount,
        string remarks)
    {
        using var request = Scoped(HttpMethod.Post, $"{Customers}/{customerId:D}/credit-entries", orgId);
        request.Content = JsonContent.Create(new CreateCreditEntryRequest(amount, remarks), options: JsonOptions);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreditEntryDto>(JsonOptions))!;
    }

    private static async Task CreateRepaymentAsync(
        HttpClient client,
        Guid orgId,
        Guid customerId,
        decimal amount,
        string remarks)
    {
        using var request = Scoped(HttpMethod.Post, $"{Customers}/{customerId:D}/repayments", orgId, SellerActor);
        request.Content = JsonContent.Create(new CreateRepaymentRequest(amount, remarks), options: JsonOptions);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<PosCatalogProductDto> CreateProductAsync(
        HttpClient client,
        Guid orgId,
        string name,
        string unit,
        decimal price,
        string sku)
    {
        using var request = Scoped(HttpMethod.Post, Products, orgId);
        request.Content = JsonContent.Create(
            new CreatePosCatalogProductRequest(name, unit, price, null, sku),
            options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PosCatalogProductDto>(JsonOptions))!;
    }

    private static async Task EnableInventoryAsync(HttpClient client, Guid orgId, Guid productId, decimal qty)
    {
        using var request = Scoped(HttpMethod.Post, $"{Inventory}/{productId:D}/enable", orgId, SellerActor);
        request.Content = JsonContent.Create(new EnableInventoryTrackingRequest(qty), options: JsonOptions);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<CustomerOrderDto> PlacePersonalUtangOrderAsync(
        HttpClient client,
        Guid orgId,
        Guid productId,
        decimal quantity) =>
        await PlacePersonalOrderAsync(client, orgId, productId, quantity, "Utang");

    private static async Task<CustomerOrderDto> PlacePersonalOrderAsync(
        HttpClient client,
        Guid orgId,
        Guid productId,
        decimal quantity,
        string paymentMethod)
    {
        using var request = PersonalScoped(
            HttpMethod.Post,
            $"/api/v1/pos/customer-orders/organizations/{orgId:D}",
            orgId,
            PersonalUser);
        request.Content = JsonContent.Create(
            new PlaceCustomerOrderRequest(
                "Pickup",
                TestBranchId,
                "Personal",
                "Ana Reyes",
                PersonalUser,
                PlatformBusinessCustomerId,
                null,
                null,
                [new PlaceCustomerOrderLineRequest(productId, quantity)],
                null,
                null,
                null,
                paymentMethod),
            options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CustomerOrderDto>(JsonOptions))!;
    }

    private static async Task<CustomerOrderDto> AcceptOrderAsync(HttpClient client, Guid orgId, Guid orderId)
    {
        using var request = Scoped(
            HttpMethod.Post,
            $"/api/v1/pos/organizations/{orgId:D}/customer-orders/{orderId:D}/accept",
            orgId,
            SellerActor);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CustomerOrderDto>(JsonOptions))!;
    }

    private static async Task MarkReadyAsync(HttpClient client, Guid orgId, Guid orderId)
    {
        using var request = Scoped(
            HttpMethod.Post,
            $"/api/v1/pos/organizations/{orgId:D}/customer-orders/{orderId:D}/mark-ready",
            orgId,
            SellerActor);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private static async Task MarkCollectedAsync(HttpClient client, Guid orgId, Guid orderId)
    {
        using var request = Scoped(
            HttpMethod.Post,
            $"/api/v1/pos/organizations/{orgId:D}/customer-orders/{orderId:D}/mark-collected",
            orgId,
            SellerActor);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<CustomerOrderDto> CompleteOrderAsync(HttpClient client, Guid orgId, Guid orderId)
    {
        using var request = Scoped(
            HttpMethod.Post,
            $"/api/v1/pos/organizations/{orgId:D}/customer-orders/{orderId:D}/complete",
            orgId,
            SellerActor);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CustomerOrderDto>(JsonOptions))!;
    }

    private static async Task CancelOrderAsCustomerAsync(HttpClient client, Guid orgId, Guid orderId)
    {
        using var request = PersonalScoped(
            HttpMethod.Post,
            $"/api/v1/pos/customer-orders/organizations/{orgId:D}/{orderId:D}/cancel",
            orgId,
            PersonalUser);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private static async Task CancelOrderAsync(HttpClient client, Guid orgId, Guid orderId)
    {
        using var request = Scoped(
            HttpMethod.Post,
            $"/api/v1/pos/organizations/{orgId:D}/customer-orders/{orderId:D}/cancel",
            orgId,
            SellerActor);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private static async Task FulfillPickupAndCompleteAsync(HttpClient client, Guid orgId, Guid orderId)
    {
        await AcceptOrderAsync(client, orgId, orderId);
        await MarkReadyAsync(client, orgId, orderId);
        await MarkCollectedAsync(client, orgId, orderId);
        await CompleteOrderAsync(client, orgId, orderId);
    }

    private static async Task<LinkedCustomerStatementSummaryDto> GetPersonalStatementAsync(
        HttpClient client,
        Guid orgId,
        Guid platformBusinessCustomerId)
    {
        using var request = PersonalScoped(
            HttpMethod.Get,
            $"/api/v1/pos/personal/linked-customers/{platformBusinessCustomerId:D}/statement?organizationId={orgId:D}",
            orgId,
            PersonalUser);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LinkedCustomerStatementSummaryDto>(JsonOptions))!;
    }

    private static async Task<LinkedCustomerOpenDebtActivityPageDto> GetPersonalOpenDebtAsync(
        HttpClient client,
        Guid orgId,
        Guid platformBusinessCustomerId)
    {
        using var request = PersonalScoped(
            HttpMethod.Get,
            $"/api/v1/pos/personal/linked-customers/{platformBusinessCustomerId:D}/open-debt-activity?organizationId={orgId:D}",
            orgId,
            PersonalUser);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LinkedCustomerOpenDebtActivityPageDto>(JsonOptions))!;
    }

    private static async Task<LinkedCustomerRecentActivityPageDto> GetPersonalRecentActivityAsync(
        HttpClient client,
        Guid orgId,
        Guid platformBusinessCustomerId)
    {
        using var request = PersonalScoped(
            HttpMethod.Get,
            $"/api/v1/pos/personal/linked-customers/{platformBusinessCustomerId:D}/activity?organizationId={orgId:D}",
            orgId,
            PersonalUser);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LinkedCustomerRecentActivityPageDto>(JsonOptions))!;
    }

    private static HttpRequestMessage Scoped(
        HttpMethod method,
        string path,
        Guid organizationId,
        Guid? actorId = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation(PosOrganizationHeaders.OrganizationHeaderName, organizationId.ToString("D"));
        if (actorId is not null)
        {
            request.Headers.TryAddWithoutValidation(PosOrganizationHeaders.ActorHeaderName, actorId.Value.ToString("D"));
        }

        return request;
    }

    private static HttpRequestMessage PersonalScoped(
        HttpMethod method,
        string path,
        Guid organizationId,
        Guid personalUserId)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation(PosOrganizationHeaders.ActorHeaderName, personalUserId.ToString("D"));
        return request;
    }

    private sealed class PosApiFactory(
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
            });
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>());
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
}
