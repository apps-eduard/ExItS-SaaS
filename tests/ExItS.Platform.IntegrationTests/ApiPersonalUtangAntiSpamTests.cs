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

    private async Task<(Guid RelationshipId, int Version)> CreateSharedPendingLoanAsync(
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
                initialLoanNotes = notes
            });
        var created = await SendOk(request);
        Assert.Equal(0m, created.GetProperty("currentBalance").GetDecimal());
        return (created.GetProperty("id").GetGuid(), created.GetProperty("version").GetInt32());
    }

    private async Task<(Guid EntryId, int Version)> RecordPendingLoanAsync(
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
        Assert.Equal("Pending", entry.GetProperty("status").GetString());
        using var balReq = Authed(HttpMethod.Get, $"/api/v1/personal/utang/relationships/{relationshipId}/balance", token);
        var bal = await SendOk(balReq);
        return (entry.GetProperty("id").GetGuid(), bal.GetProperty("version").GetInt32());
    }

    [Fact]
    public async Task Shared_pending_limit_is_directional_and_capacity_returns_after_confirm_or_dispute()
    {
        var (micaTok, micaId) = await SeedPersonalUserAsync("asmc");
        var (kizyTok, kizyId) = await SeedPersonalUserAsync("askz");
        var (luisTok, luisId) = await SeedPersonalUserAsync("aslu");

        var (relId, ver) = await CreateSharedPendingLoanAsync(micaTok, micaId, kizyId, 100m, "One");
        (_, ver) = await RecordPendingLoanAsync(micaTok, relId, 200m, "Two", ver);
        (_, ver) = await RecordPendingLoanAsync(micaTok, relId, 300m, "Three", ver);

        using var fourth = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relId}/entries",
            micaTok,
            new { entryType = "Loan", amount = 400m, notes = "Four", expectedVersion = ver });
        var fourthResponse = await _client.SendAsync(fourth);
        Assert.Equal(HttpStatusCode.Conflict, fourthResponse.StatusCode);
        var fourthBody = await fourthResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(
            ApplicationErrorCodes.PersonalUtangPendingLimitReached,
            fourthBody.GetProperty("errorCode").GetString());

        // Confirmed history does not count — balance still 0 with 3 pending.
        using var balReq = Authed(HttpMethod.Get, $"/api/v1/personal/utang/relationships/{relId}/balance", micaTok);
        Assert.Equal(0m, (await SendOk(balReq)).GetProperty("currentBalance").GetDecimal());

        // Directional: Kizy → Mica still allowed.
        var (revId, _) = await CreateSharedPendingLoanAsync(kizyTok, kizyId, micaId, 50m, "Reverse");
        Assert.NotEqual(Guid.Empty, revId);

        // Other counterparty still allowed.
        var (luisRel, _) = await CreateSharedPendingLoanAsync(micaTok, micaId, luisId, 75m, "Luis");
        Assert.NotEqual(Guid.Empty, luisRel);

        // Confirm one → capacity returns.
        using var histReq = Authed(HttpMethod.Get, $"/api/v1/personal/utang/relationships/{relId}/history", kizyTok);
        var hist = await SendOk(histReq);
        var pending = hist.EnumerateArray().First(e => e.GetProperty("status").GetString() == "Pending"
                                                       && e.GetProperty("canConfirm").GetBoolean());
        var entryId = pending.GetProperty("id").GetGuid();
        using var balK = Authed(HttpMethod.Get, $"/api/v1/personal/utang/relationships/{relId}/balance", kizyTok);
        var kizyVer = (await SendOk(balK)).GetProperty("version").GetInt32();
        using var confirm = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relId}/entries/{entryId}/confirm",
            kizyTok,
            new { expectedVersion = kizyVer });
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(confirm)).StatusCode);

        using var balAfter = Authed(HttpMethod.Get, $"/api/v1/personal/utang/relationships/{relId}/balance", micaTok);
        var afterConfirm = await SendOk(balAfter);
        ver = afterConfirm.GetProperty("version").GetInt32();
        (_, ver) = await RecordPendingLoanAsync(micaTok, relId, 15m, "After confirm", ver);

        // Dispute one → capacity returns again (first get a pending to dispute).
        using var hist2 = Authed(HttpMethod.Get, $"/api/v1/personal/utang/relationships/{relId}/history", kizyTok);
        var pending2 = (await SendOk(hist2)).EnumerateArray()
            .First(e => e.GetProperty("status").GetString() == "Pending" && e.GetProperty("canDispute").GetBoolean());
        using var balK2 = Authed(HttpMethod.Get, $"/api/v1/personal/utang/relationships/{relId}/balance", kizyTok);
        var kizyVer2 = (await SendOk(balK2)).GetProperty("version").GetInt32();
        using var dispute = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relId}/entries/{pending2.GetProperty("id").GetGuid()}/dispute",
            kizyTok,
            new { expectedVersion = kizyVer2, reason = "Amount incorrect" });
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(dispute)).StatusCode);

        using var bal3 = Authed(HttpMethod.Get, $"/api/v1/personal/utang/relationships/{relId}/balance", micaTok);
        ver = (await SendOk(bal3)).GetProperty("version").GetInt32();
        await RecordPendingLoanAsync(micaTok, relId, 16m, "After dispute", ver);
    }

    [Fact]
    public async Task Duplicate_immediate_shared_loan_is_rejected_and_private_loan_unaffected()
    {
        var (ownerTok, ownerId) = await SeedPersonalUserAsync("asdup");
        var (peerTok, peerId) = await SeedPersonalUserAsync("asdup2");

        var (relId, ver) = await CreateSharedPendingLoanAsync(ownerTok, ownerId, peerId, 500m, "Lunch");
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

    [Fact]
    public async Task Aggregated_pending_notification_updates_count_without_duplicate_rows()
    {
        var (micaTok, micaId) = await SeedPersonalUserAsync("asnt1");
        var (kizyTok, kizyId) = await SeedPersonalUserAsync("asnt2");

        var (relId, ver) = await CreateSharedPendingLoanAsync(micaTok, micaId, kizyId, 10m, "N1");
        (_, ver) = await RecordPendingLoanAsync(micaTok, relId, 20m, "N2", ver);
        await RecordPendingLoanAsync(micaTok, relId, 30m, "N3", ver);

        using var notesReq = Authed(HttpMethod.Get, "/api/v1/personal/notifications?scope=recent", kizyTok);
        var notes = await SendOk(notesReq);
        var pendingNotes = notes.EnumerateArray()
            .Where(n => n.GetProperty("relatedType").GetString() == "PersonalUtangPendingProposals")
            .ToList();
        Assert.Single(pendingNotes);
        Assert.Contains("3 Utang entries waiting for your review", pendingNotes[0].GetProperty("preview").GetString());
        Assert.False(pendingNotes[0].GetProperty("isRead").GetBoolean());
        Assert.DoesNotContain("₱", pendingNotes[0].GetProperty("preview").GetString());
        Assert.DoesNotContain("N1", pendingNotes[0].GetProperty("preview").GetString());
    }

    [Fact]
    public async Task Notification_read_reactivates_on_new_activity_and_closes_when_all_resolved()
    {
        var (micaTok, micaId) = await SeedPersonalUserAsync("asntr");
        var (kizyTok, kizyId) = await SeedPersonalUserAsync("asntr2");

        var (relId, ver) = await CreateSharedPendingLoanAsync(micaTok, micaId, kizyId, 11m, "Cycle1");
        using var list1 = Authed(HttpMethod.Get, "/api/v1/personal/notifications?scope=recent", kizyTok);
        var noteId = (await SendOk(list1)).EnumerateArray()
            .Single(n => n.GetProperty("relatedType").GetString() == "PersonalUtangPendingProposals")
            .GetProperty("id").GetGuid();

        using var markRead = Authed(HttpMethod.Post, $"/api/v1/personal/notifications/{noteId}/read", kizyTok);
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(markRead)).StatusCode);

        using var bal = Authed(HttpMethod.Get, $"/api/v1/personal/utang/relationships/{relId}/balance", micaTok);
        ver = (await SendOk(bal)).GetProperty("version").GetInt32();
        await RecordPendingLoanAsync(micaTok, relId, 12m, "Cycle1b", ver);

        using var list2 = Authed(HttpMethod.Get, "/api/v1/personal/notifications?scope=recent", kizyTok);
        var active = (await SendOk(list2)).EnumerateArray()
            .Where(n => n.GetProperty("relatedType").GetString() == "PersonalUtangPendingProposals")
            .ToList();
        Assert.Single(active);
        Assert.Equal(noteId, active[0].GetProperty("id").GetGuid());
        Assert.False(active[0].GetProperty("isRead").GetBoolean());
        Assert.Contains("2 Utang entries waiting", active[0].GetProperty("preview").GetString());

        using var hist = Authed(HttpMethod.Get, $"/api/v1/personal/utang/relationships/{relId}/history", kizyTok);
        var pendingIds = (await SendOk(hist)).EnumerateArray()
            .Where(e => e.GetProperty("status").GetString() == "Pending")
            .Select(e => e.GetProperty("id").GetGuid())
            .ToList();
        Assert.Equal(2, pendingIds.Count);
        foreach (var entryId in pendingIds)
        {
            using var balK = Authed(HttpMethod.Get, $"/api/v1/personal/utang/relationships/{relId}/balance", kizyTok);
            var kizyVer = (await SendOk(balK)).GetProperty("version").GetInt32();
            using var confirm = Authed(
                HttpMethod.Post,
                $"/api/v1/personal/utang/relationships/{relId}/entries/{entryId}/confirm",
                kizyTok,
                new { expectedVersion = kizyVer });
            Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(confirm)).StatusCode);
        }

        using var list3 = Authed(HttpMethod.Get, "/api/v1/personal/notifications?scope=recent", kizyTok);
        var afterResolve = (await SendOk(list3)).EnumerateArray()
            .Where(n => n.GetProperty("id").GetGuid() == noteId)
            .ToList();
        Assert.Single(afterResolve);
        Assert.True(afterResolve[0].GetProperty("isRead").GetBoolean());
        Assert.StartsWith("done:", afterResolve[0].GetProperty("relatedId").GetString());

        using var balAfter = Authed(HttpMethod.Get, $"/api/v1/personal/utang/relationships/{relId}/balance", micaTok);
        ver = (await SendOk(balAfter)).GetProperty("version").GetInt32();
        await RecordPendingLoanAsync(micaTok, relId, 13m, "Cycle2", ver);

        using var list4 = Authed(HttpMethod.Get, "/api/v1/personal/notifications?scope=recent", kizyTok);
        var cycles = (await SendOk(list4)).EnumerateArray()
            .Where(n => n.GetProperty("relatedType").GetString() == "PersonalUtangPendingProposals")
            .ToList();
        Assert.Equal(2, cycles.Count);
        var newActive = cycles.Single(n => n.GetProperty("relatedId").GetString()!.StartsWith("from:"));
        Assert.NotEqual(noteId, newActive.GetProperty("id").GetGuid());
        Assert.False(newActive.GetProperty("isRead").GetBoolean());
        Assert.Contains("recorded an Utang entry for your review", newActive.GetProperty("preview").GetString());
    }

    [Fact]
    public async Task Concurrent_fourth_proposals_cannot_exceed_pending_limit()
    {
        var (micaTok, micaId) = await SeedPersonalUserAsync("ascon");
        var (kizyTok, kizyId) = await SeedPersonalUserAsync("ascon2");

        var (relId, ver) = await CreateSharedPendingLoanAsync(micaTok, micaId, kizyId, 1m, "C1");
        (_, ver) = await RecordPendingLoanAsync(micaTok, relId, 2m, "C2", ver);
        await RecordPendingLoanAsync(micaTok, relId, 3m, "C3", ver);

        using var bal = Authed(HttpMethod.Get, $"/api/v1/personal/utang/relationships/{relId}/balance", micaTok);
        ver = (await SendOk(bal)).GetProperty("version").GetInt32();

        var tasks = Enumerable.Range(0, 4).Select(async i =>
        {
            using var req = Authed(
                HttpMethod.Post,
                $"/api/v1/personal/utang/relationships/{relId}/entries",
                micaTok,
                new { entryType = "Loan", amount = 40m + i, notes = $"Race{i}", expectedVersion = ver });
            return await _client.SendAsync(req);
        }).ToArray();

        var responses = await Task.WhenAll(tasks);
        var ok = responses.Count(r => r.IsSuccessStatusCode);
        var conflicts = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);
        Assert.True(ok <= 1, $"Expected at most one success, got {ok}");
        Assert.True(ok + conflicts == 4, $"Unexpected statuses: {string.Join(",", responses.Select(r => r.StatusCode))}");

        using var hist = Authed(HttpMethod.Get, $"/api/v1/personal/utang/relationships/{relId}/history", micaTok);
        var pendingCount = (await SendOk(hist)).EnumerateArray()
            .Count(e => e.GetProperty("status").GetString() == "Pending");
        Assert.True(pendingCount <= 3, $"Pending exceeded invariant: {pendingCount}");
        Assert.Equal(3, pendingCount);
    }

    [Fact]
    public async Task Daily_limit_is_directional_after_resolving_pending_slots()
    {
        var (micaTok, micaId) = await SeedPersonalUserAsync("asday");
        var (kizyTok, kizyId) = await SeedPersonalUserAsync("asday2");
        var (luisTok, luisId) = await SeedPersonalUserAsync("asday3");

        var (relId, ver) = await CreateSharedPendingLoanAsync(micaTok, micaId, kizyId, 1m, "D1");
        for (var i = 2; i <= 10; i++)
        {
            // Keep pending under 3 by confirming before each new proposal after the third.
            if (i > 3)
            {
                using var hist = Authed(HttpMethod.Get, $"/api/v1/personal/utang/relationships/{relId}/history", kizyTok);
                var pending = (await SendOk(hist)).EnumerateArray()
                    .First(e => e.GetProperty("status").GetString() == "Pending");
                using var balK = Authed(HttpMethod.Get, $"/api/v1/personal/utang/relationships/{relId}/balance", kizyTok);
                var kizyVer = (await SendOk(balK)).GetProperty("version").GetInt32();
                using var confirm = Authed(
                    HttpMethod.Post,
                    $"/api/v1/personal/utang/relationships/{relId}/entries/{pending.GetProperty("id").GetGuid()}/confirm",
                    kizyTok,
                    new { expectedVersion = kizyVer });
                Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(confirm)).StatusCode);
                using var balM = Authed(HttpMethod.Get, $"/api/v1/personal/utang/relationships/{relId}/balance", micaTok);
                ver = (await SendOk(balM)).GetProperty("version").GetInt32();
            }

            (_, ver) = await RecordPendingLoanAsync(micaTok, relId, i, $"Day{i}", ver);
        }

        // Free pending slots so the daily ceiling (not pending=3) is what blocks the 11th.
        for (var free = 0; free < 3; free++)
        {
            using var hist = Authed(HttpMethod.Get, $"/api/v1/personal/utang/relationships/{relId}/history", kizyTok);
            var pending = (await SendOk(hist)).EnumerateArray()
                .FirstOrDefault(e => e.GetProperty("status").GetString() == "Pending");
            if (pending.ValueKind == JsonValueKind.Undefined)
            {
                break;
            }

            using var balK = Authed(HttpMethod.Get, $"/api/v1/personal/utang/relationships/{relId}/balance", kizyTok);
            var kizyVer = (await SendOk(balK)).GetProperty("version").GetInt32();
            using var confirm = Authed(
                HttpMethod.Post,
                $"/api/v1/personal/utang/relationships/{relId}/entries/{pending.GetProperty("id").GetGuid()}/confirm",
                kizyTok,
                new { expectedVersion = kizyVer });
            Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(confirm)).StatusCode);
        }

        // 11th in rolling day blocked.
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

        // Reverse direction still allowed.
        var (revId, _) = await CreateSharedPendingLoanAsync(kizyTok, kizyId, micaId, 5m, "ReverseDay");
        Assert.NotEqual(Guid.Empty, revId);

        // Other counterparty still allowed.
        var (luisRel, _) = await CreateSharedPendingLoanAsync(micaTok, micaId, luisId, 6m, "LuisDay");
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

        using var connReq = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/people/{contactId}/connection-request",
            micaTok);
        var requestId = (await SendOk(connReq)).GetProperty("id").GetGuid();
        using var accept = Authed(HttpMethod.Post, $"/api/v1/personal/connections/{requestId}/accept", kizyTok);
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(accept)).StatusCode);

        var (relId, _) = await CreateSharedPendingLoanAsync(micaTok, micaId, kizyId, 10m, "BeforeBlock");

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
