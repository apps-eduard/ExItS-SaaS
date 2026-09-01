using System.Net;
using System.Net.Http.Json;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using static ExItS.PinoyBusinessPOS.IntegrationTests.PosInventoryOpsIntegrationSupport;

namespace ExItS.PinoyBusinessPOS.IntegrationTests.Support;

/// <summary>Shared Mica Store inventory closure helpers for MB2-02C-H1 and MB2-02D.</summary>
internal static class MicaStoreInventoryClosureSupport
{
    public static readonly Guid Main = BranchA;
    public static readonly Guid MicaA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid MicaB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public static async Task ReservationAuditCleanAsync(string connectionString, Guid org)
    {
        var options = new DbContextOptionsBuilder<PosDbContext>().UseNpgsql(connectionString).Options;
        await using var db = new PosDbContext(options);
        var audit = await new BranchInventoryReservationCutover(db).AuditAsync(org);
        Assert.Equal(0, audit.MismatchedBalanceCount);
    }

    public static async Task PhysicalAuditCleanAsync(HttpClient client, Guid org)
    {
        using var audit = Scoped(HttpMethod.Get, $"{Inventory}/physical-audit", org, OwnerActor, Main);
        using var response = await client.SendAsync(audit);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<InventoryPhysicalAuditResult>(JsonOptions);
        Assert.NotNull(result);
        Assert.True(result!.IsClean);
    }

    public static async Task AssertGlobalInvariantsAsync(
        string connectionString,
        HttpClient client,
        Guid org,
        Guid productId)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        async Task<decimal> Scalar(string sql)
        {
            await using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("org", org);
            cmd.Parameters.AddWithValue("product", productId);
            return Convert.ToDecimal(await cmd.ExecuteScalarAsync());
        }

        var orgOnHand = await Scalar(
            "SELECT on_hand_quantity FROM pos.inventory_accounts WHERE organization_id = @org AND product_id = @product");
        var orgReserved = await Scalar(
            "SELECT reserved_quantity FROM pos.inventory_accounts WHERE organization_id = @org AND product_id = @product");
        var branchOnHandSum = await Scalar(
            "SELECT COALESCE(SUM(on_hand_quantity), 0) FROM pos.inventory_branch_balances WHERE organization_id = @org AND product_id = @product");
        var branchReservedSum = await Scalar(
            "SELECT COALESCE(SUM(reserved_quantity), 0) FROM pos.inventory_branch_balances WHERE organization_id = @org AND product_id = @product");

        Assert.Equal(orgOnHand, branchOnHandSum);
        Assert.Equal(orgReserved, branchReservedSum);
        Assert.True(orgOnHand >= 0m);
        Assert.True(orgReserved >= 0m);
        Assert.True(orgReserved <= orgOnHand);

        var summary = await GetOrgSummaryAsync(client, org, productId);
        Assert.Equal(orgOnHand, summary.OrganizationOnHandQuantity);
        Assert.Equal(orgReserved, summary.OrganizationReservedQuantity);
        Assert.Equal(orgOnHand - orgReserved, summary.OrganizationAvailableQuantity);
        Assert.Equal(branchOnHandSum, summary.Branches.Sum(b => b.OnHandQuantity));
        Assert.Equal(branchReservedSum, summary.Branches.Sum(b => b.ReservedQuantity));

        foreach (var branch in summary.Branches)
        {
            Assert.Equal(branch.OnHandQuantity - branch.ReservedQuantity, branch.AvailableQuantity);
            Assert.True(branch.OnHandQuantity >= 0m);
            Assert.True(branch.ReservedQuantity >= 0m);
            Assert.True(branch.ReservedQuantity <= branch.OnHandQuantity);
        }
    }

    public static async Task<PosOrganizationInventoryProductDto> GetOrgSummaryAsync(
        HttpClient client,
        Guid org,
        Guid productId)
    {
        using var get = Scoped(HttpMethod.Get, $"{Inventory}/{productId:D}/organization-summary", org, OwnerActor, Main);
        using var response = await client.SendAsync(get);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PosOrganizationInventoryProductDto>(JsonOptions))!;
    }

    public static async Task CheckoutAsync(HttpClient client, Guid org, Guid branchId, Guid productId, decimal qty)
    {
        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, OwnerActor);
        using var checkout = Scoped(HttpMethod.Post, "/api/v1/pos/sales", org, OwnerActor, branchId);
        checkout.Content = JsonContent.Create(
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(productId, qty)],
                PosSaleOptions.CashPaymentMethod,
                AmountTendered: 500m),
            options: JsonOptions);
        using var response = await client.SendAsync(checkout);
        response.EnsureSuccessStatusCode();
    }

    public static async Task<InventoryTransferDto> TransferAsync(
        HttpClient client,
        Guid org,
        Guid source,
        Guid dest,
        Guid productId,
        decimal qty)
    {
        using var create = Scoped(HttpMethod.Post, $"{Inventory}/transfers", org, OwnerActor, source);
        create.Content = JsonContent.Create(
            new CreateInventoryTransferRequest(source, dest, [new InventoryTransferLineRequest(productId, qty)]),
            options: JsonOptions);
        using var createResponse = await client.SendAsync(create);
        createResponse.EnsureSuccessStatusCode();
        var transfer = (await createResponse.Content.ReadFromJsonAsync<InventoryTransferDto>(JsonOptions))!;

        using var dispatch = Scoped(HttpMethod.Post, $"{Inventory}/transfers/{transfer.TransferId:D}/dispatch", org, OwnerActor, source);
        (await client.SendAsync(dispatch)).EnsureSuccessStatusCode();

        using var receive = Scoped(HttpMethod.Post, $"{Inventory}/transfers/{transfer.TransferId:D}/receive", org, OwnerActor, dest);
        receive.Content = JsonContent.Create(
            new ReceiveInventoryTransferRequest([new InventoryTransferReceiveLineRequest(productId, qty)]),
            options: JsonOptions);
        (await client.SendAsync(receive)).EnsureSuccessStatusCode();
        return transfer;
    }

    public static async Task DirectPurchaseAsync(
        HttpClient client,
        Guid org,
        Guid branchId,
        Guid productId,
        decimal qty,
        string idempotencyKey)
    {
        using var create = Scoped(HttpMethod.Post, "/api/v1/pos/direct-purchase-receipts", org, OwnerActor, branchId);
        create.Content = JsonContent.Create(
            new CreateDirectPurchaseReceiptRequest(
                PurchaseDate: DateOnly.FromDateTime(DateTime.UtcNow),
                Lines: [new CreateDirectPurchaseReceiptLineRequest(productId, qty, UnitCost: 1m)],
                IdempotencyKey: idempotencyKey),
            options: JsonOptions);
        (await client.SendAsync(create)).EnsureSuccessStatusCode();
    }

    public static async Task AdjustAsync(HttpClient client, Guid org, Guid branchId, Guid productId, string direction, decimal qty)
    {
        using var adjust = Scoped(HttpMethod.Post, $"{Inventory}/{productId:D}/adjustments", org, OwnerActor, branchId);
        adjust.Content = JsonContent.Create(new AdjustInventoryRequest(direction, qty, "02D closure"), options: JsonOptions);
        (await client.SendAsync(adjust)).EnsureSuccessStatusCode();
    }

    public static async Task WasteAsync(HttpClient client, Guid org, Guid branchId, Guid productId, decimal qty)
    {
        using var create = Scoped(HttpMethod.Post, WasteLosses, org, OwnerActor, branchId);
        create.Content = JsonContent.Create(
            new CreateWasteLossRequest("Damaged", [new CreateWasteLossLineRequest(productId, qty)]),
            options: JsonOptions);
        (await client.SendAsync(create)).EnsureSuccessStatusCode();
    }
}
