using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Application.Access;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApiCurrentSessionProductAccessTests(PostgreSqlFixture fixture) : IAsyncLifetime
{
    private SessionApiFactory _factory = null!;
    private HttpClient _admin = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new SessionApiFactory(fixture.ConnectionString);
        _admin = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _admin.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
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
    public async Task Unauthenticated_current_session_evaluation_is_rejected()
    {
        var response = await _client.GetAsync(
            $"/api/v1/platform/auth/product-access/effective?productCode={ProductCode.PinoyLoanManager}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ApplicationErrorCodes.SessionInvalid, body.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Personal_session_is_account_scope_denied()
    {
        var (_, email, password) = await PlatformIntegrationTestUsers.RegisterPersonalWithPasswordAsync(_client, "plmpers");
        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = email, password });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionToken").GetString()!;

        using var request = Authed(
            HttpMethod.Get,
            $"/api/v1/platform/auth/product-access/effective?productCode={ProductCode.PinoyLoanManager}",
            token);
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ApplicationErrorCodes.AccountScopeDenied, body.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Platform_session_is_rejected_for_organization_product_entry()
    {
        var (_, username, password) = await PlatformIntegrationTestUsers.CreatePlatformStaffWithPasswordAsync(_admin, "plmplat");
        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = username, password });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionToken").GetString()!;

        using var request = Authed(
            HttpMethod.Get,
            $"/api/v1/platform/auth/product-access/effective?productCode={ProductCode.PinoyLoanManager}",
            token);
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ApplicationErrorCodes.AccountScopeDenied, body.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Invalid_product_code_is_rejected()
    {
        var seeded = await SeedOrgStaffWithPlmCommercialAsync(grantAccess: false);
        using var request = Authed(
            HttpMethod.Get,
            "/api/v1/platform/auth/product-access/effective?productCode=not_a_valid_code",
            seeded.Token);
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(DomainErrorCodes.InvalidProductCode, body.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Missing_assignment_returns_allowed_false_without_manage_product_access()
    {
        var seeded = await SeedOrgStaffWithPlmCommercialAsync(grantAccess: false);
        using var request = Authed(
            HttpMethod.Get,
            $"/api/v1/platform/auth/product-access/effective?productCode={ProductCode.PinoyLoanManager}&userId={Guid.NewGuid():D}&organizationId={Guid.NewGuid():D}",
            seeded.Token);
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(body.GetProperty("allowed").GetBoolean());
        Assert.Equal(EffectiveAccessReasonCodes.ProductAssignmentMissing, body.GetProperty("reasonCode").GetString());
        Assert.Equal(seeded.UserId, body.GetProperty("userId").GetGuid());
        Assert.Equal(seeded.OrganizationId, body.GetProperty("organizationId").GetGuid());

        using var privileged = Authed(
            HttpMethod.Get,
            $"/api/v1/platform/access/evaluate?userId={seeded.UserId:D}&organizationId={seeded.OrganizationId:D}&productCode={ProductCode.PinoyLoanManager}",
            seeded.Token);
        var privilegedResponse = await _client.SendAsync(privileged);
        Assert.Equal(HttpStatusCode.Forbidden, privilegedResponse.StatusCode);
    }

    [Fact]
    public async Task Allowed_self_evaluation_uses_session_user_and_selected_org()
    {
        var seeded = await SeedOrgStaffWithPlmCommercialAsync(grantAccess: true);
        using var request = Authed(
            HttpMethod.Get,
            $"/api/v1/platform/auth/product-access/effective?productCode={ProductCode.PinoyLoanManager}&userId={Guid.NewGuid():D}&organizationId={Guid.NewGuid():D}",
            seeded.Token);
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("allowed").GetBoolean());
        Assert.Equal(EffectiveAccessReasonCodes.Allowed, body.GetProperty("reasonCode").GetString());
        Assert.Equal(seeded.UserId, body.GetProperty("userId").GetGuid());
        Assert.Equal(seeded.OrganizationId, body.GetProperty("organizationId").GetGuid());
        Assert.Equal(ProductCode.PinoyLoanManager, body.GetProperty("productCode").GetString());
    }

    [Fact]
    public async Task Privileged_evaluate_remains_available_to_operator_and_pos_independent()
    {
        var seeded = await SeedOrgStaffWithPlmCommercialAsync(grantAccess: true);
        var evaluate = await _admin.GetAsync(
            $"/api/v1/platform/access/evaluate?userId={seeded.UserId:D}&organizationId={seeded.OrganizationId:D}&productCode={ProductCode.PinoyLoanManager}");
        Assert.Equal(HttpStatusCode.OK, evaluate.StatusCode);
        Assert.True((await evaluate.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("allowed").GetBoolean());

        var posDenied = await _admin.GetAsync(
            $"/api/v1/platform/access/evaluate?userId={seeded.UserId:D}&organizationId={seeded.OrganizationId:D}&productCode={ProductCode.PinoyBusinessPos}");
        Assert.Equal(HttpStatusCode.OK, posDenied.StatusCode);
        var posBody = await posDenied.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(posBody.GetProperty("allowed").GetBoolean());
    }

    private async Task<(Guid UserId, Guid OrganizationId, string Token)> SeedOrgStaffWithPlmCommercialAsync(bool grantAccess)
    {
        var (userId, _, staffLogin, password, organizationId) =
            await PlatformIntegrationTestUsers.SeedOrgMemberViaInvitationAsync(_admin, _client, "plmacc");
        await EnsurePlmCommercialAsync(organizationId);
        if (grantAccess)
        {
            var grant = await _admin.PostAsJsonAsync(
                $"/api/v1/platform/organizations/{organizationId}/product-access",
                new
                {
                    userId,
                    productCode = ProductCode.PinoyLoanManager,
                    grantedByActor = "dev-admin",
                    reason = "plm-d3-pre"
                });
            grant.EnsureSuccessStatusCode();
        }

        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = staffLogin, password });
        login.EnsureSuccessStatusCode();
        var loginBody = await login.Content.ReadFromJsonAsync<JsonElement>();
        var token = loginBody.GetProperty("sessionToken").GetString()!;
        Assert.Equal("Organization", loginBody.GetProperty("accountClass").GetString());
        Assert.Equal(organizationId, loginBody.GetProperty("selectedOrganizationId").GetGuid());
        return (userId, organizationId, token);
    }

    private async Task EnsurePlmCommercialAsync(Guid organizationId)
    {
        var created = await _admin.PostAsJsonAsync(
            "/api/v1/platform/catalog/products",
            new { code = ProductCode.PinoyLoanManager, displayName = "Pinoy Loan Manager" });
        if (!created.IsSuccessStatusCode && created.StatusCode != HttpStatusCode.Conflict)
        {
            created.EnsureSuccessStatusCode();
        }

        var plans = await _admin.GetAsync($"/api/v1/platform/catalog/products/{ProductCode.PinoyLoanManager}/plans");
        plans.EnsureSuccessStatusCode();
        var planItems = await plans.Content.ReadFromJsonAsync<JsonElement>();
        Guid planId;
        if (planItems.ValueKind != JsonValueKind.Array || planItems.GetArrayLength() == 0)
        {
            var plan = await _admin.PostAsJsonAsync(
                $"/api/v1/platform/catalog/products/{ProductCode.PinoyLoanManager}/plans",
                new { code = "plm-local-validation", displayName = "PLM Local Validation" });
            plan.EnsureSuccessStatusCode();
            planId = (await plan.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
            (await _admin.PostAsync(
                $"/api/v1/platform/catalog/products/{ProductCode.PinoyLoanManager}/plans/{planId}/activate",
                null)).EnsureSuccessStatusCode();
        }
        else
        {
            planId = planItems[0].GetProperty("id").GetGuid();
        }

        var versions = await _admin.GetAsync(
            $"/api/v1/platform/catalog/products/{ProductCode.PinoyLoanManager}/plans/{planId}/versions");
        versions.EnsureSuccessStatusCode();
        var versionItems = await versions.Content.ReadFromJsonAsync<JsonElement>();
        Guid versionId;
        if (versionItems.ValueKind != JsonValueKind.Array || versionItems.GetArrayLength() == 0)
        {
            var draft = await _admin.PostAsJsonAsync(
                $"/api/v1/platform/catalog/products/{ProductCode.PinoyLoanManager}/plans/{planId}/versions/draft",
                new
                {
                    versionNumber = 1,
                    billingPeriod = nameof(BillingPeriod.None),
                    trialEligible = true,
                    grants = Array.Empty<object>()
                });
            draft.EnsureSuccessStatusCode();
            versionId = (await draft.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
            (await _admin.PostAsync(
                $"/api/v1/platform/catalog/products/{ProductCode.PinoyLoanManager}/plans/{planId}/versions/1/publish",
                null)).EnsureSuccessStatusCode();
        }
        else
        {
            versionId = versionItems[0].GetProperty("id").GetGuid();
        }

        var trials = await _admin.GetAsync($"/api/v1/platform/catalog/products/{ProductCode.PinoyLoanManager}/trials");
        trials.EnsureSuccessStatusCode();
        var trialItems = await trials.Content.ReadFromJsonAsync<JsonElement>();
        Guid trialId;
        if (trialItems.ValueKind != JsonValueKind.Array || trialItems.GetArrayLength() == 0)
        {
            var trial = await _admin.PostAsJsonAsync(
                $"/api/v1/platform/catalog/products/{ProductCode.PinoyLoanManager}/trials",
                new
                {
                    displayName = "PLM Local Validation",
                    durationTicks = TimeSpan.FromDays(14).Ticks,
                    featureGrants = Array.Empty<object>(),
                    postExpiryFeatureGrants = Array.Empty<object>()
                });
            trial.EnsureSuccessStatusCode();
            trialId = (await trial.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        }
        else
        {
            trialId = trialItems[0].GetProperty("id").GetGuid();
        }

        var start = await _admin.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/subscriptions/trials",
            new { planId, planVersionId = versionId, trialDefinitionId = trialId });
        if (!start.IsSuccessStatusCode && start.StatusCode != HttpStatusCode.Conflict)
        {
            start.EnsureSuccessStatusCode();
        }

        var snapshot = await _admin.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/products/{ProductCode.PinoyLoanManager}/entitlements/snapshots",
            new { });
        snapshot.EnsureSuccessStatusCode();
    }
}
