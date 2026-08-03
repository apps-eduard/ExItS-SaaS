using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ExItS.Platform.IntegrationTests;

/// <summary>
/// P16-WP10 closeout: cross-account-class / cross-user / cross-org isolation,
/// invitation and migration abuse, Support Session residual, audit + privacy evidence.
/// </summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class ApiPhase16CloseoutSecurityTests(PostgreSqlFixture fixture) : IAsyncLifetime
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

    private async Task<(Guid UserId, string Username, string Password, string Email, string Token)> SeedPersonalAsync(
        string prefix)
    {
        var (userId, email, password) = await PlatformIntegrationTestUsers.RegisterPersonalWithPasswordAsync(_client, prefix);
        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = email, password });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionToken").GetString()!;
        return (userId, email, password, email, token);
    }

    private async Task<string> PromoteToPlatformAdminAsync(Guid userId, string username, string password)
    {
        (await _admin.PostAsJsonAsync(
            "/api/v1/platform/authorization/assignments",
            new
            {
                platformUserId = userId,
                role = nameof(PlatformSystemRole.PlatformAdministrator)
            })).EnsureSuccessStatusCode();
        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = username, password });
        login.EnsureSuccessStatusCode();
        return (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionToken").GetString()!;
    }

    [Fact]
    public async Task Cross_account_class_matrix_denies_foreign_surfaces()
    {
        var (personalId, personalUser, personalPass, _, personalToken) = await SeedPersonalAsync("cap");
        _ = personalId;
        var (platformId, platformUser, platformPass, _, _) = await SeedPersonalAsync("cpl");
        var platformToken = await PromoteToPlatformAdminAsync(platformId, platformUser, platformPass);

        var (orgUserId, orgUser, orgPass, _, _) = await SeedPersonalAsync("cog");
        var org = await _admin.PostAsJsonAsync(
            "/api/v1/platform/organizations",
            new { displayName = "Closeout Org", slug = Unique("corg") });
        org.EnsureSuccessStatusCode();
        var organizationId = (await org.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await _admin.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/members",
            new { userId = orgUserId, role = "OrganizationOwner" })).EnsureSuccessStatusCode();
        var orgLogin = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = orgUser, password = orgPass });
        orgLogin.EnsureSuccessStatusCode();
        var orgToken = (await orgLogin.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionToken").GetString()!;

        async Task AssertDenied(string token, string url)
        {
            using var request = Authed(HttpMethod.Get, url, token);
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(ApplicationErrorCodes.AccountScopeDenied, body.GetProperty("errorCode").GetString());
        }

        await AssertDenied(personalToken, "/api/v1/platform/users?page=1&pageSize=5");
        await AssertDenied(platformToken, "/api/v1/personal/me");
        await AssertDenied(orgToken, "/api/v1/personal/me");
        await AssertDenied(orgToken, "/api/v1/platform/users?page=1&pageSize=5");

        using var personalOk = Authed(HttpMethod.Get, "/api/v1/personal/me", personalToken);
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(personalOk)).StatusCode);
        using var platformOk = Authed(HttpMethod.Get, "/api/v1/platform/users?page=1&pageSize=5", platformToken);
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(platformOk)).StatusCode);
        _ = personalUser;
        _ = personalPass;
    }

    [Fact]
    public async Task Cross_user_cannot_list_or_mutate_foreign_personal_utang()
    {
        var (ownerId, _, _, _, ownerToken) = await SeedPersonalAsync("own");
        var (_, _, _, _, strangerToken) = await SeedPersonalAsync("str");

        using var contactRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/contacts",
            ownerToken,
            new { displayName = "Private Friend", email = "private-friend@example.com" });
        var contactId = (await (await _client.SendAsync(contactRequest)).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        using var relationshipRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/relationships",
            ownerToken,
            new
            {
                creditorUserIdentityId = ownerId,
                debtorContactId = contactId,
                currencyCode = "PHP",
                initialLoanAmount = 150m
            });
        var relationshipId = (await (await _client.SendAsync(relationshipRequest)).Content
            .ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        using var listContacts = Authed(HttpMethod.Get, "/api/v1/personal/utang/contacts", strangerToken);
        var contacts = await _client.SendAsync(listContacts);
        Assert.Equal(HttpStatusCode.OK, contacts.StatusCode);
        var contactItems = await contacts.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, contactItems.GetArrayLength());

        using var getRelationship = Authed(
            HttpMethod.Get,
            $"/api/v1/personal/utang/relationships/{relationshipId}",
            strangerToken);
        var denied = await _client.SendAsync(getRelationship);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal(
            ApplicationErrorCodes.PersonalUtangUnauthorized,
            (await denied.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errorCode").GetString());

        using var mutate = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/entries",
            strangerToken,
            new { entryType = "Payment", amount = 10m, expectedVersion = 1 });
        var mutateDenied = await _client.SendAsync(mutate);
        Assert.Equal(HttpStatusCode.Forbidden, mutateDenied.StatusCode);
    }

    [Fact]
    public async Task Cross_organization_cannot_list_foreign_customers()
    {
        async Task<(string Token, Guid OrgId)> SeedOwnerOrgAsync(string prefix)
        {
            var (userId, username, password, _, _) = await SeedPersonalAsync(prefix);
            var org = await _admin.PostAsJsonAsync(
                "/api/v1/platform/organizations",
                new { displayName = $"{prefix} Org", slug = Unique(prefix + "o") });
            org.EnsureSuccessStatusCode();
            var organizationId = (await org.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
            (await _admin.PostAsJsonAsync(
                $"/api/v1/platform/organizations/{organizationId}/members",
                new { userId, role = "OrganizationOwner", reason = "integration-test-link" })).EnsureSuccessStatusCode();
            var login = await _client.PostAsJsonAsync(
                "/api/v1/platform/auth/login",
                new { usernameOrEmail = username, password });
            login.EnsureSuccessStatusCode();
            var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionToken").GetString()!;
            return (token, organizationId);
        }

        var (tokenA, orgA) = await SeedOwnerOrgAsync("oa");
        var (tokenB, orgB) = await SeedOwnerOrgAsync("ob");

        using var create = Authed(
            HttpMethod.Post,
            $"/api/v1/organizations/{orgA}/customers",
            tokenA,
            new { displayName = "Only Org A", email = "orga-customer@example.com" });
        Assert.Equal(HttpStatusCode.Created, (await _client.SendAsync(create)).StatusCode);

        using var foreignList = Authed(
            HttpMethod.Get,
            $"/api/v1/organizations/{orgA}/customers?page=1&pageSize=20",
            tokenB);
        var denied = await _client.SendAsync(foreignList);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);

        using var ownList = Authed(
            HttpMethod.Get,
            $"/api/v1/organizations/{orgB}/customers?page=1&pageSize=20",
            tokenB);
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(ownList)).StatusCode);
    }

    [Fact]
    public async Task Wrong_invitation_type_accept_is_anti_enumeration_safe()
    {
        var (ownerId, ownerUser, ownerPass, _, _) = await SeedPersonalAsync("wio");
        var org = await _admin.PostAsJsonAsync(
            "/api/v1/platform/organizations",
            new { displayName = "Invite Org", slug = Unique("wio") });
        org.EnsureSuccessStatusCode();
        var organizationId = (await org.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await _admin.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/members",
            new { userId = ownerId, role = "OrganizationOwner" })).EnsureSuccessStatusCode();
        var orgLogin = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = ownerUser, password = ownerPass });
        orgLogin.EnsureSuccessStatusCode();
        var orgToken = (await orgLogin.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionToken").GetString()!;

        var (_, _, _, inviteeEmail, inviteeToken) = await SeedPersonalAsync("acc");
        using var staffInvite = Authed(
            HttpMethod.Post,
            $"/api/v1/organizations/{organizationId}/staff-invitations",
            orgToken,
            new { email = inviteeEmail, role = "OrganizationMember" });
        var staffResponse = await _client.SendAsync(staffInvite);
        Assert.Equal(HttpStatusCode.Created, staffResponse.StatusCode);
        var staffBody = await staffResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(
            InvitationKinds.OrganizationStaffInvitation,
            staffBody.GetProperty("invitationType").GetString());
        var staffAcceptToken = staffBody.GetProperty("acceptToken").GetString();

        using var wrongPersonal = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/invitations/accept",
            inviteeToken,
            new { token = staffAcceptToken });
        var personalWrong = await _client.SendAsync(wrongPersonal);
        Assert.Equal(HttpStatusCode.NotFound, personalWrong.StatusCode);
        Assert.Equal(
            ApplicationErrorCodes.PersonalUtangInvitationNotFound,
            (await personalWrong.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errorCode").GetString());

        using var wrongCustomer = Authed(
            HttpMethod.Post,
            "/api/v1/organizations/customer-link-requests/accept",
            inviteeToken,
            new { token = staffAcceptToken });
        var customerWrong = await _client.SendAsync(wrongCustomer);
        Assert.Equal(HttpStatusCode.NotFound, customerWrong.StatusCode);
        Assert.Equal(
            ApplicationErrorCodes.CustomerLinkRequestNotFound,
            (await customerWrong.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errorCode").GetString());

        using var correctAccept = Authed(
            HttpMethod.Post,
            "/api/v1/platform/invitations/accept",
            inviteeToken,
            new { token = staffAcceptToken });
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(correctAccept)).StatusCode);
    }

    [Fact]
    public async Task Invitation_duplicate_pending_and_immediate_resend_are_abuse_controlled()
    {
        var (lenderId, _, _, _, lenderToken) = await SeedPersonalAsync("rl");
        var (_, _, _, borrowerEmail, _) = await SeedPersonalAsync("rb");

        using var contactRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/contacts",
            lenderToken,
            new { displayName = "Borrower", email = borrowerEmail });
        var contactId = (await (await _client.SendAsync(contactRequest)).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        using var relationshipRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/relationships",
            lenderToken,
            new
            {
                creditorUserIdentityId = lenderId,
                debtorContactId = contactId,
                currencyCode = "PHP",
                initialLoanAmount = 40m
            });
        var relationshipId = (await (await _client.SendAsync(relationshipRequest)).Content
            .ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        using var invite1 = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/invitations",
            lenderToken,
            new { inviteeContactId = contactId });
        var first = await _client.SendAsync(invite1);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var invitationId = (await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        using var invite2 = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/invitations",
            lenderToken,
            new { inviteeContactId = contactId });
        var duplicate = await _client.SendAsync(invite2);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(
            ApplicationErrorCodes.PersonalUtangInvitationConflict,
            (await duplicate.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errorCode").GetString());

        using var resend = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/invitations/{invitationId}/resend",
            lenderToken);
        var resendResponse = await _client.SendAsync(resend);
        Assert.Equal(HttpStatusCode.TooManyRequests, resendResponse.StatusCode);
        Assert.Equal(
            ApplicationErrorCodes.PersonalUtangInvitationRateLimited,
            (await resendResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Contact_create_does_not_silently_link_matching_email_user()
    {
        var (_, _, _, existingEmail, _) = await SeedPersonalAsync("exu");
        var (_, _, _, _, ownerToken) = await SeedPersonalAsync("ownc");

        using var contactRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/contacts",
            ownerToken,
            new { displayName = "Looks Like Existing", email = existingEmail });
        var response = await _client.SendAsync(contactRequest);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(
            !body.TryGetProperty("linkedUserIdentityId", out var linked)
            || linked.ValueKind is JsonValueKind.Null);
        Assert.Equal("Active", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Notification_preview_redacts_amounts_from_custom_message()
    {
        var (lenderId, _, _, _, lenderToken) = await SeedPersonalAsync("np");
        var (_, _, _, borrowerEmail, borrowerToken) = await SeedPersonalAsync("nb");

        using var contactRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/contacts",
            lenderToken,
            new { displayName = "Preview Friend", email = borrowerEmail });
        var contactId = (await (await _client.SendAsync(contactRequest)).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        using var relationshipRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/relationships",
            lenderToken,
            new
            {
                creditorUserIdentityId = lenderId,
                debtorContactId = contactId,
                currencyCode = "PHP",
                initialLoanAmount = 5555m
            });
        var relationshipId = (await (await _client.SendAsync(relationshipRequest)).Content
            .ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        using var inviteRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/invitations",
            lenderToken,
            new { inviteeContactId = contactId });
        var acceptToken = (await (await _client.SendAsync(inviteRequest)).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("acceptToken").GetString();
        using var accept = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/invitations/accept",
            borrowerToken,
            new { token = acceptToken });
        (await _client.SendAsync(accept)).EnsureSuccessStatusCode();

        using var createReminder = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/reminders",
            lenderToken,
            new
            {
                scheduleType = "OneTime",
                scheduledForUtc = DateTimeOffset.UtcNow,
                message = "Please settle ₱5,555.00 soon"
            });
        var reminderId = (await (await _client.SendAsync(createReminder)).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();
        using var deliver = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/reminders/{reminderId}/deliver",
            lenderToken);
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(deliver)).StatusCode);

        using var notificationsRequest = Authed(HttpMethod.Get, "/api/v1/personal/notifications", borrowerToken);
        var notifications = await (await _client.SendAsync(notificationsRequest)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(notifications.GetArrayLength() >= 1);
        var preview = notifications[0].GetProperty("preview").GetString()!;
        Assert.DoesNotContain("5555", preview, StringComparison.Ordinal);
        Assert.DoesNotContain("5,555", preview, StringComparison.Ordinal);
        Assert.DoesNotContain("₱", preview, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Key_phase16_actions_emit_audit_records()
    {
        var (userId, username, password, _, personalToken) = await SeedPersonalAsync("aud");
        using var start = Authed(
            HttpMethod.Post,
            "/api/v1/personal/start-business",
            personalToken,
            new
            {
                displayName = "Audit Biz",
                slug = Unique("abiz"),
                activatePosEntitlement = true,
                activateProductAccess = true,
                assignPosOwnerRole = true
            });
        var startResponse = await _client.SendAsync(start);
        Assert.Equal(HttpStatusCode.Created, startResponse.StatusCode);

        var platformToken = await PromoteToPlatformAdminAsync(userId, username, password);
        // After platform promote, session is Platform — use admin operator client for audit reads.
        var audit = await _admin.GetAsync(
            $"/api/v1/platform/audit?actor={Uri.EscapeDataString($"platform-user:{userId:D}")}&action=platform.business_upgrade.completed&outcome=Succeeded&page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, audit.StatusCode);
        var body = await audit.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("totalCount").GetInt32() >= 1
            || body.GetProperty("items").GetArrayLength() >= 1);

        var roleAudit = await _admin.GetAsync(
            $"/api/v1/platform/audit?actor={Uri.EscapeDataString($"platform-user:{userId:D}")}&action=platform.product_local_role.granted&outcome=Succeeded&page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, roleAudit.StatusCode);
        var roleBody = await roleAudit.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(
            roleBody.GetProperty("totalCount").GetInt32() >= 1
            || roleBody.GetProperty("items").GetArrayLength() >= 1);

        _ = platformToken;
    }

    [Fact]
    public async Task Support_session_routes_are_unavailable()
    {
        var (userId, username, password, _, _) = await SeedPersonalAsync("sup");
        var platformToken = await PromoteToPlatformAdminAsync(userId, username, password);

        foreach (var path in new[]
                 {
                     "/api/v1/platform/support-sessions",
                     "/api/v1/platform/support-sessions/start",
                     "/api/v1/support-sessions",
                     "/api/v1/platform/support/sessions"
                 })
        {
            using var get = Authed(HttpMethod.Get, path, platformToken);
            var getResponse = await _client.SendAsync(get);
            // Residual: Support Session (ADR-018) is not implemented — callers must not succeed.
            Assert.True(
                getResponse.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden or HttpStatusCode.MethodNotAllowed,
                $"Expected unavailable status for GET {path}, got {getResponse.StatusCode}");

            using var post = Authed(HttpMethod.Post, path, platformToken, new { organizationId = Guid.NewGuid() });
            var postResponse = await _client.SendAsync(post);
            Assert.True(
                postResponse.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden or HttpStatusCode.MethodNotAllowed,
                $"Expected unavailable status for POST {path}, got {postResponse.StatusCode}");
        }
    }

    [Fact]
    public async Task Migration_replay_with_same_idempotency_key_is_safe()
    {
        var (personalToken, userId, _, _) = await SeedPersonalMigrationUserAsync("mr");

        using var contactRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/contacts",
            personalToken,
            new { displayName = "Mig Friend", phone = "+639170000077" });
        var contactId = (await (await _client.SendAsync(contactRequest)).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();
        using var relRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/relationships",
            personalToken,
            new
            {
                creditorUserIdentityId = userId,
                debtorContactId = contactId,
                currencyCode = "PHP",
                initialLoanAmount = 80m
            });
        var relationshipId = (await (await _client.SendAsync(relRequest)).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        using var start = Authed(
            HttpMethod.Post,
            "/api/v1/personal/start-business",
            personalToken,
            new { displayName = "Mig Org", slug = Unique("morg") });
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
                sourceDisposition = "Retain",
                selections = new[] { new { relationshipId, contactId } }
            });
        var previewBody = await (await _client.SendAsync(preview)).Content.ReadFromJsonAsync<JsonElement>();
        var idempotencyKey = Unique("idk");
        var executePayload = new
        {
            batchId = previewBody.GetProperty("batchId").GetGuid(),
            confirmationToken = previewBody.GetProperty("confirmationToken").GetGuid(),
            idempotencyKey
        };

        using var execute1 = Authed(
            HttpMethod.Post,
            $"/api/v1/organizations/{orgId}/utang-migrations",
            orgToken,
            executePayload);
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(execute1)).StatusCode);

        using var execute2 = Authed(
            HttpMethod.Post,
            $"/api/v1/organizations/{orgId}/utang-migrations",
            orgToken,
            executePayload);
        var replay = await _client.SendAsync(execute2);
        Assert.True(
            replay.StatusCode is HttpStatusCode.OK or HttpStatusCode.Conflict,
            $"Unexpected replay status {replay.StatusCode}");
    }

    private async Task<(string Token, Guid UserId, string Username, string Password)> SeedPersonalMigrationUserAsync(
        string prefix)
    {
        var (userId, username, password, _, token) = await SeedPersonalAsync(prefix);
        return (token, userId, username, password);
    }
}
