using System.Net;
using System.Net.Http.Json;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Offline;
using static ExItS.PinoyBusinessPOS.IntegrationTests.PosInventoryOpsIntegrationSupport;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosProductionApiTests(PosPostgreSqlFixture fixture)
{
    [Fact]
    public async Task Definition_and_run_consume_materials_produce_output_with_authoritative_cost()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var flour = await CreateProductAsync(
            client,
            org,
            "Flour",
            canBeUsedAsIngredient: true,
            usagePreset: "Ingredient");
        await EnableTrackedAsync(client, org, flour.ProductId, openingQuantity: 50m, unitCost: 10m);

        var bread = await CreateProductAsync(
            client,
            org,
            "Bread Loaf",
            isProduced: true,
            usagePreset: "MadeProduct");
        await EnableTrackedAsync(client, org, bread.ProductId, openingQuantity: 0m);

        using var defReq = Scoped(HttpMethod.Post, ProductionDefinitions, org);
        defReq.Content = JsonContent.Create(
            new CreateProductionDefinitionRequest(
                "Bread batch",
                bread.ProductId,
                OutputQuantity: 100m,
                Components: [new CreateProductionComponentRequest(flour.ProductId, 10m)]),
            options: JsonOptions);
        using var defResponse = await client.SendAsync(defReq);
        Assert.Equal(HttpStatusCode.Created, defResponse.StatusCode);
        var definition = await defResponse.Content.ReadFromJsonAsync<ProductionDefinitionDto>(JsonOptions);
        Assert.NotNull(definition);
        Assert.True(definition!.IsActive);
        Assert.Equal(bread.ProductId, definition.OutputProductId);
        Assert.Equal(10m, Assert.Single(definition.Components).BaseQuantity);

        var producedAt = new DateTimeOffset(2026, 8, 30, 14, 0, 0, TimeSpan.Zero);
        var runBody = new CreateProductionRunRequest(
            definition.ProductionDefinitionId,
            OutputQuantity: 200m,
            ReferenceNumber: "PR-REF-1",
            Notes: "Morning bake",
            ProducedAtUtc: producedAt);
        using var runReq = Scoped(HttpMethod.Post, ProductionRuns, org);
        runReq.Content = JsonContent.Create(runBody, options: JsonOptions);
        using var runResponse = await client.SendAsync(runReq);
        Assert.Equal(HttpStatusCode.Created, runResponse.StatusCode);
        var run = await runResponse.Content.ReadFromJsonAsync<ProductionRunDto>(JsonOptions);
        Assert.NotNull(run);
        Assert.Equal("Posted", run!.Status);
        Assert.Equal("Complete", run.CostStatus);
        Assert.Equal(200m, run.OutputBaseQuantity);
        Assert.Equal(20m, Assert.Single(run.Materials).ActualBaseQuantity);
        Assert.Equal(200m, run.TotalMaterialCost);
        Assert.Equal(1m, run.OutputBaseUnitCost);
        Assert.Equal(BranchA, run.BranchId);
        Assert.Equal(org, run.OrganizationId);
        Assert.NotNull(run.OutputInventoryMovementId);
        Assert.NotNull(run.Materials[0].InventoryMovementId);

        Assert.Equal(30m, await OnHandAsync(client, org, flour.ProductId));
        Assert.Equal(200m, await OnHandAsync(client, org, bread.ProductId));

        var flourMoves = await MovementsAsync(client, org, flour.ProductId);
        Assert.Contains(flourMoves, m => m.MovementType == "ProductionMaterialConsumption" && m.QuantityEffect == -20m);
        Assert.DoesNotContain(flourMoves, m => m.MovementType is "StockUse" or "SaleDeduction" or "WasteLoss");

        var breadMoves = await MovementsAsync(client, org, bread.ProductId);
        var output = Assert.Single(breadMoves, m => m.MovementType == "ProductionOutput");
        Assert.Equal(200m, output.QuantityEffect);
        Assert.Equal(1m, output.UnitCost);

        using var list = Scoped(HttpMethod.Get, $"{ProductionRuns}?page=1&pageSize=20", org);
        using var listResponse = await client.SendAsync(list);
        listResponse.EnsureSuccessStatusCode();
        var page = await listResponse.Content.ReadFromJsonAsync<PagedResult<ProductionRunListItemDto>>(JsonOptions);
        Assert.Contains(page!.Items, i => i.ProductionRunId == run.ProductionRunId);

        using var stockUse = Scoped(HttpMethod.Post, StockUses, org);
        stockUse.Content = JsonContent.Create(
            new CreateStockUseRequest(
                "InternalOperations",
                [new CreateStockUseLineRequest(bread.ProductId, 5m)],
                OccurredAtUtc: producedAt),
            options: JsonOptions);
        using var stockUseResponse = await client.SendAsync(stockUse);
        Assert.Equal(HttpStatusCode.Created, stockUseResponse.StatusCode);
        var stockUseDto = await stockUseResponse.Content.ReadFromJsonAsync<StockUseDto>(JsonOptions);
        Assert.Equal(1m, stockUseDto!.Lines[0].UnitCostSnapshot);
    }

    [Fact]
    public async Task Insufficient_materials_partial_unknown_cost_and_no_double_movement()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var known = await CreateProductAsync(client, org, "Sugar", canBeUsedAsIngredient: true, usagePreset: "Ingredient");
        await EnableTrackedAsync(client, org, known.ProductId, openingQuantity: 5m, unitCost: 2m);

        var unknown = await CreateProductAsync(client, org, "Yeast", canBeUsedAsIngredient: true, usagePreset: "Ingredient");
        await EnableTrackedAsync(client, org, unknown.ProductId, openingQuantity: 0m);
        await AdjustInWithoutCostAsync(client, org, unknown.ProductId, 20m);

        var cake = await CreateProductAsync(client, org, "Cake", isProduced: true, usagePreset: "MadeProduct");
        await EnableTrackedAsync(client, org, cake.ProductId, openingQuantity: 0m);

        using var defReq = Scoped(HttpMethod.Post, ProductionDefinitions, org);
        defReq.Content = JsonContent.Create(
            new CreateProductionDefinitionRequest(
                "Cake recipe",
                cake.ProductId,
                OutputQuantity: 10m,
                Components:
                [
                    new CreateProductionComponentRequest(known.ProductId, 2m),
                    new CreateProductionComponentRequest(unknown.ProductId, 1m)
                ]),
            options: JsonOptions);
        using var defResponse = await client.SendAsync(defReq);
        defResponse.EnsureSuccessStatusCode();
        var definition = await defResponse.Content.ReadFromJsonAsync<ProductionDefinitionDto>(JsonOptions);

        using var insufficient = Scoped(HttpMethod.Post, ProductionRuns, org);
        insufficient.Content = JsonContent.Create(
            new CreateProductionRunRequest(definition!.ProductionDefinitionId, OutputQuantity: 100m),
            options: JsonOptions);
        using var insufficientResponse = await client.SendAsync(insufficient);
        Assert.Equal(HttpStatusCode.Conflict, insufficientResponse.StatusCode);
        Assert.Equal(5m, await OnHandAsync(client, org, known.ProductId));
        Assert.Equal(20m, await OnHandAsync(client, org, unknown.ProductId));
        Assert.Equal(0m, await OnHandAsync(client, org, cake.ProductId));

        using var partial = Scoped(HttpMethod.Post, ProductionRuns, org);
        partial.Content = JsonContent.Create(
            new CreateProductionRunRequest(definition.ProductionDefinitionId, OutputQuantity: 10m),
            options: JsonOptions);
        using var partialResponse = await client.SendAsync(partial);
        Assert.Equal(HttpStatusCode.Created, partialResponse.StatusCode);
        var run = await partialResponse.Content.ReadFromJsonAsync<ProductionRunDto>(JsonOptions);
        Assert.Equal("Partial", run!.CostStatus);
        Assert.Equal(4m, run.TotalMaterialCost);
        Assert.Null(run.OutputBaseUnitCost);
        Assert.Equal(3m, await OnHandAsync(client, org, known.ProductId));
        Assert.Equal(19m, await OnHandAsync(client, org, unknown.ProductId));
        Assert.Equal(10m, await OnHandAsync(client, org, cake.ProductId));
        Assert.Equal(1, (await MovementsAsync(client, org, cake.ProductId)).Count(m => m.MovementType == "ProductionOutput"));
    }

    [Fact]
    public async Task Cross_org_permission_and_idempotent_replay_without_nested_bom()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();

        var flour = await CreateProductAsync(client, orgA, "OrgA Flour", canBeUsedAsIngredient: true, usagePreset: "Ingredient");
        await EnableTrackedAsync(client, orgA, flour.ProductId, openingQuantity: 40m, unitCost: 3m);
        var bread = await CreateProductAsync(client, orgA, "OrgA Bread", isProduced: true, usagePreset: "MadeProduct");
        await EnableTrackedAsync(client, orgA, bread.ProductId, openingQuantity: 0m);

        using var defReq = Scoped(HttpMethod.Post, ProductionDefinitions, orgA);
        defReq.Content = JsonContent.Create(
            new CreateProductionDefinitionRequest(
                "Simple loaf",
                bread.ProductId,
                OutputQuantity: 10m,
                Components: [new CreateProductionComponentRequest(flour.ProductId, 5m)]),
            options: JsonOptions);
        using var defResponse = await client.SendAsync(defReq);
        defResponse.EnsureSuccessStatusCode();
        var definition = await defResponse.Content.ReadFromJsonAsync<ProductionDefinitionDto>(JsonOptions);

        using var cross = Scoped(HttpMethod.Post, ProductionRuns, orgB);
        cross.Content = JsonContent.Create(
            new CreateProductionRunRequest(definition!.ProductionDefinitionId, OutputQuantity: 10m),
            options: JsonOptions);
        using var crossResponse = await client.SendAsync(cross);
        Assert.False(crossResponse.IsSuccessStatusCode);
        Assert.Equal(40m, await OnHandAsync(client, orgA, flour.ProductId));

        using var viewOnly = Scoped(
            HttpMethod.Post,
            ProductionRuns,
            orgA,
            status: PosSubscriptionStatuses.Active,
            grants: PosFeatureCodes.StoreInventoryView);
        viewOnly.Content = JsonContent.Create(
            new CreateProductionRunRequest(definition.ProductionDefinitionId, OutputQuantity: 10m),
            options: JsonOptions);
        using var viewOnlyResponse = await client.SendAsync(viewOnly);
        Assert.Equal(HttpStatusCode.Forbidden, viewOnlyResponse.StatusCode);

        await BootstrapOwnerAsync(client, orgA, OwnerActor);
        await AssignRoleAsync(client, orgA, OwnerActor, ReporterActor, "ReportingUser");
        using var reporterDenied = Scoped(HttpMethod.Post, ProductionRuns, orgA, ReporterActor);
        reporterDenied.Content = JsonContent.Create(
            new CreateProductionRunRequest(definition.ProductionDefinitionId, OutputQuantity: 10m),
            options: JsonOptions);
        using var reporterResponse = await client.SendAsync(reporterDenied);
        Assert.Equal(HttpStatusCode.Forbidden, reporterResponse.StatusCode);

        var body = new CreateProductionRunRequest(
            definition.ProductionDefinitionId,
            OutputQuantity: 10m,
            IdempotencyKey: "prod-idem-1");
        var hash = ComputePayloadHash(body);

        using var first = Scoped(HttpMethod.Post, ProductionRuns, orgA, OwnerActor);
        first.Content = JsonContent.Create(body, options: JsonOptions);
        AttachIdempotency(first, "prod-idem-1", hash, OfflineOperationTypes.ProductionRun);
        using var firstResponse = await client.SendAsync(first);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        var firstDto = await firstResponse.Content.ReadFromJsonAsync<ProductionRunDto>(JsonOptions);
        Assert.Equal(35m, await OnHandAsync(client, orgA, flour.ProductId));
        Assert.Equal(10m, await OnHandAsync(client, orgA, bread.ProductId));

        using var replay = Scoped(HttpMethod.Post, ProductionRuns, orgA, OwnerActor);
        replay.Content = JsonContent.Create(body, options: JsonOptions);
        AttachIdempotency(replay, "prod-idem-1", hash, OfflineOperationTypes.ProductionRun);
        using var replayResponse = await client.SendAsync(replay);
        Assert.Equal(HttpStatusCode.OK, replayResponse.StatusCode);
        var replayDto = await replayResponse.Content.ReadFromJsonAsync<ProductionRunDto>(JsonOptions);
        Assert.Equal(firstDto!.ProductionRunId, replayDto!.ProductionRunId);
        Assert.Equal(35m, await OnHandAsync(client, orgA, flour.ProductId));
        Assert.Equal(10m, await OnHandAsync(client, orgA, bread.ProductId));
        Assert.Equal(
            1,
            (await MovementsAsync(client, orgA, flour.ProductId)).Count(m => m.MovementType == "ProductionMaterialConsumption"));
        Assert.Equal(
            1,
            (await MovementsAsync(client, orgA, bread.ProductId)).Count(m => m.MovementType == "ProductionOutput"));
    }
}
