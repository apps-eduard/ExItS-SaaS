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
        Assert.Equal(100m, await OrgOnHandAsync(client, org, product.ProductId));
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
        Assert.Equal(100m, await OrgOnHandAsync(client, org, product.ProductId));

        var second = await ReceiveRawAsync(
            client,
            org,
            BranchB,
            created.TransferId,
            [new InventoryTransferReceiveLineRequest(product.ProductId, 30m)],
            idempotencyKey: "recv-full-2");
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal(100m, await OrgOnHandAsync(client, org, product.ProductId));
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
        Assert.Equal(20m, await OnHandAsync(client, org, coke.ProductId, BranchB));
        Assert.Equal(8m, await OnHandAsync(client, org, sprite.ProductId, BranchB));
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

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var early = today.AddDays(30);
        var later = today.AddDays(60);
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
        Assert.Equal(29m, await OrgOnHandAsync(client, org, product.ProductId));

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
        Assert.Equal(29m, await OrgOnHandAsync(client, org, product.ProductId));

        // Lot list is branch-scoped (exact BranchId); query destination and source separately.
        var destLots = await ListLotsAsync(client, org, product.ProductId, BranchB);
        Assert.Equal(3m, destLots.Single(l => l.ExpirationDate == early && l.BranchId == BranchB).QuantityOnHand);
        Assert.Equal(6m, destLots.Single(l => l.ExpirationDate == later && l.BranchId == BranchB).QuantityOnHand);
        var sourceLots = await ListLotsAsync(client, org, product.ProductId, BranchA);
        Assert.Equal(6m, sourceLots.Single(l => l.LotId == lotA.LotId).QuantityOnHand);
        Assert.Equal(14m, sourceLots.Single(l => l.LotId == lotB.LotId).QuantityOnHand);
    }

    [Fact]
    public async Task WrongVariant_receive_corrects_source_holds_destination_and_supports_return_inspect()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var red = await CreateProductAsync(client, org, "Apple Red", "Kilogram", 50m, $"tr-red-{org:N}"[..20]);
        var green = await CreateProductAsync(client, org, "Apple Green", "Kilogram", 50m, $"tr-grn-{org:N}"[..20]);
        await EnableAsync(client, org, red.ProductId, 100m);
        await EnableAsync(client, org, green.ProductId, 50m);

        var transfer = await CreateTransferAsync(client, org, BranchA, BranchB, red.ProductId, 10m);
        transfer = await DispatchAsync(client, org, BranchA, transfer.TransferId);
        Assert.Equal(90m, await OnHandAsync(client, org, red.ProductId, BranchA));
        Assert.Equal(50m, await OnHandAsync(client, org, green.ProductId, BranchA));

        transfer = await ReceiveAsync(
            client,
            org,
            BranchB,
            transfer.TransferId,
            [
                new InventoryTransferReceiveLineRequest(
                    red.ProductId,
                    GoodQty: 5m,
                    OtherQty: 5m,
                    OtherReasonCode: "WrongVariant",
                    OtherFollowUp: "RequestReplacement",
                    ActualReceivedProductId: green.ProductId,
                    OtherCustodyDecision: "ReturnToSource")
            ],
            idempotencyKey: $"recv-wv-{org:N}");

        Assert.Equal(95m, await OnHandAsync(client, org, red.ProductId, BranchA));
        Assert.Equal(45m, await OnHandAsync(client, org, green.ProductId, BranchA));
        Assert.Equal(5m, await OnHandAsync(client, org, red.ProductId, BranchB));
        Assert.Equal(5m, await AvailableAsync(client, org, red.ProductId, BranchB));
        Assert.Equal(5m, await OnHandAsync(client, org, green.ProductId, BranchB));
        Assert.Equal(0m, await AvailableAsync(client, org, green.ProductId, BranchB));
        Assert.Equal(5m, transfer.RemainingToDispatchQty);
        Assert.Equal(0m, transfer.WaivedQty);

        var custody = Assert.Single(transfer.ExceptionCustodies ?? []);
        Assert.Equal("WrongVariant", custody.ReasonCode);
        Assert.Equal(red.ProductId, custody.ExpectedProductId);
        Assert.Equal(green.ProductId, custody.ActualProductId);
        Assert.Equal("ReturnToSource", custody.Decision);
        Assert.Equal(5m, custody.ReplacementDemandQty);

        // Idempotent replay must not duplicate corrections.
        var replay = await ReceiveAsync(
            client,
            org,
            BranchB,
            transfer.TransferId,
            [
                new InventoryTransferReceiveLineRequest(
                    red.ProductId,
                    GoodQty: 5m,
                    OtherQty: 5m,
                    OtherReasonCode: "WrongVariant",
                    OtherFollowUp: "RequestReplacement",
                    ActualReceivedProductId: green.ProductId,
                    OtherCustodyDecision: "ReturnToSource")
            ],
            idempotencyKey: $"recv-wv-{org:N}");
        Assert.Equal(95m, await OnHandAsync(client, org, red.ProductId, BranchA));
        Assert.Equal(45m, await OnHandAsync(client, org, green.ProductId, BranchA));
        Assert.Single(replay.ExceptionCustodies ?? []);

        using (var dispatchReturn = Scoped(
                   HttpMethod.Post,
                   $"{Inventory}/transfers/exception-custodies/{custody.CustodyId:D}/dispatch-return",
                   org,
                   BranchB))
        {
            using var dispatchResponse = await client.SendAsync(dispatchReturn);
            Assert.True(dispatchResponse.IsSuccessStatusCode, await dispatchResponse.Content.ReadAsStringAsync());
        }

        Assert.Equal(0m, await OnHandAsync(client, org, green.ProductId, BranchB));
        Assert.Equal(0m, await AvailableAsync(client, org, green.ProductId, BranchB));

        using (var receiveReturn = Scoped(
                   HttpMethod.Post,
                   $"{Inventory}/transfers/exception-custodies/{custody.CustodyId:D}/receive-return",
                   org,
                   BranchA))
        {
            using var receiveResponse = await client.SendAsync(receiveReturn);
            Assert.True(receiveResponse.IsSuccessStatusCode, await receiveResponse.Content.ReadAsStringAsync());
        }

        Assert.Equal(50m, await OnHandAsync(client, org, green.ProductId, BranchA));
        Assert.Equal(50m, await AvailableAsync(client, org, green.ProductId, BranchA));

        transfer = await GetTransferAsync(client, org, BranchA, transfer.TransferId);
        var afterReceive = Assert.Single(transfer.ExceptionCustodies ?? []);
        Assert.Equal("ReceivedAtSource", afterReceive.Status);
        Assert.NotNull(afterReceive.ReturnReceivedAtUtc);

        // Idempotent re-receive must not restock again.
        using (var receiveReturnAgain = Scoped(
                   HttpMethod.Post,
                   $"{Inventory}/transfers/exception-custodies/{custody.CustodyId:D}/receive-return",
                   org,
                   BranchA))
        {
            using var receiveAgainResponse = await client.SendAsync(receiveReturnAgain);
            Assert.True(receiveAgainResponse.IsSuccessStatusCode, await receiveAgainResponse.Content.ReadAsStringAsync());
        }

        Assert.Equal(50m, await OnHandAsync(client, org, green.ProductId, BranchA));
        Assert.Equal(50m, await AvailableAsync(client, org, green.ProductId, BranchA));
        transfer = await GetTransferAsync(client, org, BranchA, transfer.TransferId);
        Assert.Equal("ReceivedAtSource", Assert.Single(transfer.ExceptionCustodies ?? []).Status);

        // Wrong variant must not use source inspection — receive return already restored sellable.
        using (var inspect = Scoped(
                   HttpMethod.Post,
                   $"{Inventory}/transfers/exception-custodies/{custody.CustodyId:D}/inspect",
                   org,
                   BranchA))
        {
            inspect.Content = JsonContent.Create(
                new InspectInventoryTransferExceptionCustodyRequest(5m, 0m),
                options: JsonOptions);
            using var inspectResponse = await client.SendAsync(inspect);
            Assert.Equal(HttpStatusCode.BadRequest, inspectResponse.StatusCode);
        }

        Assert.Equal(50m, await OnHandAsync(client, org, green.ProductId, BranchA));
        Assert.Equal(50m, await AvailableAsync(client, org, green.ProductId, BranchA));
        Assert.Equal(5m, transfer.RemainingToDispatchQty);
    }

    [Fact]
    public async Task WrongVariant_AcceptShortage_clears_remaining_and_rejects_KeepAtDestination()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var red = await CreateProductAsync(client, org, "Apple Red", "Kilogram", 50m, $"tr-red2-{org:N}"[..20]);
        var green = await CreateProductAsync(client, org, "Apple Green", "Kilogram", 50m, $"tr-grn2-{org:N}"[..20]);
        await EnableAsync(client, org, red.ProductId, 100m);
        await EnableAsync(client, org, green.ProductId, 50m);

        var transfer = await CreateTransferAsync(client, org, BranchA, BranchB, red.ProductId, 10m);
        transfer = await DispatchAsync(client, org, BranchA, transfer.TransferId);

        var keepRejected = await ReceiveRawAsync(
            client,
            org,
            BranchB,
            transfer.TransferId,
            [
                new InventoryTransferReceiveLineRequest(
                    red.ProductId,
                    GoodQty: 5m,
                    OtherQty: 5m,
                    OtherReasonCode: "WrongVariant",
                    OtherFollowUp: "AcceptShortage",
                    ActualReceivedProductId: green.ProductId,
                    OtherCustodyDecision: "KeepAtDestination")
            ]);
        Assert.Equal(HttpStatusCode.BadRequest, keepRejected.StatusCode);

        transfer = await ReceiveAsync(
            client,
            org,
            BranchB,
            transfer.TransferId,
            [
                new InventoryTransferReceiveLineRequest(
                    red.ProductId,
                    GoodQty: 5m,
                    OtherQty: 5m,
                    OtherReasonCode: "WrongVariant",
                    OtherFollowUp: "AcceptShortage",
                    ActualReceivedProductId: green.ProductId,
                    OtherCustodyDecision: "ReturnToSource")
            ],
            idempotencyKey: $"recv-wv-as-{org:N}");

        Assert.Equal(0m, transfer.RemainingToDispatchQty);
        Assert.Equal(5m, transfer.WaivedQty);
        var custody = Assert.Single(transfer.ExceptionCustodies ?? []);
        Assert.Equal(0m, custody.ReplacementDemandQty);
        Assert.Equal("ReturnToSource", custody.Decision);
        Assert.Equal(5m, await OnHandAsync(client, org, green.ProductId, BranchB));
        Assert.Equal(0m, await AvailableAsync(client, org, green.ProductId, BranchB));
    }

    [Fact]
    public async Task Replacement_chain_R2_AcceptShortage_persists_waived_qty_and_clears_remaining()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Apple", "Kilogram", 50m, $"tr-apple-{org:N}"[..20]);
        await EnableAsync(client, org, product.ProductId, 100m);

        var root = await CreateTransferAsync(client, org, BranchA, BranchB, product.ProductId, 10m);
        root = await DispatchAsync(client, org, BranchA, root.TransferId);
        root = await ReceiveAsync(
            client,
            org,
            BranchB,
            root.TransferId,
            [
                new InventoryTransferReceiveLineRequest(
                    product.ProductId,
                    GoodQty: 5m,
                    DamagedQty: 5m,
                    DamagedFollowUp: "RequestReplacement",
                    DamagedCustodyDecision: "KeepAtDestination")
            ],
            idempotencyKey: $"recv-root-{org:N}");
        Assert.Equal("ClosedWithDiscrepancy", root.Status);
        Assert.Equal(5m, root.RemainingToDispatchQty);
        Assert.Equal(0m, root.WaivedQty);

        var r1 = await PrepareRemainingAsync(client, org, BranchA, root.TransferId, $"prep-r1-{org:N}");
        Assert.Equal(5m, r1.TotalSentQty);
        r1 = await DispatchAsync(client, org, BranchA, r1.TransferId);
        r1 = await ReceiveAsync(
            client,
            org,
            BranchB,
            r1.TransferId,
            [
                new InventoryTransferReceiveLineRequest(
                    product.ProductId,
                    GoodQty: 1m,
                    DamagedQty: 4m,
                    DamagedFollowUp: "RequestReplacement",
                    DamagedCustodyDecision: "KeepAtDestination")
            ],
            idempotencyKey: $"recv-r1-{org:N}");
        Assert.Equal(4m, r1.RemainingToDispatchQty);

        var r2 = await PrepareRemainingAsync(client, org, BranchA, root.TransferId, $"prep-r2-{org:N}");
        Assert.Equal(4m, r2.TotalSentQty);
        r2 = await DispatchAsync(client, org, BranchA, r2.TransferId);
        r2 = await ReceiveAsync(
            client,
            org,
            BranchB,
            r2.TransferId,
            [
                new InventoryTransferReceiveLineRequest(
                    product.ProductId,
                    GoodQty: 2m,
                    MissingQty: 2m,
                    MissingDisposition: "AcceptShortage")
            ],
            idempotencyKey: $"recv-r2-{org:N}");

        var r2Line = Assert.Single(r2.Lines);
        Assert.Equal(4m, r2Line.SentQty);
        Assert.Equal(2m, r2Line.ReceivedQty);
        Assert.Equal(2m, r2Line.ClosedQty);
        Assert.Equal(2m, r2Line.WaivedQty);
        Assert.Equal(0m, r2Line.OutstandingQty);

        var r2ReceiptLine = Assert.Single(Assert.Single(r2.Receipts).Lines);
        Assert.Equal(2m, r2ReceiptLine.QuantityReceived);
        Assert.Equal(2m, r2ReceiptLine.QuantityMissing);
        Assert.Equal("AcceptShortage", r2ReceiptLine.MissingDisposition);
        Assert.Equal(2m, r2ReceiptLine.QuantityWaived);

        // Re-fetch to prove EF persisted WaivedQty (not only in-memory receive result).
        using var get = Scoped(HttpMethod.Get, $"{Inventory}/transfers/{r2.TransferId:D}", org, BranchA);
        using var getResponse = await client.SendAsync(get);
        getResponse.EnsureSuccessStatusCode();
        var reloaded = await getResponse.Content.ReadFromJsonAsync<InventoryTransferDto>(JsonOptions);
        Assert.NotNull(reloaded);
        Assert.Equal(2m, Assert.Single(reloaded!.Lines).WaivedQty);
        Assert.Equal(8m, reloaded.SatisfiedAtDestinationQty);
        Assert.Equal(2m, reloaded.WaivedQty);
        Assert.Equal(0m, reloaded.OpenInTransitQty);
        Assert.Equal(0m, reloaded.RemainingToDispatchQty);

        using var blockedResponse = await PrepareRemainingRawAsync(
            client,
            org,
            BranchA,
            root.TransferId,
            $"prep-r3-{org:N}");
        Assert.False(blockedResponse.IsSuccessStatusCode);
        Assert.True(
            (int)blockedResponse.StatusCode is >= 400 and < 500,
            $"Unexpected status {(int)blockedResponse.StatusCode}");
    }

    private static async Task<InventoryTransferDto> PrepareRemainingAsync(
        HttpClient client,
        Guid org,
        Guid source,
        Guid transferId,
        string idempotencyKey)
    {
        using var response = await PrepareRemainingRawAsync(client, org, source, transferId, idempotencyKey);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);
        var dto = JsonSerializer.Deserialize<InventoryTransferDto>(body, JsonOptions);
        Assert.NotNull(dto);
        return dto!;
    }

    private static async Task<HttpResponseMessage> PrepareRemainingRawAsync(
        HttpClient client,
        Guid org,
        Guid source,
        Guid transferId,
        string idempotencyKey)
    {
        var payload = "{}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
        var request = Scoped(HttpMethod.Post, $"{Inventory}/transfers/{transferId:D}/prepare-remaining", org, source);
        request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
        request.Headers.TryAddWithoutValidation("X-Pos-Payload-Hash", hash);
        request.Headers.TryAddWithoutValidation("X-Pos-Operation-Type", "inventory_transfer.create");
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        return await client.SendAsync(request);
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
        enable.Content = JsonContent.Create(
            opening > 0m
                ? new EnableInventoryTrackingRequest(OpeningQuantity: opening, UnitCost: 1m)
                : new EnableInventoryTrackingRequest(OpeningQuantity: opening),
            options: JsonOptions);
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
        Guid productId,
        Guid? branchId = null)
    {
        using var get = Scoped(
            HttpMethod.Get,
            $"{Inventory}/{productId:D}/lots?includeDepleted=true",
            org,
            branchId ?? BranchA);
        using var response = await client.SendAsync(get);
        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<PagedResult<PosInventoryLotDto>>(JsonOptions);
        return page!.Items;
    }

    [Fact]
    public async Task List_all_and_detail_are_scoped_to_acting_branch()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var branchPanay = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var product = await CreateProductAsync(client, org, "Scope Coke", "Piece", 25m, "tr-scope");
        await EnableAsync(client, org, product.ProductId, 200m);

        // Unrelated to Panay — must never appear in Panay scoped list/detail.
        var mainToIloilo = await CreateTransferAsync(client, org, BranchA, BranchB, product.ProductId, 10m);
        var mainToPanay = await CreateTransferAsync(client, org, BranchA, branchPanay, product.ProductId, 11m);
        await DispatchAsync(client, org, BranchA, mainToPanay.TransferId);
        await ReceiveAsync(
            client,
            org,
            branchPanay,
            mainToPanay.TransferId,
            [new InventoryTransferReceiveLineRequest(product.ProductId, 11m)]);
        var panayToIloilo = await CreateTransferAsync(client, org, branchPanay, BranchB, product.ProductId, 5m);

        using var allPanay = Scoped(HttpMethod.Get, $"{Inventory}/transfers", org, branchPanay);
        using var allPanayResponse = await client.SendAsync(allPanay);
        allPanayResponse.EnsureSuccessStatusCode();
        var panayAll = await allPanayResponse.Content.ReadFromJsonAsync<PagedResult<InventoryTransferListItemDto>>(JsonOptions);
        Assert.NotNull(panayAll);
        Assert.Equal(2, panayAll!.TotalCount);
        Assert.DoesNotContain(panayAll.Items, t => t.TransferId == mainToIloilo.TransferId);
        Assert.Contains(panayAll.Items, t => t.TransferId == mainToPanay.TransferId);
        Assert.Contains(panayAll.Items, t => t.TransferId == panayToIloilo.TransferId);

        using var outPanay = Scoped(HttpMethod.Get, $"{Inventory}/transfers?direction=outgoing", org, branchPanay);
        using var outResponse = await client.SendAsync(outPanay);
        var panayOut = await outResponse.Content.ReadFromJsonAsync<PagedResult<InventoryTransferListItemDto>>(JsonOptions);
        Assert.Single(panayOut!.Items);
        Assert.Equal(panayToIloilo.TransferId, panayOut.Items[0].TransferId);

        var inboundDraft = await CreateTransferAsync(client, org, BranchA, branchPanay, product.ProductId, 3m);
        await DispatchAsync(client, org, BranchA, inboundDraft.TransferId);
        using var inResponse = await client.SendAsync(
            Scoped(HttpMethod.Get, $"{Inventory}/transfers?direction=incoming", org, branchPanay));
        var panayIn = await inResponse.Content.ReadFromJsonAsync<PagedResult<InventoryTransferListItemDto>>(JsonOptions);
        Assert.Contains(panayIn!.Items, t => t.TransferId == inboundDraft.TransferId);
        Assert.DoesNotContain(panayIn.Items, t => t.TransferId == mainToIloilo.TransferId);

        using var foreignDetail = Scoped(HttpMethod.Get, $"{Inventory}/transfers/{mainToIloilo.TransferId:D}", org, branchPanay);
        using var foreignResponse = await client.SendAsync(foreignDetail);
        Assert.Equal(HttpStatusCode.NotFound, foreignResponse.StatusCode);

        using var involvedDetail = Scoped(HttpMethod.Get, $"{Inventory}/transfers/{panayToIloilo.TransferId:D}", org, branchPanay);
        using var involvedResponse = await client.SendAsync(involvedDetail);
        involvedResponse.EnsureSuccessStatusCode();
    }

    private static async Task<decimal> OnHandAsync(HttpClient client, Guid org, Guid productId, Guid? branchId = null)
    {
        var account = await AccountAsync(client, org, productId, branchId);
        return account.OnHandQuantity;
    }

    private static async Task<decimal> AvailableAsync(HttpClient client, Guid org, Guid productId, Guid? branchId = null)
    {
        var account = await AccountAsync(client, org, productId, branchId);
        return account.AvailableQuantity;
    }

    private static async Task<PosInventoryAccountDto> AccountAsync(
        HttpClient client,
        Guid org,
        Guid productId,
        Guid? branchId = null)
    {
        using var get = Scoped(HttpMethod.Get, $"{Inventory}/{productId:D}", org, branchId ?? BranchA);
        using var response = await client.SendAsync(get);
        response.EnsureSuccessStatusCode();
        var account = await response.Content.ReadFromJsonAsync<PosInventoryAccountDto>(JsonOptions);
        Assert.NotNull(account);
        return account!;
    }

    private static async Task<decimal> OrgOnHandAsync(HttpClient client, Guid org, Guid productId)
    {
        using var get = Scoped(HttpMethod.Get, $"{Inventory}/{productId:D}/organization-summary", org, BranchA);
        using var response = await client.SendAsync(get);
        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<PosOrganizationInventoryProductDto>(JsonOptions);
        return summary!.OrganizationOnHandQuantity;
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

    private static async Task<InventoryTransferDto> GetTransferAsync(
        HttpClient client,
        Guid org,
        Guid branchId,
        Guid transferId)
    {
        using var request = Scoped(HttpMethod.Get, $"{Inventory}/transfers/{transferId:D}", org, branchId);
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
