using System.Net;
using System.Net.Http.Json;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Returns;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using static ExItS.PinoyBusinessPOS.IntegrationTests.PosInventoryOpsIntegrationSupport;
using static ExItS.PinoyBusinessPOS.IntegrationTests.Support.MicaStoreInventoryClosureSupport;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

/// <summary>MB2-02D final multi-branch inventory closure certification.</summary>
[Collection(PosPostgreSqlCollection.Name)]
public sealed class BranchInventory02DFinalClosureIntegrationTests(PosPostgreSqlFixture fixture)
{
    private const string Returns = "/api/v1/pos/sale-returns";

    [Fact]
    public async Task FINAL_DUAL_AUDIT_complex_transaction_history_is_clean()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "02D Dual Audit Coke");

        await EnableTrackedAsync(client, org, product.ProductId, 50m, branchId: Main);
        await DirectPurchaseAsync(client, org, Main, product.ProductId, 10m, $"dp-{org:N}");
        await TransferAsync(client, org, Main, MicaA, product.ProductId, 15m);
        await CheckoutAsync(client, org, MicaA, product.ProductId, 5m);
        await AdjustAsync(client, org, MicaB, product.ProductId, "In", 3m);
        await WasteAsync(client, org, Main, product.ProductId, 2m);

        await AssertGlobalInvariantsAsync(fixture.ConnectionString, client, org, product.ProductId);
        await ReservationAuditCleanAsync(fixture.ConnectionString, org);
        await PhysicalAuditCleanAsync(client, org);

        var summary = await GetOrgSummaryAsync(client, org, product.ProductId);
        Assert.Equal(56m, summary.OrganizationOnHandQuantity);
        Assert.Equal(0m, summary.OrganizationReservedQuantity);
    }

    [Fact]
    public async Task FINAL_COMPLEX_E2E_normal_and_expiry_products_with_return_and_lots()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var normal = await CreateProductAsync(client, org, "02D Normal Water");
        await EnableTrackedAsync(client, org, normal.ProductId, 40m, branchId: Main);
        await TransferAsync(client, org, Main, MicaA, normal.ProductId, 10m);
        await CheckoutAsync(client, org, MicaA, normal.ProductId, 4m);

        var expiry = await CreateProductAsync(client, org, "02D Expiry Milk", tracksExpiration: true);
        await EnableTrackedAsync(
            client,
            org,
            expiry.ProductId,
            openingQuantity: 12m,
            unitCost: 5m,
            branchId: Main,
            expirationDate: new DateOnly(2026, 12, 1),
            lotNumber: "LOT-02D-A");
        await CheckoutAsync(client, org, Main, expiry.ProductId, 2m);

        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, OwnerActor);
        var returnSale = await CheckoutAsyncWithSale(client, org, MicaA, normal.ProductId, 2m);
        var line = returnSale.Lines.Single();
        using var createReturn = Scoped(HttpMethod.Post, Returns, org, OwnerActor, MicaA);
        createReturn.Content = JsonContent.Create(
            new CreateSaleReturnRequest(
                returnSale.SaleId,
                "02D return",
                [new CreateSaleReturnLineRequest(line.SaleLineId, 1m, "ReturnToStock")]),
            options: JsonOptions);
        (await client.SendAsync(createReturn)).EnsureSuccessStatusCode();

        await AssertGlobalInvariantsAsync(fixture.ConnectionString, client, org, normal.ProductId);
        await AssertGlobalInvariantsAsync(fixture.ConnectionString, client, org, expiry.ProductId);
        await ReservationAuditCleanAsync(fixture.ConnectionString, org);
        await PhysicalAuditCleanAsync(client, org);

        var normalSummary = await GetOrgSummaryAsync(client, org, normal.ProductId);
        Assert.Equal(35m, normalSummary.OrganizationOnHandQuantity);
        Assert.Equal(5m, BranchQuantity(normalSummary, MicaA));

        var expirySummary = await GetOrgSummaryAsync(client, org, expiry.ProductId);
        Assert.Equal(10m, expirySummary.OrganizationOnHandQuantity);

        var lots = await ListLotsAsync(client, org, expiry.ProductId);
        Assert.Equal(10m, lots.Sum(l => l.QuantityOnHand));
    }

    [Fact]
    public async Task FINAL_ORG_INVENTORY_aggregate_independent_of_workspace_branch()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "02D Org View");

        await EnableTrackedAsync(client, org, product.ProductId, 30m, branchId: Main);
        await TransferAsync(client, org, Main, MicaA, product.ProductId, 8m);
        await TransferAsync(client, org, Main, MicaB, product.ProductId, 5m);

        var fromMain = await GetOrgSummaryAsync(client, org, product.ProductId);
        using var fromMicaA = Scoped(HttpMethod.Get, $"{Inventory}/{product.ProductId:D}/organization-summary", org, OwnerActor, MicaA);
        using var micaAResponse = await client.SendAsync(fromMicaA);
        micaAResponse.EnsureSuccessStatusCode();
        var fromA = (await micaAResponse.Content.ReadFromJsonAsync<PosOrganizationInventoryProductDto>(JsonOptions))!;

        Assert.Equal(fromMain.OrganizationOnHandQuantity, fromA.OrganizationOnHandQuantity);
        Assert.Equal(fromMain.OrganizationReservedQuantity, fromA.OrganizationReservedQuantity);
        Assert.Equal(30m, fromMain.OrganizationOnHandQuantity);
        Assert.Equal(17m, BranchQuantity(fromMain, Main));
        Assert.Equal(8m, BranchQuantity(fromMain, MicaA));
        Assert.Equal(5m, BranchQuantity(fromMain, MicaB));
    }

    private static decimal BranchQuantity(PosOrganizationInventoryProductDto summary, Guid branchId) =>
        summary.Branches.SingleOrDefault(b => b.BranchId == branchId)?.OnHandQuantity ?? 0m;

    private static async Task<PosSaleDto> CheckoutAsyncWithSale(
        HttpClient client,
        Guid org,
        Guid branchId,
        Guid productId,
        decimal qty)
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
        return (await response.Content.ReadFromJsonAsync<PosSaleDto>(JsonOptions))!;
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
