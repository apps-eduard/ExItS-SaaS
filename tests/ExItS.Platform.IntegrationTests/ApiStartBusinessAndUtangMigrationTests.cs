using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Application.Common;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApiStartBusinessAndUtangMigrationTests(PostgreSqlFixture fixture) : IAsyncLifetime
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

    private static string Unique(string prefix) =>
        $"{prefix}{Guid.NewGuid():N}"[..Math.Min(20, prefix.Length + 32)].ToLowerInvariant();

    private async Task<(string Token, Guid UserId, string Username, string Password)> SeedPersonalUserAsync(string prefix)
    {
        var username = Unique(prefix);
        var password = "Correct-Horse-9!";
        var create = await _admin.PostAsJsonAsync(
            "/api/v1/platform/users",
            new { username, displayName = "Start Biz User", email = $"{username}@example.com" });
        create.EnsureSuccessStatusCode();
        var userId = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await _admin.PutAsJsonAsync(
            $"/api/v1/platform/users/{userId}/credentials/password",
            new { password })).EnsureSuccessStatusCode();
        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = username, password });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionToken").GetString()!;
        return (token, userId, username, password);
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
    public async Task Start_business_grants_owner_entitlement_and_pos_role_separately()
    {
        var (token, _, _, _) = await SeedPersonalUserAsync("sb");
        var slug = Unique("biz");
        using var request = Authed(
            HttpMethod.Post,
            "/api/v1/personal/start-business",
            token,
            new
            {
                displayName = "Ana Sari-Sari",
                slug,
                activatePosEntitlement = true,
                activateProductAccess = true,
                assignPosOwnerRole = true
            });
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("organizationOwnerGranted").GetBoolean());
        Assert.True(body.GetProperty("posEntitlementActivated").GetBoolean());
        Assert.True(body.GetProperty("posOwnerRoleGranted").GetBoolean());
        Assert.Equal("Owner", body.GetProperty("productLocalRoleCode").GetString());
        Assert.Equal("Organization", body.GetProperty("accountClass").GetString());
        Assert.NotEqual(Guid.Empty, body.GetProperty("organizationId").GetGuid());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("sessionToken").GetString()));
    }

    [Fact]
    public async Task Migration_preview_execute_idempotent_and_duplicate_protected()
    {
        var (personalToken, userId, _, _) = await SeedPersonalUserAsync("mig");

        using var contactRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/contacts",
            personalToken,
            new { displayName = "Customer Ana", phone = "+639170000099" });
        var contactResponse = await _client.SendAsync(contactRequest);
        Assert.Equal(HttpStatusCode.Created, contactResponse.StatusCode);
        var contactId = (await contactResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        using var relRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/relationships",
            personalToken,
            new
            {
                creditorUserIdentityId = userId,
                debtorContactId = contactId,
                currencyCode = "PHP",
                initialLoanAmount = 750m
            });
        var relResponse = await _client.SendAsync(relRequest);
        Assert.Equal(HttpStatusCode.Created, relResponse.StatusCode);
        var relationshipId = (await relResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        using var startRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/start-business",
            personalToken,
            new { displayName = "Migrated Biz", slug = Unique("mb") });
        var startResponse = await _client.SendAsync(startRequest);
        Assert.Equal(HttpStatusCode.Created, startResponse.StatusCode);
        var start = await startResponse.Content.ReadFromJsonAsync<JsonElement>();
        var orgId = start.GetProperty("organizationId").GetGuid();
        var orgToken = start.GetProperty("sessionToken").GetString()!;
        var orgProfileId = start.GetProperty("organizationAccountProfileId").GetGuid();

        // Switch back to Personal profile; Personal session must not mutate org credit/customers.
        using var profilesRequest = Authed(HttpMethod.Get, "/api/v1/platform/auth/account-profiles", orgToken);
        var profilesResponse = await _client.SendAsync(profilesRequest);
        profilesResponse.EnsureSuccessStatusCode();
        var profiles = await profilesResponse.Content.ReadFromJsonAsync<JsonElement>();
        Guid personalProfileId = Guid.Empty;
        foreach (var profile in profiles.EnumerateArray())
        {
            if (profile.GetProperty("accountClass").GetString() == "Personal")
            {
                personalProfileId = profile.GetProperty("id").GetGuid();
                break;
            }
        }

        Assert.NotEqual(Guid.Empty, personalProfileId);
        using var selectPersonal = Authed(
            HttpMethod.Post,
            "/api/v1/platform/auth/account-profiles/select",
            orgToken,
            new { accountProfileId = personalProfileId });
        var selectPersonalResponse = await _client.SendAsync(selectPersonal);
        selectPersonalResponse.EnsureSuccessStatusCode();
        var personalAgainToken = (await selectPersonalResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("sessionToken").GetString()!;

        using var personalCustomerDenied = Authed(
            HttpMethod.Post,
            $"/api/v1/organizations/{orgId}/customers",
            personalAgainToken,
            new { displayName = "Should Fail" });
        var denied = await _client.SendAsync(personalCustomerDenied);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        var deniedBody = await denied.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ApplicationErrorCodes.AccountScopeDenied, deniedBody.GetProperty("errorCode").GetString());

        // Restore Organization session for migration steps.
        using var selectOrg = Authed(
            HttpMethod.Post,
            "/api/v1/platform/auth/account-profiles/select",
            personalAgainToken,
            new { accountProfileId = orgProfileId });
        var selectOrgResponse = await _client.SendAsync(selectOrg);
        selectOrgResponse.EnsureSuccessStatusCode();
        orgToken = (await selectOrgResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("sessionToken").GetString()!;

        using var emptyPreview = Authed(
            HttpMethod.Post,
            $"/api/v1/organizations/{orgId}/utang-migrations/preview",
            orgToken,
            new
            {
                includeContact = true,
                includeOpeningBalance = true,
                sourceDisposition = "Archive",
                selections = Array.Empty<object>()
            });
        var emptyPreviewResponse = await _client.SendAsync(emptyPreview);
        Assert.Equal(HttpStatusCode.BadRequest, emptyPreviewResponse.StatusCode);

        using var previewRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/organizations/{orgId}/utang-migrations/preview",
            orgToken,
            new
            {
                includeContact = true,
                includeOpeningBalance = true,
                includeDueDatesAndNotes = true,
                sourceDisposition = "Archive",
                linkedParticipantConsentAcknowledged = false,
                selections = new[] { new { relationshipId, contactId } }
            });
        var previewResponse = await _client.SendAsync(previewRequest);
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
        var preview = await previewResponse.Content.ReadFromJsonAsync<JsonElement>();
        var batchId = preview.GetProperty("batchId").GetGuid();
        var confirmation = preview.GetProperty("confirmationToken").GetGuid();
        Assert.Equal(1, preview.GetProperty("migratableItemCount").GetInt32());

        var idempotencyKey = Unique("idk");
        using var executeRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/organizations/{orgId}/utang-migrations",
            orgToken,
            new { batchId, confirmationToken = confirmation, idempotencyKey });
        var executeResponse = await _client.SendAsync(executeRequest);
        Assert.Equal(HttpStatusCode.OK, executeResponse.StatusCode);
        var executed = await executeResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(executed.GetProperty("idempotentReplay").GetBoolean());
        Assert.Equal("Executed", executed.GetProperty("status").GetString());
        Assert.NotEqual(Guid.Empty, executed.GetProperty("items")[0].GetProperty("businessCustomerId").GetGuid());
        Assert.Equal(750m, executed.GetProperty("items")[0].GetProperty("openingBalanceAmount").GetDecimal());

        using var replayRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/organizations/{orgId}/utang-migrations",
            orgToken,
            new { batchId, confirmationToken = confirmation, idempotencyKey });
        var replayResponse = await _client.SendAsync(replayRequest);
        Assert.Equal(HttpStatusCode.OK, replayResponse.StatusCode);
        var replay = await replayResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(replay.GetProperty("idempotentReplay").GetBoolean());

        using var previewAgain = Authed(
            HttpMethod.Post,
            $"/api/v1/organizations/{orgId}/utang-migrations/preview",
            orgToken,
            new
            {
                includeContact = true,
                includeOpeningBalance = true,
                sourceDisposition = "Archive",
                selections = new[] { new { relationshipId, contactId } }
            });
        var previewAgainResponse = await _client.SendAsync(previewAgain);
        Assert.Equal(HttpStatusCode.OK, previewAgainResponse.StatusCode);
        var blockedPreview = await previewAgainResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, blockedPreview.GetProperty("blockedItemCount").GetInt32());

        using var executeBlocked = Authed(
            HttpMethod.Post,
            $"/api/v1/organizations/{orgId}/utang-migrations",
            orgToken,
            new
            {
                batchId = blockedPreview.GetProperty("batchId").GetGuid(),
                confirmationToken = blockedPreview.GetProperty("confirmationToken").GetGuid(),
                idempotencyKey = Unique("idk2")
            });
        var executeBlockedResponse = await _client.SendAsync(executeBlocked);
        Assert.Equal(HttpStatusCode.Conflict, executeBlockedResponse.StatusCode);
        var blockedBody = await executeBlockedResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ApplicationErrorCodes.UtangMigrationAlreadyMigrated, blockedBody.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Migration_rejects_wrong_organization_destination()
    {
        var (tokenA, userId, _, _) = await SeedPersonalUserAsync("wa");
        using var contactRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/contacts",
            tokenA,
            new { displayName = "Friend", phone = "+639170000088" });
        var contactId = (await (await _client.SendAsync(contactRequest)).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();
        using var relRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/relationships",
            tokenA,
            new
            {
                creditorUserIdentityId = userId,
                debtorContactId = contactId,
                currencyCode = "PHP",
                initialLoanAmount = 100m
            });
        var relationshipId = (await (await _client.SendAsync(relRequest)).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        using var startA = Authed(
            HttpMethod.Post,
            "/api/v1/personal/start-business",
            tokenA,
            new { displayName = "Org A", slug = Unique("oa") });
        var startABody = await (await _client.SendAsync(startA)).Content.ReadFromJsonAsync<JsonElement>();
        var orgA = startABody.GetProperty("organizationId").GetGuid();
        var tokenOrgA = startABody.GetProperty("sessionToken").GetString()!;

        var (tokenB, _, _, _) = await SeedPersonalUserAsync("wb");
        using var startB = Authed(
            HttpMethod.Post,
            "/api/v1/personal/start-business",
            tokenB,
            new { displayName = "Org B", slug = Unique("ob") });
        var startBBody = await (await _client.SendAsync(startB)).Content.ReadFromJsonAsync<JsonElement>();
        var orgB = startBBody.GetProperty("organizationId").GetGuid();

        using var preview = Authed(
            HttpMethod.Post,
            $"/api/v1/organizations/{orgA}/utang-migrations/preview",
            tokenOrgA,
            new
            {
                includeContact = true,
                includeOpeningBalance = true,
                sourceDisposition = "Retain",
                selections = new[] { new { relationshipId, contactId } }
            });
        var previewBody = await (await _client.SendAsync(preview)).Content.ReadFromJsonAsync<JsonElement>();

        using var wrongOrgExecute = Authed(
            HttpMethod.Post,
            $"/api/v1/organizations/{orgB}/utang-migrations",
            tokenOrgA,
            new
            {
                batchId = previewBody.GetProperty("batchId").GetGuid(),
                confirmationToken = previewBody.GetProperty("confirmationToken").GetGuid(),
                idempotencyKey = Unique("wrong")
            });
        var wrong = await _client.SendAsync(wrongOrgExecute);
        Assert.Equal(HttpStatusCode.Forbidden, wrong.StatusCode);
    }

    [Fact]
    public async Task Linked_participant_without_consent_is_blocked()
    {
        var (lenderToken, lenderId, _, _) = await SeedPersonalUserAsync("ll");
        var (borrowerToken, borrowerId, borrowerUser, borrowerPassword) = await SeedPersonalUserAsync("lb");

        using var contactRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/contacts",
            lenderToken,
            new { displayName = "Linked Friend", email = $"{borrowerUser}@example.com" });
        var contactId = (await (await _client.SendAsync(contactRequest)).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        using var relRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/relationships",
            lenderToken,
            new
            {
                creditorUserIdentityId = lenderId,
                debtorContactId = contactId,
                currencyCode = "PHP",
                initialLoanAmount = 200m
            });
        var relationshipId = (await (await _client.SendAsync(relRequest)).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        // Link via invitation accept path if available; otherwise create user-user relationship.
        using var userRel = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/relationships",
            lenderToken,
            new
            {
                creditorUserIdentityId = lenderId,
                debtorUserIdentityId = borrowerId,
                currencyCode = "PHP",
                initialLoanAmount = 50m
            });
        var userRelResponse = await _client.SendAsync(userRel);
        Assert.Equal(HttpStatusCode.Created, userRelResponse.StatusCode);
        var linkedRelationshipId = (await userRelResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        using var start = Authed(
            HttpMethod.Post,
            "/api/v1/personal/start-business",
            lenderToken,
            new { displayName = "Consent Biz", slug = Unique("cb") });
        var startBody = await (await _client.SendAsync(start)).Content.ReadFromJsonAsync<JsonElement>();
        var orgId = startBody.GetProperty("organizationId").GetGuid();
        var orgToken = startBody.GetProperty("sessionToken").GetString()!;

        using var preview = Authed(
            HttpMethod.Post,
            $"/api/v1/organizations/{orgId}/utang-migrations/preview",
            orgToken,
            new
            {
                includeContact = true,
                includeOpeningBalance = true,
                sourceDisposition = "Archive",
                linkedParticipantConsentAcknowledged = false,
                selections = new[] { new { relationshipId = linkedRelationshipId } }
            });
        var previewResponse = await _client.SendAsync(preview);
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
        var previewBody = await previewResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, previewBody.GetProperty("blockedItemCount").GetInt32());
        Assert.Equal(0, previewBody.GetProperty("migratableItemCount").GetInt32());

        using var execute = Authed(
            HttpMethod.Post,
            $"/api/v1/organizations/{orgId}/utang-migrations",
            orgToken,
            new
            {
                batchId = previewBody.GetProperty("batchId").GetGuid(),
                confirmationToken = previewBody.GetProperty("confirmationToken").GetGuid(),
                idempotencyKey = Unique("consent")
            });
        var executeResponse = await _client.SendAsync(execute);
        Assert.Equal(HttpStatusCode.Forbidden, executeResponse.StatusCode);
        var body = await executeResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ApplicationErrorCodes.UtangMigrationConsentRequired, body.GetProperty("errorCode").GetString());

        _ = borrowerToken;
        _ = borrowerPassword;
        _ = relationshipId;
    }
}
