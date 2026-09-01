using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Api.Customers;
using ExItS.PinoyBusinessPOS.Api.Suppliers;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Application.Suppliers;
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

/// <summary>MB2-04 customer/supplier branch ACL and privacy proofs.</summary>
[Collection(PosPostgreSqlCollection.Name)]
public sealed class BranchCustomerSupplierAccessIntegrationTests(PosPostgreSqlFixture fixture)
{
    private readonly string _connectionString = fixture.ConnectionString;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Guid Main = BranchA;
    private static readonly Guid Remote = BranchB;
    private static readonly Guid MicaA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid MicaB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid StoreStaff = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    private const string Customers = "/api/v1/pos/customers";
    private const string Suppliers = "/api/v1/pos/suppliers";
    private const string Sales = "/api/v1/pos/sales";

    private const string CustomerViewGrants = PosFeatureCodes.CustomerCreditView;
    private const string CustomerMutateGrants =
        $"{PosFeatureCodes.CustomerCreditView},{PosFeatureCodes.CustomerCreditCreate}";
    private const string CheckoutGrants =
        $"{PosFeatureCodes.StoreSalesView},{PosFeatureCodes.StoreSalesCreate},{PosFeatureCodes.CustomerCreditCreate}";
    private const string SupplierViewGrants = PosFeatureCodes.StoreSuppliersView;
    private const string SupplierManageGrants =
        $"{PosFeatureCodes.StoreSuppliersView},{PosFeatureCodes.StoreSuppliersManage}";

    [Fact]
    public async Task CUSTOMER_SEC_01_remote_staff_cannot_list_main_only_customer()
    {
        var (factory, client, org) = await CreateScenarioAsync();
        await using var _ = factory;

        var maria = await CreateCustomerAtBranchAsync(client, org, Main, "Maria Santos");
        await BootstrapOwnerAsync(client, org, OwnerActor);
        await AssignRoleAsync(client, org, OwnerActor, StoreStaff, "StoreManager");

        using var list = ScopedBranch(
            HttpMethod.Get,
            $"{Customers}?search=Maria",
            org,
            StoreStaff,
            Remote,
            CustomerViewGrants);
        using var response = await client.SendAsync(list);
        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<PagedResult<POSCustomerDto>>(JsonOptions);
        Assert.NotNull(page);
        Assert.DoesNotContain(page!.Items, c => c.CustomerId == maria.CustomerId);
    }

    [Fact]
    public async Task CUSTOMER_SEC_02_remote_staff_cannot_get_main_only_customer()
    {
        var (factory, client, org) = await CreateScenarioAsync();
        await using var _ = factory;

        var maria = await CreateCustomerAtBranchAsync(client, org, Main, "Maria Santos");
        await BootstrapOwnerAsync(client, org, OwnerActor);
        await AssignRoleAsync(client, org, OwnerActor, StoreStaff, "StoreManager");

        using var get = ScopedBranch(HttpMethod.Get, $"{Customers}/{maria.CustomerId:D}", org, StoreStaff, Remote, CustomerViewGrants);
        using var response = await client.SendAsync(get);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ApplicationErrorCodes.CustomerNotFound, await ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task CUSTOMER_SEC_03_remote_staff_cannot_checkout_search_main_only_customer()
    {
        var (factory, client, org) = await CreateScenarioAsync();
        await using var _ = factory;

        await CreateCustomerAtBranchAsync(client, org, Main, "Maria Santos");
        await BootstrapOwnerAsync(client, org, OwnerActor);
        await AssignRoleAsync(client, org, OwnerActor, StoreStaff, "StoreManager");

        using var search = ScopedBranch(
            HttpMethod.Get,
            $"{Customers}/checkout-search?search=Maria",
            org,
            StoreStaff,
            Remote,
            CheckoutGrants);
        using var response = await client.SendAsync(search);
        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<CheckoutCustomerSearchResult>(JsonOptions);
        Assert.NotNull(page);
        Assert.Empty(page!.Items);
    }

    [Fact]
    public async Task CUSTOMER_SEC_04_owner_can_view_main_only_customer_from_remote_branch()
    {
        var (factory, client, org) = await CreateScenarioAsync();
        await using var _ = factory;

        var maria = await CreateCustomerAtBranchAsync(client, org, Main, "Maria Santos");

        using var get = ScopedBranch(HttpMethod.Get, $"{Customers}/{maria.CustomerId:D}", org, OwnerActor, Remote, CustomerViewGrants);
        using var response = await client.SendAsync(get);
        response.EnsureSuccessStatusCode();
        var customer = await response.Content.ReadFromJsonAsync<POSCustomerDto>(JsonOptions);
        Assert.Equal("Maria Santos", customer!.DisplayName);
    }

    [Fact]
    public async Task CUSTOMER_SEC_05_create_at_branch_grants_access_only_at_acting_branch()
    {
        var (factory, client, org) = await CreateScenarioAsync();
        await using var _ = factory;

        await BootstrapOwnerAsync(client, org, OwnerActor);
        await AssignRoleAsync(client, org, OwnerActor, StoreStaff, "StoreManager");

        var created = await CreateCustomerAtBranchAsync(client, org, MicaA, "Branch Local", StoreStaff);

        using var visible = ScopedBranch(HttpMethod.Get, $"{Customers}/{created.CustomerId:D}", org, StoreStaff, MicaA, CustomerViewGrants);
        using var visibleResponse = await client.SendAsync(visible);
        visibleResponse.EnsureSuccessStatusCode();

        using var hidden = ScopedBranch(HttpMethod.Get, $"{Customers}/{created.CustomerId:D}", org, StoreStaff, MicaB, CustomerViewGrants);
        using var hiddenResponse = await client.SendAsync(hidden);
        Assert.Equal(HttpStatusCode.NotFound, hiddenResponse.StatusCode);
    }

    [Fact]
    public async Task CUSTOMER_SEC_06_sale_checkout_grants_transaction_access_at_sale_branch()
    {
        var (factory, client, org) = await CreateScenarioAsync();
        await using var _ = factory;

        var maria = await CreateCustomerAtBranchAsync(client, org, Main, "Maria Santos");
        var product = await CreateProductAsync(client, org, "SEC06 Item");
        await EnableTrackedAsync(client, org, product.ProductId, 10m, branchId: MicaA);
        await SeedBranchStockAsync(client, org, product.ProductId, MicaA, 5m);
        await BootstrapOwnerAsync(client, org, OwnerActor);
        await AssignRoleAsync(client, org, OwnerActor, StoreStaff, "StoreManager");
        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, StoreStaff);

        using var checkout = ScopedBranch(HttpMethod.Post, Sales, org, StoreStaff, MicaA, CheckoutGrants);
        checkout.Content = JsonContent.Create(
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                "Cash",
                AmountTendered: 100m,
                CustomerId: maria.CustomerId),
            options: JsonOptions);
        using var checkoutResponse = await client.SendAsync(checkout);
        Assert.Equal(HttpStatusCode.Created, checkoutResponse.StatusCode);

        using var get = ScopedBranch(HttpMethod.Get, $"{Customers}/{maria.CustomerId:D}", org, StoreStaff, MicaA, CustomerViewGrants);
        using var getResponse = await client.SendAsync(get);
        getResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CUSTOMER_SEC_07_credit_endpoints_require_branch_access()
    {
        var (factory, client, org) = await CreateScenarioAsync();
        await using var _ = factory;

        var maria = await CreateCustomerAtBranchAsync(client, org, Main, "Maria Santos");
        await BootstrapOwnerAsync(client, org, OwnerActor);
        await AssignRoleAsync(client, org, OwnerActor, StoreStaff, "StoreManager");

        using var summary = ScopedBranch(
            HttpMethod.Get,
            $"{Customers}/{maria.CustomerId:D}/credit-summary",
            org,
            StoreStaff,
            Remote,
            CustomerViewGrants);
        using var response = await client.SendAsync(summary);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CUSTOMER_SEC_08_mica_a_staff_cannot_see_mica_b_only_customer()
    {
        var (factory, client, org) = await CreateScenarioAsync();
        await using var _ = factory;

        var micaBOnly = await CreateCustomerAtBranchAsync(client, org, MicaB, "Mica B Regular");
        await BootstrapOwnerAsync(client, org, OwnerActor);
        await AssignRoleAsync(client, org, OwnerActor, StoreStaff, "StoreManager");

        using var get = ScopedBranch(HttpMethod.Get, $"{Customers}/{micaBOnly.CustomerId:D}", org, StoreStaff, MicaA, CustomerViewGrants);
        using var response = await client.SendAsync(get);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SUPPLIER_SEC_01_remote_staff_cannot_list_main_only_supplier()
    {
        var (factory, client, org) = await CreateScenarioAsync();
        await using var _ = factory;

        var alpha = await CreateSupplierAtBranchAsync(client, org, Main, "Alpha Wholesale");
        await BootstrapOwnerAsync(client, org, OwnerActor);
        await AssignRoleAsync(client, org, OwnerActor, StoreStaff, "StoreManager");

        using var list = ScopedBranch(HttpMethod.Get, $"{Suppliers}?name=Alpha", org, StoreStaff, Remote, SupplierViewGrants);
        using var response = await client.SendAsync(list);
        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<PagedResult<PosSupplierDto>>(JsonOptions);
        Assert.DoesNotContain(page!.Items, s => s.SupplierId == alpha.SupplierId);
    }

    [Fact]
    public async Task SUPPLIER_SEC_02_remote_staff_cannot_get_main_only_supplier()
    {
        var (factory, client, org) = await CreateScenarioAsync();
        await using var _ = factory;

        var alpha = await CreateSupplierAtBranchAsync(client, org, Main, "Alpha Wholesale");
        await BootstrapOwnerAsync(client, org, OwnerActor);
        await AssignRoleAsync(client, org, OwnerActor, StoreStaff, "StoreManager");

        using var get = ScopedBranch(HttpMethod.Get, $"{Suppliers}/{alpha.SupplierId:D}", org, StoreStaff, Remote, SupplierViewGrants);
        using var response = await client.SendAsync(get);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SUPPLIER_SEC_03_owner_can_view_main_only_supplier_from_remote_branch()
    {
        var (factory, client, org) = await CreateScenarioAsync();
        await using var _ = factory;

        var alpha = await CreateSupplierAtBranchAsync(client, org, Main, "Alpha Wholesale");

        using var get = ScopedBranch(HttpMethod.Get, $"{Suppliers}/{alpha.SupplierId:D}", org, OwnerActor, Remote, SupplierViewGrants);
        using var response = await client.SendAsync(get);
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task SUPPLIER_SEC_04_create_at_branch_grants_supplier_access_only_at_acting_branch()
    {
        var (factory, client, org) = await CreateScenarioAsync();
        await using var _ = factory;

        await BootstrapOwnerAsync(client, org, OwnerActor);
        await AssignRoleAsync(client, org, OwnerActor, StoreStaff, "StoreManager");

        var created = await CreateSupplierAtBranchAsync(client, org, MicaA, "Mica A Vendor", StoreStaff);

        using var visible = ScopedBranch(HttpMethod.Get, $"{Suppliers}/{created.SupplierId:D}", org, StoreStaff, MicaA, SupplierViewGrants);
        using var visibleResponse = await client.SendAsync(visible);
        visibleResponse.EnsureSuccessStatusCode();

        using var hidden = ScopedBranch(HttpMethod.Get, $"{Suppliers}/{created.SupplierId:D}", org, StoreStaff, MicaB, SupplierViewGrants);
        using var hiddenResponse = await client.SendAsync(hidden);
        Assert.Equal(HttpStatusCode.NotFound, hiddenResponse.StatusCode);
    }

    [Fact]
    public async Task SUPPLIER_SEC_05_main_staff_can_view_main_supplier()
    {
        var (factory, client, org) = await CreateScenarioAsync();
        await using var _ = factory;

        var alpha = await CreateSupplierAtBranchAsync(client, org, Main, "Alpha Wholesale");
        await BootstrapOwnerAsync(client, org, OwnerActor);
        await AssignRoleAsync(client, org, OwnerActor, StoreStaff, "StoreManager");

        using var get = ScopedBranch(HttpMethod.Get, $"{Suppliers}/{alpha.SupplierId:D}", org, StoreStaff, Main, SupplierViewGrants);
        using var response = await client.SendAsync(get);
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task SUPPLIER_SEC_06_mica_b_staff_cannot_see_main_only_supplier()
    {
        var (factory, client, org) = await CreateScenarioAsync();
        await using var _ = factory;

        var alpha = await CreateSupplierAtBranchAsync(client, org, Main, "Alpha Wholesale");
        await BootstrapOwnerAsync(client, org, OwnerActor);
        await AssignRoleAsync(client, org, OwnerActor, StoreStaff, "StoreManager");

        using var get = ScopedBranch(HttpMethod.Get, $"{Suppliers}/{alpha.SupplierId:D}", org, StoreStaff, MicaB, SupplierViewGrants);
        using var response = await client.SendAsync(get);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SUPPLIER_SEC_07_owner_lists_all_suppliers_from_any_branch()
    {
        var (factory, client, org) = await CreateScenarioAsync();
        await using var _ = factory;

        var alpha = await CreateSupplierAtBranchAsync(client, org, Main, "Alpha Wholesale");
        await CreateSupplierAtBranchAsync(client, org, MicaA, "Mica Vendor");

        using var list = ScopedBranch(HttpMethod.Get, Suppliers, org, OwnerActor, Remote, SupplierViewGrants);
        using var response = await client.SendAsync(list);
        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<PagedResult<PosSupplierDto>>(JsonOptions);
        Assert.Contains(page!.Items, s => s.SupplierId == alpha.SupplierId);
        Assert.True(page.Items.Count >= 2);
    }

    [Fact]
    public async Task SUPPLIER_SEC_08_branch_staff_list_is_scoped_to_accessible_suppliers_only()
    {
        var (factory, client, org) = await CreateScenarioAsync();
        await using var _ = factory;

        await CreateSupplierAtBranchAsync(client, org, Main, "Alpha Wholesale");
        var micaOnly = await CreateSupplierAtBranchAsync(client, org, MicaA, "Mica A Vendor");
        await BootstrapOwnerAsync(client, org, OwnerActor);
        await AssignRoleAsync(client, org, OwnerActor, StoreStaff, "StoreManager");

        using var list = ScopedBranch(HttpMethod.Get, Suppliers, org, StoreStaff, MicaA, SupplierViewGrants);
        using var response = await client.SendAsync(list);
        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<PagedResult<PosSupplierDto>>(JsonOptions);
        Assert.Contains(page!.Items, s => s.SupplierId == micaOnly.SupplierId);
        Assert.DoesNotContain(page.Items, s => s.Name == "Alpha Wholesale");
    }

    [Fact]
    public async Task MICA_E2E_maria_main_customer_and_alpha_supplier_branch_isolation()
    {
        var branchOptions = CreateBranchOptions();
        branchOptions.RestrictActor(StoreStaff, MicaA);
        await using var factory = CreateFactory(branchOptions);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        branchOptions.RegisterOrganization(org, Main, Remote, MicaA, MicaB);

        var maria = await CreateCustomerAtBranchAsync(client, org, Main, "Maria");
        var alpha = await CreateSupplierAtBranchAsync(client, org, Main, "Alpha Wholesale");
        await BootstrapOwnerAsync(client, org, OwnerActor);
        await AssignRoleAsync(client, org, OwnerActor, StoreStaff, "StoreManager");

        using var mariaAtMicaA = ScopedBranch(HttpMethod.Get, $"{Customers}/{maria.CustomerId:D}", org, StoreStaff, MicaA, CustomerViewGrants);
        using var mariaHidden = await client.SendAsync(mariaAtMicaA);
        Assert.Equal(HttpStatusCode.NotFound, mariaHidden.StatusCode);

        using var alphaAtMicaA = ScopedBranch(HttpMethod.Get, $"{Suppliers}/{alpha.SupplierId:D}", org, StoreStaff, MicaA, SupplierViewGrants);
        using var alphaHidden = await client.SendAsync(alphaAtMicaA);
        Assert.Equal(HttpStatusCode.NotFound, alphaHidden.StatusCode);

        using var mariaOwner = ScopedBranch(HttpMethod.Get, $"{Customers}/{maria.CustomerId:D}", org, OwnerActor, MicaB, CustomerViewGrants);
        using var mariaOwnerResponse = await client.SendAsync(mariaOwner);
        mariaOwnerResponse.EnsureSuccessStatusCode();

        await using var db = CreateDbContext();
        Assert.True(await db.CustomerBranchAccess.AnyAsync(
            a => a.OrganizationId == org && a.CustomerId == maria.CustomerId && a.BranchId == Main));
        Assert.True(await db.SupplierBranchAccess.AnyAsync(
            a => a.OrganizationId == org && a.SupplierId == alpha.SupplierId && a.BranchId == Main));
    }

    private static H1ProofBranchDirectoryOptions CreateBranchOptions()
    {
        var options = new H1ProofBranchDirectoryOptions();
        options.PrimaryBranchId = Main;
        return options;
    }

    private async Task<(WebApplicationFactory<Program> Factory, HttpClient Client, Guid Org)> CreateScenarioAsync()
    {
        var branchOptions = CreateBranchOptions();
        var factory = CreateFactory(branchOptions);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        branchOptions.RegisterOrganization(org, Main, Remote, MicaA, MicaB);
        await Task.CompletedTask;
        return (factory, client, org);
    }

    private WebApplicationFactory<Program> CreateFactory(H1ProofBranchDirectoryOptions branchOptions) =>
        new PartyAccessApiFactory(_connectionString, branchOptions);

    private PosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PosDbContext>()
            .UseNpgsql(_connectionString)
            .Options;
        return new PosDbContext(options);
    }

    private static async Task<POSCustomerDto> CreateCustomerAtBranchAsync(
        HttpClient client,
        Guid org,
        Guid branchId,
        string displayName,
        Guid? actorId = null)
    {
        using var request = ScopedBranch(
            HttpMethod.Post,
            Customers,
            org,
            actorId ?? OwnerActor,
            branchId,
            CustomerMutateGrants);
        request.Content = JsonContent.Create(new CreateCustomerRequest(displayName, null, null, null), options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<POSCustomerDto>(JsonOptions))!;
    }

    private static async Task<PosSupplierDto> CreateSupplierAtBranchAsync(
        HttpClient client,
        Guid org,
        Guid branchId,
        string name,
        Guid? actorId = null)
    {
        using var request = ScopedBranch(
            HttpMethod.Post,
            Suppliers,
            org,
            actorId ?? OwnerActor,
            branchId,
            SupplierManageGrants);
        request.Content = JsonContent.Create(new CreateSupplierRequest(name), options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PosSupplierDto>(JsonOptions))!;
    }

    private static HttpRequestMessage ScopedBranch(
        HttpMethod method,
        string path,
        Guid org,
        Guid actor,
        Guid branchId,
        string grants)
    {
        var request = PosIntegrationRequest.Scoped(method, path, org, actor);
        request.Headers.TryAddWithoutValidation(PosOrganizationHeaders.BranchHeaderName, branchId.ToString("D"));
        request.Headers.TryAddWithoutValidation(PosCommercialHeaders.SubscriptionStatusHeaderName, PosSubscriptionStatuses.Active);
        request.Headers.TryAddWithoutValidation(PosCommercialHeaders.FeatureGrantsHeaderName, grants);
        return request;
    }

    private static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return problem.TryGetProperty("errorCode", out var code) ? code.GetString() : null;
    }

    private sealed class PartyAccessApiFactory(string connectionString, H1ProofBranchDirectoryOptions branchOptions)
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
