using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBuyNowPayLater.Api.Access;
using ExItS.PinoyBuyNowPayLater.Application.Access;
using ExItS.PinoyBuyNowPayLater.Domain.Access;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ExItS.PinoyBuyNowPayLater.UnitTests.Access;

public sealed class BnplOperationalAccessApiTests
{
    private static readonly Guid ActorId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
    private static readonly Guid OrganizationId = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb");
    private static readonly Guid BranchId = Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc");

    [Fact]
    public async Task Access_me_unavailable_context_maps_to_service_unavailable()
    {
        await using var host = await CreateHostAsync(null);
        var response = await host.GetTestClient().GetAsync("/api/v1/bnpl/access/me");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(BnplAccessErrorCodes.ContextUnavailable, await ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task Access_me_product_assignment_denied_maps_to_forbidden()
    {
        await using var host = await CreateHostAsync(new BnplAccessContext(
            ActorId,
            OrganizationId,
            BnplProductIdentity.ProductCode,
            hasTrustedOrganizationMembership: true,
            hasTrustedOrganizationEntitlement: true,
            hasTrustedProductAssignment: false,
            BnplBranchScope.OrganizationWide(),
            BnplCapabilityPresets.SalesCapabilities));

        var response = await host.GetTestClient().GetAsync("/api/v1/bnpl/access/me");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(BnplAccessErrorCodes.ProductAccessDenied, await ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task Access_me_returns_effective_capabilities_and_branch_scope()
    {
        await using var host = await CreateHostAsync(new BnplAccessContext(
            ActorId,
            OrganizationId,
            BnplProductIdentity.ProductCode,
            hasTrustedOrganizationMembership: true,
            hasTrustedOrganizationEntitlement: true,
            hasTrustedProductAssignment: true,
            BnplBranchScope.Restricted([BranchId]),
            BnplCapabilityPresets.SalesCapabilities));

        var response = await host.GetTestClient().GetAsync("/api/v1/bnpl/access/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ActorId, body.GetProperty("actorId").GetGuid());
        Assert.Equal(OrganizationId, body.GetProperty("organizationId").GetGuid());
        Assert.Equal(BnplProductIdentity.ProductCode, body.GetProperty("productCode").GetString());
        Assert.False(body.GetProperty("organizationWideBranchAccess").GetBoolean());
        Assert.Equal(BranchId, body.GetProperty("allowedBranchIds")[0].GetGuid());
        Assert.Contains(
            BnplCapabilityCodes.ApplicationCreate,
            body.GetProperty("capabilities").EnumerateArray().Select(x => x.GetString()));
        Assert.DoesNotContain(
            BnplCapabilityCodes.ApplicationApprove,
            body.GetProperty("capabilities").EnumerateArray().Select(x => x.GetString()));
    }

    [Fact]
    public async Task Capability_guarded_probe_denies_without_required_capability()
    {
        await using var host = await CreateHostAsync(
            new BnplAccessContext(
                ActorId,
                OrganizationId,
                BnplProductIdentity.ProductCode,
                true,
                true,
                true,
                BnplBranchScope.OrganizationWide(),
                [BnplCapabilityCodes.PlanRead]),
            BnplAccessRequirement.ForCapability(BnplCapabilityCodes.ApplicationApprove));

        var response = await host.GetTestClient().GetAsync("/__test__/bnpl-capability");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(BnplAccessErrorCodes.CapabilityDenied, await ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task Production_default_composition_is_fail_closed()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddProblemDetails();
        builder.Services.AddBnplAccessBoundary();

        await using var app = builder.Build();
        app.MapGet("/api/v1/bnpl/access/me", () => Results.Text("ok", "text/plain"))
            .RequireBnplOperationalAccess();
        await app.StartAsync();

        var response = await app.GetTestClient().GetAsync("/api/v1/bnpl/access/me");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(BnplAccessErrorCodes.ContextUnavailable, await ReadErrorCodeAsync(response));
        Assert.IsType<UnavailableBnplAccessContextProvider>(
            app.Services.GetRequiredService<IBnplAccessContextProvider>());
    }

    private static async Task<WebApplication> CreateHostAsync(
        BnplAccessContext? context,
        BnplAccessRequirement? probeRequirement = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddProblemDetails();
        builder.Services.RemoveAll<IBnplAccessContextProvider>();
        builder.Services.AddSingleton<IBnplAccessContextProvider>(new FixedProvider(context));
        builder.Services.AddSingleton<IBnplOperationalAccessGuard, BnplOperationalAccessGuard>();

        var app = builder.Build();
        app.MapGet("/api/v1/bnpl/access/me", async (IBnplOperationalAccessGuard guard, CancellationToken ct) =>
            {
                var decision = await guard.EvaluateAsync(BnplAccessRequirement.None, ct);
                if (!decision.IsAllowed || decision.Context is null)
                {
                    return BnplApiResults.FromDenial(decision);
                }

                var c = decision.Context;
                return Results.Ok(new
                {
                    actorId = c.ActorId,
                    organizationId = c.OrganizationId,
                    productCode = c.ProductCode,
                    hasOrganizationMembership = c.HasTrustedOrganizationMembership,
                    hasOrganizationEntitlement = c.HasTrustedOrganizationEntitlement,
                    hasProductAssignment = c.HasTrustedProductAssignment,
                    organizationWideBranchAccess = c.BranchScope.IsOrganizationWide,
                    allowedBranchIds = c.BranchScope.AllowedBranchIds.OrderBy(id => id).ToArray(),
                    capabilities = c.Capabilities.OrderBy(x => x, StringComparer.Ordinal).ToArray()
                });
            })
            .RequireBnplOperationalAccess();

        if (probeRequirement is not null)
        {
            app.MapGet("/__test__/bnpl-capability", () => Results.Text("ok", "text/plain"))
                .RequireBnplOperationalAccess(probeRequirement);
        }

        await app.StartAsync();
        return app;
    }

    private static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        return document.RootElement.TryGetProperty("errorCode", out var value)
            ? value.GetString()
            : null;
    }

    private sealed class FixedProvider : IBnplAccessContextProvider
    {
        private readonly BnplAccessContext? _context;

        public FixedProvider(BnplAccessContext? context) => _context = context;

        public ValueTask<BnplAccessContext?> GetAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(_context);
    }
}
