using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosInventoryTransferApiTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Guid Actor = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid BranchA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid BranchB = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private const string Inventory = "/api/v1/pos/inventory";
    private const string Products = "/api/v1/pos/catalog/products";

    [Fact]
    public async Task Dispatch_does_not_credit_destination_until_receive_and_retry_is_idempotent()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Coke", "Piece", 25m, "tr-coke");
        await EnableAsync(client, org, product.ProductId, 100m);

        var created = await CreateTransferAsync(client, org, BranchA, BranchB, product.ProductId, 30m);
        Assert.Equal("Draft", created.Status);
        Assert.Equal(100m, await OnHandAsync(client, org, product.ProductId));

        var dispatched = await DispatchAsync(client, org, BranchA, created.TransferId);
        Assert.Equal("InTransit", dispatched.Status);
        Assert.Equal(70m, await OnHandAsync(client, org, product.ProductId));
        Assert.Equal(30m, dispatched.Lines[0].SentQty);

        var received = await ReceiveAsync(
            client,
            org,
            BranchB,
            created.TransferId,
            [new InventoryTransferReceiveLineRequest(product.ProductId, 30m)],
            idempotencyKey: "recv-full-1");
        Assert.Equal("Received", received.Status);
        Assert.Equal(100m, await OnHandAsync(client, org, product.ProductId));
        Assert.Equal(30m, received.Lines[0].ReceivedQty);
        Assert.Equal(30m, received.Lines[0].SentQty);

        var replay = await ReceiveAsync(
            client,
            org,
            BranchB,
            created.TransferId,
            [new InventoryTransferReceiveLineRequest(product.ProductId, 30m)],
            idempotencyKey: "recv-full-1");
        Assert.Equal("Received", replay.Status);
        Assert.Equal(100m, await OnHandAsync(client, org, product.ProductId));

        var second = await ReceiveRawAsync(
            client,
            org,
            BranchB,
            created.TransferId,
            [new InventoryTransferReceiveLineRequest(product.ProductId, 30m)],
            idempotencyKey: "recv-full-2");
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal(100m, await OnHandAsync(client, org, product.ProductId));
    }

    [Fact]
    public async Task Partial_receive_credits_only_received_qty_and_keeps_sent_qty()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var coke = await CreateProductAsync(client, org, "Coke", "Piece", 25m, "tr-coke-p");
        var sprite = await CreateProductAsync(client, org, "Sprite", "Piece", 20m, "tr-sprite-p");
        await EnableAsync(client, org, coke.ProductId, 20m);
        await EnableAsync(client, org, sprite.ProductId, 10m);

        var created = await CreateTransferAsync(
            client,
            org,
            BranchA,
            BranchB,
            lines:
            [
                new InventoryTransferLineRequest(coke.ProductId, 20m),
                new InventoryTransferLineRequest(sprite.ProductId, 10m)
            ]);
        await DispatchAsync(client, org, BranchA, created.TransferId);

        var received = await ReceiveAsync(
            client,
            org,
            BranchB,
            created.TransferId,
            [
                new InventoryTransferReceiveLineRequest(coke.ProductId, 20m),
                new InventoryTransferReceiveLineRequest(sprite.ProductId, 8m, "ShortShipment")
            ]);
        Assert.Equal("PartiallyReceived", received.Status);
        Assert.Equal(20m, received.Lines.Single(l => l.ProductId == coke.ProductId).SentQty);
        Assert.Equal(10m, received.Lines.Single(l => l.ProductId == sprite.ProductId).SentQty);
        Assert.Equal(8m, received.Lines.Single(l => l.ProductId == sprite.ProductId).ReceivedQty);
        Assert.Equal(2m, received.Lines.Single(l => l.ProductId == sprite.ProductId).DifferenceQty);
        Assert.Equal(20m, await OnHandAsync(client, org, coke.ProductId));
        Assert.Equal(8m, await OnHandAsync(client, org, sprite.ProductId));
    }

    [Fact]
    public async Task Wrong_branch_cannot_receive_and_same_branch_create_is_rejected()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Water", "Piece", 15m, "tr-water");
        await EnableAsync(client, org, product.ProductId, 10m);

        using var same = Scoped(HttpMethod.Post, $"{Inventory}/transfers", org, BranchA);
        same.Content = JsonContent.Create(
            new CreateInventoryTransferRequest(
                BranchA,
                BranchA,
                [new InventoryTransferLineRequest(product.ProductId, 1m)]),
            options: JsonOptions);
        using var sameResponse = await client.SendAsync(same);
        Assert.Equal(HttpStatusCode.BadRequest, sameResponse.StatusCode);

        var created = await CreateTransferAsync(client, org, BranchA, BranchB, product.ProductId, 4m);
        await DispatchAsync(client, org, BranchA, created.TransferId);

        var wrong = await ReceiveRawAsync(
            client,
            org,
            BranchA,
            created.TransferId,
            [new InventoryTransferReceiveLineRequest(product.ProductId, 4m)]);
        Assert.Equal(HttpStatusCode.Forbidden, wrong.StatusCode);
        Assert.Equal(6m, await OnHandAsync(client, org, product.ProductId));
    }

    [Fact]
    public async Task Transfer_preserves_lot_identity_and_partial_receive_does_not_duplicate_on_retry()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(
            client,
            org,
            "Milk 1L",
            "Piece",
            50m,
            "tr-milk-lot",
            tracksExpiration: true);
        await EnableAsync(client, org, product.ProductId, 0m);

        var early = new DateOnly(2026, 8, 20);
        var later = new DateOnly(2026, 9, 5);
        await AdjustInAsync(client, org, product.ProductId, 10m, early, "LOT-A");
        await AdjustInAsync(client, org, product.ProductId, 20m, later, "LOT-B");

        var lots = await ListLotsAsync(client, org, product.ProductId);
        var lotA = lots.Single(l => l.ExpirationDate == early);
        var lotB = lots.Single(l => l.ExpirationDate == later);

        var created = await CreateTransferAsync(
            client,
            org,
            BranchA,
            BranchB,
            [
                new InventoryTransferLineRequest(product.ProductId, 4m, lotA.LotId),
                new InventoryTransferLineRequest(product.ProductId, 6m, lotB.LotId)
            ]);
        Assert.Equal(early, created.Lines.Single(l => l.SourceLotId == lotA.LotId).ExpirationDate);
        Assert.Equal(later, created.Lines.Single(l => l.SourceLotId == lotB.LotId).ExpirationDate);

        var dispatched = await DispatchAsync(client, org, BranchA, created.TransferId);
        Assert.Equal("InTransit", dispatched.Status);
        Assert.Equal(20m, await OnHandAsync(client, org, product.ProductId));

        var lineA = dispatched.Lines.Single(l => l.SourceLotId == lotA.LotId);
        var lineB = dispatched.Lines.Single(l => l.SourceLotId == lotB.LotId);
        using var receiveRaw = await ReceiveRawAsync(
            client,
            org,
            BranchB,
            created.TransferId,
            [
                new InventoryTransferReceiveLineRequest(product.ProductId, 3m, "ShortShipment", LineId: lineA.LineId),
                new InventoryTransferReceiveLineRequest(product.ProductId, 6m, LineId: lineB.LineId)
            ],
            idempotencyKey: "recv-lot-1");
        var receiveBody = await receiveRaw.Content.ReadAsStringAsync();
        Assert.True(receiveRaw.IsSuccessStatusCode, receiveBody);
        var received = JsonSerializer.Deserialize<InventoryTransferDto>(receiveBody, JsonOptions);
        Assert.NotNull(received);
        Assert.Equal("PartiallyReceived", received.Status);
        Assert.Equal(3m, received.Lines.Single(l => l.LineId == lineA.LineId).ReceivedQty);
        Assert.Equal(1m, received.Lines.Single(l => l.LineId == lineA.LineId).DifferenceQty);
        Assert.Equal(6m, received.Lines.Single(l => l.LineId == lineB.LineId).ReceivedQty);
        Assert.Equal(early, received.Lines.Single(l => l.LineId == lineA.LineId).ExpirationDate);
        Assert.Equal(later, received.Lines.Single(l => l.LineId == lineB.LineId).ExpirationDate);
        Assert.Equal(29m, await OnHandAsync(client, org, product.ProductId));

        var replay = await ReceiveAsync(
            client,
            org,
            BranchB,
            created.TransferId,
            [
                new InventoryTransferReceiveLineRequest(product.ProductId, 3m, "ShortShipment", LineId: lineA.LineId),
                new InventoryTransferReceiveLineRequest(product.ProductId, 6m, LineId: lineB.LineId)
            ],
            idempotencyKey: "recv-lot-1");
        Assert.Equal("PartiallyReceived", replay.Status);
        Assert.Equal(29m, await OnHandAsync(client, org, product.ProductId));

        var destLots = await ListLotsAsync(client, org, product.ProductId);
        Assert.Equal(3m, destLots.Single(l => l.ExpirationDate == early && l.BranchId == BranchB).QuantityOnHand);
        Assert.Equal(6m, destLots.Single(l => l.ExpirationDate == later && l.BranchId == BranchB).QuantityOnHand);
        Assert.Equal(6m, destLots.Single(l => l.LotId == lotA.LotId).QuantityOnHand);
        Assert.Equal(14m, destLots.Single(l => l.LotId == lotB.LotId).QuantityOnHand);
    }

    private static async Task<PosCatalogProductDto> CreateProductAsync(
        HttpClient client,
        Guid org,
        string name,
        string unitOfMeasure,
        decimal sellingPrice,
        string sku,
        bool tracksExpiration = false)
    {
        using var request = Scoped(HttpMethod.Post, Products, org, BranchA);
        request.Content = JsonContent.Create(
            new CreatePosCatalogProductRequest(
                name,
                unitOfMeasure,
                sellingPrice,
                null,
                sku,
                TracksExpiration: tracksExpiration),
            options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<PosCatalogProductDto>(JsonOptions);
        Assert.NotNull(product);
        return product!;
    }

    private static async Task EnableAsync(HttpClient client, Guid org, Guid productId, decimal opening)
    {
        using var enable = Scoped(HttpMethod.Post, $"{Inventory}/{productId:D}/enable", org, BranchA);
        enable.Content = JsonContent.Create(new EnableInventoryTrackingRequest(opening), options: JsonOptions);
        (await client.SendAsync(enable)).EnsureSuccessStatusCode();
    }

    private static async Task AdjustInAsync(
        HttpClient client,
        Guid org,
        Guid productId,
        decimal qty,
        DateOnly expiry,
        string lotNumber)
    {
        using var adjust = Scoped(HttpMethod.Post, $"{Inventory}/{productId:D}/adjustments", org, BranchA);
        adjust.Content = JsonContent.Create(
            new AdjustInventoryRequest("In", qty, "Receive", ExpirationDate: expiry, LotNumber: lotNumber),
            options: JsonOptions);
        (await client.SendAsync(adjust)).EnsureSuccessStatusCode();
    }

    private static async Task<IReadOnlyList<PosInventoryLotDto>> ListLotsAsync(
        HttpClient client,
        Guid org,
        Guid productId)
    {
        using var get = Scoped(HttpMethod.Get, $"{Inventory}/{productId:D}/lots?includeDepleted=true", org, BranchA);
        using var response = await client.SendAsync(get);
        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<PagedResult<PosInventoryLotDto>>(JsonOptions);
        return page!.Items;
    }

    private static async Task<decimal> OnHandAsync(HttpClient client, Guid org, Guid productId)
    {
        using var get = Scoped(HttpMethod.Get, $"{Inventory}/{productId:D}", org, BranchA);
        using var response = await client.SendAsync(get);
        response.EnsureSuccessStatusCode();
        var account = await response.Content.ReadFromJsonAsync<PosInventoryAccountDto>(JsonOptions);
        return account!.OnHandQuantity;
    }

    private static async Task<InventoryTransferDto> CreateTransferAsync(
        HttpClient client,
        Guid org,
        Guid source,
        Guid dest,
        Guid productId,
        decimal qty) =>
        await CreateTransferAsync(client, org, source, dest, [new InventoryTransferLineRequest(productId, qty)]);

    private static async Task<InventoryTransferDto> CreateTransferAsync(
        HttpClient client,
        Guid org,
        Guid source,
        Guid dest,
        IReadOnlyList<InventoryTransferLineRequest> lines)
    {
        using var request = Scoped(HttpMethod.Post, $"{Inventory}/transfers", org, source);
        request.Content = JsonContent.Create(new CreateInventoryTransferRequest(source, dest, lines), options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<InventoryTransferDto>(JsonOptions);
        Assert.NotNull(dto);
        return dto!;
    }

    private static async Task<InventoryTransferDto> DispatchAsync(
        HttpClient client,
        Guid org,
        Guid source,
        Guid transferId)
    {
        using var request = Scoped(HttpMethod.Post, $"{Inventory}/transfers/{transferId:D}/dispatch", org, source);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<InventoryTransferDto>(JsonOptions);
        Assert.NotNull(dto);
        return dto!;
    }

    private static async Task<InventoryTransferDto> ReceiveAsync(
        HttpClient client,
        Guid org,
        Guid dest,
        Guid transferId,
        IReadOnlyList<InventoryTransferReceiveLineRequest> lines,
        string? idempotencyKey = null)
    {
        using var response = await ReceiveRawAsync(client, org, dest, transferId, lines, idempotencyKey);
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<InventoryTransferDto>(JsonOptions);
        Assert.NotNull(dto);
        return dto!;
    }

    private static async Task<HttpResponseMessage> ReceiveRawAsync(
        HttpClient client,
        Guid org,
        Guid dest,
        Guid transferId,
        IReadOnlyList<InventoryTransferReceiveLineRequest> lines,
        string? idempotencyKey = null)
    {
        var body = new ReceiveInventoryTransferRequest(lines);
        var request = Scoped(HttpMethod.Post, $"{Inventory}/transfers/{transferId:D}/receive", org, dest);
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var json = JsonSerializer.Serialize(body, JsonOptions);
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
            request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
            request.Headers.TryAddWithoutValidation("X-Pos-Payload-Hash", hash);
            request.Headers.TryAddWithoutValidation("X-Pos-Operation-Type", "inventory_transfer.receive");
        }

        request.Content = JsonContent.Create(body, options: JsonOptions);
        return await client.SendAsync(request);
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
