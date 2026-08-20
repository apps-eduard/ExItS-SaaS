using System.Net;
using System.Text.Json;
using ExItS.PinoyLoanManager.Api.Access;
using ExItS.PinoyLoanManager.Application.Access;
using ExItS.PinoyLoanManager.Domain.Access;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace ExItS.PinoyLoanManager.UnitTests.Access;

/// <summary>
/// Test-host-only guarded probe. Not part of the production PLM API surface.
/// </summary>
public sealed class PlmOperationalAccessApiTests
{
    private static readonly Guid ActorId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
    private static readonly Guid OrganizationId = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb");

    [Fact]
    public async Task Unavailable_context_maps_to_service_unavailable()
    {
        await using var host = await CreateHostAsync(null);
        var response = await host.GetTestClient().GetAsync("/__test__/plm-access");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(PlmAccessErrorCodes.ContextUnavailable, await ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task Missing_organization_maps_to_forbidden()
    {
        await using var host = await CreateHostAsync(new PlmAccessContext(
            ActorId,
            Guid.Empty,
            PlmProductIdentity.PinoyLoanManagerCode,
            hasTrustedProductAccess: true));

        var response = await host.GetTestClient().GetAsync("/__test__/plm-access");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(PlmAccessErrorCodes.OrganizationRequired, await ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task Product_access_denied_maps_to_forbidden()
    {
        await using var host = await CreateHostAsync(new PlmAccessContext(
            ActorId,
            OrganizationId,
            PlmProductIdentity.PinoyLoanManagerCode,
            hasTrustedProductAccess: false));

        var response = await host.GetTestClient().GetAsync("/__test__/plm-access");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(PlmAccessErrorCodes.ProductAccessDenied, await ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task Valid_test_context_allows_guarded_probe()
    {
        await using var host = await CreateHostAsync(new PlmAccessContext(
            ActorId,
            OrganizationId,
            PlmProductIdentity.PinoyLoanManagerCode,
            hasTrustedProductAccess: true));

        var response = await host.GetTestClient().GetAsync("/__test__/plm-access");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("ok", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Production_default_composition_is_fail_closed()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddProblemDetails();
        builder.Services.AddPlmAccessBoundary();

        await using var app = builder.Build();
        app.MapGet("/__test__/plm-access", () => Results.Text("ok", "text/plain"))
            .RequirePlmOperationalAccess();
        await app.StartAsync();

        var response = await app.GetTestClient().GetAsync("/__test__/plm-access");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(PlmAccessErrorCodes.ContextUnavailable, await ReadErrorCodeAsync(response));
        Assert.IsType<UnavailablePlmAccessContextProvider>(
            app.Services.GetRequiredService<IPlmAccessContextProvider>());
    }

    private static async Task<WebApplication> CreateHostAsync(PlmAccessContext? context)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddProblemDetails();
        builder.Services.RemoveAll<IPlmAccessContextProvider>();
        builder.Services.AddSingleton<IPlmAccessContextProvider>(new FixedProvider(context));
        builder.Services.AddSingleton<IPlmOperationalAccessGuard, PlmOperationalAccessGuard>();

        var app = builder.Build();
        app.MapGet("/__test__/plm-access", () => Results.Text("ok", "text/plain"))
            .RequirePlmOperationalAccess();
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

    private sealed class FixedProvider : IPlmAccessContextProvider
    {
        private readonly PlmAccessContext? _context;

        public FixedProvider(PlmAccessContext? context) => _context = context;

        public ValueTask<PlmAccessContext?> GetAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(_context);
    }
}
