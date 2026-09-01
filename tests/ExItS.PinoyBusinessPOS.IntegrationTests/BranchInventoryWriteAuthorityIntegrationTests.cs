using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Npgsql;
using static ExItS.PinoyBusinessPOS.IntegrationTests.PosInventoryOpsIntegrationSupport;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

/// <summary>MB2-02B branch-authoritative physical inventory write scenarios.</summary>
[Collection(PosPostgreSqlCollection.Name)]
public sealed class BranchInventoryWriteAuthorityIntegrationTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private const string Inventory = "/api/v1/pos/inventory";
    private const string DirectPurchases = "/api/v1/pos/direct-purchase-receipts";
    private const string StockCounts = "/api/v1/pos/inventory/stock-counts";

    [Fact]
    public async Task BWRITE_OPEN_01_enable_at_main_credits_main_only()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Coke Open Main");
        await EnableTrackedAsync(client, org, product.ProductId, 100m, branchId: BranchA);

        Assert.Equal(100m, await BranchOnHandAsync(client, org, product.ProductId, BranchA));
        Assert.Equal(0m, await BranchOnHandAsync(client, org, product.ProductId, BranchB));
        Assert.Equal(100m, await OrgOnHandAsync(fixture.ConnectionString, org, product.ProductId));
    }

    [Fact]
    public async Task BWRITE_OPEN_02_remote_opening_adds_remote_without_touching_main_implicit()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Coke Split Open");
        await EnableTrackedAsync(client, org, product.ProductId, 100m, branchId: BranchA);
        await AdjustAsync(client, org, product.ProductId, BranchB, "In", 20m);

        Assert.Equal(120m, await OrgOnHandAsync(fixture.ConnectionString, org, product.ProductId));
        Assert.Equal(100m, await BranchOnHandAsync(client, org, product.ProductId, BranchA));
        Assert.Equal(20m, await BranchOnHandAsync(client, org, product.ProductId, BranchB));
    }

    [Fact]
    public async Task BWRITE_ADJ_01_remote_positive_adjustment_updates_org_and_remote_only()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Coke Adjust");
        await EnableTrackedAsync(client, org, product.ProductId, 100m, branchId: BranchA);
        await AdjustAsync(client, org, product.ProductId, BranchB, "In", 25m);
        await AdjustAsync(client, org, product.ProductId, BranchB, "In", 10m);

        Assert.Equal(135m, await OrgOnHandAsync(fixture.ConnectionString, org, product.ProductId));
        Assert.Equal(100m, await BranchOnHandAsync(client, org, product.ProductId, BranchA));
        Assert.Equal(35m, await BranchOnHandAsync(client, org, product.ProductId, BranchB));
    }

    [Fact]
    public async Task BWRITE_COUNT_01_remote_count_applies_variance_not_org_replacement()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Coke Count");
        await EnableTrackedAsync(client, org, product.ProductId, 100m, branchId: BranchA);
        await AdjustAsync(client, org, product.ProductId, BranchB, "In", 25m);

        using var create = Scoped(HttpMethod.Post, StockCounts, org, branchId: BranchB);
        create.Content = JsonContent.Create(
            new CreateStockCountRequest([new CreateStockCountLineRequest(product.ProductId)], "Remote count"),
            options: JsonOptions);
        using var createResponse = await client.SendAsync(create);
        createResponse.EnsureSuccessStatusCode();
        var draft = await createResponse.Content.ReadFromJsonAsync<PosStockCountDto>(JsonOptions);
        Assert.Equal(BranchB, draft!.BranchId);

        using var start = Scoped(HttpMethod.Post, $"{StockCounts}/{draft.StockCountId:D}/start", org, branchId: BranchB);
        using var startResponse = await client.SendAsync(start);
        startResponse.EnsureSuccessStatusCode();

        using var update = Scoped(HttpMethod.Put, $"{StockCounts}/{draft.StockCountId:D}", org, branchId: BranchB);
        update.Content = JsonContent.Create(
            new UpdateStockCountRequest([new CreateStockCountLineRequest(product.ProductId, CountedQuantity: 20m)]),
            options: JsonOptions);
        using var updateResponse = await client.SendAsync(update);
        updateResponse.EnsureSuccessStatusCode();

        using var complete = Scoped(HttpMethod.Post, $"{StockCounts}/{draft.StockCountId:D}/complete", org, branchId: BranchB);
        using var completeResponse = await client.SendAsync(complete);
        completeResponse.EnsureSuccessStatusCode();

        Assert.Equal(120m, await OrgOnHandAsync(fixture.ConnectionString, org, product.ProductId));
        Assert.Equal(100m, await BranchOnHandAsync(client, org, product.ProductId, BranchA));
        Assert.Equal(20m, await BranchOnHandAsync(client, org, product.ProductId, BranchB));
    }

    [Fact]
    public async Task BWRITE_DP_01_direct_purchase_at_remote_persists_branch_and_credits_remote()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Coke DP");
        await EnableTrackedAsync(client, org, product.ProductId, 100m, branchId: BranchA);
        await AdjustAsync(client, org, product.ProductId, BranchB, "In", 20m);

        using var create = Scoped(HttpMethod.Post, DirectPurchases, org, branchId: BranchB);
        create.Content = JsonContent.Create(
            new CreateDirectPurchaseReceiptRequest(
                DateOnly.FromDateTime(DateTime.UtcNow),
                [new CreateDirectPurchaseReceiptLineRequest(product.ProductId, 30m, 5m)]),
            options: JsonOptions);
        using var createResponse = await client.SendAsync(create);
        createResponse.EnsureSuccessStatusCode();
        var receipt = await createResponse.Content.ReadFromJsonAsync<DirectPurchaseReceiptDto>(JsonOptions);
        Assert.Equal(BranchB, receipt!.ReceivingBranchId);

        Assert.Equal(150m, await OrgOnHandAsync(fixture.ConnectionString, org, product.ProductId));
        Assert.Equal(100m, await BranchOnHandAsync(client, org, product.ProductId, BranchA));
        Assert.Equal(50m, await BranchOnHandAsync(client, org, product.ProductId, BranchB));
    }

    [Fact]
    public async Task BWRITE_LEGACY_01_primary_materialized_before_inflow_not_double_counted()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Legacy Main");
        await EnableTrackedAsync(client, org, product.ProductId, 100m, branchId: BranchA);

        await AdjustAsync(client, org, product.ProductId, BranchA, "In", 10m);

        Assert.Equal(110m, await OrgOnHandAsync(fixture.ConnectionString, org, product.ProductId));
        Assert.Equal(110m, await BranchOnHandAsync(client, org, product.ProductId, BranchA));
        Assert.Equal(0, await CountBranchBalancesAsync(fixture.ConnectionString, org, product.ProductId, BranchB));
    }

    private static async Task AdjustAsync(
        HttpClient client,
        Guid org,
        Guid productId,
        Guid branchId,
        string direction,
        decimal quantity)
    {
        using var adjust = Scoped(HttpMethod.Post, $"{Inventory}/{productId:D}/adjustments", org, branchId: branchId);
        adjust.Content = JsonContent.Create(
            new AdjustInventoryRequest(direction, quantity, "BWRITE test"),
            options: JsonOptions);
        using var response = await client.SendAsync(adjust);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<decimal> OrgOnHandAsync(string connectionString, Guid org, Guid productId)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            """
            SELECT on_hand_quantity FROM pos.inventory_accounts
            WHERE organization_id = @org AND product_id = @product
            """,
            connection);
        cmd.Parameters.AddWithValue("org", org);
        cmd.Parameters.AddWithValue("product", productId);
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToDecimal(result);
    }

    private static async Task<decimal> BranchOnHandAsync(HttpClient client, Guid org, Guid productId, Guid branchId)
    {
        using var get = Scoped(HttpMethod.Get, $"{Inventory}/{productId:D}", org, branchId: branchId);
        using var response = await client.SendAsync(get);
        response.EnsureSuccessStatusCode();
        var account = await response.Content.ReadFromJsonAsync<PosInventoryAccountDto>(JsonOptions);
        return account!.OnHandQuantity;
    }

    private static async Task SeedOrgAndBranchesAsync(
        string connectionString,
        Guid org,
        Guid productId,
        decimal orgOnHand,
        decimal main,
        decimal remote)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using (var cmd = new NpgsqlCommand(
                         """
                         UPDATE pos.inventory_accounts
                         SET is_tracked = TRUE, on_hand_quantity = @onHand
                         WHERE organization_id = @org AND product_id = @product
                         """,
                         connection))
        {
            cmd.Parameters.AddWithValue("onHand", orgOnHand);
            cmd.Parameters.AddWithValue("org", org);
            cmd.Parameters.AddWithValue("product", productId);
            await cmd.ExecuteNonQueryAsync();
        }

        await UpsertBalanceAsync(connection, org, BranchA, productId, main);
        await UpsertBalanceAsync(connection, org, BranchB, productId, remote);
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

    private static async Task<int> CountBranchBalancesAsync(
        string connectionString,
        Guid org,
        Guid productId,
        Guid branchId)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            """
            SELECT COUNT(*) FROM pos.inventory_branch_balances
            WHERE organization_id = @org AND product_id = @product AND branch_id = @branch
            """,
            connection);
        cmd.Parameters.AddWithValue("org", org);
        cmd.Parameters.AddWithValue("product", productId);
        cmd.Parameters.AddWithValue("branch", branchId);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }
}
