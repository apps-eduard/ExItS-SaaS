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
using ExItS.PinoyBusinessPOS.Application.Permissions;
using ExItS.PinoyBusinessPOS.Application.Reporting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

/// <summary>Shared seed/API helpers for Stock Use / Waste-Loss / Production Postgres API tests.</summary>
internal static class PosInventoryOpsIntegrationSupport
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static readonly Guid OwnerActor = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid ReporterActor = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public static readonly Guid BranchA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid BranchB = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public const string Inventory = "/api/v1/pos/inventory";
    public const string Products = "/api/v1/pos/catalog/products";
    public const string StockUses = "/api/v1/pos/inventory/stock-uses";
    public const string WasteLosses = "/api/v1/pos/inventory/waste-losses";
    public const string ProductionDefinitions = "/api/v1/pos/inventory/production/definitions";
    public const string ProductionRuns = "/api/v1/pos/inventory/production/runs";
    public const string Permissions = "/api/v1/pos/permissions";
    public const string Profitability = "/api/v1/pos/reports/profitability";

    public static HttpRequestMessage Scoped(
        HttpMethod method,
        string path,
        Guid organizationId,
        Guid? actorId = null,
        Guid? branchId = null,
        string? status = null,
        string? grants = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation(
            PosOrganizationHeaders.OrganizationHeaderName,
            organizationId.ToString("D"));
        request.Headers.TryAddWithoutValidation(
            PosOrganizationHeaders.ActorHeaderName,
            (actorId ?? OwnerActor).ToString("D"));
        if (branchId is Guid branch)
        {
            request.Headers.TryAddWithoutValidation(
                PosOrganizationHeaders.BranchHeaderName,
                branch.ToString("D"));
        }

        if (status is not null)
        {
            request.Headers.TryAddWithoutValidation(PosCommercialHeaders.SubscriptionStatusHeaderName, status);
        }

        if (grants is not null)
        {
            request.Headers.TryAddWithoutValidation(PosCommercialHeaders.FeatureGrantsHeaderName, grants);
        }

        return request;
    }

    public static string ComputePayloadHash<T>(T body)
    {
        var json = JsonSerializer.Serialize(body, JsonOptions);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
    }

    public static void AttachIdempotency(HttpRequestMessage request, string key, string payloadHash, string operationType)
    {
        request.Headers.TryAddWithoutValidation("Idempotency-Key", key);
        request.Headers.TryAddWithoutValidation("X-Pos-Payload-Hash", payloadHash);
        request.Headers.TryAddWithoutValidation("X-Pos-Operation-Type", operationType);
    }

    public static async Task<PosCatalogProductDto> CreateProductAsync(
        HttpClient client,
        Guid org,
        string name,
        bool? canBeUsedAsIngredient = null,
        bool? isProduced = null,
        string? usagePreset = null,
        bool tracksExpiration = false,
        Guid? actorId = null)
    {
        using var request = Scoped(HttpMethod.Post, Products, org, actorId);
        request.Content = JsonContent.Create(
            new CreatePosCatalogProductRequest(
                name,
                "Piece",
                28m,
                Sku: $"sku-{Guid.NewGuid():N}"[..20],
                TracksExpiration: tracksExpiration,
                ExpirationWarningDays: tracksExpiration ? 7 : null,
                CanBeUsedAsIngredient: canBeUsedAsIngredient,
                IsProduced: isProduced,
                UsagePreset: usagePreset),
            options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PosCatalogProductDto>(JsonOptions))!;
    }

    public static async Task EnableTrackedAsync(
        HttpClient client,
        Guid org,
        Guid productId,
        decimal openingQuantity,
        decimal? unitCost = null,
        DateOnly? expirationDate = null,
        string? lotNumber = null,
        Guid? actorId = null)
    {
        using var enable = Scoped(HttpMethod.Post, $"{Inventory}/{productId:D}/enable", org, actorId);
        enable.Content = JsonContent.Create(
            openingQuantity > 0m
                ? new EnableInventoryTrackingRequest(
                    OpeningQuantity: openingQuantity,
                    UnitCost: unitCost ?? 1m,
                    ExpirationDate: expirationDate,
                    LotNumber: lotNumber)
                : new EnableInventoryTrackingRequest(OpeningQuantity: openingQuantity),
            options: JsonOptions);
        using var response = await client.SendAsync(enable);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    public static async Task AdjustInWithoutCostAsync(
        HttpClient client,
        Guid org,
        Guid productId,
        decimal quantity,
        Guid? actorId = null)
    {
        using var adjust = Scoped(HttpMethod.Post, $"{Inventory}/{productId:D}/adjustments", org, actorId);
        adjust.Content = JsonContent.Create(
            new AdjustInventoryRequest("In", quantity, "Seed without acquisition cost"),
            options: JsonOptions);
        using var response = await client.SendAsync(adjust);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    public static async Task SeedBranchStockAsync(
        HttpClient client,
        Guid org,
        Guid productId,
        Guid branchId,
        decimal quantity,
        Guid? actorId = null)
    {
        using var adjust = Scoped(
            HttpMethod.Post,
            $"{Inventory}/{productId:D}/adjustments",
            org,
            actorId,
            branchId);
        adjust.Content = JsonContent.Create(
            new AdjustInventoryRequest("In", quantity, "Seed branch stock"),
            options: JsonOptions);
        using var response = await client.SendAsync(adjust);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    public static async Task<decimal> OnHandAsync(HttpClient client, Guid org, Guid productId, Guid? actorId = null)
    {
        using var get = Scoped(HttpMethod.Get, $"{Inventory}/{productId:D}", org, actorId);
        using var response = await client.SendAsync(get);
        response.EnsureSuccessStatusCode();
        var account = await response.Content.ReadFromJsonAsync<PosInventoryAccountDto>(JsonOptions);
        return account!.OnHandQuantity;
    }

    public static async Task<IReadOnlyList<PosStockMovementDto>> MovementsAsync(
        HttpClient client,
        Guid org,
        Guid productId,
        Guid? actorId = null)
    {
        using var get = Scoped(HttpMethod.Get, $"{Inventory}/{productId:D}/movements?page=1&pageSize=100", org, actorId);
        using var response = await client.SendAsync(get);
        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<PagedResult<PosStockMovementDto>>(JsonOptions);
        return page!.Items;
    }

    public static async Task BootstrapOwnerAsync(HttpClient client, Guid org, Guid actorId)
    {
        using var effective = Scoped(HttpMethod.Get, $"{Permissions}/effective", org, actorId);
        using var response = await client.SendAsync(effective);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
    }

    public static async Task AssignRoleAsync(HttpClient client, Guid org, Guid ownerActor, Guid targetActor, string role)
    {
        using var assign = Scoped(HttpMethod.Post, $"{Permissions}/assignments", org, ownerActor);
        assign.Content = JsonContent.Create(new AssignPosRoleRequest(targetActor, role), options: JsonOptions);
        using var response = await client.SendAsync(assign);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    public static async Task<PosProfitabilityReportDto> GetProfitabilityAsync(
        HttpClient client,
        Guid org,
        DateOnly fromDate,
        DateOnly toDate,
        Guid? actorId = null)
    {
        using var get = Scoped(
            HttpMethod.Get,
            $"{Profitability}?fromDate={fromDate:yyyy-MM-dd}&toDate={toDate:yyyy-MM-dd}",
            org,
            actorId);
        using var response = await client.SendAsync(get);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PosProfitabilityReportDto>(JsonOptions))!;
    }

    public static async Task<IReadOnlyList<PosInventoryLotDto>> ListLotsAsync(
        HttpClient client,
        Guid org,
        Guid productId,
        Guid? actorId = null)
    {
        using var get = Scoped(HttpMethod.Get, $"{Inventory}/{productId:D}/lots?includeDepleted=true", org, actorId);
        using var response = await client.SendAsync(get);
        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<PagedResult<PosInventoryLotDto>>(JsonOptions);
        return page!.Items;
    }

    public sealed class PosApiFactory(string connectionString) : WebApplicationFactory<Program>
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
