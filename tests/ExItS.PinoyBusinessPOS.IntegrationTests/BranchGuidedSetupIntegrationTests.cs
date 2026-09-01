using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Branches;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using static ExItS.PinoyBusinessPOS.IntegrationTests.PosInventoryOpsIntegrationSupport;
using H1ProofBranchDirectoryOptions = ExItS.PinoyBusinessPOS.IntegrationTests.Support.H1ProofBranchDirectoryOptions;
using H1ProofOrganizationBranchDirectory = ExItS.PinoyBusinessPOS.IntegrationTests.Support.H1ProofOrganizationBranchDirectory;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

/// <summary>MB2-05 Mica C guided branch setup readiness E2E proof.</summary>
[Collection(PosPostgreSqlCollection.Name)]
public sealed class BranchGuidedSetupIntegrationTests(PosPostgreSqlFixture fixture)
{
    private readonly string _connectionString = fixture.ConnectionString;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Guid Main = BranchA;
    private static readonly Guid MicaC = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public async Task MICA_C_readiness_and_setup_progress_round_trip()
    {
        var branchOptions = new H1ProofBranchDirectoryOptions { PrimaryBranchId = Main };
        await using var factory = new GuidedSetupApiFactory(_connectionString, branchOptions);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        branchOptions.RegisterOrganization(org, Main, MicaC);

        var product = await CreateProductAsync(client, org, "Mica C Setup Item");
        await EnableTrackedAsync(client, org, product.ProductId, 5m, branchId: MicaC);

        using var readiness = Scoped(HttpMethod.Get, $"/api/v1/pos/branches/{MicaC:D}/readiness", org, MicaC);
        using var readinessResponse = await client.SendAsync(readiness);
        readinessResponse.EnsureSuccessStatusCode();
        var readinessDto = await readinessResponse.Content.ReadFromJsonAsync<BranchReadinessDto>(JsonOptions);
        Assert.NotNull(readinessDto);
        Assert.Equal(MicaC, readinessDto!.BranchId);
        Assert.Contains(readinessDto.Sections, s => s.Key == "Products");

        using var progress = Scoped(HttpMethod.Put, $"/api/v1/pos/branches/{MicaC:D}/setup-progress", org, MicaC);
        progress.Content = JsonContent.Create(
            new UpsertBranchSetupProgressRequest("Parties", MarkCompleted: false),
            options: JsonOptions);
        using var progressResponse = await client.SendAsync(progress);
        progressResponse.EnsureSuccessStatusCode();

        await using var db = new PosDbContext(
            new DbContextOptionsBuilder<PosDbContext>().UseNpgsql(_connectionString).Options);
        var row = await db.BranchSetupProgress.SingleAsync(r => r.OrganizationId == org && r.BranchId == MicaC);
        Assert.Equal("Parties", row.LastVisitedStep);
        Assert.NotNull(row.StartedAtUtc);
    }

    private static HttpRequestMessage Scoped(HttpMethod method, string path, Guid org, Guid branchId)
    {
        var request = PosIntegrationRequest.Scoped(method, path, org, OwnerActor);
        request.Headers.TryAddWithoutValidation(PosOrganizationHeaders.BranchHeaderName, branchId.ToString("D"));
        request.Headers.TryAddWithoutValidation(PosCommercialHeaders.SubscriptionStatusHeaderName, PosSubscriptionStatuses.Active);
        request.Headers.TryAddWithoutValidation(PosCommercialHeaders.FeatureGrantsHeaderName, PosFeatureCodes.StoreCatalogView);
        return request;
    }

    private sealed class GuidedSetupApiFactory(string connectionString, H1ProofBranchDirectoryOptions branchOptions)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:PosDatabase", connectionString);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IOrganizationBranchDirectory>();
                services.AddSingleton(branchOptions);
                services.AddSingleton<IOrganizationBranchDirectory, H1ProofOrganizationBranchDirectory>();
            });
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
