using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Application.Common;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApiPersonalUtangAntiSpamTests(PostgreSqlFixture fixture) : IAsyncLifetime
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

    private static string Unique(string prefix) =>
        $"{prefix}{Guid.NewGuid():N}"[..Math.Min(20, prefix.Length + 32)].ToLowerInvariant();

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

    private async Task<JsonElement> SendOk(HttpRequestMessage request)
    {
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    /// <summary>Owner-model shared create: Confirmed immediately with balance = loan amount.</summary>
    private async Task<(Guid RelationshipId, int Version)> CreateSharedOwnerLoanAsync(
        string token,
        Guid creditorId,
        Guid debtorId,
        decimal amount,
        string notes)
    {
        using var request = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/relationships",
            token,
            new
            {
                creditorUserIdentityId = creditorId,
                debtorUserIdentityId = debtorId,
                currencyCode = "PHP",
                initialLoanAmount = amount,
                initialLoanNotes = notes,
                shareWithCounterparty = true
            });
        var created = await SendOk(request);
        Assert.Equal(amount, created.GetProperty("currentBalance").GetDecimal());
        Assert.True(created.GetProperty("isLedgerOwner").GetBoolean());
        return (created.GetProperty("id").GetGuid(), created.GetProperty("version").GetInt32());
    }

    private async Task<(Guid EntryId, int Version)> RecordOwnerLoanAsync(
        string token,
        Guid relationshipId,
        decimal amount,
        string notes,
        int expectedVersion)
    {
        using var request = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/entries",
            token,
            new { entryType = "Loan", amount, notes, expectedVersion });
        var entry = await SendOk(request);
        Assert.Equal("Confirmed", entry.GetProperty("status").GetString());
        using var balReq = Authed(HttpMethod.Get, $"/api/v1/personal/utang/relationships/{relationshipId}/balance", token);
        var bal = await SendOk(balReq);
        return (entry.GetProperty("id").GetGuid(), bal.GetProperty("version").GetInt32());
    }

    [Fact]
    public async Task Owner_model_pending_limit_does_not_block_additional_confirmed_loans()
    {
        var (micaTok, micaId) = await SeedPersonalUserAsync("asmc");
        var (kizyTok, kizyId) = await SeedPersonalUserAsync("askz");
        var (luisTok, luisId) = await SeedPersonalUserAsync("aslu");

        var (relId, ver) = await CreateSharedOwnerLoanAsync(micaTok, micaId, kizyId, 100m, "One");
        (_, ver) = await RecordOwnerLoanAsync(micaTok, relId, 200m, "Two", ver);
        (_, ver) = await RecordOwnerLoanAsync(micaTok, relId, 300m, "Three", ver);
        (_, ver) = await RecordOwnerLoanAsync(micaTok, relId, 400m, "Four", ver);

        using var balReq = Authed(HttpMethod.Get, $"/api/v1/personal/utang/relationships/{relId}/balance", micaTok);
        Assert.Equal(1000m, (await SendOk(balReq)).GetProperty("currentBalance").GetDecimal());

        using var histReq = Authed(HttpMethod.Get, $"/api/v1/personal/utang/relationships/{relId}/history", micaTok);
        var pendingCount = (await SendOk(histReq)).EnumerateArray()
            .Count(e => e.GetProperty("status").GetString() == "Pending");
        Assert.Equal(0, pendingCount);

        // Directional reverse and other counterparty still allowed.
        var (revId, _) = await CreateSharedOwnerLoanAsync(kizyTok, kizyId, micaId, 50m, "Reverse");
        Assert.NotEqual(Guid.Empty, revId);
        var (luisRel, _) = await CreateSharedOwnerLoanAsync(micaTok, micaId, luisId, 75m, "Luis");
        Assert.NotEqual(Guid.Empty, luisRel);
    }

    [Fact]
    public async Task Duplicate_immediate_shared_loan_is_rejected_and_private_loan_unaffected()
    {
        var (ownerTok, ownerId) = await SeedPersonalUserAsync("asdup");
        var (peerTok, peerId) = await SeedPersonalUserAsync("asdup2");

        var (relId, ver) = await CreateSharedOwnerLoanAsync(ownerTok, ownerId, peerId, 500m, "Lunch");
        using var dup = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relId}/entries",
            ownerTok,
            new { entryType = "Loan", amount = 500m, notes = "Lunch", expectedVersion = ver });
        var dupResponse = await _client.SendAsync(dup);
        Assert.Equal(HttpStatusCode.Conflict, dupResponse.StatusCode);
        Assert.Equal(
            ApplicationErrorCodes.PersonalUtangDuplicateSubmission,
            (await dupResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errorCode").GetString());

        using var contactReq = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/contacts",
            ownerTok,
            new { displayName = "Local Only" });
        var contactId = (await SendOk(contactReq)).GetProperty("id").GetGuid();
        using var privateRel = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/relationships",
            ownerTok,
            new
            {
                creditorUserIdentityId = ownerId,
                debtorContactId = contactId,
                currencyCode = "PHP",
                initialLoanAmount = 500m,
                initialLoanNotes = "Private lunch"
            });
        var privateCreated = await SendOk(privateRel);
        Assert.Equal(500m, privateCreated.GetProperty("currentBalance").GetDecimal());
        Assert.False(privateCreated.GetProperty("isSharedLedger").GetBoolean());
    }

    private async Task<Guid> GetPendingConnectionForContactAsync(string token, Guid contactId)
    {
        using var list = Authed(HttpMethod.Get, "/api/v1/personal/connections", token);
        var body = await SendOk(list);
        var pending = body.EnumerateArray().Single(item =>
            item.GetProperty("requesterContactId").GetGuid() == contactId
            && item.GetProperty("status").GetString() == "Pending");
        return pending.GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task Shared_confirmed_notification_respects_notifications_preference()
    {
        var (micaTok, micaId) = await SeedPersonalUserAsync("asnt1");
        var (kizyTok, kizyId) = await SeedPersonalUserAsync("asnt2");

        // Connect so prefs endpoint is authorized for Kizy about Mica.
        using var publicReq = Authed(HttpMethod.Get, "/api/v1/me/public-identity", micaTok);
        var micaPublic = (await SendOk(publicReq)).GetProperty("publicUserId").GetString()!;
        using var contactReq = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/contacts",
            kizyTok,
            new
            {
                displayName = "Mica Friend",
                resolvedUserIdentityId = micaId,
                resolvedPublicUserId = micaPublic
            });
        var contactId = (await SendOk(contactReq)).GetProperty("id").GetGuid();
        var requestId = await GetPendingConnectionForContactAsync(kizyTok, contactId);
        using var accept = Authed(HttpMethod.Post, $"/api/v1/personal/connections/{requestId}/accept", micaTok);
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(accept)).StatusCode);

        var (relId, ver) = await CreateSharedOwnerLoanAsync(micaTok, micaId, kizyId, 10m, "N1");

        using var notesReq = Authed(HttpMethod.Get, "/api/v1/personal/notifications?scope=recent", kizyTok);
        var notes = await SendOk(notesReq);
        var sharedNotes = notes.EnumerateArray()
            .Where(n => n.GetProperty("relatedType").GetString() == "PersonalUtangSharedEntry")
            .ToList();
        Assert.NotEmpty(sharedNotes);

        using var prefsOff = Authed(
            HttpMethod.Put,
            $"/api/v1/personal/shared-utang-preferences/{micaId}",
            kizyTok,
            new
            {
                receiveSharedUtang = true,
                autoAcceptSharedUtang = true,
                sharedUtangNotifications = false
            });
        var prefs = await SendOk(prefsOff);
        Assert.False(prefs.GetProperty("autoAcceptSharedUtang").GetBoolean());
        Assert.False(prefs.GetProperty("sharedUtangNotifications").GetBoolean());

        await RecordOwnerLoanAsync(micaTok, relId, 20m, "N2-quiet", ver);

        using var notes2 = Authed(HttpMethod.Get, "/api/v1/personal/notifications?scope=recent", kizyTok);
        var afterOff = (await SendOk(notes2)).EnumerateArray()
            .Count(n => n.GetProperty("relatedType").GetString() == "PersonalUtangSharedEntry");
        Assert.Equal(sharedNotes.Count, afterOff);
    }

    [Fact]
    public async Task Daily_limit_is_directional_for_owner_model_confirmed_loans()
    {
        var (micaTok, micaId) = await SeedPersonalUserAsync("asday");
        var (kizyTok, kizyId) = await SeedPersonalUserAsync("asday2");
        var (luisTok, luisId) = await SeedPersonalUserAsync("asday3");

        var (relId, ver) = await CreateSharedOwnerLoanAsync(micaTok, micaId, kizyId, 1m, "D1");
        for (var i = 2; i <= 10; i++)
        {
            (_, ver) = await RecordOwnerLoanAsync(micaTok, relId, i, $"Day{i}", ver);
        }

        using var bal = Authed(HttpMethod.Get, $"/api/v1/personal/utang/relationships/{relId}/balance", micaTok);
        ver = (await SendOk(bal)).GetProperty("version").GetInt32();
        using var eleventh = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relId}/entries",
            micaTok,
            new { entryType = "Loan", amount = 99m, notes = "Day11", expectedVersion = ver });
        var eleventhResponse = await _client.SendAsync(eleventh);
        Assert.Equal(HttpStatusCode.TooManyRequests, eleventhResponse.StatusCode);
        Assert.Equal(
            ApplicationErrorCodes.PersonalUtangDailyLimitReached,
            (await eleventhResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errorCode").GetString());

        var (revId, _) = await CreateSharedOwnerLoanAsync(kizyTok, kizyId, micaId, 5m, "ReverseDay");
        Assert.NotEqual(Guid.Empty, revId);

        var (luisRel, _) = await CreateSharedOwnerLoanAsync(micaTok, micaId, luisId, 6m, "LuisDay");
        Assert.NotEqual(Guid.Empty, luisRel);
    }

    [Fact]
    public async Task Blocked_relationship_cannot_create_shared_utang_proposal()
    {
        var (micaTok, micaId) = await SeedPersonalUserAsync("asblk");
        var (kizyTok, kizyId) = await SeedPersonalUserAsync("asblk2");

        using var publicReq = Authed(HttpMethod.Get, "/api/v1/me/public-identity", kizyTok);
        var kizyPublic = (await SendOk(publicReq)).GetProperty("publicUserId").GetString()!;

        using var contactReq = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/contacts",
            micaTok,
            new
            {
                displayName = "Kizy Friend",
                resolvedUserIdentityId = kizyId,
                resolvedPublicUserId = kizyPublic
            });
        var contactId = (await SendOk(contactReq)).GetProperty("id").GetGuid();

        var requestId = await GetPendingConnectionForContactAsync(micaTok, contactId);
        using var accept = Authed(HttpMethod.Post, $"/api/v1/personal/connections/{requestId}/accept", kizyTok);
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(accept)).StatusCode);

        var (relId, _) = await CreateSharedOwnerLoanAsync(micaTok, micaId, kizyId, 10m, "BeforeBlock");

        using var block = Authed(HttpMethod.Post, $"/api/v1/personal/people/{contactId}/block", micaTok);
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(block)).StatusCode);

        using var bal = Authed(HttpMethod.Get, $"/api/v1/personal/utang/relationships/{relId}/balance", micaTok);
        var ver = (await SendOk(bal)).GetProperty("version").GetInt32();
        using var afterBlock = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relId}/entries",
            micaTok,
            new { entryType = "Loan", amount = 20m, notes = "AfterBlock", expectedVersion = ver });
        var blockedResponse = await _client.SendAsync(afterBlock);
        Assert.Equal(HttpStatusCode.Conflict, blockedResponse.StatusCode);
        var body = await blockedResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ApplicationErrorCodes.PersonalConnectionBlocked, body.GetProperty("errorCode").GetString());
    }
}
