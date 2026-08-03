using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.Platform.IntegrationTests;

/// <summary>
/// P4-WP04 closeout: exercises Platform API permission enforcement and audit trail recording end to
/// end. Development-stage actors remain unauthenticated (no real login), but once a
/// <c>X-Dev-Platform-User-Id</c> header selects a Platform User principal, real role-based permission
/// checks apply and every mutation is recorded to the append-only audit trail.
/// </summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class ApiAuthorizationAuditTests(PostgreSqlFixture fixture) : IAsyncLifetime
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

    private async Task AssignPlatformRoleAsync(Guid platformUserId, string role, Guid? organizationId = null)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/platform/authorization/assignments",
            new { platformUserId, role, organizationId });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task<Guid> CreateOrganizationAsync(string prefix)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/platform/organizations",
            new { displayName = $"{prefix} Org", slug = UniqueToken(prefix) });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    /// <summary>Seeds a full catalog product/plan/trial and starts a Trialing subscription for a fresh organization.</summary>
    private async Task<(Guid OrganizationId, string ProductCode, Guid SubscriptionId)> SeedTrialingSubscriptionAsync(string prefix)
    {
        var organizationId = await CreateOrganizationAsync(prefix);
        var productCode = $"p{Guid.NewGuid():N}"[..16];

        (await _client.PostAsJsonAsync("/api/v1/platform/catalog/products", new { code = productCode, displayName = "POS" }))
            .EnsureSuccessStatusCode();
        (await _client.PostAsJsonAsync(
            $"/api/v1/platform/catalog/products/{productCode}/features",
            new { featureCode = FeatureCode.CustomerCreditView, displayName = "View", valueType = nameof(FeatureValueType.Boolean) }))
            .EnsureSuccessStatusCode();

        var plan = await _client.PostAsJsonAsync(
            $"/api/v1/platform/catalog/products/{productCode}/plans",
            new { code = "utang", displayName = "Utang" });
        plan.EnsureSuccessStatusCode();
        var planId = (await plan.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await _client.PostAsync($"/api/v1/platform/catalog/products/{productCode}/plans/{planId}/activate", null))
            .EnsureSuccessStatusCode();

        var draft = await _client.PostAsJsonAsync(
            $"/api/v1/platform/catalog/products/{productCode}/plans/{planId}/versions/draft",
            new
            {
                versionNumber = 1,
                billingPeriod = nameof(BillingPeriod.Monthly),
                trialEligible = true,
                grants = new[] { new { featureCode = FeatureCode.CustomerCreditView, enabled = true } }
            });
        draft.EnsureSuccessStatusCode();
        var versionId = (await draft.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await _client.PostAsync($"/api/v1/platform/catalog/products/{productCode}/plans/{planId}/versions/1/publish", null))
            .EnsureSuccessStatusCode();

        var trial = await _client.PostAsJsonAsync(
            $"/api/v1/platform/catalog/products/{productCode}/trials",
            new
            {
                displayName = "Trial",
                durationTicks = TimeSpan.FromDays(21).Ticks,
                featureGrants = new[] { new { featureCode = FeatureCode.CustomerCreditView, enabled = true } },
                postExpiryFeatureGrants = Array.Empty<object>()
            });
        trial.EnsureSuccessStatusCode();
        var trialId = (await trial.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var start = await _client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/subscriptions/trials",
            new { planId, planVersionId = versionId, trialDefinitionId = trialId });
        start.EnsureSuccessStatusCode();
        var subscriptionId = (await start.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        return (organizationId, productCode, subscriptionId);
    }

    [Fact]
    public async Task DevelopmentOperator_without_header_can_mutate_as_before()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/platform/organizations",
            new { displayName = "Baseline Org", slug = UniqueToken("baseline") });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task PlatformUser_without_permission_is_denied_and_denial_is_audited()
    {
        var userA = await CreatePlatformUserAsync("permA");
        await AssignPlatformRoleAsync(userA, "BillingAdministrator");

        var blockedUsername = UniqueToken("blocked");
        var response = await SendAsync(
            HttpMethod.Post,
            "/api/v1/platform/users",
            new
            {
                username = blockedUsername,
                firstName = "Blocked",
                lastName = "User",
                displayName = "Blocked User",
                email = $"{blockedUsername}@example.com",
                platformRole = "PlatformSupport"
            },
            actingUserId: userA);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("platform.authorization.denied", problem.GetProperty("errorCode").GetString());

        var audit = await _client.GetAsync(
            $"/api/v1/platform/audit?actor={Uri.EscapeDataString($"platform-user:{userA:D}")}&outcome=Denied");
        Assert.Equal(HttpStatusCode.OK, audit.StatusCode);
        var page = await audit.Content.ReadFromJsonAsync<JsonElement>();
        var items = page.GetProperty("items").EnumerateArray().ToList();
        Assert.NotEmpty(items);
        Assert.Contains(items, item => item.GetProperty("outcome").GetString() == "Denied");
    }

    [Fact]
    public async Task PlatformUser_with_billing_role_can_manage_subscriptions_and_success_is_audited()
    {
        var userA = await CreatePlatformUserAsync("billing");
        await AssignPlatformRoleAsync(userA, "BillingAdministrator");

        var seeded = await SeedTrialingSubscriptionAsync("billingsub");

        var suspend = await SendAsync(
            HttpMethod.Post,
            $"/api/v1/platform/subscriptions/{seeded.SubscriptionId}/suspend",
            body: null,
            actingUserId: userA);

        Assert.Equal(HttpStatusCode.OK, suspend.StatusCode);
        Assert.Equal("Suspended", (await suspend.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());

        var audit = await _client.GetAsync(
            $"/api/v1/platform/audit?actor={Uri.EscapeDataString($"platform-user:{userA:D}")}&action=platform.subscription.suspended&outcome=Succeeded");
        Assert.Equal(HttpStatusCode.OK, audit.StatusCode);
        var page = await audit.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(page.GetProperty("totalCount").GetInt32() >= 1);
    }

    [Fact]
    public async Task Organization_scoped_role_grants_access_only_within_its_organization()
    {
        var userB = await CreatePlatformUserAsync("scoped");
        var org1 = await CreateOrganizationAsync("scoped1");
        var org2 = await CreateOrganizationAsync("scoped2");
        await AssignPlatformRoleAsync(userB, "PlatformSupport", org1);

        var memberCandidate = await CreatePlatformUserAsync("member");

        var deniedOnOrg2 = await SendAsync(
            HttpMethod.Post,
            $"/api/v1/platform/organizations/{org2}/members",
            new { userId = memberCandidate, role = "OrganizationMember" },
            actingUserId: userB);
        Assert.Equal(HttpStatusCode.Forbidden, deniedOnOrg2.StatusCode);

        var allowedOnOrg1 = await SendAsync(
            HttpMethod.Post,
            $"/api/v1/platform/organizations/{org1}/members",
            new { userId = memberCandidate, role = "OrganizationMember" },
            actingUserId: userB);
        Assert.Equal(HttpStatusCode.Created, allowedOnOrg1.StatusCode);
    }

    [Fact]
    public async Task Audit_reason_and_summary_text_never_leak_secret_or_phi_like_fields()
    {
        var userA = await CreatePlatformUserAsync("phicheck");
        await AssignPlatformRoleAsync(userA, "PlatformSupport");

        // Trigger a denied audit record (PlatformSupport lacks ManageOrganizations).
        await SendAsync(
            HttpMethod.Post,
            "/api/v1/platform/organizations",
            new { displayName = "Denied Org", slug = UniqueToken("deniedorg") },
            actingUserId: userA);

        var audit = await _client.GetAsync(
            $"/api/v1/platform/audit?actor={Uri.EscapeDataString($"platform-user:{userA:D}")}&outcome=Denied");
        Assert.Equal(HttpStatusCode.OK, audit.StatusCode);
        var page = await audit.Content.ReadFromJsonAsync<JsonElement>();

        string[] forbiddenSubstrings = ["password", "creditcard", "card number", "ssn", "diagnosis", "phi"];
        foreach (var item in page.GetProperty("items").EnumerateArray())
        {
            var reason = item.TryGetProperty("reason", out var r) ? r.GetString() : null;
            var summary = item.TryGetProperty("summary", out var s) ? s.GetString() : null;
            foreach (var forbidden in forbiddenSubstrings)
            {
                if (reason is not null)
                {
                    Assert.DoesNotContain(forbidden, reason, StringComparison.OrdinalIgnoreCase);
                }

                if (summary is not null)
                {
                    Assert.DoesNotContain(forbidden, summary, StringComparison.OrdinalIgnoreCase);
                }
            }
        }
    }

    [Fact]
    public async Task Resolve_current_permissions_reflects_actor_role_assignments()
    {
        var userA = await CreatePlatformUserAsync("me");
        await AssignPlatformRoleAsync(userA, "BillingAdministrator");

        var response = await SendAsync(HttpMethod.Get, "/api/v1/platform/authorization/me", actingUserId: userA);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var permissions = body.GetProperty("permissions").EnumerateArray().Select(p => p.GetString()).ToList();
        Assert.Contains("platform.permission.manage_subscriptions", permissions);
        Assert.DoesNotContain("platform.permission.manage_platform_users", permissions);
    }

    [Fact]
    public async Task List_roles_endpoint_exposes_static_role_permission_catalog()
    {
        var response = await _client.GetAsync("/api/v1/platform/authorization/roles");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var roles = await response.Content.ReadFromJsonAsync<JsonElement>();
        var roleNames = roles.EnumerateArray().Select(r => r.GetProperty("role").GetString()).ToList();
        Assert.Contains("PlatformAdministrator", roleNames);
        Assert.Contains("BillingAdministrator", roleNames);
        Assert.Contains("PlatformSupport", roleNames);
    }

    [Fact]
    public async Task List_assignments_requires_manage_platform_users_permission()
    {
        var userA = await CreatePlatformUserAsync("listassign");
        await AssignPlatformRoleAsync(userA, "BillingAdministrator");

        var denied = await SendAsync(HttpMethod.Get, "/api/v1/platform/authorization/assignments", actingUserId: userA);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);

        var allowed = await _client.GetAsync("/api/v1/platform/authorization/assignments");
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    [Fact]
    public async Task Revoke_role_assignment_succeeds_and_is_reflected_in_permissions()
    {
        var userA = await CreatePlatformUserAsync("revoke");
        var assign = await _client.PostAsJsonAsync(
            "/api/v1/platform/authorization/assignments",
            new { platformUserId = userA, role = "PlatformSupport", organizationId = (Guid?)null });
        Assert.Equal(HttpStatusCode.Created, assign.StatusCode);
        var assignmentId = (await assign.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var beforeRevoke = await SendAsync(HttpMethod.Get, "/api/v1/platform/authorization/me", actingUserId: userA);
        var beforePermissions = (await beforeRevoke.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("permissions").EnumerateArray().Select(p => p.GetString()).ToList();
        Assert.Contains("platform.permission.manage_memberships", beforePermissions);

        var revoke = await _client.PostAsJsonAsync(
            $"/api/v1/platform/authorization/assignments/{assignmentId}/revoke",
            new { reason = "no longer needed" });
        Assert.Equal(HttpStatusCode.OK, revoke.StatusCode);
        Assert.Equal("Revoked", (await revoke.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());

        var afterRevoke = await SendAsync(HttpMethod.Get, "/api/v1/platform/authorization/me", actingUserId: userA);
        var afterPermissions = (await afterRevoke.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("permissions").EnumerateArray().Select(p => p.GetString()).ToList();
        Assert.DoesNotContain("platform.permission.manage_memberships", afterPermissions);
    }

    [Fact]
    public async Task Audit_query_requires_view_audit_records_permission()
    {
        var unprivileged = await CreatePlatformUserAsync("noaudit");

        var denied = await SendAsync(HttpMethod.Get, "/api/v1/platform/audit", actingUserId: unprivileged);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);

        var allowed = await _client.GetAsync("/api/v1/platform/audit?page=1&pageSize=1");
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    [Fact]
    public async Task Get_audit_record_returns_not_found_for_unknown_id()
    {
        var response = await _client.GetAsync($"/api/v1/platform/audit/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("application.audit_record.not_found", body.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Assign_conflicting_active_role_returns_conflict()
    {
        var userA = await CreatePlatformUserAsync("conflict");
        await AssignPlatformRoleAsync(userA, "PlatformSupport");

        var duplicate = await _client.PostAsJsonAsync(
            "/api/v1/platform/authorization/assignments",
            new { platformUserId = userA, role = "PlatformSupport", organizationId = (Guid?)null });

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var body = await duplicate.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("application.role_assignment.conflict", body.GetProperty("errorCode").GetString());
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
