using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBuyNowPayLater.Api.Access;
using ExItS.PinoyBuyNowPayLater.Api.Financing;
using ExItS.PinoyBuyNowPayLater.Application;
using ExItS.PinoyBuyNowPayLater.Application.Access;
using ExItS.PinoyBuyNowPayLater.Application.Common;
using ExItS.PinoyBuyNowPayLater.Application.Customers;
using ExItS.PinoyBuyNowPayLater.Application.Financing;
using ExItS.PinoyBuyNowPayLater.Domain.Access;
using ExItS.PinoyBuyNowPayLater.Domain.Customers;
using ExItS.PinoyBuyNowPayLater.Domain.Financing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ExItS.PinoyBuyNowPayLater.UnitTests.Financing;

public sealed class BnplInstallmentPlanApiAuthorizationTests
{
    private static readonly Guid ActorId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
    private static readonly Guid OrganizationId = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb");
    private static readonly Guid BranchA = Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc");
    private static readonly Guid BranchB = Guid.Parse("eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee");
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-08-27T12:00:00Z");

    [Fact]
    public async Task Plan_read_allows_get_but_not_put()
    {
        await using var host = await CreateHostAsync(FullAccess(
            BnplCapabilityCodes.PlanRead,
            BnplCapabilityCodes.ApplicationRead));
        var (applicationId, offerId) = await SeedOfferedAsync(host);

        var put = await PutPlanAsync(host, BranchA, applicationId, offerId);
        Assert.Equal(HttpStatusCode.Forbidden, put.StatusCode);
        Assert.Equal(BnplAccessErrorCodes.CapabilityDenied, await ReadErrorCodeAsync(put));

        // seed plan via aggregate
        await AttachPlanDirectAsync(host, applicationId, offerId);
        var get = await GetPlanAsync(host, BranchA, applicationId, offerId);
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
    }

    [Fact]
    public async Task Plan_manage_allows_put()
    {
        await using var host = await CreateHostAsync(FullAccess(
            BnplCapabilityCodes.PlanManage,
            BnplCapabilityCodes.PlanRead));
        var (applicationId, offerId) = await SeedOfferedAsync(host);
        var put = await PutPlanAsync(host, BranchA, applicationId, offerId);
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
    }

    [Fact]
    public async Task Wrong_branch_denies_plan_mutation()
    {
        await using var host = await CreateHostAsync(new BnplAccessContext(
            ActorId,
            OrganizationId,
            BnplProductIdentity.ProductCode,
            true,
            true,
            true,
            BnplBranchScope.Restricted([BranchA]),
            [BnplCapabilityCodes.PlanManage, BnplCapabilityCodes.PlanRead]));

        var (applicationId, offerId) = await SeedOfferedAsync(host);
        var put = await PutPlanAsync(host, BranchB, applicationId, offerId);
        Assert.Equal(HttpStatusCode.Forbidden, put.StatusCode);
        Assert.Equal(BnplAccessErrorCodes.BranchDenied, await ReadErrorCodeAsync(put));
    }

    [Fact]
    public async Task Pos_entitlement_without_bnpl_assignment_denies()
    {
        await using var host = await CreateHostAsync(new BnplAccessContext(
            ActorId,
            OrganizationId,
            BnplProductIdentity.ProductCode,
            true,
            true,
            hasTrustedProductAssignment: false,
            BnplBranchScope.OrganizationWide(),
            [BnplCapabilityCodes.PlanManage]));

        var response = await PutPlanAsync(host, BranchA, Guid.NewGuid(), Guid.NewGuid());
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(BnplAccessErrorCodes.ProductAccessDenied, await ReadErrorCodeAsync(response));
    }

    private static BnplAccessContext FullAccess(params string[] capabilities) =>
        new(
            ActorId,
            OrganizationId,
            BnplProductIdentity.ProductCode,
            true,
            true,
            true,
            BnplBranchScope.OrganizationWide(),
            capabilities);

    private static async Task<(Guid ApplicationId, Guid OfferId)> SeedOfferedAsync(WebApplication host)
    {
        var customers = host.Services.GetRequiredService<IBnplCustomerRepository>();
        var apps = host.Services.GetRequiredService<IBnplFinancingApplicationRepository>();
        var uow = host.Services.GetRequiredService<IBnplUnitOfWork>();

        var customer = BnplCustomer.Create(OrganizationId, "Buyer", Now);
        await customers.AddAsync(customer);

        var app = BnplFinancingApplication.Create(
            OrganizationId,
            BranchA,
            customer.Id.Value,
            ActorId,
            60_000m,
            10_000m,
            Now);
        app.Submit(Now);
        app.ApproveEligibility(ActorId, Now);
        var offer = app.CreateOffer(ActorId, Now);
        await apps.AddAsync(app);
        await uow.SaveChangesAsync();
        return (app.Id.Value, offer.Id.Value);
    }

    private static async Task AttachPlanDirectAsync(WebApplication host, Guid applicationId, Guid offerId)
    {
        var apps = host.Services.GetRequiredService<IBnplFinancingApplicationRepository>();
        var uow = host.Services.GetRequiredService<IBnplUnitOfWork>();
        var app = await apps.GetByIdAsync(OrganizationId, BnplFinancingApplicationId.From(applicationId));
        app!.AttachOrReplaceInstallmentPlan(
            offerId,
            BnplInstallmentPlanId.New(),
            FiveEqual(),
            ActorId,
            Now);
        await apps.UpdateAsync(app);
        await uow.SaveChangesAsync();
    }

    private static async Task<HttpResponseMessage> PutPlanAsync(
        WebApplication host,
        Guid branchId,
        Guid applicationId,
        Guid offerId)
    {
        var client = host.GetTestClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"/api/v1/bnpl/applications/{applicationId:D}/offers/{offerId:D}/installment-plan");
        request.Headers.Add("X-Bnpl-Branch-Id", branchId.ToString("D"));
        request.Content = JsonContent.Create(new
        {
            planId = Guid.Parse("aaaaaaaa-1111-4111-8111-aaaaaaaaaaaa"),
            items = FiveEqual().Select(i => new
            {
                itemId = i.ItemId,
                sequenceNumber = i.SequenceNumber,
                principalAmount = i.PrincipalAmount,
                dueDate = i.DueDate.ToString("yyyy-MM-dd")
            })
        });
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> GetPlanAsync(
        WebApplication host,
        Guid branchId,
        Guid applicationId,
        Guid offerId)
    {
        var client = host.GetTestClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/bnpl/applications/{applicationId:D}/offers/{offerId:D}/installment-plan");
        request.Headers.Add("X-Bnpl-Branch-Id", branchId.ToString("D"));
        return await client.SendAsync(request);
    }

    private static IReadOnlyList<BnplInstallmentPlanItemDraft> FiveEqual() =>
        Enumerable.Range(1, 5)
            .Select(i => new BnplInstallmentPlanItemDraft(
                Guid.Parse($"{i:x8}-aaaa-4aaa-8aaa-aaaaaaaaaaaa"),
                i,
                10_000m,
                DateOnly.Parse("2026-10-01").AddMonths(i - 1)))
            .ToArray();

    private static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        return doc.RootElement.TryGetProperty("errorCode", out var code)
            ? code.GetString()
            : null;
    }

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
        builder.Services.AddSingleton<IBnplCustomerRepository, InMemoryCustomerRepo>();
        builder.Services.AddSingleton<IBnplFinancingApplicationRepository, InMemoryApplicationRepo>();
        builder.Services.AddSingleton<IBnplUnitOfWork, NoOpUow>();
        builder.Services.AddSingleton<IBnplClock>(_ => new FixedClock(Now));

        var app = builder.Build();
        app.MapBnplFinancingApplications();
        await app.StartAsync();
        return app;
    }

    private sealed class FixedProvider(BnplAccessContext? context) : IBnplAccessContextProvider
    {
        public ValueTask<BnplAccessContext?> GetAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(context);
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IBnplClock
    {
        public DateTimeOffset UtcNow => utcNow;
    }

    private sealed class NoOpUow : IBnplUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class InMemoryCustomerRepo : IBnplCustomerRepository
    {
        private readonly Dictionary<(Guid, Guid), BnplCustomer> _items = new();

        public Task<BnplCustomer?> GetByIdAsync(Guid organizationId, BnplCustomerId customerId, CancellationToken cancellationToken = default)
        {
            _items.TryGetValue((organizationId, customerId.Value), out var c);
            return Task.FromResult(c);
        }

        public Task<BnplCustomer?> FindByLinkedPersonalPublicUserIdAsync(Guid organizationId, string linkedPersonalPublicUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult<BnplCustomer?>(null);

        public Task<BnplCustomer?> FindByLinkedCommerceCustomerIdAsync(Guid organizationId, Guid linkedCommerceCustomerId, CancellationToken cancellationToken = default) =>
            Task.FromResult<BnplCustomer?>(null);

        public Task<(IReadOnlyList<BnplCustomer> Items, int TotalCount)> SearchAsync(Guid organizationId, string? search, BnplCustomerStatus? status, int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult(((IReadOnlyList<BnplCustomer>)Array.Empty<BnplCustomer>(), 0));

        public Task AddAsync(BnplCustomer customer, CancellationToken cancellationToken = default)
        {
            _items[(customer.OrganizationId, customer.Id.Value)] = customer;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(BnplCustomer customer, CancellationToken cancellationToken = default) => AddAsync(customer, cancellationToken);
    }

    private sealed class InMemoryApplicationRepo : IBnplFinancingApplicationRepository
    {
        private readonly Dictionary<(Guid, Guid), BnplFinancingApplication> _items = new();

        public Task<BnplFinancingApplication?> GetByIdAsync(Guid organizationId, BnplFinancingApplicationId applicationId, CancellationToken cancellationToken = default)
        {
            _items.TryGetValue((organizationId, applicationId.Value), out var a);
            return Task.FromResult(a);
        }

        public Task<(IReadOnlyList<BnplFinancingApplication> Items, int TotalCount)> SearchAsync(
            Guid organizationId, Guid? branchId, Guid? customerId, BnplFinancingApplicationStatus? status, int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult(((IReadOnlyList<BnplFinancingApplication>)Array.Empty<BnplFinancingApplication>(), 0));

        public Task AddAsync(BnplFinancingApplication application, CancellationToken cancellationToken = default)
        {
            _items[(application.OrganizationId, application.Id.Value)] = application;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(BnplFinancingApplication application, CancellationToken cancellationToken = default) =>
            AddAsync(application, cancellationToken);
    }
}
