using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using static ExItS.PinoyBusinessPOS.IntegrationTests.PosInventoryOpsIntegrationSupport;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class CatalogBranchStockIsolationIntegrationTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Guid ThirdBranch = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public async Task BRANCHLEAK_08_10_opening_stock_at_remote_branch_isolates_catalog_and_inventory_reads()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Biscuit Pack");

        await EnableTrackedAsync(client, org, product.ProductId, openingQuantity: 10m, branchId: BranchB);

        Assert.Equal(10m, await BranchInventoryOnHandAsync(client, org, product.ProductId, BranchB));
        Assert.Equal(0m, await BranchInventoryOnHandAsync(client, org, product.ProductId, BranchA));
        Assert.Equal(0m, await BranchInventoryOnHandAsync(client, org, product.ProductId, ThirdBranch));

        var remoteCatalog = await GetCatalogProductAsync(client, org, product.ProductId, BranchB);
        Assert.Equal(10m, remoteCatalog.OrganizationOnHandQuantity);
        Assert.Equal(10m, remoteCatalog.BranchOnHandQuantity);
        Assert.Equal(10m, remoteCatalog.BranchAvailableQuantity);
        Assert.Equal(10m, remoteCatalog.OnHandQuantity);
        Assert.Equal("InStock", remoteCatalog.StockStatus);

        var mainCatalog = await GetCatalogProductAsync(client, org, product.ProductId, BranchA);
        Assert.Equal(10m, mainCatalog.OrganizationOnHandQuantity);
        Assert.Equal(0m, mainCatalog.BranchOnHandQuantity);
        Assert.Equal(0m, mainCatalog.BranchAvailableQuantity);
        Assert.Equal(0m, mainCatalog.OnHandQuantity);
        Assert.Equal("OutOfStock", mainCatalog.StockStatus);

        var thirdCatalog = await GetCatalogProductAsync(client, org, product.ProductId, ThirdBranch);
        Assert.Equal(10m, thirdCatalog.OrganizationOnHandQuantity);
        Assert.Equal(0m, thirdCatalog.BranchOnHandQuantity);
        Assert.Equal(0m, thirdCatalog.BranchAvailableQuantity);
        Assert.Equal(0m, thirdCatalog.OnHandQuantity);
        Assert.Equal("OutOfStock", thirdCatalog.StockStatus);

        var remoteList = await ListCatalogAsync(client, org, BranchB);
        var remoteRow = Assert.Single(remoteList.Items, item => item.ProductId == product.ProductId);
        Assert.Equal(10m, remoteRow.BranchAvailableQuantity);

        var mainList = await ListCatalogAsync(client, org, BranchA);
        var mainRow = Assert.Single(mainList.Items, item => item.ProductId == product.ProductId);
        Assert.Equal(0m, mainRow.BranchAvailableQuantity);
    }

    [Fact]
    public async Task BRANCHLEAK_12_primary_legacy_unallocated_still_applies_only_on_main()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Legacy Pack");
        await EnableTrackedAsync(client, org, product.ProductId, openingQuantity: 10m, branchId: BranchA);

        var mainCatalog = await GetCatalogProductAsync(client, org, product.ProductId, BranchA);
        Assert.Equal(10m, mainCatalog.BranchAvailableQuantity);

        var remoteCatalog = await GetCatalogProductAsync(client, org, product.ProductId, BranchB);
        Assert.Equal(0m, remoteCatalog.BranchAvailableQuantity);
    }

    private static async Task<decimal> BranchInventoryOnHandAsync(
        HttpClient client,
        Guid org,
        Guid productId,
        Guid branchId)
    {
        using var get = Scoped(HttpMethod.Get, $"{Inventory}/{productId:D}", org, branchId: branchId);
        using var response = await client.SendAsync(get);
        response.EnsureSuccessStatusCode();
        var account = await response.Content.ReadFromJsonAsync<PosInventoryAccountDto>(JsonOptions);
        return account!.OnHandQuantity;
    }

    private static async Task<PosCatalogProductDto> GetCatalogProductAsync(
        HttpClient client,
        Guid org,
        Guid productId,
        Guid branchId)
    {
        using var get = Scoped(HttpMethod.Get, $"{Products}/{productId:D}", org, branchId: branchId);
        using var response = await client.SendAsync(get);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PosCatalogProductDto>(JsonOptions))!;
    }

    private static async Task<PagedResult<PosCatalogProductDto>> ListCatalogAsync(
        HttpClient client,
        Guid org,
        Guid branchId)
    {
        using var get = Scoped(HttpMethod.Get, $"{Products}?status=Active&page=1&pageSize=50", org, branchId: branchId);
        using var response = await client.SendAsync(get);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PagedResult<PosCatalogProductDto>>(JsonOptions))!;
    }
}
