using System.Net;
using System.Net.Http.Json;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Offline;
using static ExItS.PinoyBusinessPOS.IntegrationTests.PosInventoryOpsIntegrationSupport;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosWasteLossApiTests(PosPostgreSqlFixture fixture)
{
    [Fact]
    public async Task Create_decreases_on_hand_persists_reason_cost_and_expired_lot_path()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var product = await CreateProductAsync(client, org, "Waste Milk", tracksExpiration: true);
        await EnableTrackedAsync(
            client,
            org,
            product.ProductId,
            openingQuantity: 12m,
            unitCost: 20m,
            expirationDate: new DateOnly(2026, 7, 1),
            lotNumber: "LOT-EXP-1");
        var lot = Assert.Single(await ListLotsAsync(client, org, product.ProductId));

        var occurred = new DateTimeOffset(2026, 8, 30, 12, 0, 0, TimeSpan.Zero);
        var body = new CreateWasteLossRequest(
            "Expired",
            [new CreateWasteLossLineRequest(product.ProductId, 3m, InventoryLotId: lot.LotId)],
            ReferenceNumber: "WL-REF-1",
            Notes: "Expired fridge stock",
            OccurredAtUtc: occurred);

        using var create = Scoped(HttpMethod.Post, WasteLosses, org);
        create.Content = JsonContent.Create(body, options: JsonOptions);
        using var createResponse = await client.SendAsync(create);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<WasteLossDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal("Posted", created!.Status);
        Assert.Equal("Expired", created.Reason);
        Assert.Equal("Complete", created.CostStatus);
        Assert.Equal(60m, created.TotalCostSnapshot);
        Assert.Null(created.BranchId);
        Assert.Equal(org, created.OrganizationId);
        var line = Assert.Single(created.Lines);
        Assert.Equal(lot.LotId, line.InventoryLotId);
        Assert.Equal(20m, line.UnitCostSnapshot);
        Assert.NotNull(line.InventoryMovementId);

        Assert.Equal(9m, await OnHandAsync(client, org, product.ProductId));
        var movement = Assert.Single(await MovementsAsync(client, org, product.ProductId), m => m.MovementType == "WasteLoss");
        Assert.Equal(-3m, movement.QuantityEffect);
        Assert.Equal(20m, movement.UnitCost);

        using var list = Scoped(HttpMethod.Get, $"{WasteLosses}?page=1&pageSize=20", org);
        using var listResponse = await client.SendAsync(list);
        listResponse.EnsureSuccessStatusCode();
        var page = await listResponse.Content.ReadFromJsonAsync<PagedResult<WasteLossListItemDto>>(JsonOptions);
        Assert.Contains(page!.Items, i => i.WasteLossId == created.WasteLossId);
    }

    [Fact]
    public async Task Unknown_cost_unavailable_and_profitability_separation_from_cogs()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var known = await CreateProductAsync(client, org, "Known Waste Item");
        await EnableTrackedAsync(client, org, known.ProductId, openingQuantity: 10m, unitCost: 4m);

        var unknown = await CreateProductAsync(client, org, "Unknown Waste Item");
        await EnableTrackedAsync(client, org, unknown.ProductId, openingQuantity: 0m);
        await AdjustInWithoutCostAsync(client, org, unknown.ProductId, 10m);

        var occurred = new DateTimeOffset(2026, 8, 30, 13, 0, 0, TimeSpan.Zero);
        using var knownCreate = Scoped(HttpMethod.Post, WasteLosses, org);
        knownCreate.Content = JsonContent.Create(
            new CreateWasteLossRequest(
                "Spoiled",
                [new CreateWasteLossLineRequest(known.ProductId, 2m)],
                OccurredAtUtc: occurred),
            options: JsonOptions);
        using var knownResponse = await client.SendAsync(knownCreate);
        Assert.Equal(HttpStatusCode.Created, knownResponse.StatusCode);
        var knownDto = await knownResponse.Content.ReadFromJsonAsync<WasteLossDto>(JsonOptions);
        Assert.Equal("Complete", knownDto!.CostStatus);
        Assert.Equal(8m, knownDto.TotalCostSnapshot);

        using var unknownCreate = Scoped(HttpMethod.Post, WasteLosses, org);
        unknownCreate.Content = JsonContent.Create(
            new CreateWasteLossRequest(
                "MissingOrShrinkage",
                [new CreateWasteLossLineRequest(unknown.ProductId, 1m)],
                OccurredAtUtc: occurred),
            options: JsonOptions);
        using var unknownResponse = await client.SendAsync(unknownCreate);
        Assert.Equal(HttpStatusCode.Created, unknownResponse.StatusCode);
        var unknownDto = await unknownResponse.Content.ReadFromJsonAsync<WasteLossDto>(JsonOptions);
        Assert.Equal("Unavailable", unknownDto!.CostStatus);
        Assert.Null(unknownDto.TotalCostSnapshot);
        Assert.Null(unknownDto.Lines[0].UnitCostSnapshot);

        var report = await GetProfitabilityAsync(client, org, new DateOnly(2026, 8, 30), new DateOnly(2026, 8, 30));
        Assert.Equal(8m, report.WasteLossKnownCost);
        Assert.Equal(0m, report.StockUseKnownCost);
        Assert.Equal(0m, report.KnownCogs);
        Assert.Equal(0, report.CompletedSaleCount);
    }

    [Fact]
    public async Task Cross_org_insufficient_permission_and_idempotent_replay()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();

        var product = await CreateProductAsync(client, orgA, "Waste Guard");
        await EnableTrackedAsync(client, orgA, product.ProductId, openingQuantity: 6m, unitCost: 7m);

        using var cross = Scoped(HttpMethod.Post, WasteLosses, orgB);
        cross.Content = JsonContent.Create(
            new CreateWasteLossRequest(
                "Damaged",
                [new CreateWasteLossLineRequest(product.ProductId, 1m)]),
            options: JsonOptions);
        using var crossResponse = await client.SendAsync(cross);
        Assert.False(crossResponse.IsSuccessStatusCode);
        Assert.Equal(6m, await OnHandAsync(client, orgA, product.ProductId));

        using var insufficient = Scoped(HttpMethod.Post, WasteLosses, orgA);
        insufficient.Content = JsonContent.Create(
            new CreateWasteLossRequest(
                "Broken",
                [new CreateWasteLossLineRequest(product.ProductId, 100m)]),
            options: JsonOptions);
        using var insufficientResponse = await client.SendAsync(insufficient);
        Assert.Equal(HttpStatusCode.Conflict, insufficientResponse.StatusCode);
        Assert.Equal(6m, await OnHandAsync(client, orgA, product.ProductId));

        using var viewOnly = Scoped(
            HttpMethod.Post,
            WasteLosses,
            orgA,
            status: PosSubscriptionStatuses.Active,
            grants: PosFeatureCodes.StoreInventoryView);
        viewOnly.Content = JsonContent.Create(
            new CreateWasteLossRequest(
                "Spillage",
                [new CreateWasteLossLineRequest(product.ProductId, 1m)]),
            options: JsonOptions);
        using var viewOnlyResponse = await client.SendAsync(viewOnly);
        Assert.Equal(HttpStatusCode.Forbidden, viewOnlyResponse.StatusCode);

        await BootstrapOwnerAsync(client, orgA, OwnerActor);
        await AssignRoleAsync(client, orgA, OwnerActor, ReporterActor, "ReportingUser");
        using var reporterDenied = Scoped(HttpMethod.Post, WasteLosses, orgA, ReporterActor);
        reporterDenied.Content = JsonContent.Create(
            new CreateWasteLossRequest(
                "Other",
                [new CreateWasteLossLineRequest(product.ProductId, 1m)]),
            options: JsonOptions);
        using var reporterResponse = await client.SendAsync(reporterDenied);
        Assert.Equal(HttpStatusCode.Forbidden, reporterResponse.StatusCode);
        Assert.Equal(6m, await OnHandAsync(client, orgA, product.ProductId));

        var body = new CreateWasteLossRequest(
            "Damaged",
            [new CreateWasteLossLineRequest(product.ProductId, 2m)],
            IdempotencyKey: "waste-idem-1");
        var hash = ComputePayloadHash(body);

        using var first = Scoped(HttpMethod.Post, WasteLosses, orgA, OwnerActor);
        first.Content = JsonContent.Create(body, options: JsonOptions);
        AttachIdempotency(first, "waste-idem-1", hash, OfflineOperationTypes.WasteLoss);
        using var firstResponse = await client.SendAsync(first);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        var firstDto = await firstResponse.Content.ReadFromJsonAsync<WasteLossDto>(JsonOptions);
        Assert.Equal(4m, await OnHandAsync(client, orgA, product.ProductId));

        using var replay = Scoped(HttpMethod.Post, WasteLosses, orgA, OwnerActor);
        replay.Content = JsonContent.Create(body, options: JsonOptions);
        AttachIdempotency(replay, "waste-idem-1", hash, OfflineOperationTypes.WasteLoss);
        using var replayResponse = await client.SendAsync(replay);
        Assert.Equal(HttpStatusCode.OK, replayResponse.StatusCode);
        var replayDto = await replayResponse.Content.ReadFromJsonAsync<WasteLossDto>(JsonOptions);
        Assert.Equal(firstDto!.WasteLossId, replayDto!.WasteLossId);
        Assert.Equal(4m, await OnHandAsync(client, orgA, product.ProductId));
        Assert.Equal(1, (await MovementsAsync(client, orgA, product.ProductId)).Count(m => m.MovementType == "WasteLoss"));
    }

    [Fact]
    public async Task Invalid_branch_scope_fails_closed_when_product_belongs_to_other_org()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();
        var productB = await CreateProductAsync(client, orgB, "Foreign Waste");
        await EnableTrackedAsync(client, orgB, productB.ProductId, openingQuantity: 5m, unitCost: 1m);

        using var create = Scoped(HttpMethod.Post, WasteLosses, orgA, branchId: BranchA);
        create.Content = JsonContent.Create(
            new CreateWasteLossRequest(
                "Damaged",
                [new CreateWasteLossLineRequest(productB.ProductId, 1m)],
                BranchId: BranchA),
            options: JsonOptions);
        using var response = await client.SendAsync(create);
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(5m, await OnHandAsync(client, orgB, productB.ProductId));
    }
}
