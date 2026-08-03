using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Domain.Catalog;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApiIdentityAccessTests(PostgreSqlFixture fixture) : IAsyncLifetime
{
    private IdentityAccessApiFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new IdentityAccessApiFactory(fixture.ConnectionString);
        // No cookie jar: DevelopmentOperator stays the actor unless a session header is sent.
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
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

    [Fact]
    public async Task Platform_staff_create_without_platform_role_returns_400()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/platform/users",
            new
            {
                username = UniqueToken("norole"),
                firstName = "No",
                lastName = "Role",
                displayName = "No Role",
                email = $"{UniqueToken("norole")}@example.com"
            });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("PlatformRole is required", body.GetProperty("detail").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Platform_staff_create_requires_role_and_assigns_platform_account_only()
    {
        var username = UniqueToken("staff");
        var missingRole = await _client.PostAsJsonAsync(
            "/api/v1/platform/users",
            new
            {
                username = UniqueToken("norole"),
                firstName = "No",
                lastName = "Role",
                displayName = "No Role",
                email = $"{UniqueToken("norole")}@example.com",
                platformRole = "NotARealRole"
            });
        Assert.Equal(HttpStatusCode.BadRequest, missingRole.StatusCode);

        var create = await _client.PostAsJsonAsync(
            "/api/v1/platform/users",
            new
            {
                username,
                firstName = "Platform",
                lastName = "Staff",
                displayName = "Platform Staff",
                email = $"{username}@example.com",
                platformRole = "PlatformSupport"
            });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var userId = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var detail = await _client.GetAsync($"/api/v1/platform/users/{userId}");
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        var body = await detail.Content.ReadFromJsonAsync<JsonElement>();
        var classes = body.GetProperty("accountClasses").EnumerateArray().Select(x => x.GetString()).ToList();
        Assert.Equal(["Platform"], classes);

        var roles = await _client.GetAsync($"/api/v1/platform/authorization/assignments?platformUserId={userId}&pageSize=20");
        Assert.Equal(HttpStatusCode.OK, roles.StatusCode);
        var roleItems = (await roles.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items").EnumerateArray().ToList();
        Assert.Contains(roleItems, item => item.GetProperty("role").GetString() == "PlatformSupport");
    }

    [Fact]
    public async Task Organization_invite_provisions_organization_account_only()
    {
        var orgResponse = await _client.PostAsJsonAsync(
            "/api/v1/platform/organizations",
            new { displayName = "Staff Invite Org", slug = UniqueToken("stafforg") });
        orgResponse.EnsureSuccessStatusCode();
        var organizationId = (await orgResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var emailLocal = UniqueToken("orgstaff");
        var email = $"{emailLocal}@example.com";
        var createInvite = await _client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/invitations",
            new { email, role = "OrganizationMember" });
        Assert.Equal(HttpStatusCode.Created, createInvite.StatusCode);

        var list = await _client.GetAsync($"/api/v1/platform/users?search={emailLocal}&page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var items = (await list.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items").EnumerateArray().ToList();
        Assert.NotEmpty(items);
        var classes = items[0].GetProperty("accountClasses").EnumerateArray().Select(x => x.GetString()).ToList();
        Assert.Equal(["Organization"], classes);
        Assert.DoesNotContain(classes, c => c is "Personal" or "Platform");
    }

    [Fact]
    public async Task Platform_account_lifecycle_matrix_and_move_to_suspended()
    {
        var username = UniqueToken("life");
        var create = await _client.PostAsJsonAsync(
            "/api/v1/platform/users",
            new
            {
                username,
                firstName = "Lifecycle",
                lastName = "User",
                displayName = "Lifecycle User",
                email = $"{username}@example.com",
                platformRole = "PlatformSupport"
            });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var userId = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var suspend = await _client.PostAsJsonAsync(
            $"/api/v1/platform/users/{userId}/suspend",
            new { reason = "temporary hold" });
        Assert.Equal(HttpStatusCode.OK, suspend.StatusCode);
        Assert.Equal("Suspended", (await suspend.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());

        var reactivate = await _client.PostAsJsonAsync(
            $"/api/v1/platform/users/{userId}/reactivate",
            new { });
        Assert.Equal(HttpStatusCode.OK, reactivate.StatusCode);
        Assert.Equal("Active", (await reactivate.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());

        var deactivate = await _client.PostAsJsonAsync(
            $"/api/v1/platform/users/{userId}/deactivate",
            new { reason = "left company" });
        Assert.Equal(HttpStatusCode.OK, deactivate.StatusCode);
        Assert.Equal("Deactivated", (await deactivate.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());

        var move = await _client.PostAsJsonAsync(
            $"/api/v1/platform/users/{userId}/move-to-suspended",
            new { reason = "under review" });
        Assert.Equal(HttpStatusCode.OK, move.StatusCode);
        Assert.Equal("Suspended", (await move.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());

        var deactivateAgain = await _client.PostAsJsonAsync(
            $"/api/v1/platform/users/{userId}/deactivate",
            new { reason = "confirmed exit" });
        Assert.Equal(HttpStatusCode.OK, deactivateAgain.StatusCode);

        // DevelopmentOperator may reactivate deactivated accounts without password outside Production.
        var reactivateDeactivated = await _client.PostAsJsonAsync(
            $"/api/v1/platform/users/{userId}/reactivate",
            new { reason = "returned" });
        Assert.Equal(HttpStatusCode.OK, reactivateDeactivated.StatusCode);
        Assert.Equal("Active", (await reactivateDeactivated.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());

        var detail = await _client.GetAsync($"/api/v1/platform/users/{userId}");
        var classes = (await detail.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("accountClasses").EnumerateArray().Select(x => x.GetString()).ToList();
        Assert.Equal(["Platform"], classes);
    }

    [Fact]
    public async Task User_create_duplicate_conflicts_and_lifecycle_work()
    {
        var username = UniqueToken("user");
        var email = $"{username}@example.com";

        var create = await _client.PostAsJsonAsync(
            "/api/v1/platform/users",
            new
            {
                username,
                firstName = "Ada",
                lastName = "Lovelace",
                displayName = "Ada Lovelace",
                email,
                platformRole = "PlatformSupport"
            });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var user = await create.Content.ReadFromJsonAsync<JsonElement>();
        var userId = user.GetProperty("id").GetGuid();
        Assert.Equal("Active", user.GetProperty("status").GetString());

        var emailConflict = await _client.PostAsJsonAsync(
            "/api/v1/platform/users",
            new
            {
                username = UniqueToken("user2"),
                firstName = "Ada",
                lastName = "Two",
                displayName = "Ada Two",
                email = email.ToUpperInvariant(),
                platformRole = "PlatformSupport"
            });
        Assert.Equal(HttpStatusCode.Conflict, emailConflict.StatusCode);

        var usernameConflict = await _client.PostAsJsonAsync(
            "/api/v1/platform/users",
            new
            {
                username = username.ToUpperInvariant(),
                firstName = "Ada",
                lastName = "Three",
                displayName = "Ada Three",
                email = $"{UniqueToken("u3")}@example.com",
                platformRole = "PlatformSupport"
            });
        Assert.Equal(HttpStatusCode.Conflict, usernameConflict.StatusCode);

        var list = await _client.GetAsync($"/api/v1/platform/users?search={username}&page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.True((await list.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("totalCount").GetInt32() >= 1);

        var suspend = await _client.PostAsJsonAsync($"/api/v1/platform/users/{userId}/suspend", new { reason = "hold" });
        Assert.Equal(HttpStatusCode.OK, suspend.StatusCode);
        Assert.Equal("Suspended", (await suspend.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());

        var reactivate = await _client.PostAsync($"/api/v1/platform/users/{userId}/reactivate", null);
        Assert.Equal(HttpStatusCode.OK, reactivate.StatusCode);

        var disable = await _client.PostAsJsonAsync(
            $"/api/v1/platform/users/{userId}/disable",
            new { reason = "integration-test-deactivate" });

        Assert.Equal(HttpStatusCode.OK, disable.StatusCode);
        Assert.Equal("Deactivated", (await disable.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());
    }

    [Fact]
    public async Task Membership_and_product_access_flow_with_effective_evaluation()
    {
        var seeded = await SeedCommercialContextAsync("access");
        var username = UniqueToken("mem");
        var createUser = await _client.PostAsJsonAsync(
            "/api/v1/platform/users",
            new
            {
                username,
                firstName = "Member",
                lastName = "User",
                displayName = "Member User",
                email = $"{username}@example.com",
                platformRole = "PlatformSupport"
            });
        createUser.EnsureSuccessStatusCode();
        var userId = (await createUser.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var add = await _client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{seeded.OrganizationId}/members",
            new { userId, role = "OrganizationMember", reason = "integration-test-link" });
        Assert.Equal(HttpStatusCode.Created, add.StatusCode);
        var membershipId = (await add.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var duplicateMembership = await _client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{seeded.OrganizationId}/members",
            new { userId, role = "OrganizationOwner", reason = "integration-test-link" });
        Assert.Equal(HttpStatusCode.Conflict, duplicateMembership.StatusCode);

        var grant = await _client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{seeded.OrganizationId}/product-access",
            new { userId, productCode = seeded.ProductCode, grantedByActor = "dev-admin", reason = "test" });
        Assert.Equal(HttpStatusCode.Created, grant.StatusCode);
        var assignmentId = (await grant.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var evaluate = await _client.GetAsync(
            $"/api/v1/platform/access/evaluate?userId={userId}&organizationId={seeded.OrganizationId}&productCode={seeded.ProductCode}");
        Assert.Equal(HttpStatusCode.OK, evaluate.StatusCode);
        var allowed = await evaluate.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(allowed.GetProperty("allowed").GetBoolean());
        Assert.Equal("allowed", allowed.GetProperty("reasonCode").GetString());

        var suspendMembership = await _client.PostAsJsonAsync(
            $"/api/v1/platform/memberships/{membershipId}/suspend",
            new { reason = "temp", actorReference = "dev-admin" });
        Assert.Equal(HttpStatusCode.OK, suspendMembership.StatusCode);

        var denied = await _client.GetAsync(
            $"/api/v1/platform/access/evaluate?userId={userId}&organizationId={seeded.OrganizationId}&productCode={seeded.ProductCode}");
        var deniedBody = await denied.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(deniedBody.GetProperty("allowed").GetBoolean());
        Assert.Equal("membership_inactive", deniedBody.GetProperty("reasonCode").GetString());

        var reactivateMembership = await _client.PostAsJsonAsync(
            $"/api/v1/platform/memberships/{membershipId}/reactivate",
            new { actorReference = "dev-admin" });
        Assert.Equal(HttpStatusCode.OK, reactivateMembership.StatusCode);

        var reallowed = await _client.GetAsync(
            $"/api/v1/platform/access/evaluate?userId={userId}&organizationId={seeded.OrganizationId}&productCode={seeded.ProductCode}");
        Assert.True((await reallowed.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("allowed").GetBoolean());

        var otherOrg = await _client.PostAsJsonAsync(
            "/api/v1/platform/organizations",
            new { displayName = "Other Org", slug = UniqueToken("other") });
        otherOrg.EnsureSuccessStatusCode();
        var otherOrgId = (await otherOrg.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var crossGrant = await _client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{otherOrgId}/product-access",
            new { userId, productCode = seeded.ProductCode, grantedByActor = "dev-admin" });
        Assert.True(crossGrant.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Conflict);

        var revoke = await _client.PostAsJsonAsync(
            $"/api/v1/platform/product-access/{assignmentId}/revoke",
            new { revokedByActor = "dev-admin", reason = "done" });
        Assert.Equal(HttpStatusCode.OK, revoke.StatusCode);
        Assert.Equal("Revoked", (await revoke.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());

        var history = await _client.GetAsync(
            $"/api/v1/platform/organizations/{seeded.OrganizationId}/product-access?status=Revoked");
        Assert.Equal(HttpStatusCode.OK, history.StatusCode);
        Assert.True((await history.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("totalCount").GetInt32() >= 1);
    }

    [Fact]
    public async Task Invitation_lifecycle_create_list_accept_and_revoke()
    {
        var orgResponse = await _client.PostAsJsonAsync(
            "/api/v1/platform/organizations",
            new { displayName = "Invite Org", slug = UniqueToken("invorg") });
        orgResponse.EnsureSuccessStatusCode();
        var organizationId = (await orgResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var inviteeUsername = UniqueToken("invitee");
        var inviteeEmail = $"{inviteeUsername}@example.com";

        var createInvite = await _client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/invitations",
            new { email = inviteeEmail, role = "OrganizationMember" });
        Assert.Equal(HttpStatusCode.Created, createInvite.StatusCode);
        var inviteBody = await createInvite.Content.ReadFromJsonAsync<JsonElement>();
        var invitationId = inviteBody.GetProperty("id").GetGuid();
        var acceptToken = inviteBody.GetProperty("acceptToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(acceptToken));
        Assert.False(inviteBody.TryGetProperty("tokenHash", out _));

        var userList = await _client.GetAsync($"/api/v1/platform/users?search={inviteeUsername}&pageSize=5");
        userList.EnsureSuccessStatusCode();
        var inviteeId = (await userList.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("items")[0]
            .GetProperty("id")
            .GetGuid();

        var list = await _client.GetAsync($"/api/v1/platform/organizations/{organizationId}/invitations");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var listBody = await list.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(listBody.GetProperty("totalCount").GetInt32() >= 1);
        foreach (var item in listBody.GetProperty("items").EnumerateArray())
        {
            Assert.True(!item.TryGetProperty("acceptToken", out var t) || t.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined);
            Assert.False(item.TryGetProperty("tokenHash", out _));
        }

        var accept = await _client.PostAsJsonAsync(
            "/api/v1/platform/invitations/accept",
            new { token = acceptToken });
        // DevelopmentOperator cannot accept — requires Platform User principal.
        Assert.Equal(HttpStatusCode.Unauthorized, accept.StatusCode);

        var password = "Invitee-Accept-1!";
        (await _client.PutAsJsonAsync(
            $"/api/v1/platform/users/{inviteeId}/credentials/password",
            new { password })).EnsureSuccessStatusCode();
        (await _client.PostAsync($"/api/v1/platform/users/{inviteeId}/reactivate", null)).EnsureSuccessStatusCode();

        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = inviteeEmail, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var sessionToken = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(sessionToken));

        using var acceptRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/platform/invitations/accept")
        {
            Content = JsonContent.Create(new { token = acceptToken })
        };
        acceptRequest.Headers.Add("X-ExItS-Session-Token", sessionToken);
        var acceptAsUser = await _client.SendAsync(acceptRequest);
        Assert.Equal(HttpStatusCode.OK, acceptAsUser.StatusCode);
        Assert.Equal("OrganizationMember", (await acceptAsUser.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("role").GetString());

        using var reuseRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/platform/invitations/accept")
        {
            Content = JsonContent.Create(new { token = acceptToken })
        };
        reuseRequest.Headers.Add("X-ExItS-Session-Token", sessionToken);
        var reuse = await _client.SendAsync(reuseRequest);
        Assert.True(reuse.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Conflict or HttpStatusCode.BadRequest);

        var secondEmail = $"{UniqueToken("pending")}@example.com";
        var second = await _client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/invitations",
            new { email = secondEmail, role = "OrganizationMember", displayName = "Pending Invitee" });
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        var secondId = (await second.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var revoke = await _client.PostAsync($"/api/v1/platform/invitations/{secondId}/revoke", null);
        Assert.Equal(HttpStatusCode.OK, revoke.StatusCode);
        Assert.Equal("Revoked", (await revoke.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());
        Assert.NotEqual(Guid.Empty, invitationId);
    }

    [Fact]
    public async Task Final_governing_admin_cannot_be_revoked()
    {
        var orgResponse = await _client.PostAsJsonAsync(
            "/api/v1/platform/organizations",
            new { displayName = "Solo Org", slug = UniqueToken("solo") });
        orgResponse.EnsureSuccessStatusCode();
        var organizationId = (await orgResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var username = UniqueToken("owner");
        var createUser = await _client.PostAsJsonAsync(
            "/api/v1/platform/users",
            new
            {
                username,
                firstName = "Solo",
                lastName = "Owner",
                displayName = "Solo Owner",
                email = $"{username}@example.com",
                platformRole = "PlatformSupport"
            });
        createUser.EnsureSuccessStatusCode();
        var userId = (await createUser.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var add = await _client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/members",
            new { userId, role = "OrganizationOwner", reason = "integration-test-link" });
        Assert.Equal(HttpStatusCode.Created, add.StatusCode);
        var membershipId = (await add.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var revoke = await _client.PostAsJsonAsync(
            $"/api/v1/platform/memberships/{membershipId}/revoke",
            new { reason = "should fail", actorReference = "dev-admin" });
        Assert.Equal(HttpStatusCode.Conflict, revoke.StatusCode);
        var problem = await revoke.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("platform.membership.last_governing_admin", problem.GetProperty("errorCode").GetString());
    }

    private async Task<(Guid OrganizationId, string ProductCode)> SeedCommercialContextAsync(string prefix)
    {
        var orgResponse = await _client.PostAsJsonAsync(
            "/api/v1/platform/organizations",
            new { displayName = $"{prefix} Org", slug = UniqueToken(prefix) });
        orgResponse.EnsureSuccessStatusCode();
        var organizationId = (await orgResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

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

        var now = DateTimeOffset.UtcNow;
        (await _client.PostAsJsonAsync(
            $"/api/v1/platform/subscriptions/{subscriptionId}/activate",
            new { periodStartUtc = now, periodEndUtc = now.AddDays(30) })).EnsureSuccessStatusCode();

        var snapshot = await _client.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/products/{productCode}/entitlements/snapshots",
            new { });
        snapshot.EnsureSuccessStatusCode();

        return (organizationId, productCode);
    }

    private sealed class IdentityAccessApiFactory(string connectionString) : WebApplicationFactory<Program>
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
