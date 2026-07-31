using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.Permissions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosPermissionApiTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Guid OwnerActor = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid CashierActor = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private const string Permissions = "/api/v1/pos/permissions";

    [Fact]
    public async Task Bootstrap_owner_and_assign_cashier()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        using var effective = PosIntegrationRequest.Scoped(HttpMethod.Get, $"{Permissions}/effective", org, OwnerActor);
        using var effectiveResponse = await client.SendAsync(effective);
        var body = await effectiveResponse.Content.ReadAsStringAsync();
        Assert.True(effectiveResponse.IsSuccessStatusCode, body);
        Assert.Equal(HttpStatusCode.OK, effectiveResponse.StatusCode);
        var ownerEffective = await effectiveResponse.Content.ReadFromJsonAsync<PosEffectivePermissionsDto>(JsonOptions);
        Assert.Equal("Owner", ownerEffective!.Role);

        using var assign = PosIntegrationRequest.Scoped(HttpMethod.Post, $"{Permissions}/assignments", org, OwnerActor);
        assign.Content = JsonContent.Create(new AssignPosRoleRequest(CashierActor, "Cashier"), options: JsonOptions);
        using var assignResponse = await client.SendAsync(assign);
        Assert.Equal(HttpStatusCode.Created, assignResponse.StatusCode);

        using var cashierEffective = PosIntegrationRequest.Scoped(
            HttpMethod.Get,
            $"{Permissions}/actors/{CashierActor:D}/effective",
            org,
            OwnerActor);
        using var cashierResponse = await client.SendAsync(cashierEffective);
        Assert.Equal(HttpStatusCode.OK, cashierResponse.StatusCode);
        var cashier = await cashierResponse.Content.ReadFromJsonAsync<PosEffectivePermissionsDto>(JsonOptions);
        Assert.Equal("Cashier", cashier!.Role);
        Assert.DoesNotContain("VoidSale", cashier.AllowedCapabilities);
    }

    [Fact]
    public async Task Last_owner_cannot_be_revoked()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        using var bootstrap = PosIntegrationRequest.Scoped(HttpMethod.Get, $"{Permissions}/effective", org, OwnerActor);
        using var _ = await client.SendAsync(bootstrap);

        using var list = PosIntegrationRequest.Scoped(HttpMethod.Get, $"{Permissions}/assignments?status=Active", org, OwnerActor);
        using var listResponse = await client.SendAsync(list);
        var page = await listResponse.Content.ReadFromJsonAsync<PosRoleAssignmentListDto>(JsonOptions);
        var ownerAssignment = Assert.Single(page!.Items);

        using var revoke = PosIntegrationRequest.Scoped(
            HttpMethod.Post,
            $"{Permissions}/assignments/{ownerAssignment.AssignmentId:D}/revoke",
            org,
            OwnerActor);
        revoke.Content = JsonContent.Create(new RevokePosRoleRequest("test"), options: JsonOptions);
        using var revokeResponse = await client.SendAsync(revoke);
        Assert.Equal(HttpStatusCode.Conflict, revokeResponse.StatusCode);
    }

    [Fact]
    public async Task Operational_reports_respect_role_matrix()
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

        using var ownerOverview = PosIntegrationRequest.Scoped(
            HttpMethod.Get,
            "/api/v1/pos/reports/overview",
            org,
            OwnerActor);
        using var ownerOverviewResponse = await client.SendAsync(ownerOverview);
        Assert.Equal(HttpStatusCode.OK, ownerOverviewResponse.StatusCode);

        using var cashierOverview = PosIntegrationRequest.Scoped(
            HttpMethod.Get,
            "/api/v1/pos/reports/overview",
            org,
            CashierActor);
        using var cashierOverviewResponse = await client.SendAsync(cashierOverview);
        Assert.Equal(HttpStatusCode.Forbidden, cashierOverviewResponse.StatusCode);

        using var cashierShifts = PosIntegrationRequest.Scoped(
            HttpMethod.Get,
            "/api/v1/pos/reports/shifts-summary",
            org,
            CashierActor);
        using var cashierShiftsResponse = await client.SendAsync(cashierShifts);
        Assert.Equal(HttpStatusCode.OK, cashierShiftsResponse.StatusCode);
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
