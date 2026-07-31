using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.CashierShifts;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Registers;
using ExItS.PinoyBusinessPOS.Domain.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosRegisterApiTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Guid Actor = Guid.Parse("a1a1a1a1-a1a1-a1a1-a1a1-a1a1a1a1a1a1");
    private const string Registers = "/api/v1/pos/registers";
    private const string Shifts = "/api/v1/pos/cashier-shifts";

    [Fact]
    public async Task Create_allocates_reg_code_and_enforces_name_uniqueness()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        using var create = Scoped(HttpMethod.Post, Registers, org);
        create.Content = JsonContent.Create(new CreateRegisterRequest("  Main Counter  "), options: JsonOptions);
        using var createResponse = await client.SendAsync(create);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var register = await createResponse.Content.ReadFromJsonAsync<PosRegisterDto>(JsonOptions);
        Assert.Equal("REG-000001", register!.RegisterCode);
        Assert.Equal("Main Counter", register.Name);
        Assert.Equal("Active", register.Status);

        using var duplicate = Scoped(HttpMethod.Post, Registers, org);
        duplicate.Content = JsonContent.Create(new CreateRegisterRequest("main counter"), options: JsonOptions);
        using var duplicateResponse = await client.SendAsync(duplicate);
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
        Assert.Equal(ApplicationErrorCodes.RegisterNameConflict, await ReadErrorCodeAsync(duplicateResponse));
    }

    [Fact]
    public async Task Deactivate_blocked_while_open_shift_exists_and_open_requires_active_register()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var register = await PosShiftIntegrationSupport.EnsureRegisterAsync(client, org, Actor, "Pharmacy Counter");
        var shift = await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, Actor, 50m, register.RegisterId);
        Assert.Equal(register.RegisterId, shift.RegisterId);

        using var deactivate = Scoped(HttpMethod.Post, $"{Registers}/{register.RegisterId:D}/deactivate", org);
        using var deactivateResponse = await client.SendAsync(deactivate);
        Assert.Equal(HttpStatusCode.Conflict, deactivateResponse.StatusCode);
        Assert.Equal(DomainErrorCodes.RegisterDeactivateBlockedByOpenShift, await ReadErrorCodeAsync(deactivateResponse));

        using var otherActorOpen = Scoped(HttpMethod.Post, Shifts, org, Guid.Parse("b2b2b2b2-b2b2-b2b2-b2b2-b2b2b2b2b2b2"));
        otherActorOpen.Content = JsonContent.Create(
            new OpenCashierShiftRequest(register.RegisterId, 10m),
            options: JsonOptions);
        using var otherOpenResponse = await client.SendAsync(otherActorOpen);
        Assert.Equal(HttpStatusCode.Conflict, otherOpenResponse.StatusCode);
        Assert.Equal(DomainErrorCodes.CashierShiftRegisterConflict, await ReadErrorCodeAsync(otherOpenResponse));
    }

    [Fact]
    public async Task Cross_organization_register_is_concealed()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();

        var register = await PosShiftIntegrationSupport.EnsureRegisterAsync(client, orgA, Actor, "Org A Counter");
        using var get = Scoped(HttpMethod.Get, $"{Registers}/{register.RegisterId:D}", orgB);
        using var getResponse = await client.SendAsync(get);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    private static HttpRequestMessage Scoped(HttpMethod method, string path, Guid organizationId, Guid? actorId = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation(
            ExItS.PinoyBusinessPOS.Api.Common.PosOrganizationHeaders.OrganizationHeaderName,
            organizationId.ToString("D"));
        request.Headers.TryAddWithoutValidation(
            ExItS.PinoyBusinessPOS.Api.Common.PosOrganizationHeaders.ActorHeaderName,
            (actorId ?? Actor).ToString("D"));
        return request;
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
