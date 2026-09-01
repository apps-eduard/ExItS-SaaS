using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Api.Customers;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Sales;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using static ExItS.PinoyBusinessPOS.IntegrationTests.PosInventoryOpsIntegrationSupport;
using static ExItS.PinoyBusinessPOS.IntegrationTests.Support.MicaStoreInventoryClosureSupport;
using H1ProofBranchDirectoryOptions = ExItS.PinoyBusinessPOS.IntegrationTests.Support.H1ProofBranchDirectoryOptions;
using H1ProofCustomerOrderBranchDirectory = ExItS.PinoyBusinessPOS.IntegrationTests.Support.H1ProofCustomerOrderBranchDirectory;
using H1ProofOrganizationBranchDirectory = ExItS.PinoyBusinessPOS.IntegrationTests.Support.H1ProofOrganizationBranchDirectory;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

/// <summary>MB2-03-H1 branch pricing proof closure (historical, foreign org, auth, discount, full Mica E2E).</summary>
[Collection(PosPostgreSqlCollection.Name)]
public sealed class BranchPricing03H1IntegrationTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Guid Main = BranchA;
    private static readonly Guid Remote = BranchB;
    private static readonly Guid MicaA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid MicaB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid OrgBMain = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid OrgBBranch = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid CashierActor = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid PersonalUser = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid PlatformBusinessCustomerId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid LinkedCustomerAppUserId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    private const string Products = "/api/v1/pos/catalog/products";
    private const string Sales = "/api/v1/pos/sales";
    private const string CustomerOrdersOrg = "/api/v1/pos/customer-orders/organizations";
    private const string Storefront = "/api/v1/pos/customer-orders/organizations";
    private const string DiscountReason = "Branch courtesy";

    private const string ManagerDiscountGrants =
        $"{PosFeatureCodes.StoreSalesView},{PosFeatureCodes.StoreSalesCreate}," +
        $"{PosFeatureCodes.StoreSalesApplyCommercialDiscount}";

    private const string OwnerGrants =
        $"{ManagerDiscountGrants},{PosFeatureCodes.StoreSalesOverridePriceUnlimited}";

    [Fact]
    public async Task PRICE_H1_01_historical_sale_line_unchanged_after_branch_override_change()
    {
        var (factory, client, org) = CreateBranchClient();
        await using var _ = factory;

        var product = await CreateProductAsync(client, org, "H1-Hist", 20m);
        await SetBranchOverrideAsync(client, org, product.ProductId, MicaA, 22m);
        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, OwnerActor);

        var sale = await CheckoutAsync(client, org, MicaA, product.ProductId, 1m);
        Assert.Equal(22m, sale.Lines.Single().UnitPrice);
        var saleId = sale.SaleId;

        await using (var db = CreateDbContext())
        {
            var line = await db.SaleLines.AsNoTracking().SingleAsync(l => l.SaleId == saleId);
            Assert.Equal(22m, line.UnitPrice);
        }

        await SetBranchOverrideAsync(client, org, product.ProductId, MicaA, 30m);

        await using (var db = CreateDbContext())
        {
            var line = await db.SaleLines.AsNoTracking().SingleAsync(l => l.SaleId == saleId);
            Assert.Equal(22m, line.UnitPrice);
        }

        using var getSale = ScopedWithBranch(HttpMethod.Get, $"{Sales}/{saleId:D}", org, OwnerActor, MicaA);
        using var getResponse = await client.SendAsync(getSale);
        getResponse.EnsureSuccessStatusCode();
        var reread = await getResponse.Content.ReadFromJsonAsync<PosSaleDto>(JsonOptions);
        Assert.Equal(22m, reread!.Lines.Single().UnitPrice);

        var newSale = await CheckoutAsync(client, org, MicaA, product.ProductId, 1m);
        Assert.Equal(30m, newSale.Lines.Single().UnitPrice);
    }

    [Fact]
    public async Task PRICE_H1_02_foreign_org_branch_product_combinations_rejected()
    {
        var branchOptions = CreateBranchOptions();
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();
        branchOptions.RegisterOrganization(orgA, Main, Remote, MicaA, MicaB);
        branchOptions.RegisterOrganization(orgB, OrgBMain, OrgBBranch);
        await using var factory = CreateBranchFactory(branchOptions);
        var client = factory.CreateClient();

        var productA = await CreateProductAsync(client, orgA, "ProdA", 50m);
        var productB = await CreateProductAsync(client, orgB, "ProdB", 60m);

        await AssertOverrideRejectedAsync(client, orgA, productA.ProductId, OrgBBranch, 65m, ApplicationErrorCodes.ProductBranchInvalid);
        await AssertOverrideRejectedAsync(client, orgA, productB.ProductId, Main, 65m, ApplicationErrorCodes.ProductNotFound);
        await AssertOverrideRejectedAsync(client, orgA, productB.ProductId, OrgBBranch, 65m, ApplicationErrorCodes.ProductBranchInvalid);

        await using var db = CreateDbContext();
        Assert.False(await db.BranchProductPriceOverrides.AnyAsync(
            o => o.OrganizationId == orgA && o.ProductId == productA.ProductId));
        Assert.False(await db.BranchProductPriceOverrides.AnyAsync(
            o => o.OrganizationId == orgA && o.ProductId == productB.ProductId));
        Assert.False(await db.BranchProductPriceOverrides.AnyAsync(
            o => o.OrganizationId == orgB && o.ProductId == productB.ProductId));
    }

    [Fact]
    public async Task PRICE_H1_03_unauthorized_staff_cannot_mutate_branch_pricing()
    {
        var (factory, client, org) = CreateBranchClient();
        await using var _ = factory;

        var product = await CreateProductAsync(client, org, "H1-Auth", 50m);
        await BootstrapOwnerAsync(client, org, OwnerActor);
        await AssignRoleAsync(client, org, OwnerActor, CashierActor, "Cashier");

        await AssertPricingMutationDeniedAsync(client, org, product.ProductId, MicaA, 65m, CashierActor);

        await SetBranchOverrideAsync(client, org, product.ProductId, MicaA, 65m);

        await AssertPricingMutationDeniedAsync(client, org, product.ProductId, MicaA, 70m, CashierActor);

        using var delete = PosIntegrationRequest.Scoped(
            HttpMethod.Delete,
            $"{BranchPricingPath(product.ProductId)}?branchId={MicaA:D}",
            org,
            CashierActor);
        using var deleteResponse = await client.SendAsync(delete);
        Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);

        var pricing = await GetBranchPricingAsync(client, org, product.ProductId, MicaA);
        Assert.Equal(65m, pricing.BasePrice.EffectivePrice);
    }

    [Fact]
    public async Task PRICE_H1_04_discount_uses_branch_effective_price_as_base()
    {
        var (factory, client, org) = CreateBranchClient();
        await using var _ = factory;

        var product = await CreateProductAsync(client, org, "H1-Disc", 100m);
        await SetBranchOverrideAsync(client, org, product.ProductId, MicaA, 80m);
        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, OwnerActor);

        using var checkout = ScopedWithBranch(HttpMethod.Post, Sales, org, OwnerActor, MicaA, ManagerDiscountGrants);
        checkout.Content = JsonContent.Create(
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                PosSaleOptions.CashPaymentMethod,
                100m,
                Discounts: [new CommercialDiscountIntentRequest("Sale", "Percentage", 10m, DiscountReason)]),
            options: JsonOptions);
        using var response = await client.SendAsync(checkout);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var sale = await response.Content.ReadFromJsonAsync<PosSaleDto>(JsonOptions);

        Assert.Equal(80m, sale!.GrossSubtotal);
        Assert.Equal(8m, sale.SaleDiscountTotal);
        Assert.Equal(8m, sale.DiscountTotal);
        Assert.Equal(72m, sale.Subtotal);
        Assert.Equal(72m, sale.Total);

        var line = Assert.Single(sale.Lines);
        Assert.Equal(80m, line.UnitPrice);
        Assert.Equal(80m, line.GrossLineTotal);
        Assert.Equal(8m, line.SaleDiscountAllocatedAmount);
        Assert.Equal(72m, line.LineTotal);
    }

    [Fact]
    public async Task PRICE_H1_05_full_mica_price_lifecycle_with_inventory_audits_clean()
    {
        var (factory, client, org) = CreateCustomerOrderClient();
        await using var _ = factory;

        await CreateLinkedCustomerAsync(client, org, PlatformBusinessCustomerId, "Mica Buyer");
        await BootstrapOwnerAsync(client, org, OwnerActor);

        var product = await CreateProductAsync(client, org, "Mica Water", 20m);
        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, OwnerActor);

        await SetBranchOverrideAsync(client, org, product.ProductId, MicaA, 22m);
        await SetBranchOverrideAsync(client, org, product.ProductId, MicaB, 19m);

        Assert.Equal(20m, (await GetBranchPricingAsync(client, org, product.ProductId, Main)).BasePrice.EffectivePrice);
        Assert.Equal(22m, (await GetBranchPricingAsync(client, org, product.ProductId, MicaA)).BasePrice.EffectivePrice);
        Assert.Equal(19m, (await GetBranchPricingAsync(client, org, product.ProductId, MicaB)).BasePrice.EffectivePrice);

        var saleMain = await CheckoutAsync(client, org, Main, product.ProductId, 1m);
        var saleA = await CheckoutAsync(client, org, MicaA, product.ProductId, 1m);
        var saleB = await CheckoutAsync(client, org, MicaB, product.ProductId, 1m);
        Assert.Equal(20m, saleMain.Lines.Single().UnitPrice);
        Assert.Equal(22m, saleA.Lines.Single().UnitPrice);
        Assert.Equal(19m, saleB.Lines.Single().UnitPrice);

        await UpdateOrgPriceAsync(client, org, product, 21m);
        await SetBranchOverrideAsync(client, org, product.ProductId, MicaA, 24m);
        await RemoveBranchOverrideAsync(client, org, product.ProductId, MicaB);

        Assert.Equal(21m, (await GetBranchPricingAsync(client, org, product.ProductId, Main)).BasePrice.EffectivePrice);
        Assert.Equal(24m, (await GetBranchPricingAsync(client, org, product.ProductId, MicaA)).BasePrice.EffectivePrice);
        Assert.Equal(21m, (await GetBranchPricingAsync(client, org, product.ProductId, MicaB)).BasePrice.EffectivePrice);

        await using (var db = CreateDbContext())
        {
            Assert.Equal(20m, await LoadLineUnitPriceAsync(db, saleMain.SaleId));
            Assert.Equal(22m, await LoadLineUnitPriceAsync(db, saleA.SaleId));
            Assert.Equal(19m, await LoadLineUnitPriceAsync(db, saleB.SaleId));
        }

        var newMain = await CheckoutAsync(client, org, Main, product.ProductId, 1m);
        var newA = await CheckoutAsync(client, org, MicaA, product.ProductId, 1m);
        var newB = await CheckoutAsync(client, org, MicaB, product.ProductId, 1m);
        Assert.Equal(21m, newMain.Lines.Single().UnitPrice);
        Assert.Equal(24m, newA.Lines.Single().UnitPrice);
        Assert.Equal(21m, newB.Lines.Single().UnitPrice);

        using var storefront = PosIntegrationRequest.Scoped(
            HttpMethod.Get,
            $"{Storefront}/{org:D}/storefront?fulfillmentBranchId={MicaB:D}",
            org,
            PersonalUser);
        using var storefrontResponse = await client.SendAsync(storefront);
        storefrontResponse.EnsureSuccessStatusCode();
        var sf = await storefrontResponse.Content.ReadFromJsonAsync<CustomerStorefrontDto>(JsonOptions);
        Assert.Equal(21m, Assert.Single(sf!.Products, p => p.ProductId == product.ProductId).UnitPrice);

        var order = await PlacePersonalOrderAsync(client, org, product.ProductId, 1m, MicaA);
        Assert.Equal(24m, order.Lines.Single().UnitPrice);

        await ReservationAuditCleanAsync(fixture.ConnectionString, org);
        await PhysicalAuditCleanAsync(client, org);
    }

    private static async Task AssertPricingMutationDeniedAsync(
        HttpClient client,
        Guid org,
        Guid productId,
        Guid branchId,
        decimal price,
        Guid actorId)
    {
        using var request = PosIntegrationRequest.Scoped(
            HttpMethod.Put,
            BranchPricingPath(productId),
            org,
            actorId);
        request.Content = JsonContent.Create(
            new SetBranchProductPriceOverrideRequest(branchId, price),
            options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var code = await ReadErrorCodeAsync(response);
        Assert.True(
            code is ApplicationErrorCodes.ProductBranchPriceForbidden or DomainErrorCodes.PosRoleDenied,
            $"Unexpected error code: {code}");
    }

    private PosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PosDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;
        return new PosDbContext(options);
    }

    private static async Task<decimal> LoadLineUnitPriceAsync(PosDbContext db, Guid saleId) =>
        (await db.SaleLines.AsNoTracking().SingleAsync(l => l.SaleId == saleId)).UnitPrice;

    private static async Task AssertOverrideRejectedAsync(
        HttpClient client,
        Guid org,
        Guid productId,
        Guid branchId,
        decimal price,
        string expectedCode,
        Guid? actorId = null)
    {
        using var request = PosIntegrationRequest.Scoped(
            HttpMethod.Put,
            BranchPricingPath(productId),
            org,
            actorId ?? OwnerActor);
        request.Content = JsonContent.Create(
            new SetBranchProductPriceOverrideRequest(branchId, price),
            options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(expectedCode, await ReadErrorCodeAsync(response));
    }

    private (WebApplicationFactory<Program> factory, HttpClient client, Guid org) CreateBranchClient()
    {
        var branchOptions = CreateBranchOptions();
        var org = Guid.NewGuid();
        branchOptions.RegisterOrganization(org, Main, Remote, MicaA, MicaB);
        var factory = CreateBranchFactory(branchOptions);
        return (factory, factory.CreateClient(), org);
    }

    private (WebApplicationFactory<Program> factory, HttpClient client, Guid org) CreateCustomerOrderClient()
    {
        var branchOptions = CreateBranchOptions();
        var org = Guid.NewGuid();
        branchOptions.RegisterOrganization(org, Main, MicaA, MicaB);
        var factory = new CustomerOrderPricingApiFactory(
            fixture.ConnectionString,
            PersonalUser,
            PlatformBusinessCustomerId,
            LinkedCustomerAppUserId,
            branchOptions);
        return (factory, factory.CreateClient(), org);
    }

    private H1ProofBranchDirectoryOptions CreateBranchOptions()
    {
        var options = new H1ProofBranchDirectoryOptions { PrimaryBranchId = Main };
        return options;
    }

    private WebApplicationFactory<Program> CreateBranchFactory(H1ProofBranchDirectoryOptions? options = null)
    {
        var branchOptions = options ?? CreateBranchOptions();
        return new BranchPricingApiFactory(fixture.ConnectionString, branchOptions);
    }

    private static string BranchPricingPath(Guid productId) =>
        $"{Products}/{productId:D}/branch-pricing";

    private static async Task<PosCatalogProductDto> CreateProductAsync(
        HttpClient client,
        Guid org,
        string name,
        decimal sellingPrice)
    {
        using var request = PosIntegrationRequest.Scoped(HttpMethod.Post, Products, org, OwnerActor);
        request.Content = JsonContent.Create(
            new CreatePosCatalogProductRequest(
                name,
                "Piece",
                sellingPrice,
                Sku: $"sku-{Guid.NewGuid():N}"[..20]),
            options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PosCatalogProductDto>(JsonOptions))!;
    }

    private static async Task SetBranchOverrideAsync(
        HttpClient client,
        Guid org,
        Guid productId,
        Guid branchId,
        decimal price)
    {
        using var request = PosIntegrationRequest.Scoped(HttpMethod.Put, BranchPricingPath(productId), org, OwnerActor);
        request.Content = JsonContent.Create(
            new SetBranchProductPriceOverrideRequest(branchId, price),
            options: JsonOptions);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private static async Task RemoveBranchOverrideAsync(
        HttpClient client,
        Guid org,
        Guid productId,
        Guid branchId)
    {
        using var request = PosIntegrationRequest.Scoped(
            HttpMethod.Delete,
            $"{BranchPricingPath(productId)}?branchId={branchId:D}",
            org,
            OwnerActor);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private static async Task<BranchProductPricingDto> GetBranchPricingAsync(
        HttpClient client,
        Guid org,
        Guid productId,
        Guid branchId)
    {
        using var request = PosIntegrationRequest.Scoped(
            HttpMethod.Get,
            $"{BranchPricingPath(productId)}?branchId={branchId:D}",
            org,
            OwnerActor);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BranchProductPricingDto>(JsonOptions))!;
    }

    private static async Task UpdateOrgPriceAsync(HttpClient client, Guid org, PosCatalogProductDto product, decimal price)
    {
        using var request = PosIntegrationRequest.Scoped(HttpMethod.Put, $"{Products}/{product.ProductId:D}", org, OwnerActor);
        request.Content = JsonContent.Create(
            new UpdatePosCatalogProductRequest(product.Name, product.UnitOfMeasure, price, Sku: product.Sku),
            options: JsonOptions);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<PosSaleDto> CheckoutAsync(
        HttpClient client,
        Guid org,
        Guid branchId,
        Guid productId,
        decimal qty)
    {
        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, OwnerActor);
        using var request = ScopedWithBranch(HttpMethod.Post, Sales, org, OwnerActor, branchId, OwnerGrants);
        request.Content = JsonContent.Create(
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(productId, qty)],
                PosSaleOptions.CashPaymentMethod,
                5000m),
            options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PosSaleDto>(JsonOptions))!;
    }

    private static async Task<CustomerOrderDto> PlacePersonalOrderAsync(
        HttpClient client,
        Guid org,
        Guid productId,
        decimal quantity,
        Guid fulfillmentBranchId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{CustomerOrdersOrg}/{org:D}");
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

    private static async Task CreateLinkedCustomerAsync(
        HttpClient client,
        Guid orgId,
        Guid platformBusinessCustomerId,
        string displayName)
    {
        using var request = PosIntegrationRequest.Scoped(HttpMethod.Post, "/api/v1/pos/customers", orgId, OwnerActor);
        request.Content = JsonContent.Create(
            new CreateCustomerRequest(displayName, null, null, null, PlatformBusinessCustomerId: platformBusinessCustomerId),
            options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static HttpRequestMessage ScopedWithBranch(
        HttpMethod method,
        string path,
        Guid org,
        Guid actor,
        Guid branchId,
        string? grants = null)
    {
        var request = PosIntegrationRequest.Scoped(method, path, org, actor);
        request.Headers.TryAddWithoutValidation(PosOrganizationHeaders.BranchHeaderName, branchId.ToString("D"));
        if (!string.IsNullOrWhiteSpace(grants))
        {
            request.Headers.TryAddWithoutValidation(
                PosCommercialHeaders.SubscriptionStatusHeaderName,
                PosSubscriptionStatuses.Active);
            request.Headers.TryAddWithoutValidation(PosCommercialHeaders.FeatureGrantsHeaderName, grants);
        }

        return request;
    }

    private static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return problem.TryGetProperty("errorCode", out var code) ? code.GetString() : null;
    }

    private sealed class BranchPricingApiFactory(string connectionString, H1ProofBranchDirectoryOptions branchOptions)
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

    private sealed class CustomerOrderPricingApiFactory(
        string connectionString,
        Guid personalUserId,
        Guid platformBusinessCustomerId,
        Guid linkedCustomerAppUserId,
        H1ProofBranchDirectoryOptions branchOptions) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:PosDatabase", connectionString);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ILinkedCustomerPlatformAuthorization>();
                services.AddSingleton<ILinkedCustomerPlatformAuthorization>(
                    new LinkedCustomerAuth(personalUserId, platformBusinessCustomerId, linkedCustomerAppUserId));
                services.RemoveAll<ICustomerOrderBranchDirectory>();
                services.AddSingleton<ICustomerOrderBranchDirectory, H1ProofCustomerOrderBranchDirectory>();
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

    private sealed class LinkedCustomerAuth(
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
