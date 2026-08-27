using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBuyNowPayLater.Api.Access;
using ExItS.PinoyBuyNowPayLater.Api.Customers;
using ExItS.PinoyBuyNowPayLater.Application;
using ExItS.PinoyBuyNowPayLater.Application.Access;
using ExItS.PinoyBuyNowPayLater.Application.Common;
using ExItS.PinoyBuyNowPayLater.Application.Customers;
using ExItS.PinoyBuyNowPayLater.Domain.Access;
using ExItS.PinoyBuyNowPayLater.Domain.Customers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ExItS.PinoyBuyNowPayLater.UnitTests.Customers;

public sealed class BnplCustomerApiAuthorizationTests
{
    private static readonly Guid ActorId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
    private static readonly Guid OrganizationId = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb");
    private static readonly Guid OtherOrgId = Guid.Parse("dddddddd-dddd-4ddd-8ddd-dddddddddddd");
    private static readonly Guid BranchA = Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc");
    private static readonly Guid BranchB = Guid.Parse("eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee");

    [Fact]
    public async Task Unavailable_context_denies_customer_create()
    {
        await using var host = await CreateHostAsync(null);
        var response = await SendCreateAsync(host, BranchA, "Buyer");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Missing_branch_header_denies()
    {
        await using var host = await CreateHostAsync(FullAccessContext(BnplCapabilityPresets.SalesCapabilities));
        var client = host.GetTestClient();
        var response = await client.PostAsJsonAsync(
            "/api/v1/bnpl/customers",
            new { displayName = "Buyer" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(BnplAccessErrorCodes.BranchRequired, await ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task Wrong_branch_denies()
    {
        await using var host = await CreateHostAsync(new BnplAccessContext(
            ActorId,
            OrganizationId,
            BnplProductIdentity.ProductCode,
            true,
            true,
            true,
            BnplBranchScope.Restricted([BranchA]),
            BnplCapabilityPresets.SalesCapabilities));

        var response = await SendCreateAsync(host, BranchB, "Buyer");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(BnplAccessErrorCodes.BranchDenied, await ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task Read_capability_allows_get_but_not_create()
    {
        await using var host = await CreateHostAsync(FullAccessContext(new[] { BnplCapabilityCodes.CustomerRead }));
        var create = await SendCreateAsync(host, BranchA, "Buyer");
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        Assert.Equal(BnplAccessErrorCodes.CapabilityDenied, await ReadErrorCodeAsync(create));

        // seed via repository
        var repo = host.Services.GetRequiredService<IBnplCustomerRepository>();
        var customer = BnplCustomer.Create(OrganizationId, "Buyer", DateTimeOffset.UtcNow);
        await repo.AddAsync(customer);
        await host.Services.GetRequiredService<IBnplUnitOfWork>().SaveChangesAsync();

        var get = await SendGetAsync(host, BranchA, customer.Id.Value);
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
    }

    [Fact]
    public async Task Manage_capability_allows_create()
    {
        await using var host = await CreateHostAsync(FullAccessContext(new[]
        {
            BnplCapabilityCodes.CustomerRead,
            BnplCapabilityCodes.CustomerManage
        }));

        var response = await SendCreateAsync(host, BranchA, "Buyer");
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Pos_only_capabilities_without_bnpl_assignment_deny()
    {
        await using var host = await CreateHostAsync(new BnplAccessContext(
            ActorId,
            OrganizationId,
            BnplProductIdentity.ProductCode,
            true,
            true,
            hasTrustedProductAssignment: false,
            BnplBranchScope.OrganizationWide(),
            [BnplCapabilityCodes.CustomerManage]));

        var response = await SendCreateAsync(host, BranchA, "Buyer");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(BnplAccessErrorCodes.ProductAccessDenied, await ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task Wrong_organization_context_cannot_see_other_org_customer()
    {
        await using var host = await CreateHostAsync(FullAccessContext(BnplCapabilityPresets.SalesCapabilities));
        var repo = host.Services.GetRequiredService<IBnplCustomerRepository>();
        var customer = BnplCustomer.Create(OtherOrgId, "Secret", DateTimeOffset.UtcNow);
        await repo.AddAsync(customer);
        await host.Services.GetRequiredService<IBnplUnitOfWork>().SaveChangesAsync();

        var get = await SendGetAsync(host, BranchA, customer.Id.Value);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    private static BnplAccessContext FullAccessContext(IEnumerable<string> capabilities) =>
        new(
            ActorId,
            OrganizationId,
            BnplProductIdentity.ProductCode,
            true,
            true,
            true,
            BnplBranchScope.OrganizationWide(),
            capabilities);

    private static async Task<WebApplication> CreateHostAsync(BnplAccessContext? context)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddProblemDetails();
        builder.Services.RemoveAll<IBnplAccessContextProvider>();
        builder.Services.AddSingleton<IBnplAccessContextProvider>(new FixedProvider(context));
        builder.Services.AddSingleton<IBnplOperationalAccessGuard, BnplOperationalAccessGuard>();
        builder.Services.AddBnplApplication();
        builder.Services.AddBnplCustomerUseCases();
        builder.Services.AddSingleton<IBnplCustomerRepository, InMemoryBnplCustomerRepository>();
        builder.Services.AddSingleton<IBnplUnitOfWork, InMemoryUnitOfWork>();

        var app = builder.Build();
        app.MapBnplCustomers();
        await app.StartAsync();
        return app;
    }

    private static async Task<HttpResponseMessage> SendCreateAsync(
        WebApplication host,
        Guid branchId,
        string displayName)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/bnpl/customers")
        {
            Content = JsonContent.Create(new { displayName })
        };
        request.Headers.Add(BnplCustomerEndpoints.BranchHeaderName, branchId.ToString("D"));
        return await host.GetTestClient().SendAsync(request);
    }

    private static async Task<HttpResponseMessage> SendGetAsync(
        WebApplication host,
        Guid branchId,
        Guid customerId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/bnpl/customers/{customerId:D}");
        request.Headers.Add(BnplCustomerEndpoints.BranchHeaderName, branchId.ToString("D"));
        return await host.GetTestClient().SendAsync(request);
    }

    private static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        return document.RootElement.TryGetProperty("errorCode", out var value)
            ? value.GetString()
            : null;
    }

    private sealed class FixedProvider(BnplAccessContext? context) : IBnplAccessContextProvider
    {
        public ValueTask<BnplAccessContext?> GetAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(context);
    }

    private sealed class InMemoryUnitOfWork : IBnplUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class InMemoryBnplCustomerRepository : IBnplCustomerRepository
    {
        private readonly Dictionary<(Guid Org, Guid Id), BnplCustomer> _items = new();

        public Task<BnplCustomer?> GetByIdAsync(
            Guid organizationId,
            BnplCustomerId customerId,
            CancellationToken cancellationToken = default)
        {
            _items.TryGetValue((organizationId, customerId.Value), out var customer);
            return Task.FromResult(customer);
        }

        public Task<BnplCustomer?> FindByLinkedPersonalPublicUserIdAsync(
            Guid organizationId,
            string linkedPersonalPublicUserId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Values.FirstOrDefault(c =>
                c.OrganizationId == organizationId
                && c.LinkedPersonalPublicUserId == linkedPersonalPublicUserId));

        public Task<BnplCustomer?> FindByLinkedCommerceCustomerIdAsync(
            Guid organizationId,
            Guid linkedCommerceCustomerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Values.FirstOrDefault(c =>
                c.OrganizationId == organizationId
                && c.LinkedCommerceCustomerId == linkedCommerceCustomerId));

        public Task<(IReadOnlyList<BnplCustomer> Items, int TotalCount)> SearchAsync(
            Guid organizationId,
            string? search,
            BnplCustomerStatus? status,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            var list = _items.Values.Where(c => c.OrganizationId == organizationId).ToList();
            return Task.FromResult(((IReadOnlyList<BnplCustomer>)list.Skip(skip).Take(take).ToList(), list.Count));
        }

        public Task AddAsync(BnplCustomer customer, CancellationToken cancellationToken = default)
        {
            _items[(customer.OrganizationId, customer.Id.Value)] = customer;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(BnplCustomer customer, CancellationToken cancellationToken = default)
        {
            _items[(customer.OrganizationId, customer.Id.Value)] = customer;
            return Task.CompletedTask;
        }
    }
}
