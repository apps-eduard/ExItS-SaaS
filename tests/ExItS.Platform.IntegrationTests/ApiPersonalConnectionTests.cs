using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApiPersonalConnectionTests(PostgreSqlFixture fixture) : IAsyncLifetime
{
    private SessionApiFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new SessionApiFactory(fixture.ConnectionString);
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
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

    private async Task<(string Token, Guid UserId)> SeedPersonalUserAsync(string prefix)
    {
        var (userId, email, password) = await PlatformIntegrationTestUsers.RegisterPersonalWithPasswordAsync(_client, prefix);
        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = email, password });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionToken").GetString()!;
        return (token, userId);
    }

    private static async Task<string> GetPublicUserIdAsync(HttpClient client, string token)
    {
        using var request = Authed(HttpMethod.Get, "/api/v1/me/public-identity", token);
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("publicUserId").GetString()!;
    }

    private async Task<Guid> CreateIdentifiedContactAsync(
        string token,
        string displayName,
        Guid resolvedUserIdentityId,
        string resolvedPublicUserId)
    {
        using var request = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/contacts",
            token,
            new
            {
                displayName,
                resolvedUserIdentityId,
                resolvedPublicUserId,
            });
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(resolvedUserIdentityId, body.GetProperty("resolvedUserIdentityId").GetGuid());
        Assert.True(body.GetProperty("linkedUserIdentityId").ValueKind is JsonValueKind.Null);
        return body.GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task Add_identified_contact_persists_resolved_identity_without_request_or_notification()
    {
        var (tokenA, _) = await SeedPersonalUserAsync("conn-a");
        var (tokenB, userB) = await SeedPersonalUserAsync("conn-b");
        var publicB = await GetPublicUserIdAsync(_client, tokenB);

        var contactId = await CreateIdentifiedContactAsync(tokenA, "User B", userB, publicB);

        using var listContacts = Authed(HttpMethod.Get, "/api/v1/personal/utang/contacts", tokenA);
        var contacts = await _client.SendAsync(listContacts);
        contacts.EnsureSuccessStatusCode();
        var contact = (await contacts.Content.ReadFromJsonAsync<JsonElement>())
            .EnumerateArray()
            .Single(c => c.GetProperty("id").GetGuid() == contactId);
        Assert.Equal(userB, contact.GetProperty("resolvedUserIdentityId").GetGuid());
        Assert.Equal(publicB, contact.GetProperty("resolvedPublicUserId").GetString());

        using var listConnections = Authed(HttpMethod.Get, "/api/v1/personal/connections", tokenA);
        var connectionList = await _client.SendAsync(listConnections);
        connectionList.EnsureSuccessStatusCode();
        Assert.Empty((await connectionList.Content.ReadFromJsonAsync<JsonElement>())!.EnumerateArray());

        using var notifications = Authed(HttpMethod.Get, "/api/v1/personal/notifications", tokenB);
        var notificationList = await _client.SendAsync(notifications);
        notificationList.EnsureSuccessStatusCode();
        Assert.Empty((await notificationList.Content.ReadFromJsonAsync<JsonElement>())!.EnumerateArray());
    }

    [Fact]
    public async Task Connection_request_lifecycle_link_unlink_relink_block_unblock()
    {
        var (tokenA, _) = await SeedPersonalUserAsync("cyl-a");
        var (tokenB, userB) = await SeedPersonalUserAsync("cyl-b");
        var publicB = await GetPublicUserIdAsync(_client, tokenB);
        var contactId = await CreateIdentifiedContactAsync(tokenA, "User B", userB, publicB);

        using var requestConnection = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/people/{contactId}/connection-request",
            tokenA);
        var requestResponse = await _client.SendAsync(requestConnection);
        Assert.Equal(HttpStatusCode.Created, requestResponse.StatusCode);
        var requestBody = await requestResponse.Content.ReadFromJsonAsync<JsonElement>();
        var requestId = requestBody!.GetProperty("id").GetGuid();
        Assert.Equal("Pending", requestBody.GetProperty("status").GetString());

        using var duplicateRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/people/{contactId}/connection-request",
            tokenA);
        var duplicateResponse = await _client.SendAsync(duplicateRequest);
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

        using var listIncoming = Authed(HttpMethod.Get, "/api/v1/personal/connections", tokenB);
        var incoming = await _client.SendAsync(listIncoming);
        incoming.EnsureSuccessStatusCode();
        var incomingBody = await incoming.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(incomingBody!.EnumerateArray(), item => item.GetProperty("id").GetGuid() == requestId);

        var (tokenC, _) = await SeedPersonalUserAsync("cyl-c");
        using var badAccept = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/connections/{requestId}/accept",
            tokenC);
        var badAcceptResponse = await _client.SendAsync(badAccept);
        Assert.Equal(HttpStatusCode.Forbidden, badAcceptResponse.StatusCode);

        using var accept = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/connections/{requestId}/accept",
            tokenB);
        var acceptResponse = await _client.SendAsync(accept);
        acceptResponse.EnsureSuccessStatusCode();
        Assert.Equal(
            "Accepted",
            (await acceptResponse.Content.ReadFromJsonAsync<JsonElement>())!.GetProperty("status").GetString());

        using var contactsAfterAccept = Authed(HttpMethod.Get, "/api/v1/personal/utang/contacts", tokenA);
        var contactAfterAccept = (await (await _client.SendAsync(contactsAfterAccept)).Content.ReadFromJsonAsync<JsonElement>())
            .EnumerateArray()
            .Single(c => c.GetProperty("id").GetGuid() == contactId);
        Assert.Equal(userB, contactAfterAccept.GetProperty("linkedUserIdentityId").GetGuid());
        Assert.Equal(userB, contactAfterAccept.GetProperty("resolvedUserIdentityId").GetGuid());

        using var unlink = Authed(HttpMethod.Post, $"/api/v1/personal/people/{contactId}/unlink", tokenA);
        var unlinkResponse = await _client.SendAsync(unlink);
        unlinkResponse.EnsureSuccessStatusCode();
        var unlinked = await unlinkResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(unlinked!.GetProperty("linkedUserIdentityId").ValueKind is JsonValueKind.Null);
        Assert.Equal(userB, unlinked.GetProperty("resolvedUserIdentityId").GetGuid());

        using var relinkRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/people/{contactId}/connection-request",
            tokenA);
        var relinkResponse = await _client.SendAsync(relinkRequest);
        Assert.Equal(HttpStatusCode.Created, relinkResponse.StatusCode);
        var relinkId = (await relinkResponse.Content.ReadFromJsonAsync<JsonElement>())!.GetProperty("id").GetGuid();

        using var relinkAccept = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/connections/{relinkId}/accept",
            tokenB);
        (await _client.SendAsync(relinkAccept)).EnsureSuccessStatusCode();

        using var block = Authed(HttpMethod.Post, $"/api/v1/personal/people/{contactId}/block", tokenA);
        var blockResponse = await _client.SendAsync(block);
        blockResponse.EnsureSuccessStatusCode();
        var blocked = await blockResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotNull(blocked!.GetProperty("blockedAtUtc").GetString());
        Assert.True(blocked.GetProperty("linkedUserIdentityId").ValueKind is JsonValueKind.Null);

        using var unblock = Authed(HttpMethod.Post, $"/api/v1/personal/people/{contactId}/unblock", tokenA);
        var unblockResponse = await _client.SendAsync(unblock);
        unblockResponse.EnsureSuccessStatusCode();
        var unblocked = await unblockResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(unblocked!.GetProperty("blockedAtUtc").ValueKind is JsonValueKind.Null);
        Assert.True(unblocked.GetProperty("linkedUserIdentityId").ValueKind is JsonValueKind.Null);
    }

    [Fact]
    public async Task Decline_and_cancel_leave_contact_unlinked()
    {
        var (tokenA, _) = await SeedPersonalUserAsync("dec-a");
        var (tokenB, userB) = await SeedPersonalUserAsync("dec-b");
        var publicB = await GetPublicUserIdAsync(_client, tokenB);
        var contactId = await CreateIdentifiedContactAsync(tokenA, "User B", userB, publicB);

        using var request = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/people/{contactId}/connection-request",
            tokenA);
        var requestId = (await (await _client.SendAsync(request)).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        using var decline = Authed(HttpMethod.Post, $"/api/v1/personal/connections/{requestId}/decline", tokenB);
        (await _client.SendAsync(decline)).EnsureSuccessStatusCode();

        using var contacts = Authed(HttpMethod.Get, "/api/v1/personal/utang/contacts", tokenA);
        var contact = (await (await _client.SendAsync(contacts)).Content.ReadFromJsonAsync<JsonElement>())
            .EnumerateArray()
            .Single(c => c.GetProperty("id").GetGuid() == contactId);
        Assert.True(contact.GetProperty("linkedUserIdentityId").ValueKind is JsonValueKind.Null);

        using var request2 = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/people/{contactId}/connection-request",
            tokenA);
        var requestId2 = (await (await _client.SendAsync(request2)).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        using var revoke = Authed(HttpMethod.Post, $"/api/v1/personal/connections/{requestId2}/revoke", tokenA);
        (await _client.SendAsync(revoke)).EnsureSuccessStatusCode();

        using var contacts2 = Authed(HttpMethod.Get, "/api/v1/personal/utang/contacts", tokenA);
        var contact2 = (await (await _client.SendAsync(contacts2)).Content.ReadFromJsonAsync<JsonElement>())
            .EnumerateArray()
            .Single(c => c.GetProperty("id").GetGuid() == contactId);
        Assert.True(contact2.GetProperty("linkedUserIdentityId").ValueKind is JsonValueKind.Null);
        Assert.Equal(userB, contact2.GetProperty("resolvedUserIdentityId").GetGuid());
    }

    [Fact]
    public async Task Request_creates_notification_and_read_does_not_change_request()
    {
        var (tokenA, _) = await SeedPersonalUserAsync("not-a");
        var (tokenB, userB) = await SeedPersonalUserAsync("not-b");
        var publicB = await GetPublicUserIdAsync(_client, tokenB);
        var contactId = await CreateIdentifiedContactAsync(tokenA, "User B", userB, publicB);

        using var request = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/people/{contactId}/connection-request",
            tokenA);
        var requestId = (await (await _client.SendAsync(request)).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        using var notificationsReq = Authed(HttpMethod.Get, "/api/v1/personal/notifications", tokenB);
        var notifications = await (await _client.SendAsync(notificationsReq)).Content.ReadFromJsonAsync<JsonElement>();
        var notification = notifications!.EnumerateArray().FirstOrDefault(n =>
            string.Equals(
                n.GetProperty("relatedType").GetString(),
                "PersonalConnectionRequest",
                StringComparison.Ordinal));
        Assert.NotEqual(default, notification);

        var notificationId = notification.GetProperty("id").GetGuid();
        using var markRead = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/notifications/{notificationId}/read",
            tokenB);
        (await _client.SendAsync(markRead)).EnsureSuccessStatusCode();

        using var listPending = Authed(HttpMethod.Get, "/api/v1/personal/connections", tokenB);
        var pending = await (await _client.SendAsync(listPending)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(
            pending!.EnumerateArray(),
            item =>
                item.GetProperty("id").GetGuid() == requestId &&
                item.GetProperty("status").GetString() == "Pending");
    }

    [Fact]
    public async Task Local_contact_creation_without_identity_request_or_notification()
    {
        var (tokenA, _) = await SeedPersonalUserAsync("loc-a");

        using var create = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/contacts",
            tokenA,
            new { displayName = "Pedro Cruz", phone = "+639170000001", email = "pedro@example.com" });
        var createResponse = await _client.SendAsync(create);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var body = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body!.GetProperty("resolvedUserIdentityId").ValueKind is JsonValueKind.Null);
        Assert.True(body.GetProperty("linkedUserIdentityId").ValueKind is JsonValueKind.Null);

        using var connections = Authed(HttpMethod.Get, "/api/v1/personal/connections", tokenA);
        Assert.Empty((await (await _client.SendAsync(connections)).Content.ReadFromJsonAsync<JsonElement>())!.EnumerateArray());

        using var notifications = Authed(HttpMethod.Get, "/api/v1/personal/notifications", tokenA);
        Assert.Empty((await (await _client.SendAsync(notifications)).Content.ReadFromJsonAsync<JsonElement>())!.EnumerateArray());
    }

    [Fact]
    public async Task Unlink_preserves_active_financial_history()
    {
        var (tokenA, userA) = await SeedPersonalUserAsync("fin-a");
        var (tokenB, userB) = await SeedPersonalUserAsync("fin-b");
        var publicB = await GetPublicUserIdAsync(_client, tokenB);
        var contactId = await CreateIdentifiedContactAsync(tokenA, "Borrower Friend", userB, publicB);

        using var relationshipRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/relationships",
            tokenA,
            new
            {
                creditorUserIdentityId = userA,
                debtorContactId = contactId,
                currencyCode = "PHP",
                initialLoanAmount = 500m,
            });
        var relationshipResponse = await _client.SendAsync(relationshipRequest);
        relationshipResponse.EnsureSuccessStatusCode();
        var relationshipId = (await relationshipResponse.Content.ReadFromJsonAsync<JsonElement>())!
            .GetProperty("id").GetGuid();

        using var requestConnection = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/people/{contactId}/connection-request",
            tokenA);
        var requestId = (await (await _client.SendAsync(requestConnection)).Content.ReadFromJsonAsync<JsonElement>())!
            .GetProperty("id").GetGuid();

        using var accept = Authed(HttpMethod.Post, $"/api/v1/personal/connections/{requestId}/accept", tokenB);
        (await _client.SendAsync(accept)).EnsureSuccessStatusCode();

        using var unlink = Authed(HttpMethod.Post, $"/api/v1/personal/people/{contactId}/unlink", tokenA);
        (await _client.SendAsync(unlink)).EnsureSuccessStatusCode();

        using var lent = Authed(HttpMethod.Get, "/api/v1/personal/utang/relationships/lent", tokenA);
        var lentBody = await (await _client.SendAsync(lent)).Content.ReadFromJsonAsync<JsonElement>();
        var rel = lentBody!.EnumerateArray().Single(r => r.GetProperty("id").GetGuid() == relationshipId);
        Assert.Equal("Active", rel.GetProperty("status").GetString());
        Assert.Equal(500m, rel.GetProperty("currentBalance").GetDecimal());
    }

    [Fact]
    public async Task Block_prevents_new_connection_request()
    {
        var (tokenA, _) = await SeedPersonalUserAsync("blk-a");
        var (tokenB, userB) = await SeedPersonalUserAsync("blk-b");
        var publicB = await GetPublicUserIdAsync(_client, tokenB);
        var contactId = await CreateIdentifiedContactAsync(tokenA, "User B", userB, publicB);

        using var block = Authed(HttpMethod.Post, $"/api/v1/personal/people/{contactId}/block", tokenA);
        (await _client.SendAsync(block)).EnsureSuccessStatusCode();

        using var request = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/people/{contactId}/connection-request",
            tokenA);
        var blockedRequest = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Conflict, blockedRequest.StatusCode);
    }
}
