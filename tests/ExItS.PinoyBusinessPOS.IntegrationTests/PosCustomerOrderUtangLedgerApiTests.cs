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
    public async Task Cash_pickup_persists_requested_time_and_rejects_invalid_retries()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var customer = await CreateLinkedCustomerAsync(client, org, PlatformBusinessCustomerId, "Pickup Ana");
        await ApproveCustomerCreditAsync(client, org, customer.CustomerId, 20m);
        var product = await CreateProductAsync(client, org, "Coffee", "Piece", 75m, "co-pickup-coffee");
        await EnableInventoryAsync(client, org, product.ProductId, 2m);

        var clientOrderId = Guid.NewGuid();
        const string idempotencyKey = "pickup-place-once";
        using (var placed = await PostPlaceAsync(
            client,
            org,
            product.ProductId,
            1m,
            "Cash",
            PlatformBusinessCustomerId,
            clientOrderId,
            idempotencyKey,
            "2027-06-15",
            "14:30"))
        {
            Assert.Equal(HttpStatusCode.Created, placed.StatusCode);
            var order = (await placed.Content.ReadFromJsonAsync<CustomerOrderDto>(JsonOptions))!;
            Assert.Equal(75m, order.Total);
            Assert.Equal("Cash", order.PaymentMethod);
            Assert.Equal("2027-06-15 14:30", order.RequestedPickupLocal);
            Assert.Equal("Asia/Manila", order.RequestedPickupTimeZoneId);
            Assert.NotNull(order.RequestedPickupAtUtc);

            using var buyer = PersonalScoped(
                HttpMethod.Get,
                $"/api/v1/pos/customer-orders/mine/{order.OrderId:D}",
                org,
                PersonalUser);
            using var buyerResponse = await client.SendAsync(buyer);
            Assert.Equal(HttpStatusCode.OK, buyerResponse.StatusCode);
            var buyerOrder = (await buyerResponse.Content.ReadFromJsonAsync<CustomerOrderDto>(JsonOptions))!;
            Assert.Equal(order.OrderId, buyerOrder.OrderId);
            Assert.Equal(order.RequestedPickupLocal, buyerOrder.RequestedPickupLocal);

            using var seller = Scoped(
                HttpMethod.Get,
                $"/api/v1/pos/organizations/{org:D}/customer-orders/{order.OrderId:D}",
                org,
                SellerActor);
            using var sellerResponse = await client.SendAsync(seller);
            Assert.Equal(HttpStatusCode.OK, sellerResponse.StatusCode);
            var sellerOrder = (await sellerResponse.Content.ReadFromJsonAsync<CustomerOrderDto>(JsonOptions))!;
            Assert.Equal(order.OrderId, sellerOrder.OrderId);
            Assert.Equal("2027-06-15 14:30", sellerOrder.RequestedPickupLocal);
        }

        using (var replay = await PostPlaceAsync(
            client,
            org,
            product.ProductId,
            1m,
            "Cash",
            PlatformBusinessCustomerId,
            clientOrderId,
            idempotencyKey,
            "2027-06-15",
            "14:30"))
        {
            Assert.Equal(HttpStatusCode.Created, replay.StatusCode);
            var replayed = (await replay.Content.ReadFromJsonAsync<CustomerOrderDto>(JsonOptions))!;
            Assert.Equal(clientOrderId, replayed.OrderId);
        }

        Assert.Equal(1, await CountSellerOrdersAsync(client, org));

        using (var past = await PostPlaceAsync(
            client,
            org,
            product.ProductId,
            1m,
            "Cash",
            PlatformBusinessCustomerId,
            Guid.NewGuid(),
            "past-pickup",
            "2020-01-01",
            "10:00"))
        {
            Assert.Equal(HttpStatusCode.BadRequest, past.StatusCode);
        }

        using (var gcash = await PostPlaceAsync(
            client,
            org,
            product.ProductId,
            1m,
            "ManualGCash",
            PlatformBusinessCustomerId,
            Guid.NewGuid(),
            "gcash-missing-ref",
            null,
            null))
        {
            Assert.Equal(HttpStatusCode.BadRequest, gcash.StatusCode);
        }

        using (var utang = await PostPlaceAsync(
            client,
            org,
            product.ProductId,
            1m,
            "Utang",
            PlatformBusinessCustomerId,
            Guid.NewGuid(),
            "utang-short",
            null,
            null))
        {
            Assert.NotEqual(HttpStatusCode.Created, utang.StatusCode);
        }

        using (var unlinked = await PostPlaceAsync(
            client,
            org,
            product.ProductId,
            1m,
            "Cash",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "unlinked-buyer",
            null,
            null))
        {
            Assert.NotEqual(HttpStatusCode.Created, unlinked.StatusCode);
        }

        using (var stock = await PostPlaceAsync(
            client,
            org,
            product.ProductId,
            5m,
            "Cash",
            PlatformBusinessCustomerId,
            Guid.NewGuid(),
            "stock-conflict",
            null,
            null))
        {
            Assert.NotEqual(HttpStatusCode.Created, stock.StatusCode);
        }

        Assert.Equal(1, await CountSellerOrdersAsync(client, org));
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

    private static async Task ApproveCustomerCreditAsync(
        HttpClient client,
        Guid orgId,
        Guid customerId,
        decimal creditLimit)
    {
        using var put = Scoped(HttpMethod.Put, $"{Customers}/{customerId:D}/credit-policy", orgId, SellerActor);
        put.Content = JsonContent.Create(
            new UpsertCustomerCreditPolicyRequest(creditLimit, 30, "checkout audit"),
            options: JsonOptions);
        using var putResponse = await client.SendAsync(put);
        putResponse.EnsureSuccessStatusCode();

        using var get = Scoped(HttpMethod.Get, $"{Customers}/{customerId:D}/credit-policy", orgId, SellerActor);
        using var getResponse = await client.SendAsync(get);
        getResponse.EnsureSuccessStatusCode();
        var policy = (await getResponse.Content.ReadFromJsonAsync<CustomerCreditPolicyReadDto>(JsonOptions))!;

        using var approve = Scoped(HttpMethod.Post, $"{Customers}/{customerId:D}/credit-policy/approve", orgId, SellerActor);
        approve.Content = JsonContent.Create(
            new ApproveCustomerCreditPolicyRequest("checkout audit", policy.ExpectedUpdatedAtUtc!.Value),
            options: JsonOptions);
        using var approveResponse = await client.SendAsync(approve);
        approveResponse.EnsureSuccessStatusCode();
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
        request.Headers.TryAddWithoutValidation(PosOrganizationHeaders.BranchHeaderName, TestBranchId.ToString("D"));
        request.Content = JsonContent.Create(
            qty > 0m
                ? new EnableInventoryTrackingRequest(OpeningQuantity: qty, UnitCost: 1m)
                : new EnableInventoryTrackingRequest(OpeningQuantity: qty),
            options: JsonOptions);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<CustomerOrderDto> PlacePersonalUtangOrderAsync(
        HttpClient client,
        Guid orgId,
        Guid productId,
        decimal quantity) =>
        await PlacePersonalOrderAsync(client, orgId, productId, quantity, "Utang");

    private static async Task<HttpResponseMessage> PostPlaceAsync(
        HttpClient client,
        Guid orgId,
        Guid productId,
        decimal quantity,
        string paymentMethod,
        Guid platformBusinessCustomerId,
        Guid clientOrderId,
        string idempotencyKey,
        string? requestedPickupDate,
        string? requestedPickupTime)
    {
        var request = PersonalScoped(
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
                platformBusinessCustomerId,
                null,
                null,
                [new PlaceCustomerOrderLineRequest(productId, quantity)],
                null,
                clientOrderId,
                idempotencyKey,
                paymentMethod,
                null,
                requestedPickupDate,
                requestedPickupTime),
            options: JsonOptions);
        return await client.SendAsync(request);
    }

    private static async Task<int> CountSellerOrdersAsync(HttpClient client, Guid orgId)
    {
        using var request = Scoped(
            HttpMethod.Get,
            $"/api/v1/pos/organizations/{orgId:D}/customer-orders",
            orgId,
            SellerActor);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var page = (await response.Content.ReadFromJsonAsync<CustomerOrderPagedResult>(JsonOptions))!;
        return page.TotalCount;
    }

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
        using (var get = Scoped(
            HttpMethod.Get,
            $"/api/v1/pos/organizations/{orgId:D}/customer-orders/{orderId:D}",
            orgId,
            SellerActor))
        using (var current = await client.SendAsync(get))
        {
            current.EnsureSuccessStatusCode();
            var order = (await current.Content.ReadFromJsonAsync<CustomerOrderDto>(JsonOptions))!;
            if (order.PaymentMethod is "Cash" or "ManualGCash" && order.PaymentStatus != "Paid")
            {
                using var confirm = Scoped(
                    HttpMethod.Post,
                    $"/api/v1/pos/organizations/{orgId:D}/customer-orders/{orderId:D}/confirm-payment",
                    orgId,
                    SellerActor);
                confirm.Content = JsonContent.Create(new { amountReceived = order.Total }, options: JsonOptions);
                using var confirmed = await client.SendAsync(confirm);
                confirmed.EnsureSuccessStatusCode();
            }
        }

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
            // Host env may set LocalValidation__Enabled=true. This factory uses Testcontainers, not port 15534.
            builder.UseSetting("LocalValidation:Enabled", "false");
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
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PosDatabase"] = connectionString,
                    ["LocalValidation:Enabled"] = "false"
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
}
