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

    [Fact]
    public async Task Ensure_pwa_creates_0001_then_0002_when_busy_and_reuses_free_manual()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString, deviceEnforcementEnabled: false);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var owner = Actor;
        var cashier = Guid.Parse("c3c3c3c3-c3c3-c3c3-c3c3-c3c3c3c3c3c3");

        using var ensureEmpty = Scoped(HttpMethod.Post, $"{Registers}/ensure-available-for-pwa-shift", org, owner);
        ensureEmpty.Content = JsonContent.Create(new { }, options: JsonOptions);
        using var ensureEmptyResponse = await client.SendAsync(ensureEmpty);
        Assert.Equal(HttpStatusCode.OK, ensureEmptyResponse.StatusCode);
        var pwa1 = await ensureEmptyResponse.Content.ReadFromJsonAsync<PosRegisterDto>(JsonOptions);
        Assert.Equal("PWA-0001", pwa1!.Name);
        Assert.False(pwa1.HasOpenShift);

        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, owner, 0m, pwa1.RegisterId);

        using var ensureBusy = Scoped(HttpMethod.Post, $"{Registers}/ensure-available-for-pwa-shift", org, cashier);
        ensureBusy.Content = JsonContent.Create(new { }, options: JsonOptions);
        using var ensureBusyResponse = await client.SendAsync(ensureBusy);
        Assert.Equal(HttpStatusCode.OK, ensureBusyResponse.StatusCode);
        var pwa2 = await ensureBusyResponse.Content.ReadFromJsonAsync<PosRegisterDto>(JsonOptions);
        Assert.Equal("PWA-0002", pwa2!.Name);
        Assert.NotEqual(pwa1.RegisterId, pwa2.RegisterId);

        var cashierShift = await PosShiftIntegrationSupport.EnsureOpenShiftAsync(
            client,
            org,
            cashier,
            0m,
            pwa2.RegisterId);
        Assert.Equal(pwa2.RegisterId, cashierShift.RegisterId);

        using var createManual = Scoped(HttpMethod.Post, Registers, org, owner);
        createManual.Content = JsonContent.Create(new CreateRegisterRequest("Front Counter"), options: JsonOptions);
        using var createManualResponse = await client.SendAsync(createManual);
        createManualResponse.EnsureSuccessStatusCode();
        var manual = await createManualResponse.Content.ReadFromJsonAsync<PosRegisterDto>(JsonOptions);

        using var ensureReuse = Scoped(HttpMethod.Post, $"{Registers}/ensure-available-for-pwa-shift", org, owner);
        ensureReuse.Content = JsonContent.Create(new { }, options: JsonOptions);
        // Owner already has open shift — ensure still returns a free register (manual), not a new PWA.
        // (Shift open rules still block a second shift for the same actor separately.)
        using var ensureReuseResponse = await client.SendAsync(ensureReuse);
        Assert.Equal(HttpStatusCode.OK, ensureReuseResponse.StatusCode);
        var reused = await ensureReuseResponse.Content.ReadFromJsonAsync<PosRegisterDto>(JsonOptions);
        Assert.Equal(manual!.RegisterId, reused!.RegisterId);
        Assert.Equal("Front Counter", reused.Name);
    }

    [Fact]
    public async Task Ensure_pwa_allows_cashier_without_manage_registers_and_rejects_create()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString, deviceEnforcementEnabled: false);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var owner = Actor;
        var cashier = Guid.Parse("d4d4d4d4-d4d4-d4d4-d4d4-d4d4d4d4d4d4");

        await PosInventoryOpsIntegrationSupport.BootstrapOwnerAsync(client, org, owner);
        await PosInventoryOpsIntegrationSupport.AssignRoleAsync(client, org, owner, cashier, "Cashier");

        using var createDenied = Scoped(HttpMethod.Post, Registers, org, cashier);
        createDenied.Content = JsonContent.Create(new CreateRegisterRequest("Cashier Counter"), options: JsonOptions);
        using var createDeniedResponse = await client.SendAsync(createDenied);
        Assert.Equal(HttpStatusCode.Forbidden, createDeniedResponse.StatusCode);

        using var ensure = Scoped(HttpMethod.Post, $"{Registers}/ensure-available-for-pwa-shift", org, cashier);
        ensure.Content = JsonContent.Create(new { }, options: JsonOptions);
        using var ensureResponse = await client.SendAsync(ensure);
        Assert.Equal(HttpStatusCode.OK, ensureResponse.StatusCode);
        var register = await ensureResponse.Content.ReadFromJsonAsync<PosRegisterDto>(JsonOptions);
        Assert.Equal("PWA-0001", register!.Name);
    }

    [Fact]
    public async Task Ensure_pwa_concurrent_callers_do_not_duplicate_names()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString, deviceEnforcementEnabled: false);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var actorA = Actor;
        var actorB = Guid.Parse("e5e5e5e5-e5e5-e5e5-e5e5-e5e5e5e5e5e5");

        using var seed = Scoped(HttpMethod.Post, $"{Registers}/ensure-available-for-pwa-shift", org, actorA);
        seed.Content = JsonContent.Create(new { }, options: JsonOptions);
        using var seedResponse = await client.SendAsync(seed);
        seedResponse.EnsureSuccessStatusCode();
        var pwa1 = await seedResponse.Content.ReadFromJsonAsync<PosRegisterDto>(JsonOptions);
        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, actorA, 0m, pwa1!.RegisterId);

        async Task<PosRegisterDto> EnsureAs(Guid actor)
        {
            using var request = Scoped(HttpMethod.Post, $"{Registers}/ensure-available-for-pwa-shift", org, actor);
            request.Content = JsonContent.Create(new { }, options: JsonOptions);
            using var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<PosRegisterDto>(JsonOptions))!;
        }

        var results = await Task.WhenAll(EnsureAs(actorB), EnsureAs(Guid.Parse("f6f6f6f6-f6f6-f6f6-f6f6-f6f6f6f6f6f6")));
        Assert.All(results, r => Assert.False(r.HasOpenShift));
        Assert.All(results, r => Assert.StartsWith("PWA-", r.Name, StringComparison.OrdinalIgnoreCase));

        using var list = Scoped(HttpMethod.Get, $"{Registers}?status=Active&page=1&pageSize=50", org, actorA);
        using var listResponse = await client.SendAsync(list);
        listResponse.EnsureSuccessStatusCode();
        var page = await listResponse.Content.ReadFromJsonAsync<PagedResult<PosRegisterDto>>(JsonOptions);
        var pwaNames = page!.Items
            .Where(r => r.Name.StartsWith("PWA-", StringComparison.OrdinalIgnoreCase))
            .Select(r => r.Name.ToUpperInvariant())
            .ToList();
        Assert.Equal(pwaNames.Count, pwaNames.Distinct(StringComparer.Ordinal).Count());
        Assert.Contains("PWA-0001", pwaNames);
        Assert.True(pwaNames.Count >= 2);
    }

    [Fact]
    public async Task Ensure_pwa_blocked_when_device_enforcement_enabled()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString, deviceEnforcementEnabled: true);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        using var ensure = Scoped(HttpMethod.Post, $"{Registers}/ensure-available-for-pwa-shift", org, Actor);
        ensure.Content = JsonContent.Create(new { }, options: JsonOptions);
        using var ensureResponse = await client.SendAsync(ensure);
        Assert.Equal(HttpStatusCode.Forbidden, ensureResponse.StatusCode);
        Assert.Equal(
            ApplicationErrorCodes.PwaRegisterEnsureDeviceEnforcementEnabled,
            await ReadErrorCodeAsync(ensureResponse));
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

    private sealed class PosApiFactory(string connectionString, bool deviceEnforcementEnabled = true)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:PosDatabase", connectionString);
            builder.UseSetting("LocalValidation:Enabled", "false");
            builder.UseSetting(
                "PosDeviceAuthorization:EnforcementEnabled",
                deviceEnforcementEnabled ? "true" : "false");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PosDatabase"] = connectionString,
                    ["LocalValidation:Enabled"] = "false",
                    ["PosDeviceAuthorization:EnforcementEnabled"] = deviceEnforcementEnabled ? "true" : "false"
                });
            });
        }
    }
}
