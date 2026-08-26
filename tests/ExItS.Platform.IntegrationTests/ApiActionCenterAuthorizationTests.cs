using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Application.Admin;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Infrastructure.Authorization;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApiActionCenterAuthorizationTests(PostgreSqlFixture fixture)
{
    private const string ActionCenterRoute = "/api/v1/platform/admin/action-center";

    [Fact]
    public async Task ViewPortfolio_only_user_cannot_receive_payment_account_job_or_health_actions()
    {
        await using var factory = new Factory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var (userId, _, _) = await PlatformIntegrationTestUsers.CreatePlatformStaffWithPasswordAsync(
            client,
            "acport",
            nameof(PlatformSystemRole.PlatformAuditor));

        using var request = new HttpRequestMessage(HttpMethod.Get, ActionCenterRoute);
        request.Headers.Add(DevelopmentPlatformActorAccessor.DevPlatformUserIdHeader, userId.ToString("D"));
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var categories = await ReadCategoriesAsync(response);
        Assert.DoesNotContain(ActionCenterCategories.Payment, categories);
        Assert.DoesNotContain(ActionCenterCategories.Account, categories);
        Assert.DoesNotContain(ActionCenterCategories.Job, categories);
        Assert.DoesNotContain(ActionCenterCategories.Health, categories);
        Assert.DoesNotContain(ActionCenterCategories.Subscription, categories);
        Assert.All(categories, category =>
            Assert.Equal(ActionCenterCategories.Usage, category));
    }

    [Fact]
    public async Task Billing_authorized_user_receives_payment_actions_when_present()
    {
        await using var factory = new Factory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var (userId, _, _) = await PlatformIntegrationTestUsers.CreatePlatformStaffWithPasswordAsync(
            client,
            "acbill",
            nameof(PlatformSystemRole.BillingAdministrator));

        using var request = new HttpRequestMessage(HttpMethod.Get, ActionCenterRoute);
        request.Headers.Add(DevelopmentPlatformActorAccessor.DevPlatformUserIdHeader, userId.ToString("D"));
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var categories = await ReadCategoriesAsync(response);
        Assert.DoesNotContain(ActionCenterCategories.Account, categories);
        Assert.DoesNotContain(ActionCenterCategories.Job, categories);
        Assert.DoesNotContain(ActionCenterCategories.Health, categories);

        // BillingAdministrator may receive payment/subscription/usage when data exists.
        Assert.All(
            categories,
            category => Assert.Contains(
                category,
                new[]
                {
                    ActionCenterCategories.Payment,
                    ActionCenterCategories.Subscription,
                    ActionCenterCategories.Usage
                }));
    }

    [Fact]
    public async Task Platform_administrator_may_receive_all_action_categories()
    {
        await using var factory = new Factory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var (userId, _, _) = await PlatformIntegrationTestUsers.CreatePlatformStaffWithPasswordAsync(
            client,
            "acadm",
            nameof(PlatformSystemRole.PlatformAdministrator));

        using var request = new HttpRequestMessage(HttpMethod.Get, ActionCenterRoute);
        request.Headers.Add(DevelopmentPlatformActorAccessor.DevPlatformUserIdHeader, userId.ToString("D"));
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var categories = await ReadCategoriesAsync(response);
        Assert.All(
            categories,
            category => Assert.Contains(
                category,
                new[]
                {
                    ActionCenterCategories.Payment,
                    ActionCenterCategories.Subscription,
                    ActionCenterCategories.Usage,
                    ActionCenterCategories.Account,
                    ActionCenterCategories.Job,
                    ActionCenterCategories.Health,
                    ActionCenterCategories.Organization
                }));
    }

    [Fact]
    public void Access_scope_maps_permissions_without_over_granting()
    {
        var portfolioOnly = ActionCenterAccessScope.FromPermissions(
            new HashSet<string>(StringComparer.Ordinal) { PlatformPermission.ViewPortfolio });
        Assert.True(portfolioOnly.IncludeUsage);
        Assert.False(portfolioOnly.IncludeSubscriptions);
        Assert.False(portfolioOnly.IncludePayments);
        Assert.False(portfolioOnly.IncludeAccounts);
        Assert.False(portfolioOnly.IncludeJobs);
        Assert.False(portfolioOnly.IncludeHealth);

        var billing = ActionCenterAccessScope.FromPermissions(
            new HashSet<string>(StringComparer.Ordinal)
            {
                PlatformPermission.ViewPortfolio,
                PlatformPermission.ManageSubscriptions,
                PlatformPermission.ManageManualPayments
            });
        Assert.True(billing.IncludeUsage);
        Assert.True(billing.IncludeSubscriptions);
        Assert.True(billing.IncludePayments);
        Assert.False(billing.IncludeAccounts);
        Assert.False(billing.IncludeJobs);
        Assert.False(billing.IncludeHealth);

        var admin = ActionCenterAccessScope.FromPermissions(
            new HashSet<string>(PlatformPermission.All, StringComparer.Ordinal));
        Assert.True(admin.IncludeUsage);
        Assert.True(admin.IncludeSubscriptions);
        Assert.True(admin.IncludePayments);
        Assert.True(admin.IncludeAccounts);
        Assert.True(admin.IncludeJobs);
        Assert.True(admin.IncludeHealth);
    }

    private static async Task<HashSet<string>> ReadCategoriesAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("items", out var items));
        return items.EnumerateArray()
            .Select(item => item.GetProperty("category").GetString()!)
            .ToHashSet(StringComparer.Ordinal);
    }

    private sealed class Factory : WebApplicationFactory<Program>
    {
        private readonly string _connectionString;

        public Factory(string connectionString) => _connectionString = connectionString;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:PlatformDatabase", _connectionString);
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PlatformDatabase"] = _connectionString
                });
            });
        }
    }
}
