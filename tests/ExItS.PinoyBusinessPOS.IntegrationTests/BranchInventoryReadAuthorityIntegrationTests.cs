using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class BranchInventoryReadAuthorityIntegrationTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Guid Actor = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid MainBranch = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid RemoteBranch = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ThirdBranch = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private const string Inventory = "/api/v1/pos/inventory";
    private const string Products = "/api/v1/pos/catalog/products";

    [Fact]
    public async Task BINV_API_08_missing_branch_context_returns_branch_required()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        using var request = new HttpRequestMessage(HttpMethod.Get, Inventory);
        request.Headers.TryAddWithoutValidation(PosOrganizationHeaders.OrganizationHeaderName, org.ToString("D"));
        request.Headers.TryAddWithoutValidation(PosOrganizationHeaders.ActorHeaderName, Actor.ToString("D"));
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ApplicationErrorCodes.InventoryBranchRequired, await ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task BINV_READ_04_primary_legacy_unallocated_and_secondary_zero()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Legacy Coke");
        await EnableAsync(client, org, product.ProductId, 100m);

        Assert.Equal(100m, await OnHandAsync(client, org, product.ProductId, MainBranch));
        Assert.Equal(0m, await OnHandAsync(client, org, product.ProductId, RemoteBranch));
    }

    [Fact]
    public async Task BINV_READ_01_02_03_explicit_balances_per_branch()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Split Coke");
        await EnableAsync(client, org, product.ProductId, 0m);
        await SeedBalancesAsync(fixture.ConnectionString, org, product.ProductId, organizationOnHand: 125m, main: 100m, remote: 25m);

        Assert.Equal(100m, await OnHandAsync(client, org, product.ProductId, MainBranch));
        Assert.Equal(25m, await OnHandAsync(client, org, product.ProductId, RemoteBranch));
        Assert.Equal(0m, await OnHandAsync(client, org, product.ProductId, ThirdBranch));
    }

    [Fact]
    public async Task BINV_READ_05_partial_legacy_main_implicit_remote_explicit()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Partial Legacy");
        await EnableAsync(client, org, product.ProductId, 0m);
        await SeedBalancesAsync(fixture.ConnectionString, org, product.ProductId, organizationOnHand: 125m, main: null, remote: 25m);

        Assert.Equal(100m, await OnHandAsync(client, org, product.ProductId, MainBranch));
        Assert.Equal(25m, await OnHandAsync(client, org, product.ProductId, RemoteBranch));
    }

    [Fact]
    public async Task BINV_READ_06_list_read_does_not_create_branch_balance_row()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "No Materialize");
        await EnableAsync(client, org, product.ProductId, 50m);

        using var list = Scoped(HttpMethod.Get, Inventory, org, RemoteBranch);
        using var listResponse = await client.SendAsync(list);
        if (!listResponse.IsSuccessStatusCode)
        {
            var body = await listResponse.Content.ReadAsStringAsync();
            Assert.Fail($"List inventory failed: {(int)listResponse.StatusCode} {body}");
        }

        var count = await CountBranchBalancesAsync(fixture.ConnectionString, org, product.ProductId, RemoteBranch);
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task BINV_REORDER_01_02_branch_low_stock_differs_by_branch()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Reorder Split");
        await EnableAsync(client, org, product.ProductId, 0m, reorderLevel: 20m);
        await SeedBalancesAsync(fixture.ConnectionString, org, product.ProductId, organizationOnHand: 105m, main: 100m, remote: 5m);
        await UpsertBranchReorderAsync(fixture.ConnectionString, org, RemoteBranch, product.ProductId, reorderLevel: 10m);

        var main = await OnHandAsync(client, org, product.ProductId, MainBranch);
        var remote = await OnHandAsync(client, org, product.ProductId, RemoteBranch);
        Assert.Equal(100m, main);
        Assert.Equal(5m, remote);
        Assert.False((await GetAccountAsync(client, org, product.ProductId, MainBranch))!.IsLowStock);
        Assert.True((await GetAccountAsync(client, org, product.ProductId, RemoteBranch))!.IsLowStock);
    }

    private static async Task<decimal> OnHandAsync(HttpClient client, Guid org, Guid productId, Guid branchId)
    {
        var account = await GetAccountAsync(client, org, productId, branchId);
        Assert.NotNull(account);
        return account!.OnHandQuantity;
    }

    private static async Task<PosInventoryAccountDto?> GetAccountAsync(
        HttpClient client,
        Guid org,
        Guid productId,
        Guid branchId)
    {
        using var get = Scoped(HttpMethod.Get, $"{Inventory}/{productId:D}", org, branchId);
        using var response = await client.SendAsync(get);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PosInventoryAccountDto>(JsonOptions);
    }

    private static async Task EnableAsync(
        HttpClient client,
        Guid org,
        Guid productId,
        decimal opening,
        decimal? reorderLevel = null)
    {
        using var enable = Scoped(HttpMethod.Post, $"{Inventory}/{productId:D}/enable", org, MainBranch);
        enable.Content = JsonContent.Create(
            new EnableInventoryTrackingRequest(opening, reorderLevel, UnitCost: 1m),
            options: JsonOptions);
        (await client.SendAsync(enable)).EnsureSuccessStatusCode();
    }

    private static async Task SeedBalancesAsync(
        string connectionString,
        Guid org,
        Guid productId,
        decimal organizationOnHand,
        decimal? main,
        decimal? remote)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using (var cmd = new NpgsqlCommand(
                         """
                         UPDATE pos.inventory_accounts
                         SET on_hand_quantity = @onHand
                         WHERE organization_id = @org AND product_id = @product
                         """,
                         connection))
        {
            cmd.Parameters.AddWithValue("onHand", organizationOnHand);
            cmd.Parameters.AddWithValue("org", org);
            cmd.Parameters.AddWithValue("product", productId);
            await cmd.ExecuteNonQueryAsync();
        }

        if (main is not null)
        {
            await UpsertBalanceAsync(connection, org, MainBranch, productId, main.Value);
        }

        if (remote is not null)
        {
            await UpsertBalanceAsync(connection, org, RemoteBranch, productId, remote.Value);
        }
    }

    private static async Task UpsertBranchReorderAsync(
        string connectionString,
        Guid org,
        Guid branchId,
        Guid productId,
        decimal reorderLevel)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            """
            INSERT INTO pos.inventory_branch_reorder_settings
                (organization_id, branch_id, product_id, reorder_level, reorder_quantity, updated_at_utc, updated_by)
            VALUES (@org, @branch, @product, @level, NULL, NOW() AT TIME ZONE 'UTC', @actor)
            ON CONFLICT (organization_id, branch_id, product_id)
            DO UPDATE SET reorder_level = EXCLUDED.reorder_level, updated_at_utc = EXCLUDED.updated_at_utc, updated_by = EXCLUDED.updated_by
            """,
            connection);
        cmd.Parameters.AddWithValue("org", org);
        cmd.Parameters.AddWithValue("branch", branchId);
        cmd.Parameters.AddWithValue("product", productId);
        cmd.Parameters.AddWithValue("level", reorderLevel);
        cmd.Parameters.AddWithValue("actor", Actor);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task UpsertBalanceAsync(
        NpgsqlConnection connection,
        Guid org,
        Guid branchId,
        Guid productId,
        decimal onHand)
    {
        await using var cmd = new NpgsqlCommand(
            """
            INSERT INTO pos.inventory_branch_balances
                (organization_id, branch_id, product_id, on_hand_quantity, updated_at_utc)
            VALUES (@org, @branch, @product, @onHand, NOW() AT TIME ZONE 'UTC')
            ON CONFLICT (organization_id, branch_id, product_id)
            DO UPDATE SET on_hand_quantity = EXCLUDED.on_hand_quantity, updated_at_utc = EXCLUDED.updated_at_utc
            """,
            connection);
        cmd.Parameters.AddWithValue("org", org);
        cmd.Parameters.AddWithValue("branch", branchId);
        cmd.Parameters.AddWithValue("product", productId);
        cmd.Parameters.AddWithValue("onHand", onHand);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<int> CountBranchBalancesAsync(string connectionString, Guid org, Guid productId, Guid branchId)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            """
            SELECT COUNT(*)::int
            FROM pos.inventory_branch_balances
            WHERE organization_id = @org AND branch_id = @branch AND product_id = @product
            """,
            connection);
        cmd.Parameters.AddWithValue("org", org);
        cmd.Parameters.AddWithValue("branch", branchId);
        cmd.Parameters.AddWithValue("product", productId);
        return (int)(await cmd.ExecuteScalarAsync() ?? 0);
    }

    private static async Task<PosCatalogProductDto> CreateProductAsync(HttpClient client, Guid org, string name)
    {
        using var request = Scoped(HttpMethod.Post, Products, org, MainBranch);
        request.Content = JsonContent.Create(new CreatePosCatalogProductRequest(name, "Piece", 25m), options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PosCatalogProductDto>(JsonOptions))!;
    }

    private static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return problem.TryGetProperty("errorCode", out var code) ? code.GetString() : null;
    }

    private static HttpRequestMessage Scoped(HttpMethod method, string path, Guid organizationId, Guid branchId)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation(PosOrganizationHeaders.OrganizationHeaderName, organizationId.ToString("D"));
        request.Headers.TryAddWithoutValidation(PosOrganizationHeaders.ActorHeaderName, Actor.ToString("D"));
        request.Headers.TryAddWithoutValidation(PosOrganizationHeaders.BranchHeaderName, branchId.ToString("D"));
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
