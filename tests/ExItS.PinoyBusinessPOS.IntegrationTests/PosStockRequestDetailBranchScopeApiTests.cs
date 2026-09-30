using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

/// <summary>
/// GET stock-request detail/activity require X-Pos-Branch-Id and only allow source or destination
/// (same not-found style as transfer detail). Full-access users stay scoped to the current workspace branch.
/// </summary>
[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosStockRequestDetailBranchScopeApiTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Guid Actor = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid SourceWarehouse = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid DestinationRetail = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid UnrelatedBranch = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private const string Inventory = "/api/v1/pos/inventory";
    private const string Products = "/api/v1/pos/catalog/products";

    [Fact]
    public async Task Get_detail_and_activity_allow_source_and_destination_only()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Scope Coke", "Piece", 25m, "sr-scope-coke");
        var stockRequestId = await SeedStockRequestAsync(org, product.ProductId);

        using var sourceDetail = Scoped(
            HttpMethod.Get,
            $"{Inventory}/stock-requests/{stockRequestId:D}",
            org,
            SourceWarehouse);
        using var sourceDetailResponse = await client.SendAsync(sourceDetail);
        sourceDetailResponse.EnsureSuccessStatusCode();
        var sourceDto = await sourceDetailResponse.Content.ReadFromJsonAsync<StockRequestDto>(JsonOptions);
        Assert.NotNull(sourceDto);
        Assert.Equal(stockRequestId, sourceDto!.StockRequestId);

        using var destDetail = Scoped(
            HttpMethod.Get,
            $"{Inventory}/stock-requests/{stockRequestId:D}",
            org,
            DestinationRetail);
        using var destDetailResponse = await client.SendAsync(destDetail);
        destDetailResponse.EnsureSuccessStatusCode();

        // Full-access acting workspace that is neither source nor destination → not found.
        using var foreignDetail = Scoped(
            HttpMethod.Get,
            $"{Inventory}/stock-requests/{stockRequestId:D}",
            org,
            UnrelatedBranch);
        using var foreignDetailResponse = await client.SendAsync(foreignDetail);
        Assert.Equal(HttpStatusCode.NotFound, foreignDetailResponse.StatusCode);

        using var sourceActivity = Scoped(
            HttpMethod.Get,
            $"{Inventory}/stock-requests/{stockRequestId:D}/activity",
            org,
            SourceWarehouse);
        using var sourceActivityResponse = await client.SendAsync(sourceActivity);
        sourceActivityResponse.EnsureSuccessStatusCode();

        using var destActivity = Scoped(
            HttpMethod.Get,
            $"{Inventory}/stock-requests/{stockRequestId:D}/activity",
            org,
            DestinationRetail);
        using var destActivityResponse = await client.SendAsync(destActivity);
        destActivityResponse.EnsureSuccessStatusCode();

        using var foreignActivity = Scoped(
            HttpMethod.Get,
            $"{Inventory}/stock-requests/{stockRequestId:D}/activity",
            org,
            UnrelatedBranch);
        using var foreignActivityResponse = await client.SendAsync(foreignActivity);
        Assert.Equal(HttpStatusCode.NotFound, foreignActivityResponse.StatusCode);

        using var missingBranch = new HttpRequestMessage(
            HttpMethod.Get,
            $"{Inventory}/stock-requests/{stockRequestId:D}");
        missingBranch.Headers.TryAddWithoutValidation(
            PosOrganizationHeaders.OrganizationHeaderName,
            org.ToString("D"));
        missingBranch.Headers.TryAddWithoutValidation(
            PosOrganizationHeaders.ActorHeaderName,
            Actor.ToString("D"));
        using var missingBranchResponse = await client.SendAsync(missingBranch);
        Assert.Equal(HttpStatusCode.BadRequest, missingBranchResponse.StatusCode);
    }

    private async Task<Guid> SeedStockRequestAsync(Guid org, Guid productId)
    {
        var stockRequestId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            INSERT INTO pos.stock_requests (
                id, organization_id, destination_location_id, requested_source_location_id,
                request_number, status, notes, requested_by, created_at_utc, updated_at_utc)
            VALUES (
                @id, @org, @dest, @source,
                @number, 'Pending', NULL, @actor, @now, @now);

            INSERT INTO pos.stock_request_lines (
                id, stock_request_id, organization_id, product_id, line_number,
                requested_quantity, name_snapshot, unit_of_measure)
            VALUES (
                @lineId, @id, @org, @product, 1,
                10, 'Scope Coke', 'Piece');
            """;
        cmd.Parameters.AddWithValue("id", stockRequestId);
        cmd.Parameters.AddWithValue("org", org);
        cmd.Parameters.AddWithValue("dest", DestinationRetail);
        cmd.Parameters.AddWithValue("source", SourceWarehouse);
        cmd.Parameters.AddWithValue("number", $"SR-{DateTime.UtcNow:yyMMdd}-901");
        cmd.Parameters.AddWithValue("actor", Actor);
        cmd.Parameters.AddWithValue("now", now);
        cmd.Parameters.AddWithValue("lineId", lineId);
        cmd.Parameters.AddWithValue("product", productId);
        await cmd.ExecuteNonQueryAsync();
        return stockRequestId;
    }

    private static async Task<PosCatalogProductDto> CreateProductAsync(
        HttpClient client,
        Guid org,
        string name,
        string unitOfMeasure,
        decimal sellingPrice,
        string sku)
    {
        using var request = Scoped(HttpMethod.Post, Products, org, DestinationRetail);
        request.Content = JsonContent.Create(
            new CreatePosCatalogProductRequest(
                name,
                unitOfMeasure,
                sellingPrice,
                null,
                sku),
            options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<PosCatalogProductDto>(JsonOptions);
        Assert.NotNull(product);
        return product!;
    }

    private static HttpRequestMessage Scoped(HttpMethod method, string path, Guid organizationId, Guid branchId)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation(
            PosOrganizationHeaders.OrganizationHeaderName,
            organizationId.ToString("D"));
        request.Headers.TryAddWithoutValidation(
            PosOrganizationHeaders.ActorHeaderName,
            Actor.ToString("D"));
        request.Headers.TryAddWithoutValidation(
            PosOrganizationHeaders.BranchHeaderName,
            branchId.ToString("D"));
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
