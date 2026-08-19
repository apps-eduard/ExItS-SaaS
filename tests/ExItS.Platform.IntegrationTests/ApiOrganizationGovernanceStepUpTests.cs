using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Governance;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Infrastructure;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExItS.Platform.IntegrationTests;

/// <summary>P28-WP15F: governance password step-up grants and critical action enforcement.</summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class ApiOrganizationGovernanceStepUpTests(PostgreSqlFixture fixture) : IAsyncLifetime
{
    private SessionApiFactory _factory = null!;
    private HttpClient _client = null!;
    private HttpClient _admin = null!;

    public Task InitializeAsync()
    {
        _factory = new SessionApiFactory(fixture.ConnectionString);
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        _admin = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
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

    private async Task<(Guid UserId, string Email, string Password)> RegisterPersonalAsync(string prefix) =>
        await PlatformIntegrationTestUsers.RegisterPersonalWithPasswordAsync(_client, prefix);

    private async Task<string> LoginAsync(string email, string password)
    {
        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
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

    private async Task<(Guid OrganizationId, string OwnerPassword, string Token, Guid PrimaryBranchId)> SeedOwnerSessionAsync(string prefix)
    {
        await EnsureMvpCatalogAsync();

        var (_, email, password) = await RegisterPersonalAsync(prefix);
        var personalToken = await LoginAsync(email, password);

        using var businessTypesRequest = Authed(
            HttpMethod.Get,
            "/api/v1/personal/onboarding/business-types",
            personalToken);
        var businessTypesResponse = await _client.SendAsync(businessTypesRequest);
        businessTypesResponse.EnsureSuccessStatusCode();
        var businessTypes = await businessTypesResponse.Content.ReadFromJsonAsync<JsonElement>();
        var primaryBusinessTypeId = businessTypes!.EnumerateArray()
            .First()
            .GetProperty("id")
            .GetGuid();

        var slug = UniqueToken(prefix);
        using var start = Authed(
            HttpMethod.Post,
            "/api/v1/personal/start-business",
            personalToken,
            new
            {
                displayName = $"{prefix} Org",
                slug,
                primaryBusinessTypeId,
                productCode = ProductCode.PinoyBusinessPos,
                planKey = MvpPosPlanCodes.Growth,
                billingCycle = "Monthly",
                startAsTrial = true,
                payNow = false,
                activatePosEntitlement = true,
                activateProductAccess = true,
                assignPosOwnerRole = false
            });
        var startResponse = await _client.SendAsync(start);
        startResponse.EnsureSuccessStatusCode();
        var started = await startResponse.Content.ReadFromJsonAsync<JsonElement>();

        var organizationId = started!.GetProperty("organizationId").GetGuid();
        var primaryBranchId = started.GetProperty("primaryBranchId").GetGuid();

        // Use platform login token (not the personal start-business token) so PlatformAuthz can
        // populate PlatformUserId for step-up authentication.
        var token = await LoginAsync(email, password);
        using var selectOrg = Authed(
            HttpMethod.Put,
            "/api/v1/platform/auth/organization-context",
            token,
            new { organizationId });
        var selectResponse = await _client.SendAsync(selectOrg);
        selectResponse.EnsureSuccessStatusCode();

        // Some authorization paths rely on organization context claims being present
        // in the current platform session. Re-login after selecting organization context
        // to ensure claims are synchronized.
        var refreshedToken = await LoginAsync(email, password);

        // Ensure the refreshed token is in trusted selected organization context;
        // critical membership governance authz depends on this.
        using var me = Authed(HttpMethod.Get, "/api/v1/platform/auth/me", refreshedToken);
        var meResponse = await _client.SendAsync(me);
        meResponse.EnsureSuccessStatusCode();
        var meBody = await meResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(organizationId, meBody!.GetProperty("selectedOrganizationId").GetGuid());
        var actorUserId = meBody.GetProperty("userId").GetGuid();

        // Verify that the selected-organization actor is actually the governing
        // Organization Owner seat (required by step-up protected membership changes).
        var membersAdmin = await _admin.GetAsync(
            $"/api/v1/platform/organizations/{organizationId:D}/members?status=Active&pageSize=50");
        membersAdmin.EnsureSuccessStatusCode();
        var membersBody = await membersAdmin.Content.ReadFromJsonAsync<JsonElement>();
        var ownerItem = membersBody!.GetProperty("items").EnumerateArray()
            .First(i => string.Equals(i.GetProperty("role").GetString(), "OrganizationOwner", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(actorUserId, ownerItem.GetProperty("userId").GetGuid());

        return (organizationId, password, refreshedToken, primaryBranchId);
    }

    private async Task<Guid> SeedStaffMembershipAsync(Guid organizationId)
    {
        var emailLocal = UniqueToken("staff");
        var contactEmail = $"{emailLocal}@example.com";
        var invite = await _admin.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId:D}/invitations",
            new { email = contactEmail, role = "OrganizationMember", requireEmailVerification = false });
        invite.EnsureSuccessStatusCode();
        var acceptToken = (await invite.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("acceptToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(acceptToken));

        var accept = await _client.PostAsJsonAsync(
            "/api/v1/platform/invitations/accept",
            new { token = acceptToken, password = "Correct-Horse-9!" });
        accept.EnsureSuccessStatusCode();

        using var members = await _admin.GetAsync(
            $"/api/v1/platform/organizations/{organizationId:D}/members?status=Active&pageSize=50");
        members.EnsureSuccessStatusCode();
        var page = await members.Content.ReadFromJsonAsync<JsonElement>();
        return page!.GetProperty("items").EnumerateArray()
            .First(m => !string.Equals(m.GetProperty("role").GetString(), "OrganizationOwner", StringComparison.Ordinal))
            .GetProperty("id")
            .GetGuid();
    }

    private async Task<string> IssueStepUpAsync(
        Guid organizationId,
        string token,
        string password,
        string actionCode,
        string targetType,
        Guid targetId)
    {
        using var request = Authed(
            HttpMethod.Post,
            $"/api/v1/platform/organizations/{organizationId:D}/governance/step-up",
            token,
            new
            {
                actionCode,
                targetType,
                targetId,
                currentPassword = password
            });
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("stepUpToken").GetString()!;
    }

    private async Task<Guid> StartBusinessTrialAsync(Guid organizationId)
    {
        await EnsureMvpCatalogAsync();

        var plans = await _admin.GetAsync(
            $"/api/v1/platform/catalog/plans?productCode={ProductCode.PinoyBusinessPos}&status=Active&pageSize=20");
        plans.EnsureSuccessStatusCode();

        var businessPlan = (await plans.Content.ReadFromJsonAsync<JsonElement>())!
            .GetProperty("items")
            .EnumerateArray()
            .First(p => string.Equals(
                p.GetProperty("planKey").GetString(),
                MvpPosPlanCodes.Growth,
                StringComparison.Ordinal));

        var trial = await _admin.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId:D}/subscriptions/trials",
            new
            {
                planId = businessPlan.GetProperty("id").GetGuid(),
                planVersionId = (await GetPublishedVersionIdAsync(businessPlan.GetProperty("id").GetGuid())),
                trialDefinitionId = (await EnsureTrialDefinitionAsync())
            });

        trial.EnsureSuccessStatusCode();
        return (await trial.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private async Task<Guid> GetPublishedVersionIdAsync(Guid planId)
    {
        var versions = await _admin.GetAsync(
            $"/api/v1/platform/catalog/products/{ProductCode.PinoyBusinessPos}/plans/{planId}/versions");
        versions.EnsureSuccessStatusCode();
        return (await versions.Content.ReadFromJsonAsync<JsonElement>())!
            .EnumerateArray()
            .First(v => string.Equals(
                v.GetProperty("status").GetString(),
                nameof(PlanVersionStatus.Published),
                StringComparison.Ordinal))
            .GetProperty("id")
            .GetGuid();
    }

    private async Task<Guid> EnsureTrialDefinitionAsync()
    {
        var trials = await _admin.GetAsync(
            $"/api/v1/platform/catalog/products/{ProductCode.PinoyBusinessPos}/trials");
        trials.EnsureSuccessStatusCode();
        var items = (await trials.Content.ReadFromJsonAsync<JsonElement>())!.EnumerateArray().ToList();
        if (items.Count > 0)
        {
            return items[0].GetProperty("id").GetGuid();
        }

        var createFeature = await _admin.PostAsJsonAsync(
            $"/api/v1/platform/catalog/products/{ProductCode.PinoyBusinessPos}/features",
            new
            {
                featureCode = FeatureCode.CustomerCreditView,
                displayName = "View Credit",
                valueType = nameof(FeatureValueType.Boolean)
            });
        if (createFeature.StatusCode != HttpStatusCode.Created
            && createFeature.StatusCode != HttpStatusCode.BadRequest
            && createFeature.StatusCode != HttpStatusCode.Conflict)
        {
            createFeature.EnsureSuccessStatusCode();
        }

        var createTrial = await _admin.PostAsJsonAsync(
            $"/api/v1/platform/catalog/products/{ProductCode.PinoyBusinessPos}/trials",
            new
            {
                displayName = "MVP Trial",
                durationIso = "P14D",
                featureGrants = new[] { new { featureCode = FeatureCode.CustomerCreditView, enabled = true } }
            });
        if (createTrial.IsSuccessStatusCode)
        {
            return (await createTrial.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        }

        var retryList = await _admin.GetAsync(
            $"/api/v1/platform/catalog/products/{ProductCode.PinoyBusinessPos}/trials");
        retryList.EnsureSuccessStatusCode();
        var retryItems = (await retryList.Content.ReadFromJsonAsync<JsonElement>())!.EnumerateArray().ToList();
        if (retryItems.Count > 0)
        {
            return retryItems[0].GetProperty("id").GetGuid();
        }

        createTrial.EnsureSuccessStatusCode();
        return (await createTrial.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private async Task EnsureMvpCatalogAsync()
    {
        // The step-up tests are filter-runnable; we seed MVP POS plans and trials here for isolation.
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PlatformDatabase"] = fixture.ConnectionString
            })
            .Build();

        var services = new ServiceCollection();
        services.AddPlatformPersistence(configuration);
        services.AddLogging();
        services.AddScoped<CreateProduct>();
        services.AddScoped<CreateFeatureDefinition>();
        services.AddScoped<CreatePlan>();
        services.AddScoped<ActivatePlan>();
        services.AddScoped<UpdatePlanCommercialPackage>();
        services.AddScoped<CreateDraftPlanVersion>();
        services.AddScoped<PublishExistingPlanVersion>();
        services.AddScoped<CreateTrialDefinition>();
        services.AddScoped<RetirePlan>();
        services.AddScoped<EnsureMvpPosPlans>();

        await using var provider = services.BuildServiceProvider();
        var createProduct = provider.GetRequiredService<CreateProduct>();
        var productResult = await createProduct.ExecuteAsync(ProductCode.PinoyBusinessPos, "Pinoy Business POS");
        if (!productResult.IsSuccess && productResult.ErrorCode != ApplicationErrorCodes.DuplicateProductCode)
        {
            throw new InvalidOperationException(
                $"POS product seed failed: {productResult.ErrorCode} {productResult.ErrorMessage}");
        }

        await provider.GetRequiredService<EnsureMvpPosPlans>().ExecuteAsync();
    }

    [Fact]
    public async Task Correct_password_step_up_allows_critical_membership_suspend()
    {
        var (organizationId, password, token, _) = await SeedOwnerSessionAsync("stepok");
        var membershipId = await SeedStaffMembershipAsync(organizationId);

        var stepUpToken = await IssueStepUpAsync(
            organizationId,
            token,
            password,
            GovernanceCriticalActionCodes.MembershipSuspend,
            GovernanceStepUpTargetTypes.OrganizationMembership,
            membershipId);

        using var suspend = Authed(
            HttpMethod.Post,
            $"/api/v1/platform/memberships/{membershipId:D}/suspend",
            token,
            new { reason = "integration-test suspend", stepUpToken });
        var response = await _client.SendAsync(suspend);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
            var errorCode = problem.ValueKind == JsonValueKind.Object && problem.TryGetProperty("errorCode", out var ec)
                ? ec.GetString()
                : null;
            var errorMessage = problem.ValueKind == JsonValueKind.Object && problem.TryGetProperty("errorMessage", out var em)
                ? em.GetString()
                : null;

            throw new Xunit.Sdk.XunitException(
                $"Expected OK but got {response.StatusCode}. errorCode={errorCode}; errorMessage={errorMessage}");
        }
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Wrong_password_denies_step_up_issue()
    {
        var (organizationId, _, token, primaryBranchId) = await SeedOwnerSessionAsync("stepbad");

        using var request = Authed(
            HttpMethod.Post,
            $"/api/v1/platform/organizations/{organizationId:D}/governance/step-up",
            token,
            new
            {
                actionCode = GovernanceCriticalActionCodes.BranchSuspend,
                targetType = GovernanceStepUpTargetTypes.OrganizationBranch,
                targetId = primaryBranchId,
                currentPassword = "Not-The-Password-123!"
            });
        var response = await _client.SendAsync(request);
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Critical_action_without_step_up_is_denied()
    {
        var (organizationId, _, token, _) = await SeedOwnerSessionAsync("stepreq");
        var membershipId = await SeedStaffMembershipAsync(organizationId);

        using var suspend = Authed(
            HttpMethod.Post,
            $"/api/v1/platform/memberships/{membershipId:D}/suspend",
            token,
            new { reason = "missing step-up token", stepUpToken = (string?)null });
        var response = await _client.SendAsync(suspend);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Consumed_step_up_token_cannot_be_replayed()
    {
        var (organizationId, password, token, _) = await SeedOwnerSessionAsync("stepreplay");
        var membershipId = await SeedStaffMembershipAsync(organizationId);

        var stepUpToken = await IssueStepUpAsync(
            organizationId,
            token,
            password,
            GovernanceCriticalActionCodes.MembershipRevoke,
            GovernanceStepUpTargetTypes.OrganizationMembership,
            membershipId);

        using var first = Authed(
            HttpMethod.Post,
            $"/api/v1/platform/memberships/{membershipId:D}/revoke",
            token,
            new { reason = "first revoke consume", stepUpToken });
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(first)).StatusCode);

        using var replay = Authed(
            HttpMethod.Post,
            $"/api/v1/platform/memberships/{membershipId:D}/revoke",
            token,
            new { reason = "replay should fail", stepUpToken });
        var replayResponse = await _client.SendAsync(replay);
        Assert.Equal(HttpStatusCode.Conflict, replayResponse.StatusCode);
    }

    [Fact]
    public async Task Step_up_token_scoped_to_different_target_is_denied()
    {
        var (organizationId, password, token, _) = await SeedOwnerSessionAsync("steptarget");
        var membershipA = await SeedStaffMembershipAsync(organizationId);
        var membershipB = await SeedStaffMembershipAsync(organizationId);

        var stepUpToken = await IssueStepUpAsync(
            organizationId,
            token,
            password,
            GovernanceCriticalActionCodes.MembershipSuspend,
            GovernanceStepUpTargetTypes.OrganizationMembership,
            membershipA);

        using var wrongTarget = Authed(
            HttpMethod.Post,
            $"/api/v1/platform/memberships/{membershipB:D}/suspend",
            token,
            new { reason = "wrong target membership", stepUpToken });
        var response = await _client.SendAsync(wrongTarget);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Primary_branch_cannot_be_suspended()
    {
        var (organizationId, password, token, primaryBranchId) = await SeedOwnerSessionAsync("stepprimary");

        var stepUpToken = await IssueStepUpAsync(
            organizationId,
            token,
            password,
            GovernanceCriticalActionCodes.BranchSuspend,
            GovernanceStepUpTargetTypes.OrganizationBranch,
            primaryBranchId);

        using var suspend = Authed(
            HttpMethod.Post,
            $"/api/v1/platform/organizations/{organizationId:D}/branches/{primaryBranchId:D}/suspend",
            token,
            new { reason = "attempt primary suspend", stepUpToken });
        var response = await _client.SendAsync(suspend);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Step_up_success_audit_omits_password()
    {
        var (organizationId, password, token, primaryBranchId) = await SeedOwnerSessionAsync("stepaudit");

        _ = await IssueStepUpAsync(
            organizationId,
            token,
            password,
            GovernanceCriticalActionCodes.BranchSuspend,
            GovernanceStepUpTargetTypes.OrganizationBranch,
            primaryBranchId);

        using var auditRequest = Authed(
            HttpMethod.Get,
            $"/api/v1/platform/organizations/{organizationId:D}/audit?action={PlatformAuditActions.GovernanceStepUpSucceeded}&outcome=Succeeded",
            token);
        var auditResponse = await _client.SendAsync(auditRequest);
        auditResponse.EnsureSuccessStatusCode();
        var page = await auditResponse.Content.ReadFromJsonAsync<JsonElement>();
        var summary = page!.GetProperty("items").EnumerateArray().First().GetProperty("summary").GetString();
        Assert.NotNull(summary);
        Assert.DoesNotContain(password, summary, StringComparison.Ordinal);
        Assert.Contains("PasswordStepUp", summary, StringComparison.Ordinal);
    }
}
