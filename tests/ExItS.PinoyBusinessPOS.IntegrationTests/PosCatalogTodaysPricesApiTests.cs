using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosCatalogTodaysPricesApiTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private const string Products = "/api/v1/pos/catalog/products";
    private const string Prices = "/api/v1/pos/catalog/products/prices";

    [Fact]
    public async Task Bulk_price_update_supports_partial_success_and_preserves_unchanged_rows()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var otherOrg = Guid.NewGuid();

        var tomato = await CreateProductAsync(client, org, new CreatePosCatalogProductRequest(
            "Tomato",
            "Kilogram",
            120m,
            SellingMode: "ByWeight"));
        var bangus = await CreateProductAsync(client, org, new CreatePosCatalogProductRequest(
            "Bangus",
            "Kilogram",
            220m,
            SellingMode: "ByWeight"));
        var coke = await CreateProductAsync(client, org, new CreatePosCatalogProductRequest(
            "Coke",
            "Bottle",
            25m,
            SellingMode: "PerItem"));
        var foreign = await CreateProductAsync(client, otherOrg, new CreatePosCatalogProductRequest(
            "Hijack",
            "Piece",
            9m));

        tomato = await GetProductAsync(client, org, tomato.ProductId);
        bangus = await GetProductAsync(client, org, bangus.ProductId);
        coke = await GetProductAsync(client, org, coke.ProductId);

        using var request = Scoped(HttpMethod.Post, Prices, org);
        request.Content = JsonContent.Create(new UpdatePosCatalogProductPricesRequest(
        [
            new UpdatePosCatalogProductPriceItem(tomato.ProductId, 135m, tomato.UpdatedAtUtc),
            new UpdatePosCatalogProductPriceItem(bangus.ProductId, 220m, bangus.UpdatedAtUtc), // unchanged
            new UpdatePosCatalogProductPriceItem(coke.ProductId, 27m, coke.UpdatedAtUtc),
            new UpdatePosCatalogProductPriceItem(foreign.ProductId, 1m), // cross-org
            new UpdatePosCatalogProductPriceItem(tomato.ProductId, 140m) // duplicate
        ]));

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<UpdatePosCatalogProductPricesResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(5, body!.Results.Count);
        Assert.Equal(3, body.SucceededCount);
        Assert.Equal(2, body.FailedCount);
        Assert.Equal(2, body.ChangedCount);

        var tomatoResult = body.Results[0];
        Assert.True(tomatoResult.Succeeded);
        Assert.True(tomatoResult.Changed);
        Assert.Equal(135m, tomatoResult.Product!.SellingPrice);
        Assert.Equal("ByWeight", tomatoResult.Product.SellingMode);

        var bangusResult = body.Results[1];
        Assert.True(bangusResult.Succeeded);
        Assert.False(bangusResult.Changed);
        Assert.Equal(220m, bangusResult.Product!.SellingPrice);
        Assert.Equal(bangus.UpdatedAtUtc, bangusResult.Product.UpdatedAtUtc);

        Assert.True(body.Results[2].Succeeded);
        Assert.Equal(27m, body.Results[2].Product!.SellingPrice);
        Assert.Equal("PerItem", body.Results[2].Product.SellingMode);

        Assert.False(body.Results[3].Succeeded);
        Assert.Equal(ApplicationErrorCodes.ProductNotFound, body.Results[3].ErrorCode);

        Assert.False(body.Results[4].Succeeded);
        Assert.Equal(ApplicationErrorCodes.CatalogPriceBulkDuplicate, body.Results[4].ErrorCode);

        using var reread = Scoped(HttpMethod.Get, $"{Products}/{tomato.ProductId:D}", org);
        using var rereadResponse = await client.SendAsync(reread);
        rereadResponse.EnsureSuccessStatusCode();
        var persisted = await rereadResponse.Content.ReadFromJsonAsync<PosCatalogProductDto>(JsonOptions);
        Assert.Equal(135m, persisted!.SellingPrice);
        Assert.Equal("Tomato", persisted.Name);
    }

    [Fact]
    public async Task Bulk_price_update_rejects_negative_and_requires_manage_catalog()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var eggs = await CreateProductAsync(client, org, new CreatePosCatalogProductRequest(
            "Eggs",
            "Piece",
            9m));
        eggs = await GetProductAsync(client, org, eggs.ProductId);

        using var negative = Scoped(HttpMethod.Post, Prices, org);
        negative.Content = JsonContent.Create(new UpdatePosCatalogProductPricesRequest(
        [
            new UpdatePosCatalogProductPriceItem(eggs.ProductId, -1m, eggs.UpdatedAtUtc)
        ]));
        using var negativeResponse = await client.SendAsync(negative);
        negativeResponse.EnsureSuccessStatusCode();
        var negativeBody = await negativeResponse.Content.ReadFromJsonAsync<UpdatePosCatalogProductPricesResponse>(JsonOptions);
        Assert.False(negativeBody!.Results[0].Succeeded);
        Assert.Equal(DomainErrorCodes.InvalidProductSellingPrice, negativeBody.Results[0].ErrorCode);

        using var viewOnly = Scoped(
            HttpMethod.Post,
            Prices,
            org,
            status: PosSubscriptionStatuses.Active,
            grants: PosFeatureCodes.StoreCatalogView);
        viewOnly.Content = JsonContent.Create(new UpdatePosCatalogProductPricesRequest(
        [
            new UpdatePosCatalogProductPriceItem(eggs.ProductId, 10m, eggs.UpdatedAtUtc)
        ]));
        using var viewOnlyResponse = await client.SendAsync(viewOnly);
        Assert.Equal(HttpStatusCode.Forbidden, viewOnlyResponse.StatusCode);

        using var empty = Scoped(HttpMethod.Post, Prices, org);
        empty.Content = JsonContent.Create(new UpdatePosCatalogProductPricesRequest([]));
        using var emptyResponse = await client.SendAsync(empty);
        Assert.Equal(HttpStatusCode.BadRequest, emptyResponse.StatusCode);
        Assert.Equal(ApplicationErrorCodes.CatalogPriceBulkEmpty, await ReadErrorCodeAsync(emptyResponse));
    }

    private static async Task<PosCatalogProductDto> GetProductAsync(HttpClient client, Guid org, Guid productId)
    {
        using var request = Scoped(HttpMethod.Get, $"{Products}/{productId:D}", org);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var product = await response.Content.ReadFromJsonAsync<PosCatalogProductDto>(JsonOptions);
        Assert.NotNull(product);
        return product!;
    }

    private static async Task<PosCatalogProductDto> CreateProductAsync(
        HttpClient client,
        Guid org,
        CreatePosCatalogProductRequest body)
    {
        using var request = Scoped(HttpMethod.Post, Products, org);
        request.Content = JsonContent.Create(body);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<PosCatalogProductDto>(JsonOptions);
        Assert.NotNull(product);
        return product!;
    }

    private static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return problem.TryGetProperty("errorCode", out var code) ? code.GetString() : null;
    }

    private static HttpRequestMessage Scoped(
        HttpMethod method,
        string path,
        Guid organizationId,
        string? status = null,
        string? grants = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation(
            PosOrganizationHeaders.OrganizationHeaderName,
            organizationId.ToString("D"));

        if (status is not null)
        {
            request.Headers.TryAddWithoutValidation(PosCommercialHeaders.SubscriptionStatusHeaderName, status);
        }

        if (grants is not null)
        {
            request.Headers.TryAddWithoutValidation(PosCommercialHeaders.FeatureGrantsHeaderName, grants);
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
