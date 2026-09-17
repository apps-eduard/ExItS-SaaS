using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.ConnectedSuppliers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosBusinessCustomerCreditPolicyApiTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly Guid Actor = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    [Fact]
    public async Task Get_credit_policy_returns_200_NotConfigured_for_active_seller_connection()
    {
        var sellerOrg = Guid.NewGuid();
        var buyerOrg = Guid.NewGuid();
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var connectionId = await SeedActiveRelationshipAsync(factory, sellerOrg, buyerOrg);

        var client = factory.CreateClient();
        using var request = Scoped(
            HttpMethod.Get,
            $"/api/v1/pos/connected-suppliers/business-customers/{connectionId:D}/credit-policy",
            sellerOrg);
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<BusinessCustomerCreditPolicyReadDto>(JsonOptions);
        Assert.NotNull(dto);
        Assert.Equal(connectionId, dto!.ConnectionId);
        Assert.Equal(sellerOrg, dto.SellerOrganizationId);
        Assert.Equal(buyerOrg, dto.BuyerOrganizationId);
        Assert.Equal("NotConfigured", dto.Status);
        Assert.Null(dto.CreditLimit);
        Assert.Null(dto.DefaultTermDays);
        Assert.Equal(0m, dto.OutstandingAmount);
        Assert.Equal(0m, dto.AvailableCredit);
    }

    [Fact]
    public async Task Get_credit_policy_is_registered_without_AmbiguousMatch_on_leaf()
    {
        var sellerOrg = Guid.NewGuid();
        var buyerOrg = Guid.NewGuid();
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var connectionId = await SeedActiveRelationshipAsync(factory, sellerOrg, buyerOrg);
        var client = factory.CreateClient();

        using var request = Scoped(
            HttpMethod.Get,
            $"/api/v1/pos/connected-suppliers/business-customers/{connectionId:D}/credit-policy",
            sellerOrg);
        using var response = await client.SendAsync(request);

        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var endpointSource = await File.ReadAllTextAsync(
            Path.Combine(
                FindRepoRoot(),
                "src",
                "Products",
                "PinoyBusinessPOS",
                "ExItS.PinoyBusinessPOS.Api",
                "Credit",
                "BusinessCustomerCreditPolicyEndpoints.cs"));
        Assert.Contains("MapBusinessCustomerCreditPolicyEndpoints", endpointSource, StringComparison.Ordinal);
        Assert.DoesNotContain("group.MapGet(\"\",", endpointSource, StringComparison.Ordinal);
        Assert.DoesNotContain("group.MapPut(\"\",", endpointSource, StringComparison.Ordinal);

        var programSource = await File.ReadAllTextAsync(
            Path.Combine(
                FindRepoRoot(),
                "src",
                "Products",
                "PinoyBusinessPOS",
                "ExItS.PinoyBusinessPOS.Api",
                "Program.cs"));
        Assert.Contains("MapBusinessCustomerCreditPolicyEndpoints()", programSource, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Get_credit_policy_cross_org_fails_closed()
    {
        var sellerOrg = Guid.NewGuid();
        var buyerOrg = Guid.NewGuid();
        var otherOrg = Guid.NewGuid();
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var connectionId = await SeedActiveRelationshipAsync(factory, sellerOrg, buyerOrg);
        var client = factory.CreateClient();

        using var forged = Scoped(
            HttpMethod.Get,
            $"/api/v1/pos/connected-suppliers/business-customers/{connectionId:D}/credit-policy",
            otherOrg);
        using var forgedResponse = await client.SendAsync(forged);
        Assert.Equal(HttpStatusCode.NotFound, forgedResponse.StatusCode);

        using var buyerAsSeller = Scoped(
            HttpMethod.Get,
            $"/api/v1/pos/connected-suppliers/business-customers/{connectionId:D}/credit-policy",
            buyerOrg);
        using var buyerResponse = await client.SendAsync(buyerAsSeller);
        Assert.Equal(HttpStatusCode.NotFound, buyerResponse.StatusCode);
    }

    [Fact]
    public async Task Upsert_approve_flow_and_no_pos_customer_stub_required()
    {
        var sellerOrg = Guid.NewGuid();
        var buyerOrg = Guid.NewGuid();
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var connectionId = await SeedActiveRelationshipAsync(factory, sellerOrg, buyerOrg);
        var client = factory.CreateClient();

        await using (var db = CreateDb(factory))
        {
            Assert.Equal(0, await db.Customers.CountAsync(c => c.OrganizationId == sellerOrg));
        }

        using var put = Scoped(
            HttpMethod.Put,
            $"/api/v1/pos/connected-suppliers/business-customers/{connectionId:D}/credit-policy",
            sellerOrg);
        put.Content = JsonContent.Create(new UpsertBusinessCustomerCreditPolicyRequest(50_000m, 30, "cfg"));
        using var putResponse = await client.SendAsync(put);
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);
        var pending = await putResponse.Content.ReadFromJsonAsync<BusinessCustomerCreditPolicyReadDto>(JsonOptions);
        Assert.Equal("PendingApproval", pending!.Status);

        // Re-read so ExpectedUpdatedAtUtc matches PostgreSQL-persisted precision.
        using var getPending = Scoped(
            HttpMethod.Get,
            $"/api/v1/pos/connected-suppliers/business-customers/{connectionId:D}/credit-policy",
            sellerOrg);
        using var getPendingResponse = await client.SendAsync(getPending);
        Assert.Equal(HttpStatusCode.OK, getPendingResponse.StatusCode);
        pending = await getPendingResponse.Content.ReadFromJsonAsync<BusinessCustomerCreditPolicyReadDto>(JsonOptions);
        Assert.NotNull(pending!.ExpectedUpdatedAtUtc);

        using var approve = Scoped(
            HttpMethod.Post,
            $"/api/v1/pos/connected-suppliers/business-customers/{connectionId:D}/credit-policy/approve",
            sellerOrg);
        approve.Content = JsonContent.Create(
            new ApproveBusinessCustomerCreditPolicyRequest("ok", pending.ExpectedUpdatedAtUtc!.Value));
        using var approveResponse = await client.SendAsync(approve);
        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);
        var approved = await approveResponse.Content.ReadFromJsonAsync<BusinessCustomerCreditPolicyReadDto>(JsonOptions);
        Assert.Equal("Approved", approved!.Status);
        Assert.Equal(50_000m, approved.AvailableCredit);

        await using (var db = CreateDb(factory))
        {
            Assert.Equal(0, await db.Customers.CountAsync(c => c.OrganizationId == sellerOrg));
            Assert.Equal(1, await db.BusinessCustomerCreditPolicies.CountAsync(p =>
                p.SellerOrganizationId == sellerOrg && p.BuyerOrganizationId == buyerOrg));
        }
    }

    private async Task<Guid> SeedActiveRelationshipAsync(
        PosApiFactory factory,
        Guid sellerOrg,
        Guid buyerOrg)
    {
        var connectionId = Guid.NewGuid();
        var now = DateTimeOffset.Parse("2026-09-10T12:00:00Z");
        await using var db = CreateDb(factory);
        await db.Database.MigrateAsync();
        db.ConnectedSupplierRelationships.Add(new ConnectedSupplierRelationshipRecord
        {
            Id = connectionId,
            BuyerOrganizationId = buyerOrg,
            SupplierOrganizationId = sellerOrg,
            Status = 1, // Active
            RequestedAtUtc = now,
            RespondedAtUtc = now,
            InitiatedByParty = 1, // Supplier (business customer invite)
            BuyerDisplayNameSnapshot = "Kizy Bakery",
            BuyerPublicOrganizationIdSnapshot = "ORGKIZY01",
            SupplierDisplayNameSnapshot = "Mica Grocery",
            SupplierPublicOrganizationIdSnapshot = "ORGMICA01",
            CatalogSharingMode = 0,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        await db.SaveChangesAsync();
        return connectionId;
    }

    private static PosDbContext CreateDb(PosApiFactory factory)
    {
        var options = new DbContextOptionsBuilder<PosDbContext>()
            .UseNpgsql(factory.ConnectionString)
            .Options;
        return new PosDbContext(options);
    }

    private static HttpRequestMessage Scoped(HttpMethod method, string path, Guid organizationId)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation(
            PosOrganizationHeaders.OrganizationHeaderName,
            organizationId.ToString("D"));
        request.Headers.TryAddWithoutValidation(
            PosOrganizationHeaders.ActorHeaderName,
            Actor.ToString("D"));
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

    private sealed class PosApiFactory : WebApplicationFactory<Program>
    {
        public PosApiFactory(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public string ConnectionString { get; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:PosDatabase", ConnectionString);
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PosDatabase"] = ConnectionString
                });
            });
        }
    }
}
