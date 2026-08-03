using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.OperationalSetup;
using ExItS.PinoyBusinessPOS.Application.Permissions;
using ExItS.PinoyBusinessPOS.Application.Registers;
using ExItS.PinoyBusinessPOS.Domain.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosOperationalSetupApiTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Guid OwnerActor = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid CashierActor = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private const string SetupPath = "/api/v1/pos/operational-setup";
    private const string Permissions = "/api/v1/pos/permissions";
    private const string Registers = "/api/v1/pos/registers";

    [Fact]
    public async Task Complete_setup_creates_default_register_and_marks_completed()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        using var bootstrap = PosIntegrationRequest.Scoped(HttpMethod.Get, $"{Permissions}/effective", org, OwnerActor);
        using var _ = await client.SendAsync(bootstrap);

        using var getBefore = PosIntegrationRequest.Scoped(HttpMethod.Get, SetupPath, org, OwnerActor);
        using var getBeforeResponse = await client.SendAsync(getBefore);
        Assert.Equal(HttpStatusCode.OK, getBeforeResponse.StatusCode);
        var incomplete = await getBeforeResponse.Content.ReadFromJsonAsync<PosOperationalSetupDto>(JsonOptions);
        Assert.False(incomplete!.IsCompleted);
        Assert.Equal("PHP", incomplete.CurrencyCode);

        using var complete = PosIntegrationRequest.Scoped(HttpMethod.Post, $"{SetupPath}/complete", org, OwnerActor);
        complete.Content = JsonContent.Create(
            new CompleteOperationalSetupRequest(
                "Sari-Sari Store",
                "PHP",
                "TaxExclusive",
                12m,
                ReceiptHeader: "Thank you"),
            options: JsonOptions);
        using var completeResponse = await client.SendAsync(complete);
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
        var completed = await completeResponse.Content.ReadFromJsonAsync<PosOperationalSetupDto>(JsonOptions);
        Assert.True(completed!.IsCompleted);
        Assert.Equal("Sari-Sari Store", completed.StoreDisplayName);
        Assert.NotNull(completed.DefaultRegisterId);
        Assert.NotNull(completed.CompletedAtUtc);

        using var listRegisters = PosIntegrationRequest.Scoped(HttpMethod.Get, Registers, org, OwnerActor);
        using var listResponse = await client.SendAsync(listRegisters);
        var registers = await listResponse.Content.ReadFromJsonAsync<PagedResult<PosRegisterDto>>(JsonOptions);
        var mainRegister = Assert.Single(registers!.Items);
        Assert.Equal("Main Register", mainRegister.Name);
        Assert.Equal(completed.DefaultRegisterId, mainRegister.RegisterId);
    }

    [Fact]
    public async Task Cashier_cannot_complete_operational_setup()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        using var bootstrap = PosIntegrationRequest.Scoped(HttpMethod.Get, $"{Permissions}/effective", org, OwnerActor);
        using var _ = await client.SendAsync(bootstrap);

        using var assign = PosIntegrationRequest.Scoped(HttpMethod.Post, $"{Permissions}/assignments", org, OwnerActor);
        assign.Content = JsonContent.Create(new AssignPosRoleRequest(CashierActor, "Cashier"), options: JsonOptions);
        using var assignResponse = await client.SendAsync(assign);
        Assert.Equal(HttpStatusCode.Created, assignResponse.StatusCode);

        using var complete = PosIntegrationRequest.Scoped(HttpMethod.Post, $"{SetupPath}/complete", org, CashierActor);
        complete.Content = JsonContent.Create(
            new CompleteOperationalSetupRequest("Blocked Store", "PHP", "TaxExclusive", 0m),
            options: JsonOptions);
        using var completeResponse = await client.SendAsync(complete);
        Assert.Equal(HttpStatusCode.Forbidden, completeResponse.StatusCode);
        Assert.Equal(DomainErrorCodes.PosRoleDenied, await ReadErrorCodeAsync(completeResponse));
    }

    [Fact]
    public async Task Cross_organization_setup_is_isolated()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();

        using var bootstrapA = PosIntegrationRequest.Scoped(HttpMethod.Get, $"{Permissions}/effective", orgA, OwnerActor);
        using var _a = await client.SendAsync(bootstrapA);

        using var completeA = PosIntegrationRequest.Scoped(HttpMethod.Post, $"{SetupPath}/complete", orgA, OwnerActor);
        completeA.Content = JsonContent.Create(
            new CompleteOperationalSetupRequest("Org A Store", "PHP", "TaxExclusive", 0m),
            options: JsonOptions);
        using var completeAResponse = await client.SendAsync(completeA);
        Assert.Equal(HttpStatusCode.OK, completeAResponse.StatusCode);

        using var bootstrapB = PosIntegrationRequest.Scoped(HttpMethod.Get, $"{Permissions}/effective", orgB, OwnerActor);
        using var _b = await client.SendAsync(bootstrapB);

        using var getB = PosIntegrationRequest.Scoped(HttpMethod.Get, SetupPath, orgB, OwnerActor);
        using var getBResponse = await client.SendAsync(getB);
        var setupB = await getBResponse.Content.ReadFromJsonAsync<PosOperationalSetupDto>(JsonOptions);
        Assert.False(setupB!.IsCompleted);
        Assert.Equal(string.Empty, setupB.StoreDisplayName);
    }

    private static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        return document.RootElement.TryGetProperty("errorCode", out var code) ? code.GetString() : null;
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
