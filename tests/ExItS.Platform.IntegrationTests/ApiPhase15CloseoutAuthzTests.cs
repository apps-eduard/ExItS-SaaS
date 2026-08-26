using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.Platform.IntegrationTests;

/// <summary>
/// P15-WP07 closeout: payment/product-access read authz, org-admin directory denial,
/// audit action hygiene, and invitation summary must not embed invitee email.
/// </summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class ApiPhase15CloseoutAuthzTests(PostgreSqlFixture fixture) : IAsyncLifetime
{
    private const string DevUserHeader = "X-Dev-Platform-User-Id";

    private AuthorizationAuditApiFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new AuthorizationAuditApiFactory(fixture.ConnectionString);
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private static string UniqueToken(string prefix) =>
        $"{prefix}{Guid.NewGuid():N}"[..Math.Min(24, prefix.Length + 32)].ToLowerInvariant();

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object? body = null, Guid? actingUserId = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        if (actingUserId is not null)
        {
            request.Headers.Add(DevUserHeader, actingUserId.Value.ToString("D"));
        }

        return await _client.SendAsync(request);
    }

    private async Task<Guid> CreatePlatformUserAsync(string prefix)
    {
        var (userId, _, _) = await PlatformIntegrationTestUsers.CreatePlatformStaffWithPasswordAsync(_client, prefix);
        return userId;
    }

    private async Task AssignPlatformRoleAsync(Guid platformUserId, string role, Guid? organizationId = null) =>
        await PlatformIntegrationTestUsers.EnsurePlatformRoleAsync(_client, platformUserId, role, organizationId);

    private async Task ReplacePlatformRoleAsync(Guid platformUserId, string role, Guid? organizationId = null) =>
        await PlatformIntegrationTestUsers.ReplaceWithPlatformRoleAsync(_client, platformUserId, role, organizationId);

    private async Task RevokeAllPlatformRolesAsync(Guid platformUserId) =>
        await PlatformIntegrationTestUsers.RevokeAllActivePlatformRolesAsync(_client, platformUserId);

    private async Task<Guid> CreateOrganizationAsync(string prefix)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/platform/organizations",
            new { displayName = $"{prefix} Org", slug = UniqueToken(prefix) });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task Payment_reads_require_manage_manual_payments()
    {
        var orgId = await CreateOrganizationAsync("payorg");
        var support = await CreatePlatformUserAsync("paysup");
        // Staff create already seeds PlatformSupport (lacks ManageManualPayments).

        var create = await _client.PostAsJsonAsync(
            "/api/v1/platform/payments/manual",
            new
            {
                organizationId = orgId,
                productCode = "pos",
                amount = 100m,
                currencyCode = "PHP",
                method = "Cash",
                externalReference = UniqueToken("ref"),
                paidAtUtc = DateTimeOffset.UtcNow
            });
        // Catalog product may be missing — still exercise read authz with empty/missing payment id.
        _ = create;

        var deniedList = await SendAsync(
            HttpMethod.Get,
            $"/api/v1/platform/payments?organizationId={orgId:D}",
            actingUserId: support);
        // PlatformSupport lacks ManageManualPayments.
        Assert.Equal(HttpStatusCode.Forbidden, deniedList.StatusCode);

        var deniedOrgList = await SendAsync(
            HttpMethod.Get,
            $"/api/v1/platform/organizations/{orgId:D}/payments",
            actingUserId: support);
        Assert.Equal(HttpStatusCode.Forbidden, deniedOrgList.StatusCode);

        var deniedGet = await SendAsync(
            HttpMethod.Get,
            $"/api/v1/platform/payments/{Guid.NewGuid():D}",
            actingUserId: support);
        Assert.Equal(HttpStatusCode.Forbidden, deniedGet.StatusCode);
    }

    [Fact]
    public async Task Product_access_user_list_and_evaluate_require_authorization()
    {
        var orgId = await CreateOrganizationAsync("pacorg");
        var userId = await CreatePlatformUserAsync("pacuser");
        var billing = await CreatePlatformUserAsync("pacbill");
        // Exclusive BillingAdministrator — default PlatformSupport includes ManageProductAccess.
        await ReplacePlatformRoleAsync(billing, "BillingAdministrator");

        var deniedUserList = await SendAsync(
            HttpMethod.Get,
            $"/api/v1/platform/users/{userId:D}/product-access",
            actingUserId: billing);
        Assert.Equal(HttpStatusCode.Forbidden, deniedUserList.StatusCode);

        var deniedEvaluate = await SendAsync(
            HttpMethod.Get,
            $"/api/v1/platform/access/evaluate?userId={userId:D}&organizationId={orgId:D}&productCode=pos",
            actingUserId: billing);
        Assert.Equal(HttpStatusCode.Forbidden, deniedEvaluate.StatusCode);
    }

    [Fact]
    public async Task Organization_admin_cannot_list_platform_user_directory()
    {
        var orgId = await CreateOrganizationAsync("dirorg");
        var adminUser = await CreatePlatformUserAsync("diradmin");
        await RevokeAllPlatformRolesAsync(adminUser);
        var memberAdd = await _client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{orgId:D}/members",
            new { userId = adminUser, role = "OrganizationOwner" });
        memberAdd.EnsureSuccessStatusCode();

        // Org owner without platform roles — no ManagePlatformUsers / ViewPortfolio.
        var deniedUsers = await SendAsync(HttpMethod.Get, "/api/v1/platform/users", actingUserId: adminUser);
        Assert.Equal(HttpStatusCode.Forbidden, deniedUsers.StatusCode);

        var deniedRoles = await SendAsync(
            HttpMethod.Get,
            "/api/v1/platform/authorization/role-definitions",
            actingUserId: adminUser);
        Assert.Equal(HttpStatusCode.Forbidden, deniedRoles.StatusCode);

        var deniedOrgs = await SendAsync(HttpMethod.Get, "/api/v1/platform/organizations", actingUserId: adminUser);
        Assert.Equal(HttpStatusCode.Forbidden, deniedOrgs.StatusCode);
    }

    [Fact]
    public async Task Invitation_create_audit_omits_invitee_email_from_summary()
    {
        var orgId = await CreateOrganizationAsync("invorg");
        var inviteEmail = $"{UniqueToken("invite")}@example.com";

        var create = await _client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{orgId:D}/invitations",
            new { email = inviteEmail, role = "OrganizationMember" });
        create.EnsureSuccessStatusCode();
        var invitationId = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var audit = await _client.GetAsync(
            $"/api/v1/platform/audit?action=platform.invitation.created&outcome=Succeeded&organizationId={orgId:D}");
        audit.EnsureSuccessStatusCode();
        var page = await audit.Content.ReadFromJsonAsync<JsonElement>();
        var match = page.GetProperty("items").EnumerateArray()
            .FirstOrDefault(i =>
                string.Equals(i.GetProperty("targetId").GetString(), invitationId.ToString("D"), StringComparison.Ordinal));
        Assert.Equal(JsonValueKind.Object, match.ValueKind);
        var summary = match.TryGetProperty("summary", out var s) ? s.GetString() : null;
        Assert.NotNull(summary);
        Assert.DoesNotContain(inviteEmail, summary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Created organization invitation", summary, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Denied_user_list_uses_access_checked_action()
    {
        var billing = await CreatePlatformUserAsync("denylist");
        await ReplacePlatformRoleAsync(billing, "BillingAdministrator");

        var denied = await SendAsync(HttpMethod.Get, "/api/v1/platform/users", actingUserId: billing);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);

        var audit = await _client.GetAsync(
            $"/api/v1/platform/audit?actor={Uri.EscapeDataString($"platform-user:{billing:D}")}&outcome=Denied&action=platform.access.checked");
        audit.EnsureSuccessStatusCode();
        var page = await audit.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(page.GetProperty("items").GetArrayLength() > 0);
    }

    private sealed class AuthorizationAuditApiFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:PlatformDatabase", connectionString);
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PlatformDatabase"] = connectionString
                });
            });
        }
    }
}
