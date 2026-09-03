using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApiAccountScopeIsolationTests(PostgreSqlFixture fixture) : IAsyncLifetime
{
    private SessionApiFactory _factory = null!;
    private HttpClient _admin = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new SessionApiFactory(fixture.ConnectionString);
        _admin = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _admin.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private static string Unique(string prefix) =>
        $"{prefix}{Guid.NewGuid():N}"[..Math.Min(20, prefix.Length + 32)].ToLowerInvariant();

    private async Task<(Guid UserId, string Username, string Password)> SeedUserAsync(string prefix) =>
        await PlatformIntegrationTestUsers.CreatePlatformStaffWithPasswordAsync(_admin, prefix);

    private async Task<(Guid UserId, string Email, string Password)> SeedPersonalUserAsync(string prefix) =>
        await PlatformIntegrationTestUsers.RegisterPersonalWithPasswordAsync(_client, prefix);

    private async Task<(Guid UserId, string StaffLogin, string Password, Guid OrganizationId)> SeedOrgMemberAsync(
        string prefix,
        string role = "OrganizationMember")
    {
        var (userId, _, staffLogin, password, organizationId) =
            await PlatformIntegrationTestUsers.SeedOrgMemberViaInvitationAsync(_admin, _client, prefix, role);
        return (userId, staffLogin, password, organizationId);
    }

    private async Task<JsonElement> LoginAsync(string username, string password)
    {
        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = username, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        return await login.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static HttpRequestMessage Authed(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-ExItS-Session-Token", token);
        return request;
    }

    private static async Task AssertScopeDeniedAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ApplicationErrorCodes.AccountScopeDenied, body.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Personal_session_cannot_call_platform_admin_apis()
    {
        var (_, email, password) = await SeedPersonalUserAsync("pers");
        var login = await LoginAsync(email, password);
        Assert.Equal("Personal", login.GetProperty("accountClass").GetString());
        var token = login.GetProperty("sessionToken").GetString()!;

        using var users = Authed(HttpMethod.Get, "/api/v1/platform/users?page=1&pageSize=10", token);
        await AssertScopeDeniedAsync(await _client.SendAsync(users));

        using var personal = Authed(HttpMethod.Get, "/api/v1/personal/me", token);
        var allowed = await _client.SendAsync(personal);
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    [Fact]
    public async Task Personal_session_can_list_pending_staff_invitations()
    {
        var (_, email, password) = await SeedPersonalUserAsync("stfinv");
        var login = await LoginAsync(email, password);
        Assert.Equal("Personal", login.GetProperty("accountClass").GetString());
        var token = login.GetProperty("sessionToken").GetString()!;

        using var pending = Authed(HttpMethod.Get, "/api/v1/platform/invitations/my-pending", token);
        var response = await _client.SendAsync(pending);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, body.ValueKind);
    }

    [Fact]
    public async Task Organization_session_cannot_list_personal_staff_invitations()
    {
        var (_, staffLogin, password, _) = await SeedOrgMemberAsync("stforg");
        var login = await LoginAsync(staffLogin, password);
        Assert.Equal("Organization", login.GetProperty("accountClass").GetString());
        var token = login.GetProperty("sessionToken").GetString()!;

        using var pending = Authed(HttpMethod.Get, "/api/v1/platform/invitations/my-pending", token);
        await AssertScopeDeniedAsync(await _client.SendAsync(pending));
    }

    [Fact]
    public async Task Personal_and_organization_sessions_can_bootstrap_antiforgery_and_logout()
    {
        // React Admin Sign out POSTs /auth/logout (scope-exempt) but first GETs antiforgery/token.
        // Non-Platform sessions must not be blocked by account_scope_denied on that bootstrap.
        var (_, personalEmail, personalPassword) = await SeedPersonalUserAsync("afpers");
        var personalLogin = await LoginAsync(personalEmail, personalPassword);
        Assert.Equal("Personal", personalLogin.GetProperty("accountClass").GetString());
        var personalToken = personalLogin.GetProperty("sessionToken").GetString()!;

        using (var antiforgery = Authed(HttpMethod.Get, "/api/v1/platform/antiforgery/token", personalToken))
        {
            var response = await _client.SendAsync(antiforgery);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        using (var logout = Authed(HttpMethod.Post, "/api/v1/platform/auth/logout", personalToken))
        {
            var response = await _client.SendAsync(logout);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        var (_, orgStaffLogin, orgPassword, _) = await SeedOrgMemberAsync("aforg");
        var orgLogin = await LoginAsync(orgStaffLogin, orgPassword);
        Assert.Equal("Organization", orgLogin.GetProperty("accountClass").GetString());
        var orgToken = orgLogin.GetProperty("sessionToken").GetString()!;

        using (var antiforgery = Authed(HttpMethod.Get, "/api/v1/platform/antiforgery/token", orgToken))
        {
            var response = await _client.SendAsync(antiforgery);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        using (var logout = Authed(HttpMethod.Post, "/api/v1/platform/auth/logout", orgToken))
        {
            var response = await _client.SendAsync(logout);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }

    [Fact]
    public async Task Platform_session_cannot_call_personal_apis()
    {
        var (userId, username, password) = await SeedUserAsync("plat");
        var assign = await _admin.PostAsJsonAsync(
            "/api/v1/platform/authorization/assignments",
            new
            {
                platformUserId = userId,
                role = nameof(PlatformSystemRole.PlatformAdministrator)
            });
        Assert.Equal(HttpStatusCode.Created, assign.StatusCode);

        var login = await LoginAsync(username, password);
        Assert.Equal("Platform", login.GetProperty("accountClass").GetString());
        var token = login.GetProperty("sessionToken").GetString()!;

        using var personal = Authed(HttpMethod.Get, "/api/v1/personal/me", token);
        await AssertScopeDeniedAsync(await _client.SendAsync(personal));

        using var users = Authed(HttpMethod.Get, "/api/v1/platform/users?page=1&pageSize=10", token);
        var allowed = await _client.SendAsync(users);
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    [Fact]
    public async Task Organization_session_cannot_call_platform_or_personal_apis()
    {
        var (userId, staffLogin, password, _) = await SeedOrgMemberAsync("orgs");
        _ = userId;
        var login = await LoginAsync(staffLogin, password);
        Assert.Equal("Organization", login.GetProperty("accountClass").GetString());
        var token = login.GetProperty("sessionToken").GetString()!;

        using var users = Authed(HttpMethod.Get, "/api/v1/platform/users?page=1&pageSize=10", token);
        await AssertScopeDeniedAsync(await _client.SendAsync(users));

        using var personal = Authed(HttpMethod.Get, "/api/v1/personal/me", token);
        await AssertScopeDeniedAsync(await _client.SendAsync(personal));
    }

    [Fact]
    public async Task Authenticated_sessions_of_every_class_can_reach_merchant_catalog_discovery()
    {
        // Merchant discovery (/api/v1/catalog/*) is an authenticated cross-scope surface,
        // mirroring /api/v1/commercial. Personal, Organization, and Platform sessions may browse;
        // the endpoint enforces its own session auth and returns only published/Active data.
        var (_, personalEmail, personalPassword) = await SeedPersonalUserAsync("catpers");
        var personalToken = (await LoginAsync(personalEmail, personalPassword))
            .GetProperty("sessionToken").GetString()!;

        var (_, orgStaffLogin, orgPassword, _) = await SeedOrgMemberAsync("catorg");
        var orgToken = (await LoginAsync(orgStaffLogin, orgPassword))
            .GetProperty("sessionToken").GetString()!;

        var (platformUserId, platformUsername, platformPassword) = await SeedUserAsync("catplat");
        var assign = await _admin.PostAsJsonAsync(
            "/api/v1/platform/authorization/assignments",
            new
            {
                platformUserId,
                role = nameof(PlatformSystemRole.PlatformAdministrator)
            });
        Assert.Equal(HttpStatusCode.Created, assign.StatusCode);
        var platformToken = (await LoginAsync(platformUsername, platformPassword))
            .GetProperty("sessionToken").GetString()!;

        foreach (var token in new[] { personalToken, orgToken, platformToken })
        {
            using var req = Authed(
                HttpMethod.Get,
                "/api/v1/catalog/products/search?page=1&pageSize=5",
                token);
            var res = await _client.SendAsync(req);

            Assert.NotEqual(HttpStatusCode.Forbidden, res.StatusCode);
            Assert.NotEqual(HttpStatusCode.Unauthorized, res.StatusCode);
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        }
    }

    [Fact]
    public async Task Unauthenticated_merchant_catalog_discovery_is_unauthorized()
    {
        using var req = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/v1/catalog/products/search?page=1&pageSize=5");
        var res = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Client_cannot_forge_account_class_via_profile_select_of_other_user()
    {
        var (userAId, emailA, passwordA) = await SeedPersonalUserAsync("fora");
        var (userBId, emailB, passwordB) = await SeedPersonalUserAsync("forb");
        _ = userAId;
        _ = userBId;
        _ = passwordB;

        var loginB = await LoginAsync(emailB, passwordB);
        var foreignProfileId = loginB.GetProperty("accountProfileId").GetGuid();

        var loginA = await LoginAsync(emailA, passwordA);
        var tokenA = loginA.GetProperty("sessionToken").GetString()!;

        using var select = Authed(HttpMethod.Post, "/api/v1/platform/auth/account-profiles/select", tokenA);
        select.Content = JsonContent.Create(new { accountProfileId = foreignProfileId });
        var denied = await _client.SendAsync(select);
        Assert.Equal(HttpStatusCode.BadRequest, denied.StatusCode);
        var body = await denied.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ApplicationErrorCodes.AccountProfileNotAvailable, body.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Organization_owner_session_can_resolve_empty_authorization_me_but_not_platform_users()
    {
        // Owner remains a Personal identity with org membership (not invite-created staff).
        var (userId, email, password) = await SeedPersonalUserAsync("orgz");
        var org = await _admin.PostAsJsonAsync(
            "/api/v1/platform/organizations",
            new { displayName = "Owner Org", slug = Unique("orgzo") });
        org.EnsureSuccessStatusCode();
        var organizationId = (await org.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await _admin.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/members",
            new { userId, role = "OrganizationOwner", reason = "integration-test-owner-link" }))
            .EnsureSuccessStatusCode();

        var login = await LoginAsync(email, password);
        Assert.Equal("Organization", login.GetProperty("accountClass").GetString());
        var token = login.GetProperty("sessionToken").GetString()!;

        using var meAuthz = Authed(HttpMethod.Get, "/api/v1/platform/authorization/me", token);
        var authz = await _client.SendAsync(meAuthz);
        Assert.Equal(HttpStatusCode.OK, authz.StatusCode);
        var authzBody = await authz.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, authzBody.GetProperty("permissions").GetArrayLength());

        using var users = Authed(HttpMethod.Get, "/api/v1/platform/users?page=1&pageSize=10&search=admin", token);
        await AssertScopeDeniedAsync(await _client.SendAsync(users));

        using var userById = Authed(HttpMethod.Get, $"/api/v1/platform/users/{userId}", token);
        await AssertScopeDeniedAsync(await _client.SendAsync(userById));

        using var members = Authed(HttpMethod.Get, $"/api/v1/platform/organizations/{organizationId}/members?page=1&pageSize=10", token);
        var membersOk = await _client.SendAsync(members);
        Assert.Equal(HttpStatusCode.OK, membersOk.StatusCode);
    }

    [Fact]
    public async Task PUBSTORE_authenticated_sessions_and_anonymous_can_call_public_store_discovery()
    {
        // Public store landing/branches are AllowAnonymous discovery surfaces. Authenticated
        // Organization/Personal/Platform cookies must not be account-scope denied.
        // Use a stable non-existent public id — success vs not-found is LookupPublicStore*;
        // this test only asserts account-scope does not block.
        const string publicOrgId = "ORG000000";
        var landingPath = $"/api/v1/public/stores/{publicOrgId}";
        var branchesPath = $"/api/v1/public/stores/{publicOrgId}/branches";

        // Avoid cookie bleed across account classes on the shared HandleCookies client.
        using var authedClient = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        static async Task AssertNotScopeDeniedAsync(HttpResponseMessage res)
        {
            Assert.NotEqual(HttpStatusCode.Forbidden, res.StatusCode);
            if (res.Content.Headers.ContentType?.MediaType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true)
            {
                var body = await res.Content.ReadFromJsonAsync<JsonElement>();
                if (body.ValueKind == JsonValueKind.Object && body.TryGetProperty("errorCode", out var code))
                {
                    Assert.NotEqual(ApplicationErrorCodes.AccountScopeDenied, code.GetString());
                }
            }
        }

        // PUBSTORE-01 / PUBSTORE-02 anonymous
        using (var anonLanding = new HttpRequestMessage(HttpMethod.Get, landingPath))
        {
            await AssertNotScopeDeniedAsync(await _client.SendAsync(anonLanding));
        }

        using (var anonBranches = new HttpRequestMessage(HttpMethod.Get, branchesPath))
        {
            await AssertNotScopeDeniedAsync(await _client.SendAsync(anonBranches));
        }

        var (_, personalEmail, personalPassword) = await SeedPersonalUserAsync("pubpers");
        var personalToken = (await LoginAsync(personalEmail, personalPassword))
            .GetProperty("sessionToken").GetString()!;

        var (_, orgStaffLogin, orgPassword, _) = await SeedOrgMemberAsync("puborg");
        var orgToken = (await LoginAsync(orgStaffLogin, orgPassword))
            .GetProperty("sessionToken").GetString()!;

        var (platformUserId, platformUsername, platformPassword) = await SeedUserAsync("pubplat");
        var assign = await _admin.PostAsJsonAsync(
            "/api/v1/platform/authorization/assignments",
            new
            {
                platformUserId,
                role = nameof(PlatformSystemRole.PlatformAdministrator)
            });
        Assert.Equal(HttpStatusCode.Created, assign.StatusCode);
        var platformToken = (await LoginAsync(platformUsername, platformPassword))
            .GetProperty("sessionToken").GetString()!;

        // PUBSTORE-03..06 Organization / Personal / Platform
        foreach (var token in new[] { orgToken, personalToken, platformToken })
        {
            using var landing = Authed(HttpMethod.Get, landingPath, token);
            await AssertNotScopeDeniedAsync(await authedClient.SendAsync(landing));

            using var branches = Authed(HttpMethod.Get, branchesPath, token);
            await AssertNotScopeDeniedAsync(await authedClient.SendAsync(branches));
        }

        // PUBSTORE-07 protected cross-scope routes remain denied
        using (var orgToPersonal = Authed(HttpMethod.Get, "/api/v1/personal/me", orgToken))
        {
            await AssertScopeDeniedAsync(await authedClient.SendAsync(orgToPersonal));
        }

        using (var personalToPlatform = Authed(HttpMethod.Get, "/api/v1/platform/users?page=1&pageSize=10", personalToken))
        {
            await AssertScopeDeniedAsync(await authedClient.SendAsync(personalToPlatform));
        }

        using (var platformToPersonal = Authed(HttpMethod.Get, "/api/v1/personal/me", platformToken))
        {
            await AssertScopeDeniedAsync(await authedClient.SendAsync(platformToPersonal));
        }

        // PUBSTORE-08 invalid ORG keeps safe non-scope response (not account_scope_denied)
        using (var missing = Authed(HttpMethod.Get, "/api/v1/public/stores/ORG999999", orgToken))
        {
            await AssertNotScopeDeniedAsync(await authedClient.SendAsync(missing));
        }
    }
}
