using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.CashierShifts;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Registers;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Domain.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

/// <summary>
/// SHIFTUSER: actor-owned shifts per register — one open shift per register, sales bind to the
/// authenticated actor's own open shift only.
/// </summary>
[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosShiftActorOwnershipApiTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Guid OwnerActor = Guid.Parse("a1a1a1a1-a1a1-a1a1-a1a1-a1a1a1a1a1a1");
    private static readonly Guid ManagerActor = Guid.Parse("b2b2b2b2-b2b2-b2b2-b2b2-b2b2b2b2b2b2");

    private const string Shifts = "/api/v1/pos/cashier-shifts";
    private const string Sales = "/api/v1/pos/sales";
    private const string Products = "/api/v1/pos/catalog/products";
    private const string Registers = "/api/v1/pos/registers";

    [Fact]
    public async Task SHIFTUSER_register_lock_and_actor_owned_sale_binding()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var otherOrg = Guid.NewGuid();

        var register1 = await PosShiftIntegrationSupport.EnsureRegisterAsync(
            client,
            org,
            OwnerActor,
            "Register 1");
        using var createReg2 = Scoped(HttpMethod.Post, Registers, org, OwnerActor);
        createReg2.Content = JsonContent.Create(new CreateRegisterRequest("Register 2"), options: JsonOptions);
        using var createReg2Response = await client.SendAsync(createReg2);
        createReg2Response.EnsureSuccessStatusCode();
        var register2 = (await createReg2Response.Content.ReadFromJsonAsync<PosRegisterDto>(JsonOptions))!;

        // SHIFTUSER-01 Owner opens Register1 and can sell.
        var ownerShift = await PosShiftIntegrationSupport.EnsureOpenShiftAsync(
            client,
            org,
            OwnerActor,
            50m,
            register1.RegisterId);
        Assert.Equal(register1.RegisterId, ownerShift.RegisterId);

        var product = await CreateProductAsync(client, org, OwnerActor, "Pan", "Piece", 10m, "shiftuser-pan");
        using var ownerSaleReq = Scoped(HttpMethod.Post, Sales, org, OwnerActor);
        ownerSaleReq.Content = JsonContent.Create(
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                "Cash",
                AmountTendered: 20m,
                ShiftId: ownerShift.ShiftId),
            options: JsonOptions);
        using var ownerSaleResponse = await client.SendAsync(ownerSaleReq);
        ownerSaleResponse.EnsureSuccessStatusCode();
        var ownerSale = await ownerSaleResponse.Content.ReadFromJsonAsync<PosSaleDto>(JsonOptions);
        Assert.Equal(OwnerActor, ownerSale!.RecordedBy);
        Assert.Equal(ownerShift.ShiftId, ownerSale.ShiftId);
        Assert.Equal(register1.RegisterId, ownerShift.RegisterId);

        // Register list exposes open-shift actor for UX.
        using var getReg1 = Scoped(HttpMethod.Get, $"{Registers}/{register1.RegisterId:D}", org, ManagerActor);
        using var getReg1Response = await client.SendAsync(getReg1);
        getReg1Response.EnsureSuccessStatusCode();
        var reg1Dto = await getReg1Response.Content.ReadFromJsonAsync<PosRegisterDto>(JsonOptions);
        Assert.True(reg1Dto!.HasOpenShift);
        Assert.Equal(OwnerActor, reg1Dto.OpenShiftActorId);

        // SHIFTUSER-02 Manager cannot open a second shift on Register1.
        using var managerOpenR1 = Scoped(HttpMethod.Post, Shifts, org, ManagerActor);
        managerOpenR1.Content = JsonContent.Create(
            new OpenCashierShiftRequest(register1.RegisterId, 10m),
            options: JsonOptions);
        using var managerOpenR1Response = await client.SendAsync(managerOpenR1);
        Assert.Equal(HttpStatusCode.Conflict, managerOpenR1Response.StatusCode);
        var conflictProblem = await managerOpenR1Response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(
            DomainErrorCodes.CashierShiftRegisterConflict,
            conflictProblem.TryGetProperty("errorCode", out var conflictCode)
                ? conflictCode.GetString()
                : null);
        Assert.Equal(
            OwnerActor.ToString("D"),
            conflictProblem.TryGetProperty("openShiftActorId", out var opener)
                ? opener.GetString()
                : null);

        // SHIFTUSER-03 Manager cannot sell using Owner ShiftId (no manager open shift).
        using var forgedNoShift = Scoped(HttpMethod.Post, Sales, org, ManagerActor);
        forgedNoShift.Content = JsonContent.Create(
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                "Cash",
                AmountTendered: 20m,
                ShiftId: ownerShift.ShiftId),
            options: JsonOptions);
        using var forgedNoShiftResponse = await client.SendAsync(forgedNoShift);
        Assert.Equal(HttpStatusCode.Conflict, forgedNoShiftResponse.StatusCode);
        Assert.Equal(
            ApplicationErrorCodes.CashierShiftNoOpenShift,
            await ReadErrorCodeAsync(forgedNoShiftResponse));

        // SHIFTUSER-04/05/06 Manager opens Register2 and sells on own shift.
        var managerShift = await PosShiftIntegrationSupport.EnsureOpenShiftAsync(
            client,
            org,
            ManagerActor,
            25m,
            register2.RegisterId);
        Assert.Equal(register2.RegisterId, managerShift.RegisterId);
        Assert.NotEqual(ownerShift.ShiftId, managerShift.ShiftId);

        // SHIFTUSER-08 forged foreign ShiftId while manager has own open shift → mismatch.
        using var forgedMismatch = Scoped(HttpMethod.Post, Sales, org, ManagerActor);
        forgedMismatch.Content = JsonContent.Create(
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                "Cash",
                AmountTendered: 20m,
                ShiftId: ownerShift.ShiftId),
            options: JsonOptions);
        using var forgedMismatchResponse = await client.SendAsync(forgedMismatch);
        Assert.Equal(HttpStatusCode.Conflict, forgedMismatchResponse.StatusCode);
        Assert.Equal(
            ApplicationErrorCodes.CashierShiftMismatch,
            await ReadErrorCodeAsync(forgedMismatchResponse));

        // SHIFTUSER-07 sale persists authenticated cashier + own ShiftId + own RegisterId.
        using var managerSaleReq = Scoped(HttpMethod.Post, Sales, org, ManagerActor);
        managerSaleReq.Content = JsonContent.Create(
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                "Cash",
                AmountTendered: 20m,
                ShiftId: managerShift.ShiftId),
            options: JsonOptions);
        using var managerSaleResponse = await client.SendAsync(managerSaleReq);
        managerSaleResponse.EnsureSuccessStatusCode();
        var managerSale = await managerSaleResponse.Content.ReadFromJsonAsync<PosSaleDto>(JsonOptions);
        Assert.Equal(ManagerActor, managerSale!.RecordedBy);
        Assert.Equal(managerShift.ShiftId, managerSale.ShiftId);
        Assert.Equal(register2.RegisterId, managerShift.RegisterId);
        Assert.NotEqual(ownerSale.ShiftId, managerSale.ShiftId);

        // SHIFTUSER-09 closing Owner shift frees Register1.
        using var closeOwner = Scoped(HttpMethod.Post, $"{Shifts}/{ownerShift.ShiftId:D}/close", org, OwnerActor);
        closeOwner.Content = JsonContent.Create(new CloseCashierShiftRequest(60m), options: JsonOptions);
        using var closeOwnerResponse = await client.SendAsync(closeOwner);
        closeOwnerResponse.EnsureSuccessStatusCode();

        using var reopenR1 = Scoped(HttpMethod.Post, Shifts, org, OwnerActor);
        // Owner still has no open shift after close; Manager still holds Register2.
        // Use a third actor? Owner can reopen Register1 after close.
        reopenR1.Content = JsonContent.Create(
            new OpenCashierShiftRequest(register1.RegisterId, 5m),
            options: JsonOptions);
        using var reopenR1Response = await client.SendAsync(reopenR1);
        reopenR1Response.EnsureSuccessStatusCode();

        // SHIFTUSER-10 branch/org isolation: other org cannot see register.
        using var foreignGet = Scoped(HttpMethod.Get, $"{Registers}/{register1.RegisterId:D}", otherOrg, OwnerActor);
        using var foreignGetResponse = await client.SendAsync(foreignGet);
        Assert.Equal(HttpStatusCode.NotFound, foreignGetResponse.StatusCode);
    }

    private static async Task<PosCatalogProductDto> CreateProductAsync(
        HttpClient client,
        Guid org,
        Guid actorId,
        string name,
        string uom,
        decimal price,
        string sku)
    {
        using var request = Scoped(HttpMethod.Post, Products, org, actorId);
        request.Content = JsonContent.Create(
            new CreatePosCatalogProductRequest(name, uom, price, null, sku),
            options: JsonOptions);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PosCatalogProductDto>(JsonOptions))!;
    }

    private static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        if (problem.TryGetProperty("errorCode", out var code))
        {
            return code.GetString();
        }

        if (problem.TryGetProperty("extensions", out var extensions)
            && extensions.TryGetProperty("errorCode", out var nested))
        {
            return nested.GetString();
        }

        return null;
    }

    private static HttpRequestMessage Scoped(
        HttpMethod method,
        string path,
        Guid organizationId,
        Guid actorId)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation(
            ExItS.PinoyBusinessPOS.Api.Common.PosOrganizationHeaders.OrganizationHeaderName,
            organizationId.ToString("D"));
        request.Headers.TryAddWithoutValidation(
            ExItS.PinoyBusinessPOS.Api.Common.PosOrganizationHeaders.ActorHeaderName,
            actorId.ToString("D"));
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
