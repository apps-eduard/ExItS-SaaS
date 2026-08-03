using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Application.Common;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApiPersonalUtangInvitationTests(PostgreSqlFixture fixture) : IAsyncLifetime
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

    private async Task<(string Token, Guid UserId, string Email)> SeedPersonalUserAsync(string prefix)
    {
        var (userId, email, password) = await PlatformIntegrationTestUsers.RegisterPersonalWithPasswordAsync(_client, prefix);
        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = email, password });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionToken").GetString()!;
        return (token, userId, email);
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
    public async Task Invitation_accept_links_participant_without_org_or_product_role()
    {
        var (lenderToken, lenderId, _) = await SeedPersonalUserAsync("lend");
        var (borrowerToken, borrowerId, borrowerEmail) = await SeedPersonalUserAsync("borr");

        using var contactRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/contacts",
            lenderToken,
            new { displayName = "Borrower Friend", email = borrowerEmail });
        var contactResponse = await _client.SendAsync(contactRequest);
        Assert.Equal(HttpStatusCode.Created, contactResponse.StatusCode);
        var contactId = (await contactResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        using var relationshipRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/relationships",
            lenderToken,
            new
            {
                creditorUserIdentityId = lenderId,
                debtorContactId = contactId,
                currencyCode = "PHP",
                initialLoanAmount = 250m
            });
        var relationshipResponse = await _client.SendAsync(relationshipRequest);
        Assert.Equal(HttpStatusCode.Created, relationshipResponse.StatusCode);
        var relationshipId = (await relationshipResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        using var inviteRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/invitations",
            lenderToken,
            new { inviteeContactId = contactId });
        var inviteResponse = await _client.SendAsync(inviteRequest);
        Assert.Equal(HttpStatusCode.Created, inviteResponse.StatusCode);
        var inviteBody = await inviteResponse.Content.ReadFromJsonAsync<JsonElement>();
        var acceptToken = inviteBody.GetProperty("acceptToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(acceptToken));
        Assert.Equal("Pending", inviteBody.GetProperty("status").GetString());

        using var badTokenRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/invitations/accept",
            borrowerToken,
            new { token = "not-a-real-token" });
        var badTokenResponse = await _client.SendAsync(badTokenRequest);
        Assert.Equal(HttpStatusCode.NotFound, badTokenResponse.StatusCode);
        var badBody = await badTokenResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(
            ApplicationErrorCodes.PersonalUtangInvitationNotFound,
            badBody.GetProperty("errorCode").GetString());

        using var acceptRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/invitations/accept",
            borrowerToken,
            new { token = acceptToken });
        var acceptResponse = await _client.SendAsync(acceptRequest);
        Assert.Equal(HttpStatusCode.OK, acceptResponse.StatusCode);
        var acceptBody = await acceptResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(borrowerId, acceptBody.GetProperty("linkedUserIdentityId").GetGuid());
        Assert.False(acceptBody.GetProperty("createdOrganizationMembership").GetBoolean());
        Assert.False(acceptBody.GetProperty("grantedProductRole").GetBoolean());

        using var sharedViewRequest = Authed(
            HttpMethod.Get,
            $"/api/v1/personal/utang/relationships/{relationshipId}",
            borrowerToken);
        var sharedViewResponse = await _client.SendAsync(sharedViewRequest);
        Assert.Equal(HttpStatusCode.OK, sharedViewResponse.StatusCode);
        var shared = await sharedViewResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(borrowerId, shared.GetProperty("debtorUserIdentityId").GetGuid());
        Assert.Equal(JsonValueKind.Null, shared.GetProperty("debtorContactId").ValueKind);
        Assert.Equal(250m, shared.GetProperty("currentBalance").GetDecimal());
    }

    [Fact]
    public async Task Reminder_delivery_is_rate_limited_and_minimizes_preview()
    {
        var (lenderToken, lenderId, _) = await SeedPersonalUserAsync("rlend");
        var (borrowerToken, borrowerId, borrowerEmail) = await SeedPersonalUserAsync("rborr");

        using var contactRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/contacts",
            lenderToken,
            new { displayName = "Rate Limit Friend", email = borrowerEmail });
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
                initialLoanAmount = 9999m
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
        using var acceptRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/invitations/accept",
            borrowerToken,
            new { token = acceptToken });
        (await _client.SendAsync(acceptRequest)).EnsureSuccessStatusCode();

        async Task<Guid> CreateAndDeliverAsync()
        {
            using var createReminder = Authed(
                HttpMethod.Post,
                $"/api/v1/personal/utang/relationships/{relationshipId}/reminders",
                lenderToken,
                new
                {
                    scheduleType = "OneTime",
                    scheduledForUtc = DateTimeOffset.UtcNow,
                    message = "Friendly reminder"
                });
            var createResponse = await _client.SendAsync(createReminder);
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            var reminderId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

            using var deliver = Authed(
                HttpMethod.Post,
                $"/api/v1/personal/utang/reminders/{reminderId}/deliver",
                lenderToken);
            var deliverResponse = await _client.SendAsync(deliver);
            return (deliverResponse.StatusCode, reminderId) switch
            {
                (HttpStatusCode.OK, var id) => id,
                _ => throw new InvalidOperationException($"Unexpected deliver status {deliverResponse.StatusCode}")
            };
        }

        var firstReminderId = await CreateAndDeliverAsync();

        using var notificationsRequest = Authed(HttpMethod.Get, "/api/v1/personal/notifications", borrowerToken);
        var notificationsResponse = await _client.SendAsync(notificationsRequest);
        Assert.Equal(HttpStatusCode.OK, notificationsResponse.StatusCode);
        var notifications = await notificationsResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(notifications.GetArrayLength() >= 1);
        var preview = notifications[0].GetProperty("preview").GetString()!;
        Assert.DoesNotContain("9999", preview, StringComparison.Ordinal);
        Assert.DoesNotContain("₱", preview, StringComparison.Ordinal);

        using var auditRequest = Authed(
            HttpMethod.Get,
            $"/api/v1/personal/utang/delivery-audit?reminderId={firstReminderId}",
            lenderToken);
        var auditResponse = await _client.SendAsync(auditRequest);
        Assert.Equal(HttpStatusCode.OK, auditResponse.StatusCode);
        var audit = await auditResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(audit.GetArrayLength() >= 1);

        using var secondCreate = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/reminders",
            lenderToken,
            new
            {
                scheduleType = "OneTime",
                scheduledForUtc = DateTimeOffset.UtcNow,
                message = "Another reminder"
            });
        var secondReminderId = (await (await _client.SendAsync(secondCreate)).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();
        using var secondDeliver = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/reminders/{secondReminderId}/deliver",
            lenderToken);
        var secondDeliverResponse = await _client.SendAsync(secondDeliver);
        Assert.Equal(HttpStatusCode.TooManyRequests, secondDeliverResponse.StatusCode);
        var rateBody = await secondDeliverResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(
            ApplicationErrorCodes.PersonalReminderRateLimited,
            rateBody.GetProperty("errorCode").GetString());

        _ = borrowerId;
    }

    [Fact]
    public async Task Invitation_revoke_and_invalid_token_are_anti_enumeration_safe()
    {
        var (lenderToken, lenderId, _) = await SeedPersonalUserAsync("vlend");
        var (borrowerToken, _, borrowerEmail) = await SeedPersonalUserAsync("vborr");

        using var contactRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/contacts",
            lenderToken,
            new { displayName = "Revoke Friend", email = borrowerEmail });
        var contactId = (await (await _client.SendAsync(contactRequest)).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        using var relationshipRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/relationships",
            lenderToken,
            new { creditorUserIdentityId = lenderId, debtorContactId = contactId, currencyCode = "PHP" });
        var relationshipId = (await (await _client.SendAsync(relationshipRequest)).Content
            .ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        using var inviteRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/invitations",
            lenderToken,
            new { inviteeContactId = contactId });
        var inviteBody = await (await _client.SendAsync(inviteRequest)).Content.ReadFromJsonAsync<JsonElement>();
        var invitationId = inviteBody.GetProperty("id").GetGuid();
        var acceptToken = inviteBody.GetProperty("acceptToken").GetString();

        using var revokeRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/invitations/{invitationId}/revoke",
            lenderToken);
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(revokeRequest)).StatusCode);

        using var acceptAfterRevoke = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/invitations/accept",
            borrowerToken,
            new { token = acceptToken });
        var acceptResponse = await _client.SendAsync(acceptAfterRevoke);
        Assert.Equal(HttpStatusCode.NotFound, acceptResponse.StatusCode);
        var body = await acceptResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(
            ApplicationErrorCodes.PersonalUtangInvitationNotFound,
            body.GetProperty("errorCode").GetString());
    }
}
