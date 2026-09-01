using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Api.Customers;
using ExItS.PinoyBusinessPOS.Api.Suppliers;
using ExItS.PinoyBusinessPOS.Application.Branches;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Application.Purchasing;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Application.Suppliers;
using ExItS.PinoyBusinessPOS.Domain.Parties;
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

/// <summary>MB2-04-H1 supplier runtime grants and PRIVACY-04 history scoping proofs.</summary>
[Collection(PosPostgreSqlCollection.Name)]
public sealed class PartyBranchAccess04H1IntegrationTests(PosPostgreSqlFixture fixture)
{
    private readonly string _connectionString = fixture.ConnectionString;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Guid Main = BranchA;
    private static readonly Guid MicaA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid MicaB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid StoreStaff = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    private const string Customers = "/api/v1/pos/customers";
    private const string Suppliers = "/api/v1/pos/suppliers";
    private const string Sales = "/api/v1/pos/sales";
    private const string DirectPurchases = "/api/v1/pos/direct-purchase-receipts";
    private const string PurchaseOrders = "/api/v1/pos/purchase-orders";
    private const string CustomerBranchAccess = "/api/v1/pos/parties/customers";
    private const string SupplierBranchAccess = "/api/v1/pos/parties/suppliers";

    private const string CustomerViewGrants = PosFeatureCodes.CustomerCreditView;
    private const string CustomerRepayGrants =
        $"{PosFeatureCodes.CustomerCreditView},{PosFeatureCodes.CustomerCreditRepay}";
    private const string CheckoutGrants =
        $"{PosFeatureCodes.StoreSalesView},{PosFeatureCodes.StoreSalesCreate},{PosFeatureCodes.CustomerCreditCreate}";
    private const string SupplierManageGrants =
        $"{PosFeatureCodes.StoreSuppliersView},{PosFeatureCodes.StoreSuppliersManage}";
    private const string PurchasingGrants =
        $"{SupplierManageGrants},store-purchasing-manage,{PosFeatureCodes.StoreInventoryView},{PosFeatureCodes.StoreInventoryManage}";

    [Fact]
    public async Task PARTY_H1_SUP_01_direct_purchase_at_mica_a_grants_alpha_at_mica_a_only()
    {
        var (factory, client, org) = await CreateScenarioAsync();
        await using var _ = factory;

        var alpha = await CreateSupplierAtBranchAsync(client, org, Main, "Alpha Wholesale");
        var product = await CreateProductAsync(client, org, "H1 DP Item");
        await EnableTrackedAsync(client, org, product.ProductId, 10m, branchId: MicaA);
        await BootstrapOwnerAsync(client, org, OwnerActor);

        using var create = ScopedBranch(HttpMethod.Post, DirectPurchases, org, OwnerActor, MicaA, PurchasingGrants);
        create.Content = JsonContent.Create(
            new CreateDirectPurchaseReceiptRequest(
                DateOnly.FromDateTime(DateTime.UtcNow),
                [new CreateDirectPurchaseReceiptLineRequest(product.ProductId, 5m, 8m)],
                SupplierId: alpha.SupplierId),
            options: JsonOptions);
        using var response = await client.SendAsync(create);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        await using var db = CreateDbContext();
        Assert.True(await db.SupplierBranchAccess.AnyAsync(a =>
            a.OrganizationId == org
            && a.SupplierId == alpha.SupplierId
            && a.BranchId == MicaA
            && a.GrantSource == nameof(PartyBranchGrantSource.Transaction)));
        Assert.False(await db.SupplierBranchAccess.AnyAsync(a =>
            a.OrganizationId == org
            && a.SupplierId == alpha.SupplierId
            && a.BranchId == MicaB));
    }

    [Fact]
    public async Task PARTY_H1_SUP_02_grn_receive_at_mica_b_grants_supplier_at_mica_b()
    {
        var (factory, client, org) = await CreateScenarioAsync();
        await using var _ = factory;

        var supplier = await CreateSupplierAtBranchAsync(client, org, Main, "Beta Supply");
        var product = await CreateProductAsync(client, org, "H1 GRN Item");
        await EnableTrackedAsync(client, org, product.ProductId, 20m, branchId: MicaB);
        await BootstrapOwnerAsync(client, org, OwnerActor);

        using var createPo = ScopedBranch(HttpMethod.Post, PurchaseOrders, org, OwnerActor, MicaB, PurchasingGrants);
        createPo.Content = JsonContent.Create(
            new CreatePurchaseOrderRequest(
                supplier.SupplierId,
                DateOnly.FromDateTime(DateTime.UtcNow),
                [new CreatePurchaseOrderLineRequest(product.ProductId, 5m, 10m)]),
            options: JsonOptions);
        using var poResponse = await client.SendAsync(createPo);
        poResponse.EnsureSuccessStatusCode();
        var po = await poResponse.Content.ReadFromJsonAsync<PosPurchaseOrderDto>(JsonOptions);

        using var submit = ScopedBranch(HttpMethod.Post, $"{PurchaseOrders}/{po!.PurchaseOrderId:D}/submit", org, OwnerActor, MicaB, PurchasingGrants);
        submit.Headers.TryAddWithoutValidation("X-Pos-Idempotency-Key", Guid.NewGuid().ToString("D"));
        using var submitResponse = await client.SendAsync(submit);
        submitResponse.EnsureSuccessStatusCode();

        using var receive = ScopedBranch(HttpMethod.Post, $"{PurchaseOrders}/{po.PurchaseOrderId:D}/receive", org, OwnerActor, MicaB, PurchasingGrants);
        receive.Content = JsonContent.Create(
            new ReceivePurchaseOrderRequest([new ReceivePurchaseOrderLineRequest(product.ProductId, 5m)]),
            options: JsonOptions);
        using var receiveResponse = await client.SendAsync(receive);
        Assert.Equal(HttpStatusCode.Created, receiveResponse.StatusCode);

        await using var db = CreateDbContext();
        Assert.True(await db.SupplierBranchAccess.AnyAsync(a =>
            a.OrganizationId == org
            && a.SupplierId == supplier.SupplierId
            && a.BranchId == MicaB
            && a.GrantSource == nameof(PartyBranchGrantSource.Transaction)));
    }

    [Fact]
    public async Task PARTY_H1_SUP_03_direct_purchase_grant_is_idempotent_on_retry()
    {
        var (factory, client, org) = await CreateScenarioAsync();
        await using var _ = factory;

        var alpha = await CreateSupplierAtBranchAsync(client, org, Main, "Alpha Wholesale");
        var product = await CreateProductAsync(client, org, "H1 Idem Item");
        await EnableTrackedAsync(client, org, product.ProductId, 10m, branchId: MicaA);
        await BootstrapOwnerAsync(client, org, OwnerActor);

        var idempotencyKey = $"dp-{Guid.NewGuid():N}";
        var body = new CreateDirectPurchaseReceiptRequest(
            DateOnly.FromDateTime(DateTime.UtcNow),
            [new CreateDirectPurchaseReceiptLineRequest(product.ProductId, 2m, 5m)],
            SupplierId: alpha.SupplierId,
            IdempotencyKey: idempotencyKey);

        using var first = ScopedBranch(HttpMethod.Post, DirectPurchases, org, OwnerActor, MicaA, PurchasingGrants);
        first.Content = JsonContent.Create(body, options: JsonOptions);
        using var firstResponse = await client.SendAsync(first);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        using var second = ScopedBranch(HttpMethod.Post, DirectPurchases, org, OwnerActor, MicaA, PurchasingGrants);
        second.Content = JsonContent.Create(body, options: JsonOptions);
        using var secondResponse = await client.SendAsync(second);
        Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);

        await using var db = CreateDbContext();
        var grantCount = await db.SupplierBranchAccess.CountAsync(a =>
            a.OrganizationId == org
            && a.SupplierId == alpha.SupplierId
            && a.BranchId == MicaA
            && a.GrantSource == nameof(PartyBranchGrantSource.Transaction));
        Assert.Equal(1, grantCount);
    }

    [Fact]
    public async Task PRIVACY_04_01_branch_staff_credit_list_scoped_to_acting_branch_sales()
    {
        var (factory, client, org) = await CreateScenarioAsync();
        await using var _ = factory;

        var maria = await CreateCustomerAtBranchAsync(client, org, Main, "Maria Privacy");
        var product = await CreateProductAsync(client, org, "P04 Item");
        await EnableTrackedAsync(client, org, product.ProductId, 50m, branchId: Main);
        await EnableTrackedAsync(client, org, product.ProductId, 50m, branchId: MicaA);
        await SeedBranchStockAsync(client, org, product.ProductId, Main, 5m);
        await SeedBranchStockAsync(client, org, product.ProductId, MicaA, 5m);
        await BootstrapOwnerAsync(client, org, OwnerActor);
        await AssignRoleAsync(client, org, OwnerActor, StoreStaff, "StoreManager");
        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, StoreStaff);

        await CheckoutCreditSaleAsync(client, org, StoreStaff, Main, maria.CustomerId, product.ProductId);
        await CheckoutCreditSaleAsync(client, org, StoreStaff, MicaA, maria.CustomerId, product.ProductId);

        using var ownerEntries = ScopedBranch(
            HttpMethod.Get,
            $"{Customers}/{maria.CustomerId:D}/credit-entries",
            org,
            OwnerActor,
            MicaA,
            CustomerViewGrants);
        using var ownerResponse = await client.SendAsync(ownerEntries);
        ownerResponse.EnsureSuccessStatusCode();
        var ownerPage = await ownerResponse.Content.ReadFromJsonAsync<PagedResult<CreditEntryDto>>(JsonOptions);
        Assert.True(ownerPage!.TotalCount >= 2);

        using var staffEntries = ScopedBranch(
            HttpMethod.Get,
            $"{Customers}/{maria.CustomerId:D}/credit-entries",
            org,
            StoreStaff,
            MicaA,
            CustomerViewGrants);
        using var staffResponse = await client.SendAsync(staffEntries);
        staffResponse.EnsureSuccessStatusCode();
        var staffPage = await staffResponse.Content.ReadFromJsonAsync<PagedResult<CreditEntryDto>>(JsonOptions);
        Assert.Equal(1, staffPage!.TotalCount);
    }

    [Fact]
    public async Task PRIVACY_04_02_branch_staff_without_access_gets_not_found_on_credit_summary()
    {
        var (factory, client, org) = await CreateScenarioAsync();
        await using var _ = factory;

        var maria = await CreateCustomerAtBranchAsync(client, org, Main, "Maria Main Only");
        await BootstrapOwnerAsync(client, org, OwnerActor);
        await AssignRoleAsync(client, org, OwnerActor, StoreStaff, "StoreManager");

        using var summary = ScopedBranch(
            HttpMethod.Get,
            $"{Customers}/{maria.CustomerId:D}/credit-summary",
            org,
            StoreStaff,
            MicaA,
            CustomerViewGrants);
        using var response = await client.SendAsync(summary);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PRIVACY_04_03_explicit_assign_does_not_expose_other_branch_history()
    {
        var (factory, client, org) = await CreateScenarioAsync();
        await using var _ = factory;

        var maria = await CreateCustomerAtBranchAsync(client, org, Main, "Maria Cross Branch");
        var product = await CreateProductAsync(client, org, "P04 Cross");
        await EnableTrackedAsync(client, org, product.ProductId, 25m, branchId: Main);
        await SeedBranchStockAsync(client, org, product.ProductId, Main, 3m);
        await BootstrapOwnerAsync(client, org, OwnerActor);
        await AssignRoleAsync(client, org, OwnerActor, StoreStaff, "StoreManager");
        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, StoreStaff);
        await CheckoutCreditSaleAsync(client, org, StoreStaff, Main, maria.CustomerId, product.ProductId);

        using var grant = ScopedBranch(
            HttpMethod.Post,
            $"{CustomerBranchAccess}/{maria.CustomerId:D}/branch-access",
            org,
            OwnerActor,
            MicaA,
            CustomerViewGrants);
        grant.Content = JsonContent.Create(new GrantPartyBranchAccessRequest(MicaA), options: JsonOptions);
        using var grantResponse = await client.SendAsync(grant);
        grantResponse.EnsureSuccessStatusCode();

        using var staffEntries = ScopedBranch(
            HttpMethod.Get,
            $"{Customers}/{maria.CustomerId:D}/credit-entries",
            org,
            StoreStaff,
            MicaA,
            CustomerViewGrants);
        using var staffResponse = await client.SendAsync(staffEntries);
        staffResponse.EnsureSuccessStatusCode();
        var staffPage = await staffResponse.Content.ReadFromJsonAsync<PagedResult<CreditEntryDto>>(JsonOptions);
        Assert.Equal(0, staffPage!.TotalCount);
    }

    [Fact]
    public async Task PRIVACY_04_04_owner_sees_full_credit_history_after_explicit_assign()
    {
        var (factory, client, org) = await CreateScenarioAsync();
        await using var _ = factory;

        var maria = await CreateCustomerAtBranchAsync(client, org, Main, "Maria Owner View");
        var product = await CreateProductAsync(client, org, "P04 Owner");
        await EnableTrackedAsync(client, org, product.ProductId, 30m, branchId: Main);
        await SeedBranchStockAsync(client, org, product.ProductId, Main, 2m);
        await BootstrapOwnerAsync(client, org, OwnerActor);
        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, OwnerActor);
        await CheckoutCreditSaleAsync(client, org, OwnerActor, Main, maria.CustomerId, product.ProductId);

        using var grant = ScopedBranch(
            HttpMethod.Post,
            $"{CustomerBranchAccess}/{maria.CustomerId:D}/branch-access",
            org,
            OwnerActor,
            MicaA,
            CustomerViewGrants);
        grant.Content = JsonContent.Create(new GrantPartyBranchAccessRequest(MicaA), options: JsonOptions);
        using var grantResponse = await client.SendAsync(grant);
        grantResponse.EnsureSuccessStatusCode();

        using var ownerEntries = ScopedBranch(
            HttpMethod.Get,
            $"{Customers}/{maria.CustomerId:D}/credit-entries",
            org,
            OwnerActor,
            MicaA,
            CustomerViewGrants);
        using var ownerResponse = await client.SendAsync(ownerEntries);
        ownerResponse.EnsureSuccessStatusCode();
        var ownerPage = await ownerResponse.Content.ReadFromJsonAsync<PagedResult<CreditEntryDto>>(JsonOptions);
        Assert.True(ownerPage!.TotalCount >= 1);
    }

    [Fact]
    public async Task PRIVACY_04_05_revoke_explicit_assign_removes_only_explicit_source()
    {
        var (factory, client, org) = await CreateScenarioAsync();
        await using var _ = factory;

        var maria = await CreateCustomerAtBranchAsync(client, org, Main, "Maria Revoke");
        await BootstrapOwnerAsync(client, org, OwnerActor);

        using var grant = ScopedBranch(
            HttpMethod.Post,
            $"{CustomerBranchAccess}/{maria.CustomerId:D}/branch-access",
            org,
            OwnerActor,
            MicaA,
            CustomerViewGrants);
        grant.Content = JsonContent.Create(new GrantPartyBranchAccessRequest(MicaA), options: JsonOptions);
        using var grantResponse = await client.SendAsync(grant);
        grantResponse.EnsureSuccessStatusCode();

        using var revoke = ScopedBranch(
            HttpMethod.Delete,
            $"{CustomerBranchAccess}/{maria.CustomerId:D}/branch-access",
            org,
            OwnerActor,
            MicaA,
            CustomerViewGrants);
        revoke.Content = JsonContent.Create(new GrantPartyBranchAccessRequest(MicaA), options: JsonOptions);
        using var revokeResponse = await client.SendAsync(revoke);
        revokeResponse.EnsureSuccessStatusCode();

        await using var db = CreateDbContext();
        Assert.False(await db.CustomerBranchAccess.AnyAsync(a =>
            a.OrganizationId == org
            && a.CustomerId == maria.CustomerId
            && a.BranchId == MicaA
            && a.GrantSource == nameof(PartyBranchGrantSource.ExplicitAssign)));
        Assert.True(await db.CustomerBranchAccess.AnyAsync(a =>
            a.OrganizationId == org
            && a.CustomerId == maria.CustomerId
            && a.BranchId == Main));
    }

    [Fact]
    public async Task PRIVACY_04_06_branch_staff_ledger_hides_repayments()
    {
        var (factory, client, org) = await CreateScenarioAsync();
        await using var _ = factory;

        var maria = await CreateCustomerAtBranchAsync(client, org, MicaA, "Maria Ledger");
        var product = await CreateProductAsync(client, org, "P04 Ledger");
        await EnableTrackedAsync(client, org, product.ProductId, 40m, branchId: MicaA);
        await SeedBranchStockAsync(client, org, product.ProductId, MicaA, 2m);
        await BootstrapOwnerAsync(client, org, OwnerActor);
        await AssignRoleAsync(client, org, OwnerActor, StoreStaff, "StoreManager");
        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, OwnerActor);
        await CheckoutCreditSaleAsync(client, org, OwnerActor, MicaA, maria.CustomerId, product.ProductId);

        using var repay = ScopedBranch(HttpMethod.Post, $"{Customers}/{maria.CustomerId:D}/repayments", org, OwnerActor, MicaA, CustomerRepayGrants);
        repay.Content = JsonContent.Create(new { amount = 10m, remarks = "partial" }, options: JsonOptions);
        using var repayResponse = await client.SendAsync(repay);
        repayResponse.EnsureSuccessStatusCode();

        using var staffLedger = ScopedBranch(
            HttpMethod.Get,
            $"{Customers}/{maria.CustomerId:D}/ledger",
            org,
            StoreStaff,
            MicaA,
            CustomerViewGrants);
        using var staffResponse = await client.SendAsync(staffLedger);
        staffResponse.EnsureSuccessStatusCode();
        var staffPage = await staffResponse.Content.ReadFromJsonAsync<PagedResult<LedgerEntryDto>>(JsonOptions);
        Assert.DoesNotContain(staffPage!.Items, e => e.EntryType == "Repayment");
    }

    [Fact]
    public async Task PRIVACY_04_07_supplier_explicit_assign_grants_branch_visibility_only()
    {
        var (factory, client, org) = await CreateScenarioAsync();
        await using var _ = factory;

        var alpha = await CreateSupplierAtBranchAsync(client, org, Main, "Alpha Explicit");
        await BootstrapOwnerAsync(client, org, OwnerActor);
        await AssignRoleAsync(client, org, OwnerActor, StoreStaff, "StoreManager");

        using var grant = ScopedBranch(
            HttpMethod.Post,
            $"{SupplierBranchAccess}/{alpha.SupplierId:D}/branch-access",
            org,
            OwnerActor,
            MicaA,
            SupplierManageGrants);
        grant.Content = JsonContent.Create(new GrantPartyBranchAccessRequest(MicaA), options: JsonOptions);
        using var grantResponse = await client.SendAsync(grant);
        grantResponse.EnsureSuccessStatusCode();

        using var visible = ScopedBranch(HttpMethod.Get, $"{Suppliers}/{alpha.SupplierId:D}", org, StoreStaff, MicaA, SupplierManageGrants);
        using var visibleResponse = await client.SendAsync(visible);
        visibleResponse.EnsureSuccessStatusCode();

        using var hidden = ScopedBranch(HttpMethod.Get, $"{Suppliers}/{alpha.SupplierId:D}", org, StoreStaff, MicaB, SupplierManageGrants);
        using var hiddenResponse = await client.SendAsync(hidden);
        Assert.Equal(HttpStatusCode.NotFound, hiddenResponse.StatusCode);
    }

    private static async Task CheckoutCreditSaleAsync(
        HttpClient client,
        Guid org,
        Guid actor,
        Guid branchId,
        Guid customerId,
        Guid productId)
    {
        using var checkout = ScopedBranch(HttpMethod.Post, Sales, org, actor, branchId, CheckoutGrants);
        checkout.Content = JsonContent.Create(
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(productId, 1m)],
                "Utang",
                CustomerId: customerId),
            options: JsonOptions);
        using var response = await client.SendAsync(checkout);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static H1ProofBranchDirectoryOptions CreateBranchOptions()
    {
        var options = new H1ProofBranchDirectoryOptions { PrimaryBranchId = Main };
        return options;
    }

    private async Task<(WebApplicationFactory<Program> Factory, HttpClient Client, Guid Org)> CreateScenarioAsync()
    {
        var branchOptions = CreateBranchOptions();
        var factory = CreateFactory(branchOptions);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        branchOptions.RegisterOrganization(org, Main, MicaA, MicaB);
        await Task.CompletedTask;
        return (factory, client, org);
    }

    private WebApplicationFactory<Program> CreateFactory(H1ProofBranchDirectoryOptions branchOptions) =>
        new PartyAccessH1ApiFactory(_connectionString, branchOptions);

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
        string displayName)
    {
        using var request = ScopedBranch(
            HttpMethod.Post,
            Customers,
            org,
            OwnerActor,
            branchId,
            $"{PosFeatureCodes.CustomerCreditView},{PosFeatureCodes.CustomerCreditCreate}");
        request.Content = JsonContent.Create(new CreateCustomerRequest(displayName, null, null, null), options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<POSCustomerDto>(JsonOptions))!;
    }

    private static async Task<PosSupplierDto> CreateSupplierAtBranchAsync(
        HttpClient client,
        Guid org,
        Guid branchId,
        string name)
    {
        using var request = ScopedBranch(
            HttpMethod.Post,
            Suppliers,
            org,
            OwnerActor,
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

    private sealed class PartyAccessH1ApiFactory(string connectionString, H1ProofBranchDirectoryOptions branchOptions)
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