using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Application.Returns;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosSaleReturnApiTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Guid Actor = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    private const string Returns = "/api/v1/pos/sale-returns";
    private const string Sales = "/api/v1/pos/sales";
    private const string Products = "/api/v1/pos/catalog/products";

    [Fact]
    public async Task Cash_return_restock_and_blocks_void()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var product = await CreateProductAsync(client, org, "Bigas", "Kilogram", 62m, sku: "ret-rice");
        await EnableInventoryAsync(client, org, product.ProductId);
        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, Actor, openingCashAmount: 500m);

        var sale = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 2m)],
                PosSaleOptions.CashPaymentMethod,
                200m));

        var line = sale.Lines.Single();
        var body = new CreateSaleReturnRequest(
            sale.SaleId,
            "Wrong item",
            [new CreateSaleReturnLineRequest(line.SaleLineId, 1m, "ReturnToStock")]);

        using var create = PosIntegrationRequest.Scoped(HttpMethod.Post, Returns, org, Actor);
        create.Content = JsonContent.Create(body, options: JsonOptions);
        using var createResponse = await client.SendAsync(create);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var saleReturn = await createResponse.Content.ReadFromJsonAsync<PosSaleReturnDto>(JsonOptions);
        Assert.NotNull(saleReturn);
        Assert.StartsWith("RET-", saleReturn!.ReturnNumber, StringComparison.Ordinal);
        Assert.Equal(62m, saleReturn.TotalRefundAmount);
        Assert.Equal(PosSaleOptions.CashPaymentMethod, saleReturn.RefundMethod);
        Assert.NotNull(saleReturn.CashierShiftId);

        using var voidAttempt = PosIntegrationRequest.Scoped(HttpMethod.Post, $"{Sales}/{sale.SaleId:D}/void", org, Actor);
        voidAttempt.Content = JsonContent.Create(new VoidSaleRequest("should fail"), options: JsonOptions);
        using var voidResponse = await client.SendAsync(voidAttempt);
        Assert.Equal(HttpStatusCode.Conflict, voidResponse.StatusCode);
    }

    [Fact]
    public async Task Create_return_is_idempotent_with_return_id()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var returnId = Guid.NewGuid();

        var product = await CreateProductAsync(client, org, "Kape", "Sachet", 8.5m, sku: "ret-kape");
        var sale = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 2m)],
                PosSaleOptions.CashPaymentMethod,
                50m));

        var line = sale.Lines.Single();
        var body = new CreateSaleReturnRequest(
            sale.SaleId,
            "Test",
            [new CreateSaleReturnLineRequest(line.SaleLineId, 1m, "DoNotRestock")],
            ReturnId: returnId);

        var json = JsonSerializer.Serialize(body, JsonOptions);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();

        using var first = PosIntegrationRequest.Scoped(HttpMethod.Post, Returns, org, Actor);
        first.Headers.TryAddWithoutValidation("Idempotency-Key", returnId.ToString("D"));
        first.Headers.TryAddWithoutValidation("X-Pos-Payload-Hash", hash);
        first.Headers.TryAddWithoutValidation("X-Pos-Operation-Id", returnId.ToString("D"));
        first.Headers.TryAddWithoutValidation("X-Pos-Operation-Type", OfflineOperationTypes.SaleReturnCreate);
        first.Content = JsonContent.Create(body, options: JsonOptions);
        using var firstResponse = await client.SendAsync(first);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        var created = await firstResponse.Content.ReadFromJsonAsync<PosSaleReturnDto>(JsonOptions);

        using var second = PosIntegrationRequest.Scoped(HttpMethod.Post, Returns, org, Actor);
        second.Headers.TryAddWithoutValidation("Idempotency-Key", returnId.ToString("D"));
        second.Headers.TryAddWithoutValidation("X-Pos-Payload-Hash", hash);
        second.Headers.TryAddWithoutValidation("X-Pos-Operation-Id", returnId.ToString("D"));
        second.Headers.TryAddWithoutValidation("X-Pos-Operation-Type", OfflineOperationTypes.SaleReturnCreate);
        second.Content = JsonContent.Create(body, options: JsonOptions);
        using var secondResponse = await client.SendAsync(second);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        var replay = await secondResponse.Content.ReadFromJsonAsync<PosSaleReturnDto>(JsonOptions);

        Assert.Equal(created!.ReturnId, replay!.ReturnId);
        Assert.Equal(created.ReturnNumber, replay.ReturnNumber);
    }

    [Fact]
    public async Task Refundable_endpoint_reports_remaining_quantities()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var product = await CreateProductAsync(client, org, "Tinapay", "Piece", 5m, sku: "ret-bread");
        var sale = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 4m)],
                PosSaleOptions.CashPaymentMethod,
                50m));

        using var refundable = PosIntegrationRequest.Scoped(HttpMethod.Get, $"{Returns}/refundable/{sale.SaleId:D}", org, Actor);
        using var response = await client.SendAsync(refundable);
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<PosRefundableSaleDto>(JsonOptions);
        Assert.NotNull(dto);
        Assert.Single(dto!.Lines);
        Assert.Equal(4m, dto.Lines[0].RefundableQuantity);
        Assert.Equal(20m, dto.Lines[0].RefundableAmount);
    }

    private static async Task<PosSaleDto> CheckoutAsync(HttpClient client, Guid org, CheckoutSaleRequest body)
    {
        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, Actor).ConfigureAwait(false);
        using var request = PosIntegrationRequest.Scoped(HttpMethod.Post, Sales, org, Actor);
        request.Content = JsonContent.Create(body, options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PosSaleDto>(JsonOptions))!;
    }

    private static async Task<PosCatalogProductDto> CreateProductAsync(
        HttpClient client,
        Guid org,
        string name,
        string unitOfMeasure,
        decimal sellingPrice,
        string? sku = null)
    {
        using var request = PosIntegrationRequest.Scoped(HttpMethod.Post, Products, org, Actor);
        request.Content = JsonContent.Create(
            new CreatePosCatalogProductRequest(name, unitOfMeasure, sellingPrice, null, sku),
            options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PosCatalogProductDto>(JsonOptions))!;
    }

    private static async Task EnableInventoryAsync(HttpClient client, Guid org, Guid productId)
    {
        using var request = PosIntegrationRequest.Scoped(HttpMethod.Post, $"/api/v1/pos/inventory/{productId:D}/enable", org, Actor);
        request.Content = JsonContent.Create(new EnableInventoryTrackingRequest(100m), options: JsonOptions);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
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

[Collection(PosPostgreSqlCollection.Name)]
public sealed class AddPosSaleReturnsMigrationTests(PosPostgreSqlFixture fixture)
{
    private const string TargetMigration = "AddPosSaleReturns";

    [Fact]
    public async Task AddPosSaleReturns_applies_rolls_back_and_reapplies()
    {
        var options = new DbContextOptionsBuilder<PosDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync();
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.Contains(applied, m => m.Contains(TargetMigration, StringComparison.Ordinal));
        }

        var tables = await QueryNamesAsync(
            """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'pos'
              AND table_name IN ('sale_returns', 'sale_return_lines', 'sale_return_number_sequences')
            """);
        Assert.Contains("sale_returns", tables);
        Assert.Contains("sale_return_lines", tables);
        Assert.Contains("sale_return_number_sequences", tables);

        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync("20260731035548_AddPosCashierShifts");
        }

        Assert.DoesNotContain("sale_returns", await QueryPosTablesAsync());

        await using (var context = new PosDbContext(options))
        {
            await context.Database.MigrateAsync();
        }

        Assert.Contains("sale_returns", await QueryPosTablesAsync());
    }

    private async Task<IReadOnlyList<string>> QueryPosTablesAsync() =>
        await QueryNamesAsync(
            """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'pos'
            """);

    private async Task<IReadOnlyList<string>> QueryNamesAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var names = new List<string>();
        while (await reader.ReadAsync())
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }
}
