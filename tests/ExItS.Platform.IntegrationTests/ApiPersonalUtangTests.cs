using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Application.Common;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApiPersonalUtangTests(PostgreSqlFixture fixture) : IAsyncLifetime
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

    [Fact]
    public async Task Personal_utang_lifecycle_reconciles_balances()
    {
        var (lenderToken, lenderId) = await SeedPersonalUserAsync("lend");
        var (borrowerToken, borrowerId) = await SeedPersonalUserAsync("borr");

        using var contactRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/contacts",
            lenderToken,
            new { displayName = "Borrower Friend", phone = "+639170000001" });
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
                initialLoanAmount = 1000m,
                initialLoanNotes = "Test loan"
            });
        var relationshipResponse = await _client.SendAsync(relationshipRequest);
        Assert.Equal(HttpStatusCode.Created, relationshipResponse.StatusCode);
        var relationship = await relationshipResponse.Content.ReadFromJsonAsync<JsonElement>();
        var relationshipId = relationship.GetProperty("id").GetGuid();
        Assert.Equal(1000m, relationship.GetProperty("currentBalance").GetDecimal());
        Assert.Equal("Lent", relationship.GetProperty("perspective").GetString());

        using var paymentRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/entries",
            lenderToken,
            new
            {
                entryType = "Payment",
                amount = 400m,
                expectedVersion = relationship.GetProperty("version").GetInt32()
            });
        var paymentResponse = await _client.SendAsync(paymentRequest);
        Assert.Equal(HttpStatusCode.Created, paymentResponse.StatusCode);
        var paymentBody = await paymentResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(600m, paymentBody.GetProperty("balanceAfter").GetDecimal());

        using var balanceRequest = Authed(
            HttpMethod.Get,
            $"/api/v1/personal/utang/relationships/{relationshipId}/balance",
            lenderToken);
        var balanceResponse = await _client.SendAsync(balanceRequest);
        Assert.Equal(HttpStatusCode.OK, balanceResponse.StatusCode);
        Assert.Equal(600m, (await balanceResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("currentBalance").GetDecimal());

        using var lentRequest = Authed(HttpMethod.Get, "/api/v1/personal/utang/relationships/lent", lenderToken);
        var lentList = await _client.SendAsync(lentRequest);
        Assert.Equal(HttpStatusCode.OK, lentList.StatusCode);
        var lentItems = await lentList.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, lentItems.GetArrayLength());

        using var linkedRelationshipRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/relationships",
            borrowerToken,
            new
            {
                creditorUserIdentityId = lenderId,
                debtorUserIdentityId = borrowerId,
                initialLoanAmount = 250m,
                initialLoanNotes = "Test purpose"
            });
        var linkedResponse = await _client.SendAsync(linkedRelationshipRequest);
        Assert.Equal(HttpStatusCode.Created, linkedResponse.StatusCode);
        var linked = await linkedResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(linked.GetProperty("isSharedLedger").GetBoolean());
        // Shared initial loan is Pending until counterparty confirms — confirmed balance stays 0.
        Assert.Equal(0m, linked.GetProperty("currentBalance").GetDecimal());

        using var borrowedRequest = Authed(HttpMethod.Get, "/api/v1/personal/utang/relationships/borrowed", borrowerToken);
        var borrowedList = await _client.SendAsync(borrowedRequest);
        Assert.Equal(HttpStatusCode.OK, borrowedList.StatusCode);
        Assert.Equal(1, (await borrowedList.Content.ReadFromJsonAsync<JsonElement>()).GetArrayLength());
    }

    [Fact]
    public async Task Initial_loan_and_adjustment_require_purpose_note_payment_does_not()
    {
        var (token, userId) = await SeedPersonalUserAsync("note");

        using var contactRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/contacts",
            token,
            new { displayName = "Local Friend", phone = "+639170000099" });
        var contactResponse = await _client.SendAsync(contactRequest);
        Assert.Equal(HttpStatusCode.Created, contactResponse.StatusCode);
        var contactId = (await contactResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        using var missingNotesRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/relationships",
            token,
            new
            {
                creditorUserIdentityId = userId,
                debtorContactId = contactId,
                currencyCode = "PHP",
                initialLoanAmount = 100m,
                initialLoanNotes = "   "
            });
        var missingNotesResponse = await _client.SendAsync(missingNotesRequest);
        Assert.Equal(HttpStatusCode.BadRequest, missingNotesResponse.StatusCode);
        var missingBody = await missingNotesResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(
            "platform.personal.utang.notes.required",
            missingBody.GetProperty("errorCode").GetString());

        using var createRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/relationships",
            token,
            new
            {
                creditorUserIdentityId = userId,
                debtorContactId = contactId,
                currencyCode = "PHP",
                initialLoanAmount = 100m,
                initialLoanNotes = "School allowance"
            });
        var createResponse = await _client.SendAsync(createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var relationship = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var relationshipId = relationship.GetProperty("id").GetGuid();
        var version = relationship.GetProperty("version").GetInt32();

        using var paymentRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/entries",
            token,
            new { entryType = "Payment", amount = 10m, expectedVersion = version, notes = (string?)null });
        var paymentResponse = await _client.SendAsync(paymentRequest);
        Assert.Equal(HttpStatusCode.Created, paymentResponse.StatusCode);

        version = await AuthedBalanceVersionAsync(token, relationshipId);

        using var loanMissingRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/entries",
            token,
            new { entryType = "Loan", amount = 25m, expectedVersion = version, notes = "" });
        var loanMissingResponse = await _client.SendAsync(loanMissingRequest);
        Assert.Equal(HttpStatusCode.BadRequest, loanMissingResponse.StatusCode);
        Assert.Equal(
            "platform.personal.utang.notes.required",
            (await loanMissingResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errorCode").GetString());

        using var historyRequest = Authed(
            HttpMethod.Get,
            $"/api/v1/personal/utang/relationships/{relationshipId}/history",
            token);
        var historyResponse = await _client.SendAsync(historyRequest);
        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);
        var history = await historyResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(
            history.EnumerateArray(),
            e => e.GetProperty("entryType").GetString() == "Loan"
                 && e.GetProperty("notes").GetString() == "School allowance");
    }

    private async Task<int> AuthedBalanceVersionAsync(string token, Guid relationshipId)
    {
        using var balanceRequest = Authed(
            HttpMethod.Get,
            $"/api/v1/personal/utang/relationships/{relationshipId}/balance",
            token);
        var balanceResponse = await _client.SendAsync(balanceRequest);
        balanceResponse.EnsureSuccessStatusCode();
        return (await balanceResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("version").GetInt32();
    }

    [Fact]
    public async Task Shared_ledger_confirm_dispute_and_idempotent_retry()
    {
        var (lenderToken, lenderId) = await SeedPersonalUserAsync("shrl");
        var (borrowerToken, borrowerId) = await SeedPersonalUserAsync("shrb");
        var (strangerToken, _) = await SeedPersonalUserAsync("shrs");

        using var createRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/relationships",
            lenderToken,
            new
            {
                creditorUserIdentityId = lenderId,
                debtorUserIdentityId = borrowerId,
                currencyCode = "PHP"
            });
        var created = await contactResponse(await _client.SendAsync(createRequest));
        var relationshipId = created.GetProperty("id").GetGuid();
        Assert.Equal(0m, created.GetProperty("currentBalance").GetDecimal());

        using var loanRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/entries",
            lenderToken,
            new
            {
                entryType = "Loan",
                amount = 1000m,
                expectedVersion = created.GetProperty("version").GetInt32(),
                notes = "Groceries"
            });
        var loanResponse = await _client.SendAsync(loanRequest);
        Assert.Equal(HttpStatusCode.Created, loanResponse.StatusCode);
        var loan = await loanResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Pending", loan.GetProperty("status").GetString());
        Assert.Equal(0m, loan.GetProperty("balanceAfter").GetDecimal());
        var entryId = loan.GetProperty("id").GetGuid();

        using var balancePending = Authed(
            HttpMethod.Get,
            $"/api/v1/personal/utang/relationships/{relationshipId}/balance",
            lenderToken);
        Assert.Equal(0m, (await contactResponse(await _client.SendAsync(balancePending)))
            .GetProperty("currentBalance").GetDecimal());

        using var selfConfirm = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/entries/{entryId}/confirm",
            lenderToken,
            new { expectedVersion = (int?)null });
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(selfConfirm)).StatusCode);

        using var strangerConfirm = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/entries/{entryId}/confirm",
            strangerToken,
            new { });
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(strangerConfirm)).StatusCode);

        using var confirmRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/entries/{entryId}/confirm",
            borrowerToken,
            new { });
        var confirmed = await contactResponse(await _client.SendAsync(confirmRequest));
        Assert.Equal("Confirmed", confirmed.GetProperty("status").GetString());
        Assert.Equal(1000m, confirmed.GetProperty("balanceAfter").GetDecimal());

        using var confirmRetry = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/entries/{entryId}/confirm",
            borrowerToken,
            new { });
        var retry = await contactResponse(await _client.SendAsync(confirmRetry));
        Assert.Equal("Confirmed", retry.GetProperty("status").GetString());
        Assert.Equal(1000m, retry.GetProperty("balanceAfter").GetDecimal());

        using var balanceConfirmed = Authed(
            HttpMethod.Get,
            $"/api/v1/personal/utang/relationships/{relationshipId}/balance",
            borrowerToken);
        Assert.Equal(1000m, (await contactResponse(await _client.SendAsync(balanceConfirmed)))
            .GetProperty("currentBalance").GetDecimal());

        using var paymentRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/entries",
            borrowerToken,
            new { entryType = "Payment", amount = 300m });
        var payment = await contactResponse(await _client.SendAsync(paymentRequest));
        Assert.Equal("Pending", payment.GetProperty("status").GetString());
        var paymentId = payment.GetProperty("id").GetGuid();

        using var paymentConfirm = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/entries/{paymentId}/confirm",
            lenderToken,
            new { });
        Assert.Equal(700m, (await contactResponse(await _client.SendAsync(paymentConfirm)))
            .GetProperty("balanceAfter").GetDecimal());

        using var disputedLoanRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/entries",
            lenderToken,
            new { entryType = "Loan", amount = 500m, notes = "Test purpose" });
        var disputedLoan = await contactResponse(await _client.SendAsync(disputedLoanRequest));
        var disputedId = disputedLoan.GetProperty("id").GetGuid();

        using var disputeRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/entries/{disputedId}/dispute",
            borrowerToken,
            new { reason = "Amount is incorrect." });
        var disputed = await contactResponse(await _client.SendAsync(disputeRequest));
        Assert.Equal("Disputed", disputed.GetProperty("status").GetString());

        using var finalBalance = Authed(
            HttpMethod.Get,
            $"/api/v1/personal/utang/relationships/{relationshipId}/balance",
            lenderToken);
        Assert.Equal(700m, (await contactResponse(await _client.SendAsync(finalBalance)))
            .GetProperty("currentBalance").GetDecimal());
    }

    [Fact]
    public async Task Private_to_linked_preserves_balance_and_new_entries_pending()
    {
        var (ownerToken, ownerId) = await SeedPersonalUserAsync("p2lo");
        var (inviteeToken, _, inviteeEmail) = await SeedPersonalUserWithEmailAsync("p2li");

        using var contactRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/contacts",
            ownerToken,
            new { displayName = "Juan", email = inviteeEmail });
        var contact = await contactResponse(await _client.SendAsync(contactRequest));
        var contactId = contact.GetProperty("id").GetGuid();

        using var relationshipRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/relationships",
            ownerToken,
            new
            {
                creditorUserIdentityId = ownerId,
                debtorContactId = contactId,
                initialLoanAmount = 2000m,
                initialLoanNotes = "Test purpose"
            });
        var relationship = await contactResponse(await _client.SendAsync(relationshipRequest));
        var relationshipId = relationship.GetProperty("id").GetGuid();
        Assert.Equal(2000m, relationship.GetProperty("currentBalance").GetDecimal());
        Assert.True(relationship.GetProperty("isPrivate").GetBoolean());

        using var paymentRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/entries",
            ownerToken,
            new
            {
                entryType = "Payment",
                amount = 500m,
                expectedVersion = relationship.GetProperty("version").GetInt32()
            });
        await contactResponse(await _client.SendAsync(paymentRequest));

        using var inviteRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/invitations",
            ownerToken,
            new { inviteeContactId = contactId });
        var invitation = await contactResponse(await _client.SendAsync(inviteRequest));
        var acceptToken = invitation.GetProperty("acceptToken").GetString()!;

        using var acceptRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/invitations/accept",
            inviteeToken,
            new { token = acceptToken });
        var accept = await contactResponse(await _client.SendAsync(acceptRequest));
        Assert.Equal(relationshipId, accept.GetProperty("debtRelationshipId").GetGuid());

        using var detailRequest = Authed(
            HttpMethod.Get,
            $"/api/v1/personal/utang/relationships/{relationshipId}",
            ownerToken);
        var detail = await contactResponse(await _client.SendAsync(detailRequest));
        Assert.Equal(1500m, detail.GetProperty("currentBalance").GetDecimal());
        Assert.True(detail.GetProperty("isSharedLedger").GetBoolean());

        using var historyRequest = Authed(
            HttpMethod.Get,
            $"/api/v1/personal/utang/relationships/{relationshipId}/history",
            inviteeToken);
        var history = await contactResponse(await _client.SendAsync(historyRequest));
        Assert.Equal(2, history.GetArrayLength());
        Assert.All(history.EnumerateArray(), e => Assert.Equal("Confirmed", e.GetProperty("status").GetString()));

        using var postLinkLoan = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/entries",
            ownerToken,
            new
            {
                entryType = "Loan", amount = 400m, notes = "Test purpose",
                expectedVersion = detail.GetProperty("version").GetInt32()
            });
        var pendingLoan = await contactResponse(await _client.SendAsync(postLinkLoan));
        Assert.Equal("Pending", pendingLoan.GetProperty("status").GetString());

        using var balanceStill = Authed(
            HttpMethod.Get,
            $"/api/v1/personal/utang/relationships/{relationshipId}/balance",
            ownerToken);
        Assert.Equal(1500m, (await contactResponse(await _client.SendAsync(balanceStill)))
            .GetProperty("currentBalance").GetDecimal());

        using var confirmPostLink = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/entries/{pendingLoan.GetProperty("id").GetGuid()}/confirm",
            inviteeToken,
            new { });
        Assert.Equal(1900m, (await contactResponse(await _client.SendAsync(confirmPostLink)))
            .GetProperty("balanceAfter").GetDecimal());
    }

    [Fact]
    public async Task Add_by_exits_id_resolves_identity_without_auto_link_or_notification()
    {
        var (ownerToken, _) = await SeedPersonalUserAsync("adlnk");
        var (targetToken, targetId, _) = await SeedPersonalUserWithEmailAsync("tgtlk");

        // Same resolve path React People uses before POST /contacts.
        using var identityRequest = Authed(HttpMethod.Get, "/api/v1/me/public-identity", targetToken);
        var identity = await contactResponse(await _client.SendAsync(identityRequest));
        var publicUserId = identity.GetProperty("publicUserId").GetString()!;

        using var resolveRequest = Authed(
            HttpMethod.Post,
            "/api/v1/users/resolve-public-id",
            ownerToken,
            new { publicUserIdOrQrPayload = publicUserId, purpose = "utang-people" });
        var resolved = await contactResponse(await _client.SendAsync(resolveRequest));
        Assert.Equal(targetId, resolved.GetProperty("userIdentityId").GetGuid());
        Assert.Equal(publicUserId, resolved.GetProperty("publicUserId").GetString());

        using var createRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/contacts",
            ownerToken,
            new
            {
                displayName = "Should Be Overridden",
                linkedUserIdentityId = resolved.GetProperty("userIdentityId").GetGuid(),
                publicUserId = resolved.GetProperty("publicUserId").GetString()
            });
        var created = await contactResponse(await _client.SendAsync(createRequest));
        // Add-by-ExItS-ID resolves identity only — connection/link is a separate step.
        Assert.True(created.TryGetProperty("linkedUserIdentityId", out var linked)
            && linked.ValueKind is JsonValueKind.Null);
        Assert.Equal(targetId, created.GetProperty("resolvedUserIdentityId").GetGuid());
        Assert.Equal(publicUserId, created.GetProperty("resolvedPublicUserId").GetString());
        Assert.Equal(publicUserId, created.GetProperty("publicUserId").GetString());

        using var notificationsRequest = Authed(
            HttpMethod.Get,
            "/api/v1/personal/notifications",
            targetToken);
        var notifications = await contactResponse(await _client.SendAsync(notificationsRequest));
        Assert.DoesNotContain(
            notifications.EnumerateArray(),
            n => n.GetProperty("relatedType").GetString() == "personal_contact"
                 && n.GetProperty("title").GetString() == "Added to People");

        using var duplicateRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/contacts",
            ownerToken,
            new
            {
                displayName = "Duplicate",
                resolvedUserIdentityId = resolved.GetProperty("userIdentityId").GetGuid(),
                resolvedPublicUserId = resolved.GetProperty("publicUserId").GetString()
            });
        var duplicateResponse = await _client.SendAsync(duplicateRequest);
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
        var duplicateBody = await duplicateResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(
            ApplicationErrorCodes.PersonalContactIdentityConflict,
            duplicateBody.GetProperty("errorCode").GetString());
    }

    private async Task<(string Token, Guid UserId, string Email)> SeedPersonalUserWithEmailAsync(string prefix)
    {
        var (userId, email, password) = await PlatformIntegrationTestUsers.RegisterPersonalWithPasswordAsync(_client, prefix);
        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = email, password });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionToken").GetString()!;
        return (token, userId, email);
    }

    [Fact]
    public async Task Unrelated_user_cannot_read_relationship()
    {
        var (ownerToken, ownerId) = await SeedPersonalUserAsync("ownr");
        var (otherToken, _) = await SeedPersonalUserAsync("othr");
        _ = ownerId;

        using var contactRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/contacts",
            ownerToken,
            new { displayName = "Private Contact" });
        var contactId = (await contactResponse(await _client.SendAsync(contactRequest)))
            .GetProperty("id").GetGuid();

        using var relationshipRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/relationships",
            ownerToken,
            new
            {
                creditorUserIdentityId = ownerId,
                debtorContactId = contactId,
                initialLoanAmount = 100m,
                initialLoanNotes = "Test purpose"
            });
        var relationshipId = (await contactResponse(await _client.SendAsync(relationshipRequest)))
            .GetProperty("id").GetGuid();

        using var denied = Authed(
            HttpMethod.Get,
            $"/api/v1/personal/utang/relationships/{relationshipId}",
            otherToken);
        var response = await _client.SendAsync(denied);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ApplicationErrorCodes.PersonalUtangUnauthorized, body.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Stale_expected_version_returns_conflict()
    {
        var (token, userId) = await SeedPersonalUserAsync("conf");

        using var contactRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/contacts",
            token,
            new { displayName = "Conflict Contact" });
        var contactId = (await contactResponse(await _client.SendAsync(contactRequest)))
            .GetProperty("id").GetGuid();

        using var relationshipRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/relationships",
            token,
            new
            {
                creditorUserIdentityId = userId,
                debtorContactId = contactId,
                initialLoanAmount = 50m,
                initialLoanNotes = "Test purpose"
            });
        var relationship = await contactResponse(await _client.SendAsync(relationshipRequest));
        var relationshipId = relationship.GetProperty("id").GetGuid();
        var staleVersion = relationship.GetProperty("version").GetInt32() - 1;

        using var entryRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/entries",
            token,
            new
            {
                entryType = "Payment",
                amount = 10m,
                expectedVersion = staleVersion
            });
        var response = await _client.SendAsync(entryRequest);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ApplicationErrorCodes.ConcurrencyConflict, body.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Duplicate_active_contact_email_returns_conflict()
    {
        var (token, _) = await SeedPersonalUserAsync("emdup");

        using var first = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/contacts",
            token,
            new { displayName = "First", email = "twin@example.com" });
        Assert.Equal(HttpStatusCode.Created, (await _client.SendAsync(first)).StatusCode);

        using var second = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/contacts",
            token,
            new { displayName = "Second", email = "Twin@Example.com" });
        var response = await _client.SendAsync(second);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ApplicationErrorCodes.PersonalContactEmailConflict, body.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Create_relationship_with_linked_contact_canonicalizes_to_shared_user_participants()
    {
        var (ownerToken, ownerId) = await SeedPersonalUserAsync("canA");
        var (targetToken, targetId, _) = await SeedPersonalUserWithEmailAsync("canB");

        using var identityRequest = Authed(HttpMethod.Get, "/api/v1/me/public-identity", targetToken);
        var publicUserId = (await contactResponse(await _client.SendAsync(identityRequest)))
            .GetProperty("publicUserId").GetString()!;

        using var createContact = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/contacts",
            ownerToken,
            new { displayName = "Linked B", linkedUserIdentityId = targetId, publicUserId });
        var contactId = (await contactResponse(await _client.SendAsync(createContact)))
            .GetProperty("id").GetGuid();

        using var linkContact = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/contacts/{contactId}/link",
            ownerToken,
            new { linkedUserIdentityId = targetId, publicUserId });
        Assert.Equal(targetId, (await contactResponse(await _client.SendAsync(linkContact)))
            .GetProperty("linkedUserIdentityId").GetGuid());

        using var createRel = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/relationships",
            ownerToken,
            new
            {
                creditorUserIdentityId = ownerId,
                debtorContactId = contactId,
                initialLoanAmount = 1000m,
                initialLoanNotes = "Test purpose"
            });
        var rel = await contactResponse(await _client.SendAsync(createRel));
        Assert.True(rel.GetProperty("isSharedLedger").GetBoolean());
        Assert.Equal(ownerId, rel.GetProperty("creditorUserIdentityId").GetGuid());
        Assert.Equal(targetId, rel.GetProperty("debtorUserIdentityId").GetGuid());
        Assert.True(rel.TryGetProperty("debtorContactId", out var debtorContact)
            && debtorContact.ValueKind is JsonValueKind.Null);
        Assert.Equal(0m, rel.GetProperty("currentBalance").GetDecimal());

        var relationshipId = rel.GetProperty("id").GetGuid();
        using var historyAsTarget = Authed(
            HttpMethod.Get,
            $"/api/v1/personal/utang/relationships/{relationshipId}/history",
            targetToken);
        var history = await contactResponse(await _client.SendAsync(historyAsTarget));
        Assert.Contains(
            history.EnumerateArray(),
            e => e.GetProperty("status").GetString() == "Pending"
                 && e.GetProperty("amount").GetDecimal() == 1000m);
    }

    [Fact]
    public async Task I_borrowed_linked_contact_canonicalizes_creditor_to_linked_user()
    {
        var (ownerToken, ownerId) = await SeedPersonalUserAsync("borA");
        var (targetToken, targetId, _) = await SeedPersonalUserWithEmailAsync("borB");

        using var identityRequest = Authed(HttpMethod.Get, "/api/v1/me/public-identity", targetToken);
        var publicUserId = (await contactResponse(await _client.SendAsync(identityRequest)))
            .GetProperty("publicUserId").GetString()!;

        using var createContact = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/contacts",
            ownerToken,
            new { displayName = "Creditor B", linkedUserIdentityId = targetId, publicUserId });
        var contactId = (await contactResponse(await _client.SendAsync(createContact)))
            .GetProperty("id").GetGuid();

        using var linkContact = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/contacts/{contactId}/link",
            ownerToken,
            new { linkedUserIdentityId = targetId, publicUserId });
        Assert.Equal(targetId, (await contactResponse(await _client.SendAsync(linkContact)))
            .GetProperty("linkedUserIdentityId").GetGuid());

        using var createRel = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/relationships",
            ownerToken,
            new
            {
                creditorContactId = contactId,
                debtorUserIdentityId = ownerId,
                initialLoanAmount = 600m,
                initialLoanNotes = "Test purpose"
            });
        var rel = await contactResponse(await _client.SendAsync(createRel));
        Assert.True(rel.GetProperty("isSharedLedger").GetBoolean());
        Assert.Equal(targetId, rel.GetProperty("creditorUserIdentityId").GetGuid());
        Assert.Equal(ownerId, rel.GetProperty("debtorUserIdentityId").GetGuid());
    }

    [Fact]
    public async Task Link_existing_orphan_contact_promotes_private_relationship()
    {
        var (ownerToken, ownerId) = await SeedPersonalUserAsync("prmA");
        var (targetToken, targetId, _) = await SeedPersonalUserWithEmailAsync("prmB");

        using var orphanRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/contacts",
            ownerToken,
            new { displayName = "Orphan B" });
        var orphan = await contactResponse(await _client.SendAsync(orphanRequest));
        var contactId = orphan.GetProperty("id").GetGuid();
        Assert.True(orphan.TryGetProperty("linkedUserIdentityId", out var linkedBefore)
            && linkedBefore.ValueKind is JsonValueKind.Null);

        using var createRel = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/relationships",
            ownerToken,
            new
            {
                creditorUserIdentityId = ownerId,
                debtorContactId = contactId,
                initialLoanAmount = 2000m,
                initialLoanNotes = "Test purpose"
            });
        var relBefore = await contactResponse(await _client.SendAsync(createRel));
        var relationshipId = relBefore.GetProperty("id").GetGuid();
        Assert.False(relBefore.GetProperty("isSharedLedger").GetBoolean());
        Assert.Equal(2000m, relBefore.GetProperty("currentBalance").GetDecimal());

        using var payRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/entries",
            ownerToken,
            new { entryType = "Payment", amount = 500m, expectedVersion = relBefore.GetProperty("version").GetInt32() });
        (await _client.SendAsync(payRequest)).EnsureSuccessStatusCode();

        using var balBefore = Authed(
            HttpMethod.Get,
            $"/api/v1/personal/utang/relationships/{relationshipId}/balance",
            ownerToken);
        Assert.Equal(1500m, (await contactResponse(await _client.SendAsync(balBefore)))
            .GetProperty("currentBalance").GetDecimal());

        using var identityRequest = Authed(HttpMethod.Get, "/api/v1/me/public-identity", targetToken);
        var publicUserId = (await contactResponse(await _client.SendAsync(identityRequest)))
            .GetProperty("publicUserId").GetString()!;

        using var linkRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/contacts/{contactId}/link",
            ownerToken,
            new { linkedUserIdentityId = targetId, publicUserId });
        var linked = await contactResponse(await _client.SendAsync(linkRequest));
        Assert.Equal(contactId, linked.GetProperty("id").GetGuid());
        Assert.Equal(targetId, linked.GetProperty("linkedUserIdentityId").GetGuid());
        Assert.Equal(publicUserId, linked.GetProperty("publicUserId").GetString());

        using var listRequest = Authed(HttpMethod.Get, "/api/v1/personal/utang/contacts", ownerToken);
        var list = await contactResponse(await _client.SendAsync(listRequest));
        Assert.Equal(1, list.GetArrayLength());

        using var relAfterRequest = Authed(
            HttpMethod.Get,
            $"/api/v1/personal/utang/relationships/{relationshipId}",
            ownerToken);
        var relAfter = await contactResponse(await _client.SendAsync(relAfterRequest));
        Assert.Equal(relationshipId, relAfter.GetProperty("id").GetGuid());
        Assert.True(relAfter.GetProperty("isSharedLedger").GetBoolean());
        Assert.Equal(targetId, relAfter.GetProperty("debtorUserIdentityId").GetGuid());
        Assert.Equal(1500m, relAfter.GetProperty("currentBalance").GetDecimal());

        using var newLoan = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/entries",
            ownerToken,
            new { entryType = "Loan", amount = 400m, notes = "Test purpose", expectedVersion = relAfter.GetProperty("version").GetInt32() });
        var pending = await contactResponse(await _client.SendAsync(newLoan));
        Assert.Equal("Pending", pending.GetProperty("status").GetString());

        using var balPending = Authed(
            HttpMethod.Get,
            $"/api/v1/personal/utang/relationships/{relationshipId}/balance",
            ownerToken);
        Assert.Equal(1500m, (await contactResponse(await _client.SendAsync(balPending)))
            .GetProperty("currentBalance").GetDecimal());

        using var relForTarget = Authed(
            HttpMethod.Get,
            $"/api/v1/personal/utang/relationships/{relationshipId}",
            targetToken);
        var targetVersion = (await contactResponse(await _client.SendAsync(relForTarget)))
            .GetProperty("version").GetInt32();
        using var confirmOk = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/entries/{pending.GetProperty("id").GetGuid()}/confirm",
            targetToken,
            new { expectedVersion = targetVersion });
        (await _client.SendAsync(confirmOk)).EnsureSuccessStatusCode();

        using var balFinal = Authed(
            HttpMethod.Get,
            $"/api/v1/personal/utang/relationships/{relationshipId}/balance",
            ownerToken);
        Assert.Equal(1900m, (await contactResponse(await _client.SendAsync(balFinal)))
            .GetProperty("currentBalance").GetDecimal());
    }

    private static async Task<JsonElement> contactResponse(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}
