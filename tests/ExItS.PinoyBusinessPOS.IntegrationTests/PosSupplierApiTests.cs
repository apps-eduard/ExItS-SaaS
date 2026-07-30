using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Suppliers;
using ExItS.PinoyBusinessPOS.Domain.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosSupplierApiTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Guid Actor = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

    private const string Suppliers = "/api/v1/pos/suppliers";

    [Fact]
    public async Task Create_supplier_allocates_sup_code_and_requires_name()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        using var create = Scoped(HttpMethod.Post, Suppliers, org);
        create.Content = JsonContent.Create(
            new CreateSupplierRequest(
                "  Acme Trading  ",
                ContactPerson: "Maria",
                MobileNumber: "0917-111-2222",
                Email: "buyer@acme.test",
                TaxOrRegistrationNumber: "123-456-789"),
            options: JsonOptions);
        using var createResponse = await client.SendAsync(create);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var supplier = await createResponse.Content.ReadFromJsonAsync<PosSupplierDto>(JsonOptions);
        Assert.NotNull(supplier);
        Assert.Equal("SUP-000001", supplier!.SupplierCode);
        Assert.Equal("Acme Trading", supplier.Name);
        Assert.Equal("Active", supplier.Status);
        Assert.Equal(org, supplier.OrganizationId);
        Assert.Equal("Maria", supplier.ContactPerson);
        Assert.Equal("0917-111-2222", supplier.MobileNumber);
        Assert.Equal("buyer@acme.test", supplier.Email);
        Assert.Equal("123-456-789", supplier.TaxOrRegistrationNumber);

        using var second = Scoped(HttpMethod.Post, Suppliers, org);
        second.Content = JsonContent.Create(new CreateSupplierRequest("Beta Supply"), options: JsonOptions);
        using var secondResponse = await client.SendAsync(second);
        var secondSupplier = await secondResponse.Content.ReadFromJsonAsync<PosSupplierDto>(JsonOptions);
        Assert.Equal("SUP-000002", secondSupplier!.SupplierCode);

        using var blank = Scoped(HttpMethod.Post, Suppliers, org);
        blank.Content = JsonContent.Create(new CreateSupplierRequest("   "), options: JsonOptions);
        using var blankResponse = await client.SendAsync(blank);
        Assert.Equal(HttpStatusCode.BadRequest, blankResponse.StatusCode);
        Assert.Equal(DomainErrorCodes.InvalidSupplierName, await ReadErrorCodeAsync(blankResponse));
    }

    [Fact]
    public async Task Active_duplicate_name_email_mobile_and_tax_return_conflicts()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        _ = await CreateSupplierAsync(client, org, new CreateSupplierRequest(
            "Acme Trading",
            MobileNumber: "0917-111-2222",
            Email: "buyer@acme.test",
            TaxOrRegistrationNumber: "123-456-789"));

        using var duplicateName = Scoped(HttpMethod.Post, Suppliers, org);
        duplicateName.Content = JsonContent.Create(new CreateSupplierRequest("acme trading"), options: JsonOptions);
        using var nameResponse = await client.SendAsync(duplicateName);
        Assert.Equal(HttpStatusCode.Conflict, nameResponse.StatusCode);
        Assert.Equal(ApplicationErrorCodes.SupplierNameConflict, await ReadErrorCodeAsync(nameResponse));

        using var duplicateEmail = Scoped(HttpMethod.Post, Suppliers, org);
        duplicateEmail.Content = JsonContent.Create(
            new CreateSupplierRequest("Other Co", Email: "BUYER@ACME.TEST"),
            options: JsonOptions);
        using var emailResponse = await client.SendAsync(duplicateEmail);
        Assert.Equal(HttpStatusCode.Conflict, emailResponse.StatusCode);
        Assert.Equal(ApplicationErrorCodes.SupplierEmailConflict, await ReadErrorCodeAsync(emailResponse));

        using var duplicateMobile = Scoped(HttpMethod.Post, Suppliers, org);
        duplicateMobile.Content = JsonContent.Create(
            new CreateSupplierRequest("Other Co 2", MobileNumber: "09171112222"),
            options: JsonOptions);
        using var mobileResponse = await client.SendAsync(duplicateMobile);
        Assert.Equal(HttpStatusCode.Conflict, mobileResponse.StatusCode);
        Assert.Equal(ApplicationErrorCodes.SupplierMobileConflict, await ReadErrorCodeAsync(mobileResponse));

        using var duplicateTax = Scoped(HttpMethod.Post, Suppliers, org);
        duplicateTax.Content = JsonContent.Create(
            new CreateSupplierRequest("Other Co 3", TaxOrRegistrationNumber: "123456789"),
            options: JsonOptions);
        using var taxResponse = await client.SendAsync(duplicateTax);
        Assert.Equal(HttpStatusCode.Conflict, taxResponse.StatusCode);
        Assert.Equal(ApplicationErrorCodes.SupplierTaxConflict, await ReadErrorCodeAsync(taxResponse));
    }

    [Fact]
    public async Task List_supports_filters_pagination_and_name_ordering()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var zed = await CreateSupplierAsync(client, org, new CreateSupplierRequest("Zed Wholesale"));
        var amy = await CreateSupplierAsync(client, org, new CreateSupplierRequest("Amy Distributors"));
        var mia = await CreateSupplierAsync(client, org, new CreateSupplierRequest(
            "Mia Merchants", Email: "mia@example.test"));
        _ = await CreateSupplierAsync(client, org, new CreateSupplierRequest("Bob Builders"));

        using var page1 = Scoped(HttpMethod.Get, $"{Suppliers}?page=1&pageSize=2", org);
        using var page1Response = await client.SendAsync(page1);
        page1Response.EnsureSuccessStatusCode();
        var first = await page1Response.Content.ReadFromJsonAsync<PagedResult<PosSupplierDto>>(JsonOptions);
        Assert.Equal(new[] { "Amy Distributors", "Bob Builders" }, first!.Items.Select(i => i.Name).ToArray());

        using var page2 = Scoped(HttpMethod.Get, $"{Suppliers}?page=2&pageSize=2", org);
        using var page2Response = await client.SendAsync(page2);
        var second = await page2Response.Content.ReadFromJsonAsync<PagedResult<PosSupplierDto>>(JsonOptions);
        Assert.Equal(new[] { "Mia Merchants", "Zed Wholesale" }, second!.Items.Select(i => i.Name).ToArray());

        using var byName = Scoped(HttpMethod.Get, $"{Suppliers}?name=amy", org);
        using var byNameResponse = await client.SendAsync(byName);
        var named = await byNameResponse.Content.ReadFromJsonAsync<PagedResult<PosSupplierDto>>(JsonOptions);
        Assert.Contains(named!.Items, i => i.SupplierId == amy.SupplierId);

        using var byCode = Scoped(HttpMethod.Get, $"{Suppliers}?supplierCode=SUP-000003", org);
        using var byCodeResponse = await client.SendAsync(byCode);
        var coded = await byCodeResponse.Content.ReadFromJsonAsync<PagedResult<PosSupplierDto>>(JsonOptions);
        Assert.Contains(coded!.Items, i => i.SupplierId == mia.SupplierId);

        using var byEmail = Scoped(HttpMethod.Get, $"{Suppliers}?email=mia@example", org);
        using var byEmailResponse = await client.SendAsync(byEmail);
        var emailed = await byEmailResponse.Content.ReadFromJsonAsync<PagedResult<PosSupplierDto>>(JsonOptions);
        Assert.Contains(emailed!.Items, i => i.SupplierId == mia.SupplierId);

        using var activeOnly = Scoped(HttpMethod.Get, $"{Suppliers}?status=Active", org);
        using var activeResponse = await client.SendAsync(activeOnly);
        var active = await activeResponse.Content.ReadFromJsonAsync<PagedResult<PosSupplierDto>>(JsonOptions);
        Assert.Equal(4, active!.TotalCount);
        Assert.Contains(active.Items, i => i.SupplierId == zed.SupplierId);
    }

    [Fact]
    public async Task Get_update_deactivate_activate_and_cross_org_isolation()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();

        var created = await CreateSupplierAsync(client, orgA, new CreateSupplierRequest("Lifecycle Co"));
        var originalCode = created.SupplierCode;

        using var get = Scoped(HttpMethod.Get, $"{Suppliers}/{created.SupplierId:D}", orgA);
        using var getResponse = await client.SendAsync(get);
        getResponse.EnsureSuccessStatusCode();
        var fetched = await getResponse.Content.ReadFromJsonAsync<PosSupplierDto>(JsonOptions);
        Assert.Equal(originalCode, fetched!.SupplierCode);

        using var crossGet = Scoped(HttpMethod.Get, $"{Suppliers}/{created.SupplierId:D}", orgB);
        using var crossGetResponse = await client.SendAsync(crossGet);
        Assert.Equal(HttpStatusCode.NotFound, crossGetResponse.StatusCode);

        using var update = Scoped(HttpMethod.Put, $"{Suppliers}/{created.SupplierId:D}", orgA);
        update.Content = JsonContent.Create(new UpdateSupplierRequest(
            "Lifecycle Co Updated",
            fetched.UpdatedAtUtc,
            ContactPerson: "Juan"),
            options: JsonOptions);
        using var updateResponse = await client.SendAsync(update);
        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<PosSupplierDto>(JsonOptions);
        Assert.Equal("Lifecycle Co Updated", updated!.Name);
        Assert.Equal("Juan", updated.ContactPerson);
        Assert.Equal(originalCode, updated.SupplierCode);

        using var deactivate = Scoped(HttpMethod.Post, $"{Suppliers}/{created.SupplierId:D}/deactivate", orgA);
        using var deactivateResponse = await client.SendAsync(deactivate);
        deactivateResponse.EnsureSuccessStatusCode();
        var inactive = await deactivateResponse.Content.ReadFromJsonAsync<PosSupplierDto>(JsonOptions);
        Assert.Equal("Inactive", inactive!.Status);

        using var activate = Scoped(HttpMethod.Post, $"{Suppliers}/{created.SupplierId:D}/activate", orgA);
        using var activateResponse = await client.SendAsync(activate);
        activateResponse.EnsureSuccessStatusCode();
        var reactivated = await activateResponse.Content.ReadFromJsonAsync<PosSupplierDto>(JsonOptions);
        Assert.Equal("Active", reactivated!.Status);
    }

    [Fact]
    public async Task Stale_expected_updated_at_returns_concurrency_conflict()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var created = await CreateSupplierAsync(client, org, new CreateSupplierRequest("Race Co"));

        using var stale = Scoped(HttpMethod.Put, $"{Suppliers}/{created.SupplierId:D}", org);
        stale.Content = JsonContent.Create(new UpdateSupplierRequest(
            "Stale",
            created.UpdatedAtUtc.AddSeconds(-30)),
            options: JsonOptions);
        using var staleResponse = await client.SendAsync(stale);
        Assert.Equal(HttpStatusCode.Conflict, staleResponse.StatusCode);
        Assert.Equal(ApplicationErrorCodes.SupplierConcurrencyConflict, await ReadErrorCodeAsync(staleResponse));
    }

    [Fact]
    public async Task Reactivation_fails_when_active_name_is_taken()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var original = await CreateSupplierAsync(client, org, new CreateSupplierRequest("Shared Name"));

        using var deactivate = Scoped(HttpMethod.Post, $"{Suppliers}/{original.SupplierId:D}/deactivate", org);
        (await client.SendAsync(deactivate)).EnsureSuccessStatusCode();

        _ = await CreateSupplierAsync(client, org, new CreateSupplierRequest("Shared Name"));

        using var reactivate = Scoped(HttpMethod.Post, $"{Suppliers}/{original.SupplierId:D}/activate", org);
        using var reactivateResponse = await client.SendAsync(reactivate);
        Assert.Equal(HttpStatusCode.Conflict, reactivateResponse.StatusCode);
        Assert.Equal(ApplicationErrorCodes.SupplierNameConflict, await ReadErrorCodeAsync(reactivateResponse));
    }

    [Fact]
    public void Update_supplier_request_has_no_supplier_code_field()
    {
        var properties = typeof(UpdateSupplierRequest)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .ToArray();

        Assert.DoesNotContain(properties, p => string.Equals(p, "SupplierCode", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Manage_and_view_grants_gate_mutations_and_reads()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        using var deniedCreate = Scoped(
            HttpMethod.Post,
            Suppliers,
            org,
            status: PosSubscriptionStatuses.Active,
            grants: PosFeatureCodes.StoreSuppliersView);
        deniedCreate.Content = JsonContent.Create(new CreateSupplierRequest("Denied"), options: JsonOptions);
        using var deniedCreateResponse = await client.SendAsync(deniedCreate);
        Assert.Equal(HttpStatusCode.Forbidden, deniedCreateResponse.StatusCode);

        var created = await CreateSupplierAsync(client, org, new CreateSupplierRequest("Allowed"));

        using var deniedView = Scoped(
            HttpMethod.Get,
            $"{Suppliers}/{created.SupplierId:D}",
            org,
            status: PosSubscriptionStatuses.Active,
            grants: PosFeatureCodes.StoreCatalogView);
        using var deniedViewResponse = await client.SendAsync(deniedView);
        Assert.Equal(HttpStatusCode.Forbidden, deniedViewResponse.StatusCode);

        using var continuityView = Scoped(
            HttpMethod.Get,
            Suppliers,
            org,
            status: PosSubscriptionStatuses.PastDue,
            grants: PosFeatureCodes.StoreSuppliersView);
        using var continuityViewResponse = await client.SendAsync(continuityView);
        continuityViewResponse.EnsureSuccessStatusCode();

        using var continuityCreate = Scoped(
            HttpMethod.Post,
            Suppliers,
            org,
            status: PosSubscriptionStatuses.PastDue,
            grants: PosFeatureCodes.StoreSuppliersView);
        continuityCreate.Content = JsonContent.Create(new CreateSupplierRequest("Nope"), options: JsonOptions);
        using var continuityCreateResponse = await client.SendAsync(continuityCreate);
        Assert.Equal(HttpStatusCode.Forbidden, continuityCreateResponse.StatusCode);

        using var continuityManageDenied = Scoped(
            HttpMethod.Put,
            $"{Suppliers}/{created.SupplierId:D}",
            org,
            status: PosSubscriptionStatuses.PastDue,
            grants: $"{PosFeatureCodes.StoreSuppliersView},{PosFeatureCodes.StoreSuppliersManage}");
        continuityManageDenied.Content = JsonContent.Create(new UpdateSupplierRequest(
            "Still denied",
            created.UpdatedAtUtc),
            options: JsonOptions);
        using var continuityManageResponse = await client.SendAsync(continuityManageDenied);
        Assert.Equal(HttpStatusCode.Forbidden, continuityManageResponse.StatusCode);
    }

    [Fact]
    public async Task Supplier_endpoints_exclude_purchasing_receiving_and_payables_routes()
    {
        var root = FindRepoRoot();
        var endpoints = await File.ReadAllTextAsync(Path.Combine(
            root,
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Api",
            "Suppliers",
            "SupplierEndpoints.cs"));

        foreach (var forbidden in new[]
                 {
                     "MapGroup(\"/api/v1/pos/purchase",
                     "MapGroup(\"/api/v1/pos/receiving",
                     "MapGroup(\"/api/v1/pos/payables",
                     "MapPost(\"/purchase",
                     "MapPost(\"/receive",
                     "PurchaseOrderEndpoints",
                     "GoodsReceiptEndpoints",
                     "AccountsPayableEndpoints"
                 })
        {
            Assert.DoesNotContain(forbidden, endpoints, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static async Task<PosSupplierDto> CreateSupplierAsync(
        HttpClient client,
        Guid org,
        CreateSupplierRequest body)
    {
        using var request = Scoped(HttpMethod.Post, Suppliers, org);
        request.Content = JsonContent.Create(body, options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var supplier = await response.Content.ReadFromJsonAsync<PosSupplierDto>(JsonOptions);
        Assert.NotNull(supplier);
        return supplier!;
    }

    private static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return problem.TryGetProperty("errorCode", out var code) ? code.GetString() : null;
    }

    private static HttpRequestMessage Scoped(
        HttpMethod method,
        string path,
        Guid organizationId,
        string? status = null,
        string? grants = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation(
            PosOrganizationHeaders.OrganizationHeaderName,
            organizationId.ToString("D"));
        request.Headers.TryAddWithoutValidation(
            PosOrganizationHeaders.ActorHeaderName,
            Actor.ToString("D"));

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

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ExItS.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
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
