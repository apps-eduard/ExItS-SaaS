using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApiOrganizationProductFilterTests(PostgreSqlFixture fixture) : IAsyncLifetime
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

    private static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    private async Task<Guid> CreateOrganizationAsync(string displayName, string slugPrefix)
    {
        var create = await _admin.PostAsJsonAsync(
            "/api/v1/platform/organizations",
            new { displayName, slug = Unique(slugPrefix) });
        create.EnsureSuccessStatusCode();
        return (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private async Task<(string ProductCode, Guid PlanId, Guid VersionId, Guid TrialId)> SeedCatalogAsync(string prefix)
    {
        var candidate = Unique(prefix);
        var productCode = candidate[..Math.Min(30, candidate.Length)];
        (await _admin.PostAsJsonAsync(
            "/api/v1/platform/catalog/products",
            new { code = productCode, displayName = $"{prefix} Product" })).EnsureSuccessStatusCode();

        (await _admin.PostAsJsonAsync(
            $"/api/v1/platform/catalog/products/{productCode}/features",
            new
            {
                featureCode = FeatureCode.CustomerCreditView,
                displayName = "View Credit",
                valueType = nameof(FeatureValueType.Boolean)
            })).EnsureSuccessStatusCode();

        var plan = await _admin.PostAsJsonAsync(
            $"/api/v1/platform/catalog/products/{productCode}/plans",
            new { code = "std", displayName = "Standard" });
        plan.EnsureSuccessStatusCode();
        var planId = (await plan.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        (await _admin.PostAsync(
            $"/api/v1/platform/catalog/products/{productCode}/plans/{planId}/activate",
            null)).EnsureSuccessStatusCode();

        var draft = await _admin.PostAsJsonAsync(
            $"/api/v1/platform/catalog/products/{productCode}/plans/{planId}/versions/draft",
            new
            {
                versionNumber = 1,
                billingPeriod = nameof(BillingPeriod.Monthly),
                trialEligible = true,
                grants = new[]
                {
                    new { featureCode = FeatureCode.CustomerCreditView, enabled = true }
                }
            });
        draft.EnsureSuccessStatusCode();
        var versionId = (await draft.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        (await _admin.PostAsync(
            $"/api/v1/platform/catalog/products/{productCode}/plans/{planId}/versions/1/publish",
            null)).EnsureSuccessStatusCode();

        var trial = await _admin.PostAsJsonAsync(
            $"/api/v1/platform/catalog/products/{productCode}/trials",
            new
            {
                displayName = "Trial",
                durationTicks = TimeSpan.FromDays(14).Ticks,
                featureGrants = new[] { new { featureCode = FeatureCode.CustomerCreditView, enabled = true } },
                postExpiryFeatureGrants = Array.Empty<object>()
            });
        trial.EnsureSuccessStatusCode();
        var trialId = (await trial.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        return (productCode, planId, versionId, trialId);
    }

    private async Task StartTrialAsync(Guid organizationId, Guid planId, Guid versionId, Guid trialId)
    {
        var start = await _admin.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/subscriptions/trials",
            new { planId, planVersionId = versionId, trialDefinitionId = trialId });
        start.EnsureSuccessStatusCode();
    }

    private static IReadOnlyList<JsonElement> Items(JsonElement body) =>
        body.GetProperty("items").EnumerateArray().ToList();

    private static IReadOnlyList<Guid> Ids(JsonElement body) =>
        Items(body).Select(i => i.GetProperty("id").GetGuid()).ToList();

    [Fact]
    public async Task Organization_list_filters_by_subscription_product_without_duplicates()
    {
        var marker = Unique("opf");
        var (productX, planX, versionX, trialX) = await SeedCatalogAsync("px");
        var (productY, planY, versionY, trialY) = await SeedCatalogAsync("py");

        var orgA = await CreateOrganizationAsync($"{marker} Alpha", "opfa");
        var orgB = await CreateOrganizationAsync($"{marker} Beta", "opfb");
        var orgC = await CreateOrganizationAsync($"{marker} Gamma", "opfg");

        await StartTrialAsync(orgA, planX, versionX, trialX);
        await StartTrialAsync(orgB, planY, versionY, trialY);
        await StartTrialAsync(orgC, planX, versionX, trialX);
        await StartTrialAsync(orgC, planY, versionY, trialY);

        (await _admin.PostAsync($"/api/v1/platform/organizations/{orgC}/suspend", null)).EnsureSuccessStatusCode();

        var noFilter = await _admin.GetAsync($"/api/v1/platform/organizations?search={Uri.EscapeDataString(marker)}&page=1&pageSize=50");
        Assert.Equal(HttpStatusCode.OK, noFilter.StatusCode);
        var noFilterBody = await noFilter.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(3, noFilterBody.GetProperty("totalCount").GetInt32());
        Assert.Equal(3, Ids(noFilterBody).Distinct().Count());
        Assert.Contains(orgA, Ids(noFilterBody));
        Assert.Contains(orgB, Ids(noFilterBody));
        Assert.Contains(orgC, Ids(noFilterBody));

        var filterX = await _admin.GetAsync($"/api/v1/platform/organizations?productCode={productX}&page=1&pageSize=50");
        Assert.Equal(HttpStatusCode.OK, filterX.StatusCode);
        var filterXBody = await filterX.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, filterXBody.GetProperty("totalCount").GetInt32());
        var idsX = Ids(filterXBody);
        Assert.Equal(2, idsX.Distinct().Count());
        Assert.Contains(orgA, idsX);
        Assert.Contains(orgC, idsX);
        Assert.DoesNotContain(orgB, idsX);

        var filterY = await _admin.GetAsync($"/api/v1/platform/organizations?productCode={productY}&page=1&pageSize=50");
        Assert.Equal(HttpStatusCode.OK, filterY.StatusCode);
        var filterYBody = await filterY.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, filterYBody.GetProperty("totalCount").GetInt32());
        var idsY = Ids(filterYBody);
        Assert.Equal(2, idsY.Distinct().Count());
        Assert.Contains(orgB, idsY);
        Assert.Contains(orgC, idsY);
        Assert.DoesNotContain(orgA, idsY);

        var searchX = await _admin.GetAsync(
            $"/api/v1/platform/organizations?productCode={productX}&search={Uri.EscapeDataString($"{marker} Alpha")}&page=1&pageSize=50");
        var searchXBody = await searchX.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, searchXBody.GetProperty("totalCount").GetInt32());
        Assert.Equal(orgA, Assert.Single(Ids(searchXBody)));

        var statusX = await _admin.GetAsync(
            $"/api/v1/platform/organizations?productCode={productX}&status=Suspended&page=1&pageSize=50");
        var statusXBody = await statusX.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, statusXBody.GetProperty("totalCount").GetInt32());
        Assert.Equal(orgC, Assert.Single(Ids(statusXBody)));

        var sorted = await _admin.GetAsync(
            $"/api/v1/platform/organizations?productCode={productX}&sortBy=Slug&sortDesc=true&page=1&pageSize=50");
        var sortedIds = Ids(await sorted.Content.ReadFromJsonAsync<JsonElement>());
        Assert.Equal(2, sortedIds.Count);
        var slugs = Items(await (await _admin.GetAsync(
            $"/api/v1/platform/organizations?productCode={productX}&sortBy=Slug&sortDesc=true&page=1&pageSize=50"))
            .Content.ReadFromJsonAsync<JsonElement>())
            .Select(i => i.GetProperty("slug").GetString()!)
            .ToList();
        Assert.True(string.CompareOrdinal(slugs[0], slugs[1]) >= 0);

        var page1 = await _admin.GetAsync(
            $"/api/v1/platform/organizations?productCode={productX}&sortBy=Slug&page=1&pageSize=1");
        var page1Body = await page1.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, page1Body.GetProperty("totalCount").GetInt32());
        Assert.Single(Ids(page1Body));
        var page2 = await _admin.GetAsync(
            $"/api/v1/platform/organizations?productCode={productX}&sortBy=Slug&page=2&pageSize=1");
        var page2Body = await page2.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, page2Body.GetProperty("totalCount").GetInt32());
        Assert.Single(Ids(page2Body));
        Assert.Equal(2, Ids(page1Body).Concat(Ids(page2Body)).Distinct().Count());
        Assert.DoesNotContain(orgB, Ids(page1Body).Concat(Ids(page2Body)));

        var invalid = await _admin.GetAsync("/api/v1/platform/organizations?productCode=NOT_VALID");
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        var unknown = await _admin.GetAsync("/api/v1/platform/organizations?productCode=no-such-catalog-product");
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);

        var (_, email, password) = await PlatformIntegrationTestUsers.RegisterPersonalWithPasswordAsync(_client, "opfu");
        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = email, password });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionToken").GetString()!;

        using var unfiltered = new HttpRequestMessage(HttpMethod.Get, "/api/v1/platform/organizations");
        unfiltered.Headers.Add("X-ExItS-Session-Token", token);
        var unfilteredDenied = await _client.SendAsync(unfiltered);
        Assert.Equal(HttpStatusCode.Forbidden, unfilteredDenied.StatusCode);

        using var filtered = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/platform/organizations?productCode={productX}");
        filtered.Headers.Add("X-ExItS-Session-Token", token);
        var filteredDenied = await _client.SendAsync(filtered);
        Assert.Equal(HttpStatusCode.Forbidden, filteredDenied.StatusCode);
    }
}
