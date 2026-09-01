using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Api.Customers;
using ExItS.PinoyBusinessPOS.Api.Suppliers;
using ExItS.PinoyBusinessPOS.Application.Branches;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
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

/// <summary>MB2-05 explicit party branch assign/revoke setup API proofs.</summary>
[Collection(PosPostgreSqlCollection.Name)]
public sealed class BranchSetupPartyAccessIntegrationTests(PosPostgreSqlFixture fixture)
{
    private readonly string _connectionString = fixture.ConnectionString;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Guid Main = BranchA;
    private static readonly Guid MicaA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid StoreStaff = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    private const string CustomerAccess = "/api/v1/pos/parties/customers";
    private const string SupplierAccess = "/api/v1/pos/parties/suppliers";
    private const string Customers = "/api/v1/pos/customers";
    private const string Suppliers = "/api/v1/pos/suppliers";

    [Fact]
    public async Task SETUP_PARTY_01_owner_grants_customer_explicit_assign()
    {
        var (factory, client, org) = await CreateScenarioAsync();
        await using var _ = factory;
        var maria = await CreateCustomerAsync(client, org, Main, "Maria Setup");

        using var grant = OwnerScoped(HttpMethod.Post, $"{CustomerAccess}/{maria.CustomerId:D}/branch-access", org, MicaA);
        grant.Content = JsonContent.Create(new GrantPartyBranchAccessRequest(MicaA), options: JsonOptions);
        using var response = await client.SendAsync(grant);
        response.EnsureSuccessStatusCode();

        await using var db = CreateDbContext();
        Assert.True(await db.CustomerBranchAccess.AnyAsync(a =>
            a.OrganizationId == org && a.CustomerId == maria.CustomerId && a.BranchId == MicaA
            && a.GrantSource == nameof(PartyBranchGrantSource.ExplicitAssign)));
    }

    [Fact]
    public async Task SETUP_PARTY_02_staff_cannot_grant_customer_explicit_assign()
    {
        var (factory, client, org) = await CreateScenarioAsync();
        await using var _ = factory;
        await BootstrapOwnerAsync(client, org, OwnerActor);
        await AssignRoleAsync(client, org, OwnerActor, StoreStaff, "StoreManager");
        var maria = await CreateCustomerAsync(client, org, Main, "Maria Staff Deny");

        using var grant = StaffScoped(HttpMethod.Post, $"{CustomerAccess}/{maria.CustomerId:D}/branch-access", org, MicaA, StoreStaff);
        grant.Content = JsonContent.Create(new GrantPartyBranchAccessRequest(MicaA), options: JsonOptions);
        using var response = await client.SendAsync(grant);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SETUP_PARTY_03_owner_revokes_customer_explicit_assign_only()
    {
        var (factory, client, org) = await CreateScenarioAsync();
        await using var _ = factory;
        var maria = await CreateCustomerAsync(client, org, Main, "Maria Revoke Setup");

        using var grant = OwnerScoped(HttpMethod.Post, $"{CustomerAccess}/{maria.CustomerId:D}/branch-access", org, MicaA);
        grant.Content = JsonContent.Create(new GrantPartyBranchAccessRequest(MicaA), options: JsonOptions);
        (await client.SendAsync(grant)).EnsureSuccessStatusCode();

        using var revoke = OwnerScoped(HttpMethod.Delete, $"{CustomerAccess}/{maria.CustomerId:D}/branch-access", org, MicaA);
        revoke.Content = JsonContent.Create(new GrantPartyBranchAccessRequest(MicaA), options: JsonOptions);
        (await client.SendAsync(revoke)).EnsureSuccessStatusCode();

        await using var db = CreateDbContext();
        Assert.False(await db.CustomerBranchAccess.AnyAsync(a =>
            a.GrantSource == nameof(PartyBranchGrantSource.ExplicitAssign)
            && a.CustomerId == maria.CustomerId && a.BranchId == MicaA));
    }

    [Fact]
    public async Task SETUP_PARTY_04_owner_grants_supplier_explicit_assign()
    {
        var (factory, client, org) = await CreateScenarioAsync();
        await using var _ = factory;
        var alpha = await CreateSupplierAsync(client, org, Main, "Alpha Setup");

        using var grant = OwnerScoped(HttpMethod.Post, $"{SupplierAccess}/{alpha.SupplierId:D}/branch-access", org, MicaA);
        grant.Content = JsonContent.Create(new GrantPartyBranchAccessRequest(MicaA), options: JsonOptions);
        using var response = await client.SendAsync(grant);
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task SETUP_PARTY_05_foreign_org_customer_rejected()
    {
        var (factory, client, org) = await CreateScenarioAsync();
        await using var _ = factory;
        var otherOrg = Guid.NewGuid();
        var maria = await CreateCustomerAsync(client, org, Main, "Maria Foreign");

        using var grant = OwnerScoped(HttpMethod.Post, $"{CustomerAccess}/{maria.CustomerId:D}/branch-access", otherOrg, MicaA);
        grant.Content = JsonContent.Create(new GrantPartyBranchAccessRequest(MicaA), options: JsonOptions);
        using var response = await client.SendAsync(grant);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SETUP_PARTY_06_unknown_customer_rejected()
    {
        var (factory, client, org) = await CreateScenarioAsync();
        await using var _ = factory;

        using var grant = OwnerScoped(HttpMethod.Post, $"{CustomerAccess}/{Guid.NewGuid():D}/branch-access", org, MicaA);
        grant.Content = JsonContent.Create(new GrantPartyBranchAccessRequest(MicaA), options: JsonOptions);
        using var response = await client.SendAsync(grant);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SETUP_PARTY_07_grant_is_idempotent_per_source()
    {
        var (factory, client, org) = await CreateScenarioAsync();
        await using var _ = factory;
        var maria = await CreateCustomerAsync(client, org, Main, "Maria Idem Setup");
        var body = JsonContent.Create(new GrantPartyBranchAccessRequest(MicaA), options: JsonOptions);

        using var first = OwnerScoped(HttpMethod.Post, $"{CustomerAccess}/{maria.CustomerId:D}/branch-access", org, MicaA);
        first.Content = body;
        (await client.SendAsync(first)).EnsureSuccessStatusCode();

        using var second = OwnerScoped(HttpMethod.Post, $"{CustomerAccess}/{maria.CustomerId:D}/branch-access", org, MicaA);
        second.Content = JsonContent.Create(new GrantPartyBranchAccessRequest(MicaA), options: JsonOptions);
        (await client.SendAsync(second)).EnsureSuccessStatusCode();

        await using var db = CreateDbContext();
        Assert.Equal(1, await db.CustomerBranchAccess.CountAsync(a =>
            a.CustomerId == maria.CustomerId && a.BranchId == MicaA
            && a.GrantSource == nameof(PartyBranchGrantSource.ExplicitAssign)));
    }

    [Fact]
    public async Task SETUP_PARTY_08_staff_cannot_revoke_supplier_explicit_assign()
    {
        var (factory, client, org) = await CreateScenarioAsync();
        await using var _ = factory;
        await BootstrapOwnerAsync(client, org, OwnerActor);
        await AssignRoleAsync(client, org, OwnerActor, StoreStaff, "StoreManager");
        var alpha = await CreateSupplierAsync(client, org, Main, "Alpha Staff Deny");

        using var revoke = StaffScoped(HttpMethod.Delete, $"{SupplierAccess}/{alpha.SupplierId:D}/branch-access", org, MicaA, StoreStaff);
        revoke.Content = JsonContent.Create(new GrantPartyBranchAccessRequest(MicaA), options: JsonOptions);
        using var response = await client.SendAsync(revoke);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static H1ProofBranchDirectoryOptions CreateBranchOptions() =>
        new() { PrimaryBranchId = Main };

    private async Task<(WebApplicationFactory<Program> Factory, HttpClient Client, Guid Org)> CreateScenarioAsync()
    {
        var branchOptions = CreateBranchOptions();
        var factory = new SetupPartyApiFactory(_connectionString, branchOptions);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        branchOptions.RegisterOrganization(org, Main, MicaA);
        await Task.CompletedTask;
        return (factory, client, org);
    }

    private PosDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<PosDbContext>().UseNpgsql(_connectionString).Options);

    private static async Task<POSCustomerDto> CreateCustomerAsync(HttpClient client, Guid org, Guid branchId, string name)
    {
        using var request = OwnerScoped(HttpMethod.Post, Customers, org, branchId);
        request.Content = JsonContent.Create(new CreateCustomerRequest(name, null, null, null), options: JsonOptions);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<POSCustomerDto>(JsonOptions))!;
    }

    private static async Task<PosSupplierDto> CreateSupplierAsync(HttpClient client, Guid org, Guid branchId, string name)
    {
        using var request = OwnerScoped(HttpMethod.Post, Suppliers, org, branchId);
        request.Content = JsonContent.Create(new CreateSupplierRequest(name), options: JsonOptions);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PosSupplierDto>(JsonOptions))!;
    }

    private const string CustomerMutateGrants =
        $"{PosFeatureCodes.CustomerCreditView},{PosFeatureCodes.CustomerCreditCreate}";
    private const string SupplierManageGrants =
        $"{PosFeatureCodes.StoreSuppliersView},{PosFeatureCodes.StoreSuppliersManage}";
    private const string PartyAssignGrants =
        $"{CustomerMutateGrants},{SupplierManageGrants}";

    private static HttpRequestMessage OwnerScoped(HttpMethod method, string path, Guid org, Guid branchId) =>
        Scoped(method, path, org, OwnerActor, branchId, PartyAssignGrants);

    private static HttpRequestMessage StaffScoped(HttpMethod method, string path, Guid org, Guid branchId, Guid staff) =>
        Scoped(method, path, org, staff, branchId, PosFeatureCodes.CustomerCreditView);

    private static HttpRequestMessage Scoped(HttpMethod method, string path, Guid org, Guid actor, Guid branchId, string grants)
    {
        var request = PosIntegrationRequest.Scoped(method, path, org, actor);
        request.Headers.TryAddWithoutValidation(PosOrganizationHeaders.BranchHeaderName, branchId.ToString("D"));
        request.Headers.TryAddWithoutValidation(PosCommercialHeaders.SubscriptionStatusHeaderName, PosSubscriptionStatuses.Active);
        request.Headers.TryAddWithoutValidation(PosCommercialHeaders.FeatureGrantsHeaderName, grants);
        return request;
    }

    private sealed class SetupPartyApiFactory(string connectionString, H1ProofBranchDirectoryOptions branchOptions)
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
