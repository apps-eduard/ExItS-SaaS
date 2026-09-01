using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Api.Customers;
using ExItS.PinoyBusinessPOS.Api.Offline;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using static ExItS.PinoyBusinessPOS.IntegrationTests.PosInventoryOpsIntegrationSupport;
using H1ProofBranchDirectoryOptions = ExItS.PinoyBusinessPOS.IntegrationTests.Support.H1ProofBranchDirectoryOptions;
using H1ProofCustomerOrderBranchDirectory = ExItS.PinoyBusinessPOS.IntegrationTests.Support.H1ProofCustomerOrderBranchDirectory;
using H1ProofOrganizationBranchDirectory = ExItS.PinoyBusinessPOS.IntegrationTests.Support.H1ProofOrganizationBranchDirectory;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

/// <summary>MB2-03 branch pricing / effective price authority proofs (PRICE-01…15 + Mica E2E).</summary>
[Collection(PosPostgreSqlCollection.Name)]
public sealed class BranchPricingIntegrationTests(PosPostgreSqlFixture fixture)
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
    private static readonly Guid PersonalUser = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid PlatformBusinessCustomerId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid LinkedCustomerAppUserId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    private const string Products = "/api/v1/pos/catalog/products";
    private const string Customers = "/api/v1/pos/customers";
    private const string Sales = "/api/v1/pos/sales";
    private const string Authorities = "/api/v1/pos/offline-price-authorities";
    private const string CustomerOrdersOrg = "/api/v1/pos/customer-orders/organizations";
    private const string Storefront = "/api/v1/pos/customer-orders/organizations";
    private const string OverrideReason = "Branch manager adjustment";

    private const string ManagerGrants =
        $"{PosFeatureCodes.StoreSalesView},{PosFeatureCodes.StoreSalesCreate}," +
        $"{PosFeatureCodes.StoreSalesOverridePrice}";

    private const string OwnerGrants =
        $"{ManagerGrants},{PosFeatureCodes.StoreSalesOverridePriceUnlimited}";

    [Fact]
    public async Task PRICE_01_main_default_remote_override_main_stays_default()
    {
        var (factory, client, org) = CreateBranchClient();
        await using var _ = factory;

        var product = await CreateProductAsync(client, org, "PRICE01", 50m);
        await SetBranchOverrideAsync(client, org, product.ProductId, Remote, 65m);

        var mainPricing = await GetBranchPricingAsync(client, org, product.ProductId, Main);
        Assert.Equal(50m, mainPricing.BasePrice.EffectivePrice);
        Assert.False(mainPricing.BasePrice.HasBranchPriceOverride);

        var remotePricing = await GetBranchPricingAsync(client, org, product.ProductId, Remote);
        Assert.Equal(65m, remotePricing.BasePrice.EffectivePrice);
        Assert.True(remotePricing.BasePrice.HasBranchPriceOverride);
    }

    [Fact]
    public async Task PRICE_02_org_default_change_preserves_remote_override()
    {
        var (factory, client, org) = CreateBranchClient();
        await using var _ = factory;

        var product = await CreateProductAsync(client, org, "PRICE02", 50m);
        await SetBranchOverrideAsync(client, org, product.ProductId, Remote, 65m);
        await UpdateOrgPriceAsync(client, org, product, 55m);

        var mainPricing = await GetBranchPricingAsync(client, org, product.ProductId, Main);
        Assert.Equal(55m, mainPricing.BasePrice.EffectivePrice);

        var remotePricing = await GetBranchPricingAsync(client, org, product.ProductId, Remote);
        Assert.Equal(65m, remotePricing.BasePrice.EffectivePrice);
    }

    [Fact]
    public async Task PRICE_03_unit_override_independent_of_base()
    {
        var (factory, client, org) = CreateBranchClient();
        await using var _ = factory;

        var product = await CreateProductAsync(
            client,
            org,
            "PRICE03",
            50m,
            units:
            [
                new PosCatalogProductUnitInput(
                    "Sell",
                    "Pack",
                    "Pk",
                    6m,
                    SellingPrice: 280m)
            ]);
        var sellUnit = product.Units!.Single(u => u.Kind == "Sell");
        await SetBranchOverrideAsync(client, org, product.ProductId, Remote, 65m);
        await SetBranchOverrideAsync(client, org, product.ProductId, Remote, 300m, sellUnit.UnitId);

        var pricing = await GetBranchPricingAsync(client, org, product.ProductId, Remote);
        Assert.Equal(65m, pricing.BasePrice.EffectivePrice);
        var unit = Assert.Single(pricing.UnitPrices);
        Assert.Equal(280m, unit.OrganizationDefaultPrice);
        Assert.Equal(300m, unit.EffectivePrice);
    }

    [Fact]
    public async Task PRICE_04_sale_override_does_not_change_branch_override()
    {
        var (factory, client, org) = CreateBranchClient();
        await using var _ = factory;

        var product = await CreateProductAsync(client, org, "PRICE04", 50m);
        await SetBranchOverrideAsync(client, org, product.ProductId, Remote, 65m);
        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, OwnerActor);

        using var checkout = ScopedWithBranch(HttpMethod.Post, Sales, org, OwnerActor, Remote, OwnerGrants);
        checkout.Content = JsonContent.Create(
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                PosSaleOptions.CashPaymentMethod,
                100m,
                PriceOverrides:
                [
                    new SalePriceOverrideIntentRequest(
                        60m,
                        OverrideReason,
                        LineNumber: 1,
                        ExpectedBaselineUnitPrice: 65m)
                ]),
            options: JsonOptions);
        using var response = await client.SendAsync(checkout);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var sale = await response.Content.ReadFromJsonAsync<PosSaleDto>(JsonOptions);
        Assert.Equal(60m, sale!.Lines.Single().UnitPrice);

        var remotePricing = await GetBranchPricingAsync(client, org, product.ProductId, Remote);
        Assert.Equal(65m, remotePricing.BasePrice.EffectivePrice);
    }

    [Fact]
    public async Task PRICE_05_offline_lease_uses_remote_effective_price()
    {
        var (factory, client, org) = CreateBranchClient();
        await using var _ = factory;

        var product = await CreateProductAsync(client, org, "PRICE05", 50m);
        await SetBranchOverrideAsync(client, org, product.ProductId, Remote, 65m);

        var issued = await IssueAuthoritiesAsync(client, org, Remote, [product.ProductId]);
        var authority = Assert.Single(issued.Authorities);
        Assert.Equal(Remote, authority.BranchId);
        Assert.Equal(65m, authority.UnitPrice);
    }

    [Fact]
    public async Task PRICE_06_remove_override_reverts_to_org_default()
    {
        var (factory, client, org) = CreateBranchClient();
        await using var _ = factory;

        var product = await CreateProductAsync(client, org, "PRICE06", 50m);
        await SetBranchOverrideAsync(client, org, product.ProductId, Remote, 65m);
        await RemoveBranchOverrideAsync(client, org, product.ProductId, Remote);

        var pricing = await GetBranchPricingAsync(client, org, product.ProductId, Remote);
        Assert.Equal(50m, pricing.BasePrice.EffectivePrice);
        Assert.False(pricing.BasePrice.HasBranchPriceOverride);
    }

    [Fact]
    public async Task PRICE_07_negative_branch_price_rejected()
    {
        var (factory, client, org) = CreateBranchClient();
        await using var _ = factory;

        var product = await CreateProductAsync(client, org, "PRICE07", 50m);
        using var request = ScopedWithBranch(HttpMethod.Put, BranchPricingPath(product.ProductId), org, OwnerActor, Main);
        request.Content = JsonContent.Create(
            new SetBranchProductPriceOverrideRequest(Remote, -1m),
            options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(ApplicationErrorCodes.ProductBranchPriceInvalid, await ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task PRICE_08_inactive_branch_override_rejected()
    {
        var branchOptions = CreateBranchOptions();
        var org = Guid.NewGuid();
        branchOptions.RegisterOrganization(org, Main, Remote, MicaA, MicaB);
        branchOptions.SetInactive(org, Remote);
        await using var factory = CreateBranchFactory(branchOptions);
        var client = factory.CreateClient();

        var product = await CreateProductAsync(client, org, "PRICE08", 50m);
        using var request = ScopedWithBranch(HttpMethod.Put, BranchPricingPath(product.ProductId), org, OwnerActor, Main);
        request.Content = JsonContent.Create(
            new SetBranchProductPriceOverrideRequest(Remote, 65m),
            options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(ApplicationErrorCodes.ProductBranchInvalid, await ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task PRICE_09_get_branch_pricing_returns_default_override_effective()
    {
        var (factory, client, org) = CreateBranchClient();
        await using var _ = factory;

        var product = await CreateProductAsync(client, org, "PRICE09", 50m);
        await SetBranchOverrideAsync(client, org, product.ProductId, Remote, 65m);

        var pricing = await GetBranchPricingAsync(client, org, product.ProductId, Remote);
        Assert.Equal(50m, pricing.BasePrice.OrganizationDefaultPrice);
        Assert.Equal(65m, pricing.BasePrice.BranchOverridePrice);
        Assert.Equal(65m, pricing.BasePrice.EffectivePrice);
        Assert.True(pricing.BasePrice.HasBranchPriceOverride);
    }

    [Fact]
    public async Task PRICE_10_checkout_at_remote_uses_branch_effective_price()
    {
        var (factory, client, org) = CreateBranchClient();
        await using var _ = factory;

        var product = await CreateProductAsync(client, org, "PRICE10", 50m);
        await SetBranchOverrideAsync(client, org, product.ProductId, Remote, 65m);
        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, OwnerActor);

        var sale = await CheckoutAsync(client, org, Remote, product.ProductId, 2m);
        var line = Assert.Single(sale.Lines);
        Assert.Equal(65m, line.UnitPrice);
        Assert.Equal(130m, sale.Total);
    }

    [Fact]
    public async Task PRICE_11_customer_order_uses_fulfillment_branch_price()
    {
        var (factory, client, org) = CreateCustomerOrderClient();
        await using var _ = factory;

        await CreateLinkedCustomerAsync(client, org, PlatformBusinessCustomerId, "Pricing Buyer");
        await BootstrapOwnerAsync(client, org, OwnerActor);
        var product = await CreateProductAsync(client, org, "PRICE11", 50m);
        await SetBranchOverrideAsync(client, org, product.ProductId, MicaA, 72m);

        var order = await PlacePersonalOrderAsync(client, org, product.ProductId, 2m, MicaA);
        var line = Assert.Single(order.Lines);
        Assert.Equal(72m, line.UnitPrice);
        Assert.Equal(144m, order.MerchandiseSubtotal);
    }

    [Fact]
    public async Task PRICE_12_storefront_shows_fulfillment_branch_effective_price()
    {
        var (factory, client, org) = CreateCustomerOrderClient();
        await using var _ = factory;

        var product = await CreateProductAsync(client, org, "PRICE12", 50m);
        await SetBranchOverrideAsync(client, org, product.ProductId, MicaB, 88m);

        using var request = PosIntegrationRequest.Scoped(
            HttpMethod.Get,
            $"{Storefront}/{org:D}/storefront?fulfillmentBranchId={MicaB:D}",
            org,
            PersonalUser);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var storefront = await response.Content.ReadFromJsonAsync<CustomerStorefrontDto>(JsonOptions);
        var row = Assert.Single(storefront!.Products, p => p.ProductId == product.ProductId);
        Assert.Equal(88m, row.UnitPrice);
    }

    [Fact]
    public async Task PRICE_13_catalog_list_enriches_effective_price_with_branch_header()
    {
        var (factory, client, org) = CreateBranchClient();
        await using var _ = factory;

        var product = await CreateProductAsync(client, org, "PRICE13", 50m);
        await SetBranchOverrideAsync(client, org, product.ProductId, Remote, 65m);

        using var request = PosIntegrationRequest.Scoped(HttpMethod.Get, Products, org, OwnerActor);
        request.Headers.TryAddWithoutValidation(PosOrganizationHeaders.BranchHeaderName, Remote.ToString("D"));
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<PagedResult<PosCatalogProductDto>>(JsonOptions);
        var row = Assert.Single(page!.Items, p => p.ProductId == product.ProductId);
        Assert.Equal(65m, row.EffectiveSellingPrice);
        Assert.True(row.HasBranchPriceOverride);
    }

    [Fact]
    public async Task PRICE_14_main_checkout_does_not_use_remote_override()
    {
        var (factory, client, org) = CreateBranchClient();
        await using var _ = factory;

        var product = await CreateProductAsync(client, org, "PRICE14", 50m);
        await SetBranchOverrideAsync(client, org, product.ProductId, Remote, 65m);
        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, OwnerActor);

        var sale = await CheckoutAsync(client, org, Main, product.ProductId, 1m);
        Assert.Equal(50m, sale.Lines.Single().UnitPrice);
    }

    [Fact]
    public async Task PRICE_15_offline_lease_branch_a_never_uses_branch_b_price()
    {
        var (factory, client, org) = CreateBranchClient();
        await using var _ = factory;

        var product = await CreateProductAsync(client, org, "PRICE15", 50m);
        await SetBranchOverrideAsync(client, org, product.ProductId, MicaA, 70m);
        await SetBranchOverrideAsync(client, org, product.ProductId, MicaB, 90m);

        var micaA = await IssueAuthoritiesAsync(client, org, MicaA, [product.ProductId]);
        var micaB = await IssueAuthoritiesAsync(client, org, MicaB, [product.ProductId]);
        Assert.Equal(70m, micaA.Authorities.Single().UnitPrice);
        Assert.Equal(90m, micaB.Authorities.Single().UnitPrice);
    }

    [Fact]
    public async Task MICA_price_e2e_mica_a_checkout_uses_mica_a_override_not_mica_b()
    {
        var (factory, client, org) = CreateBranchClient();
        await using var _ = factory;

        var product = await CreateProductAsync(client, org, "Mica Coke", 50m);
        await SetBranchOverrideAsync(client, org, product.ProductId, MicaA, 77m);
        await SetBranchOverrideAsync(client, org, product.ProductId, MicaB, 99m);
        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, OwnerActor);

        var sale = await CheckoutAsync(client, org, MicaA, product.ProductId, 3m);
        Assert.Equal(77m, sale.Lines.Single().UnitPrice);
        Assert.Equal(231m, sale.Total);
    }

    [Fact]
    public async Task Migration_AddBranchProductPriceOverrides_applies_rolls_back_and_reapplies()
    {
        const string target = "AddBranchProductPriceOverrides";
        const string previous = "ReconcileBranchInventoryReservations";
        const string table = "branch_product_price_overrides";

        var options = new DbContextOptionsBuilder<PosDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync();
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.Contains(applied, m => m.Contains(target, StringComparison.Ordinal));
        }

        Assert.Contains("selling_price", await ColumnsAsync(table));

        await using (var context = new PosDbContext(options))
        {
            var prev = (await context.Database.GetAppliedMigrationsAsync())
                .Single(m => m.Contains(previous, StringComparison.Ordinal));
            await context.Database.MigrateAsync(prev);
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.DoesNotContain(applied, m => m.Contains(target, StringComparison.Ordinal));
        }

        Assert.Empty(await ColumnsAsync(table));

        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync();
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.Contains(applied, m => m.Contains(target, StringComparison.Ordinal));
        }

        Assert.Contains("product_unit_id", await ColumnsAsync(table));
    }

    private async Task<List<string>> ColumnsAsync(string table)
    {
        await using var connection = new Npgsql.NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT column_name
            FROM information_schema.columns
            WHERE table_schema = 'pos' AND table_name = @table
            ORDER BY ordinal_position;
            """;
        command.Parameters.AddWithValue("table", table);
        var columns = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns.Add(reader.GetString(0));
        }

        return columns;
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
        decimal sellingPrice,
        IReadOnlyList<PosCatalogProductUnitInput>? units = null)
    {
        using var request = PosIntegrationRequest.Scoped(HttpMethod.Post, Products, org, OwnerActor);
        request.Content = JsonContent.Create(
            new CreatePosCatalogProductRequest(
                name,
                "Piece",
                sellingPrice,
                Sku: $"sku-{Guid.NewGuid():N}"[..20],
                Units: units),
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
        decimal price,
        Guid? unitId = null)
    {
        using var request = PosIntegrationRequest.Scoped(HttpMethod.Put, BranchPricingPath(productId), org, OwnerActor);
        request.Content = JsonContent.Create(
            new SetBranchProductPriceOverrideRequest(branchId, price, unitId),
            options: JsonOptions);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private static async Task RemoveBranchOverrideAsync(
        HttpClient client,
        Guid org,
        Guid productId,
        Guid branchId,
        Guid? unitId = null)
    {
        var path = $"{BranchPricingPath(productId)}?branchId={branchId:D}";
        if (unitId is Guid uid && uid != Guid.Empty)
        {
            path += $"&unitId={uid:D}";
        }

        using var request = PosIntegrationRequest.Scoped(HttpMethod.Delete, path, org, OwnerActor);
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
        using var request = ScopedWithBranch(HttpMethod.Post, Sales, org, OwnerActor, branchId);
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

    private static async Task<IssueOfflinePriceAuthoritiesResponse> IssueAuthoritiesAsync(
        HttpClient client,
        Guid org,
        Guid branchId,
        List<Guid> productIds)
    {
        using var request = ScopedWithBranch(HttpMethod.Post, Authorities, org, OwnerActor, branchId);
        request.Content = JsonContent.Create(new IssueOfflinePriceAuthoritiesRequest(productIds), options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<IssueOfflinePriceAuthoritiesResponse>(JsonOptions))!;
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
                "Pricing Buyer",
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

    private static async Task CreateLinkedCustomerAsync(
        HttpClient client,
        Guid orgId,
        Guid platformBusinessCustomerId,
        string displayName)
    {
        using var request = PosIntegrationRequest.Scoped(HttpMethod.Post, Customers, orgId, OwnerActor);
        request.Content = JsonContent.Create(
            new CreateCustomerRequest(displayName, null, null, null, PlatformBusinessCustomerId: platformBusinessCustomerId),
            options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
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
