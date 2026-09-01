using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Offline;
using static ExItS.PinoyBusinessPOS.IntegrationTests.PosInventoryOpsIntegrationSupport;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosStockUseApiTests(PosPostgreSqlFixture fixture)
{
    [Fact]
    public async Task Create_decreases_on_hand_persists_movement_cost_reason_and_history()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Stock Use Soap");
        await EnableTrackedAsync(client, org, product.ProductId, openingQuantity: 20m, unitCost: 12.5m);

        var occurred = new DateTimeOffset(2026, 8, 30, 10, 0, 0, TimeSpan.Zero);
        var body = new CreateStockUseRequest(
            "InternalOperations",
            [new CreateStockUseLineRequest(product.ProductId, 4m)],
            ReferenceNumber: "SU-REF-1",
            Notes: "Kitchen sample",
            OccurredAtUtc: occurred);

        using var create = Scoped(HttpMethod.Post, StockUses, org);
        create.Content = JsonContent.Create(body, options: JsonOptions);
        using var createResponse = await client.SendAsync(create);
        var createBody = await createResponse.Content.ReadAsStringAsync();
        Assert.True(
            createResponse.StatusCode == HttpStatusCode.Created,
            $"Expected Created, got {createResponse.StatusCode}: {createBody}");
        var created = JsonSerializer.Deserialize<StockUseDto>(createBody, JsonOptions);
        Assert.NotNull(created);
        Assert.Equal("Posted", created!.Status);
        Assert.Equal("InternalOperations", created.Reason);
        Assert.Equal("Kitchen sample", created.Notes);
        Assert.Equal("SU-REF-1", created.ReferenceNumber);
        Assert.Equal(BranchA, created.BranchId);
        Assert.Equal(org, created.OrganizationId);
        var line = Assert.Single(created.Lines);
        Assert.Equal(4m, line.BaseQuantity);
        Assert.Equal(12.5m, line.UnitCostSnapshot);
        Assert.Equal(50m, line.LineCostSnapshot);
        Assert.NotNull(line.InventoryMovementId);

        Assert.Equal(16m, await OnHandAsync(client, org, product.ProductId));
        var movements = await MovementsAsync(client, org, product.ProductId);
        var stockUseMovement = Assert.Single(movements, m => m.MovementType == "StockUse");
        Assert.Equal(-4m, stockUseMovement.QuantityEffect);
        Assert.Equal(12.5m, stockUseMovement.UnitCost);
        Assert.Equal(line.InventoryMovementId, stockUseMovement.MovementId);

        using var get = Scoped(HttpMethod.Get, $"{StockUses}/{created.StockUseId:D}", org);
        using var getResponse = await client.SendAsync(get);
        getResponse.EnsureSuccessStatusCode();
        var fetched = await getResponse.Content.ReadFromJsonAsync<StockUseDto>(JsonOptions);
        Assert.Equal(created.StockUseId, fetched!.StockUseId);

        using var list = Scoped(HttpMethod.Get, $"{StockUses}?page=1&pageSize=20", org);
        using var listResponse = await client.SendAsync(list);
        listResponse.EnsureSuccessStatusCode();
        var page = await listResponse.Content.ReadFromJsonAsync<PagedResult<StockUseListItemDto>>(JsonOptions);
        Assert.Contains(page!.Items, i => i.StockUseId == created.StockUseId);
    }

    [Fact]
    public async Task Branch_scoped_create_requires_branch_balance_and_persists_branch_id()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Branch Stock Use");
        await EnableTrackedAsync(client, org, product.ProductId, openingQuantity: 0m);
        await SeedBranchStockAsync(client, org, product.ProductId, BranchA, 10m);

        using var create = Scoped(HttpMethod.Post, StockUses, org, branchId: BranchA);
        create.Content = JsonContent.Create(
            new CreateStockUseRequest(
                "StaffUse",
                [new CreateStockUseLineRequest(product.ProductId, 3m)],
                BranchId: BranchA),
            options: JsonOptions);
        using var response = await client.SendAsync(create);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<StockUseDto>(JsonOptions);
        Assert.Equal(BranchA, dto!.BranchId);
        Assert.Equal(7m, await OnHandAsync(client, org, product.ProductId));
    }

    [Fact]
    public async Task Unknown_cost_stays_null_and_profitability_keeps_stock_use_out_of_sale_cogs()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var known = await CreateProductAsync(client, org, "Known Cost Flour");
        await EnableTrackedAsync(client, org, known.ProductId, openingQuantity: 10m, unitCost: 5m);

        var unknown = await CreateProductAsync(client, org, "Unknown Cost Oil");
        await EnableTrackedAsync(client, org, unknown.ProductId, openingQuantity: 0m);
        await AdjustInWithoutCostAsync(client, org, unknown.ProductId, 10m);

        var occurred = new DateTimeOffset(2026, 8, 30, 11, 0, 0, TimeSpan.Zero);
        using var knownCreate = Scoped(HttpMethod.Post, StockUses, org);
        knownCreate.Content = JsonContent.Create(
            new CreateStockUseRequest(
                "StaffUse",
                [new CreateStockUseLineRequest(known.ProductId, 2m)],
                OccurredAtUtc: occurred),
            options: JsonOptions);
        using var knownResponse = await client.SendAsync(knownCreate);
        Assert.Equal(HttpStatusCode.Created, knownResponse.StatusCode);
        var knownDto = await knownResponse.Content.ReadFromJsonAsync<StockUseDto>(JsonOptions);
        Assert.Equal(5m, knownDto!.Lines[0].UnitCostSnapshot);

        using var unknownCreate = Scoped(HttpMethod.Post, StockUses, org);
        unknownCreate.Content = JsonContent.Create(
            new CreateStockUseRequest(
                "Other",
                [new CreateStockUseLineRequest(unknown.ProductId, 3m)],
                OccurredAtUtc: occurred),
            options: JsonOptions);
        using var unknownResponse = await client.SendAsync(unknownCreate);
        Assert.Equal(HttpStatusCode.Created, unknownResponse.StatusCode);
        var unknownDto = await unknownResponse.Content.ReadFromJsonAsync<StockUseDto>(JsonOptions);
        Assert.Null(unknownDto!.Lines[0].UnitCostSnapshot);
        Assert.Null(unknownDto.Lines[0].LineCostSnapshot);
        Assert.Null(Assert.Single(await MovementsAsync(client, org, unknown.ProductId), m => m.MovementType == "StockUse").UnitCost);

        var report = await GetProfitabilityAsync(client, org, new DateOnly(2026, 8, 30), new DateOnly(2026, 8, 30));
        Assert.Equal(10m, report.StockUseKnownCost);
        Assert.Equal(0m, report.KnownCogs);
        Assert.Equal(0m, report.WasteLossKnownCost);
        Assert.Equal(0, report.CompletedSaleCount);
    }

    [Fact]
    public async Task Cross_org_insufficient_stock_permission_and_idempotent_replay()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();

        var product = await CreateProductAsync(client, orgA, "Guarded Soap");
        await EnableTrackedAsync(client, orgA, product.ProductId, openingQuantity: 5m, unitCost: 2m);

        using var cross = Scoped(HttpMethod.Post, StockUses, orgB);
        cross.Content = JsonContent.Create(
            new CreateStockUseRequest(
                "InternalOperations",
                [new CreateStockUseLineRequest(product.ProductId, 1m)]),
            options: JsonOptions);
        using var crossResponse = await client.SendAsync(cross);
        Assert.False(crossResponse.IsSuccessStatusCode);
        Assert.Equal(5m, await OnHandAsync(client, orgA, product.ProductId));

        using var insufficient = Scoped(HttpMethod.Post, StockUses, orgA);
        insufficient.Content = JsonContent.Create(
            new CreateStockUseRequest(
                "InternalOperations",
                [new CreateStockUseLineRequest(product.ProductId, 50m)]),
            options: JsonOptions);
        using var insufficientResponse = await client.SendAsync(insufficient);
        Assert.Equal(HttpStatusCode.Conflict, insufficientResponse.StatusCode);
        Assert.Equal(5m, await OnHandAsync(client, orgA, product.ProductId));

        using var viewOnly = Scoped(
            HttpMethod.Post,
            StockUses,
            orgA,
            status: PosSubscriptionStatuses.Active,
            grants: PosFeatureCodes.StoreInventoryView);
        viewOnly.Content = JsonContent.Create(
            new CreateStockUseRequest(
                "InternalOperations",
                [new CreateStockUseLineRequest(product.ProductId, 1m)]),
            options: JsonOptions);
        using var viewOnlyResponse = await client.SendAsync(viewOnly);
        Assert.Equal(HttpStatusCode.Forbidden, viewOnlyResponse.StatusCode);
        Assert.Equal(5m, await OnHandAsync(client, orgA, product.ProductId));

        await BootstrapOwnerAsync(client, orgA, OwnerActor);
        await AssignRoleAsync(client, orgA, OwnerActor, ReporterActor, "ReportingUser");
        using var reporterDenied = Scoped(HttpMethod.Post, StockUses, orgA, ReporterActor);
        reporterDenied.Content = JsonContent.Create(
            new CreateStockUseRequest(
                "InternalOperations",
                [new CreateStockUseLineRequest(product.ProductId, 1m)]),
            options: JsonOptions);
        using var reporterResponse = await client.SendAsync(reporterDenied);
        Assert.Equal(HttpStatusCode.Forbidden, reporterResponse.StatusCode);
        Assert.Equal(5m, await OnHandAsync(client, orgA, product.ProductId));

        var body = new CreateStockUseRequest(
            "SampleOrTesting",
            [new CreateStockUseLineRequest(product.ProductId, 2m)],
            IdempotencyKey: "stock-use-idem-1");
        var hash = ComputePayloadHash(body);

        using var first = Scoped(HttpMethod.Post, StockUses, orgA, OwnerActor);
        first.Content = JsonContent.Create(body, options: JsonOptions);
        AttachIdempotency(first, "stock-use-idem-1", hash, OfflineOperationTypes.StockUse);
        using var firstResponse = await client.SendAsync(first);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        var firstDto = await firstResponse.Content.ReadFromJsonAsync<StockUseDto>(JsonOptions);
        Assert.Equal(3m, await OnHandAsync(client, orgA, product.ProductId));

        using var replay = Scoped(HttpMethod.Post, StockUses, orgA, OwnerActor);
        replay.Content = JsonContent.Create(body, options: JsonOptions);
        AttachIdempotency(replay, "stock-use-idem-1", hash, OfflineOperationTypes.StockUse);
        using var replayResponse = await client.SendAsync(replay);
        Assert.Equal(HttpStatusCode.OK, replayResponse.StatusCode);
        var replayDto = await replayResponse.Content.ReadFromJsonAsync<StockUseDto>(JsonOptions);
        Assert.Equal(firstDto!.StockUseId, replayDto!.StockUseId);
        Assert.Equal(3m, await OnHandAsync(client, orgA, product.ProductId));
        Assert.Equal(1, (await MovementsAsync(client, orgA, product.ProductId)).Count(m => m.MovementType == "StockUse"));
    }

    [Fact]
    public async Task Opening_stock_then_branch_scoped_stock_use_succeeds_without_preseed()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Opening Then Stock Use");
        await EnableTrackedAsync(client, org, product.ProductId, openingQuantity: 20m, unitCost: 4m);
        Assert.Equal(20m, await OnHandAsync(client, org, product.ProductId));

        using var create = Scoped(HttpMethod.Post, StockUses, org, branchId: BranchA);
        create.Content = JsonContent.Create(
            new CreateStockUseRequest(
                "InternalOperations",
                [new CreateStockUseLineRequest(product.ProductId, 5m)],
                BranchId: BranchA),
            options: JsonOptions);
        using var response = await client.SendAsync(create);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(
            response.StatusCode == HttpStatusCode.Created,
            $"Expected Created after opening stock, got {response.StatusCode}: {body}");
        Assert.Equal(15m, await OnHandAsync(client, org, product.ProductId));
        var dto = await response.Content.ReadFromJsonAsync<StockUseDto>(JsonOptions);
        Assert.Equal(BranchA, dto!.BranchId);
    }

    [Fact]
    public async Task Direct_purchase_then_branch_scoped_stock_use_succeeds()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "DP Then Stock Use");
        await EnableTrackedAsync(client, org, product.ProductId, openingQuantity: 0m);

        using var dp = Scoped(HttpMethod.Post, "/api/v1/pos/direct-purchase-receipts", org);
        dp.Content = JsonContent.Create(
            new CreateDirectPurchaseReceiptRequest(
                DateOnly.FromDateTime(DateTime.UtcNow),
                [new CreateDirectPurchaseReceiptLineRequest(product.ProductId, 10m, 7m)],
                SourceName: "Cash market",
                PaidNow: 70m,
                PaymentMethodAtReceipt: "Cash",
                IdempotencyKey: Guid.NewGuid().ToString("D")),
            options: JsonOptions);
        using var dpResponse = await client.SendAsync(dp);
        Assert.Equal(HttpStatusCode.Created, dpResponse.StatusCode);
        Assert.Equal(10m, await OnHandAsync(client, org, product.ProductId));

        using var create = Scoped(HttpMethod.Post, StockUses, org, branchId: BranchA);
        create.Content = JsonContent.Create(
            new CreateStockUseRequest(
                "StaffUse",
                [new CreateStockUseLineRequest(product.ProductId, 3m)],
                BranchId: BranchA),
            options: JsonOptions);
        using var response = await client.SendAsync(create);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(
            response.StatusCode == HttpStatusCode.Created,
            $"Expected Created after DP, got {response.StatusCode}: {body}");
        Assert.Equal(7m, await OnHandAsync(client, org, product.ProductId));
    }

    [Fact]
    public async Task After_primary_branch_materializes_balance_other_branch_cannot_spend_unallocated()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Multi Branch Guard");
        await EnableTrackedAsync(client, org, product.ProductId, openingQuantity: 20m, unitCost: 2m);

        using var first = Scoped(HttpMethod.Post, StockUses, org, branchId: BranchA);
        first.Content = JsonContent.Create(
            new CreateStockUseRequest(
                "InternalOperations",
                [new CreateStockUseLineRequest(product.ProductId, 5m)],
                BranchId: BranchA),
            options: JsonOptions);
        using var firstResponse = await client.SendAsync(first);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(15m, await OnHandAsync(client, org, product.ProductId));

        using var second = Scoped(HttpMethod.Post, StockUses, org, branchId: BranchB);
        second.Content = JsonContent.Create(
            new CreateStockUseRequest(
                "InternalOperations",
                [new CreateStockUseLineRequest(product.ProductId, 1m)],
                BranchId: BranchB),
            options: JsonOptions);
        using var secondResponse = await client.SendAsync(second);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
        Assert.Equal(15m, await OnHandAsync(client, org, product.ProductId));
    }

    [Fact]
    public async Task Invalid_branch_product_scope_fails_closed_via_org_isolation()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();
        var productB = await CreateProductAsync(client, orgB, "OrgB Only");
        await EnableTrackedAsync(client, orgB, productB.ProductId, openingQuantity: 8m, unitCost: 3m);

        using var create = Scoped(HttpMethod.Post, StockUses, orgA, branchId: BranchA);
        create.Content = JsonContent.Create(
            new CreateStockUseRequest(
                "InternalOperations",
                [new CreateStockUseLineRequest(productB.ProductId, 1m)],
                BranchId: BranchA),
            options: JsonOptions);
        using var response = await client.SendAsync(create);
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(8m, await OnHandAsync(client, orgB, productB.ProductId));
    }
}
