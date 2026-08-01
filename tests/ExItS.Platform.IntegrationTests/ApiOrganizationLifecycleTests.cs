using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApiOrganizationLifecycleTests(PostgreSqlFixture fixture) : IAsyncLifetime
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

    private static string UniqueToken(string prefix) =>
        $"{prefix}{Guid.NewGuid():N}"[..Math.Min(24, prefix.Length + 32)].ToLowerInvariant();

    private async Task<Guid> CreateOrganizationAsync(string prefix)
    {
        var create = await _admin.PostAsJsonAsync(
            "/api/v1/platform/organizations",
            new { displayName = $"{prefix} Org", slug = UniqueToken(prefix) });
        create.EnsureSuccessStatusCode();
        return (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private async Task<(Guid UserId, string Username, string Password)> SeedUserWithPasswordAsync(string prefix)
    {
        var username = UniqueToken(prefix);
        var password = "Correct-Horse-9!";
        var create = await _admin.PostAsJsonAsync(
            "/api/v1/platform/users",
            new { username, displayName = "Org Admin", email = $"{username}@example.com" });
        create.EnsureSuccessStatusCode();
        var userId = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await _admin.PutAsJsonAsync(
            $"/api/v1/platform/users/{userId}/credentials/password",
            new { password })).EnsureSuccessStatusCode();
        return (userId, username, password);
    }

    private async Task<string> LoginAsync(string username, string password)
    {
        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = username, password });
        login.EnsureSuccessStatusCode();
        return (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionToken").GetString()!;
    }

    private static HttpRequestMessage Authed(HttpMethod method, string url, string token, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-ExItS-Session-Token", token);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return request;
    }

    [Fact]
    public async Task Organization_list_supports_paging_search_filter_and_sort()
    {
        var slugA = UniqueToken("alpha");
        var slugB = UniqueToken("beta");
        (await _admin.PostAsJsonAsync(
            "/api/v1/platform/organizations",
            new { displayName = "Alpha Lifecycle Clinic", slug = slugA })).EnsureSuccessStatusCode();
        (await _admin.PostAsJsonAsync(
            "/api/v1/platform/organizations",
            new { displayName = "Beta Lifecycle Clinic", slug = slugB })).EnsureSuccessStatusCode();

        var search = await _admin.GetAsync("/api/v1/platform/organizations?search=Alpha%20Lifecycle&page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, search.StatusCode);
        var searchBody = await search.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(searchBody.GetProperty("totalCount").GetInt32() >= 1);
        Assert.Contains(
            searchBody.GetProperty("items").EnumerateArray(),
            i => i.GetProperty("slug").GetString() == slugA);

        var sorted = await _admin.GetAsync(
            "/api/v1/platform/organizations?search=Lifecycle%20Clinic&sortBy=Slug&sortDesc=true&page=1&pageSize=50");
        Assert.Equal(HttpStatusCode.OK, sorted.StatusCode);
        var sortedItems = (await sorted.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items").EnumerateArray().ToList();
        Assert.True(sortedItems.Count >= 2);
        var slugIndex = sortedItems.Select(i => i.GetProperty("slug").GetString()!).ToList();
        Assert.True(string.CompareOrdinal(slugIndex[0], slugIndex[^1]) >= 0);
    }

    [Fact]
    public async Task Organization_profile_update_branding_lifecycle_and_concurrency()
    {
        var organizationId = await CreateOrganizationAsync("life");
        var get = await _admin.GetAsync($"/api/v1/platform/organizations/{organizationId}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var org = await get.Content.ReadFromJsonAsync<JsonElement>();
        var updatedAt = org.GetProperty("updatedAtUtc").GetDateTimeOffset();

        var update = await _admin.PutAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}",
            new
            {
                displayName = "Lifecycle Updated",
                slug = org.GetProperty("slug").GetString(),
                legalName = "Lifecycle Legal LLC",
                contactEmail = "ops@lifecycle.example",
                contactPhone = "+15550100",
                addressLine1 = "1 Main",
                city = "Manila",
                countryCode = "PH",
                timeZoneId = "UTC",
                locale = "en-US",
                currencyCode = "PHP",
                expectedUpdatedAtUtc = updatedAt
            });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = await update.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Lifecycle Updated", updated.GetProperty("displayName").GetString());
        Assert.Equal("ops@lifecycle.example", updated.GetProperty("profile").GetProperty("contactEmail").GetString());
        Assert.Equal("PHP", updated.GetProperty("profile").GetProperty("currencyCode").GetString());

        var stale = await _admin.PutAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}",
            new
            {
                displayName = "Stale",
                expectedUpdatedAtUtc = updatedAt
            });
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);

        var branding = await _admin.PutAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/branding",
            new
            {
                brandDisplayName = "Lifecycle Brand",
                logoUrl = "https://cdn.example.com/logo.png",
                primaryColor = "#1677FF",
                accentColor = "#08979C",
                expectedUpdatedAtUtc = updated.GetProperty("updatedAtUtc").GetDateTimeOffset()
            });
        Assert.Equal(HttpStatusCode.OK, branding.StatusCode);
        Assert.Equal(
            "#1677FF",
            (await branding.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("branding").GetProperty("primaryColor").GetString());

        var badLogo = await _admin.PutAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/branding",
            new { logoUrl = "javascript:alert(1)" });
        Assert.Equal(HttpStatusCode.BadRequest, badLogo.StatusCode);

        var suspend = await _admin.PostAsync($"/api/v1/platform/organizations/{organizationId}/suspend", null);
        Assert.Equal(HttpStatusCode.OK, suspend.StatusCode);
        Assert.Equal("Suspended", (await suspend.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());

        var reactivate = await _admin.PostAsync($"/api/v1/platform/organizations/{organizationId}/reactivate", null);
        Assert.Equal(HttpStatusCode.OK, reactivate.StatusCode);
        Assert.Equal("Active", (await reactivate.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());

        var close = await _admin.PostAsync($"/api/v1/platform/organizations/{organizationId}/close", null);
        Assert.Equal(HttpStatusCode.OK, close.StatusCode);
        Assert.Equal("Closed", (await close.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());

        var reopen = await _admin.PostAsync($"/api/v1/platform/organizations/{organizationId}/reactivate", null);
        Assert.Equal(HttpStatusCode.Conflict, reopen.StatusCode);

        var slugConflict = await _admin.PostAsJsonAsync(
            "/api/v1/platform/organizations",
            new { displayName = "Dup", slug = org.GetProperty("slug").GetString() });
        Assert.Equal(HttpStatusCode.Conflict, slugConflict.StatusCode);

        var audit = await _admin.GetAsync(
            $"/api/v1/platform/audit?organizationId={organizationId}&page=1&pageSize=50");
        Assert.Equal(HttpStatusCode.OK, audit.StatusCode);
        var actions = (await audit.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("items")
            .EnumerateArray()
            .Select(i => i.GetProperty("actionCode").GetString())
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("platform.organization.updated", actions);
        Assert.Contains("platform.organization.branding_updated", actions);
        Assert.Contains("platform.organization.suspended", actions);
        Assert.Contains("platform.organization.reactivated", actions);
        Assert.Contains("platform.organization.closed", actions);
    }

    [Fact]
    public async Task Organization_admin_can_edit_trusted_org_profile_but_not_slug_or_other_org()
    {
        var organizationId = await CreateOrganizationAsync("oa");
        var foreignOrgId = await CreateOrganizationAsync("fx");
        var (userId, username, password) = await SeedUserWithPasswordAsync("oaadm");

        (await _admin.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/members",
            new { userId, role = "OrganizationAdministrator" })).EnsureSuccessStatusCode();

        var token = await LoginAsync(username, password);
        using (var select = Authed(
                   HttpMethod.Put,
                   "/api/v1/platform/auth/organization-context",
                   token,
                   new { organizationId }))
        {
            (await _client.SendAsync(select)).EnsureSuccessStatusCode();
        }

        using (var getTrusted = Authed(HttpMethod.Get, $"/api/v1/platform/organizations/{organizationId}", token))
        {
            var getResponse = await _client.SendAsync(getTrusted);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            var trusted = await getResponse.Content.ReadFromJsonAsync<JsonElement>();

            using var put = Authed(
                HttpMethod.Put,
                $"/api/v1/platform/organizations/{organizationId}",
                token,
                new
                {
                    displayName = "Org Admin Edited",
                    contactEmail = "admin@oa.example",
                    locale = "en-PH",
                    currencyCode = "PHP",
                    timeZoneId = "UTC",
                    expectedUpdatedAtUtc = trusted.GetProperty("updatedAtUtc").GetDateTimeOffset()
                });
            var putResponse = await _client.SendAsync(put);
            Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);
        }

        using (var slugAttempt = Authed(
                   HttpMethod.Put,
                   $"/api/v1/platform/organizations/{organizationId}",
                   token,
                   new { slug = UniqueToken("hack") }))
        {
            var slugResponse = await _client.SendAsync(slugAttempt);
            Assert.Equal(HttpStatusCode.Forbidden, slugResponse.StatusCode);
        }

        using (var foreign = Authed(
                   HttpMethod.Put,
                   $"/api/v1/platform/organizations/{foreignOrgId}",
                   token,
                   new { displayName = "Cross Org" }))
        {
            var foreignResponse = await _client.SendAsync(foreign);
            Assert.Equal(HttpStatusCode.Forbidden, foreignResponse.StatusCode);
        }

        using (var suspend = Authed(HttpMethod.Post, $"/api/v1/platform/organizations/{organizationId}/suspend", token))
        {
            var suspendResponse = await _client.SendAsync(suspend);
            Assert.Equal(HttpStatusCode.Forbidden, suspendResponse.StatusCode);
        }

        using (var create = Authed(
                   HttpMethod.Post,
                   "/api/v1/platform/organizations",
                   token,
                   new { displayName = "Escalation Org", slug = UniqueToken("esc") }))
        {
            var createResponse = await _client.SendAsync(create);
            Assert.Equal(HttpStatusCode.Forbidden, createResponse.StatusCode);
        }
    }
}
