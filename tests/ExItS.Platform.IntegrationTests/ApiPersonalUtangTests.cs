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
        // Owner-model: shared initial loan is Confirmed immediately.
        Assert.Equal(250m, linked.GetProperty("currentBalance").GetDecimal());
        Assert.True(linked.GetProperty("isLedgerOwner").GetBoolean());

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
    public async Task Shared_ledger_owner_writes_confirmed_and_non_owner_mutations_denied()
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
        Assert.True(created.GetProperty("isLedgerOwner").GetBoolean());
        Assert.Equal(lenderId, created.GetProperty("ledgerOwnerUserIdentityId").GetGuid());

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
        Assert.Equal("Confirmed", loan.GetProperty("status").GetString());
        Assert.Equal(1000m, loan.GetProperty("balanceAfter").GetDecimal());
        var entryId = loan.GetProperty("id").GetGuid();

        using var balanceConfirmed = Authed(
            HttpMethod.Get,
            $"/api/v1/personal/utang/relationships/{relationshipId}/balance",
            lenderToken);
        Assert.Equal(1000m, (await contactResponse(await _client.SendAsync(balanceConfirmed)))
            .GetProperty("currentBalance").GetDecimal());

        // Confirm on already-Confirmed is idempotent for counterparty.
        using var confirmRetry = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/entries/{entryId}/confirm",
            borrowerToken,
            new { });
        var retry = await contactResponse(await _client.SendAsync(confirmRetry));
        Assert.Equal("Confirmed", retry.GetProperty("status").GetString());
        Assert.Equal(1000m, retry.GetProperty("balanceAfter").GetDecimal());

        // Non-owner (borrower) cannot record payment.
        using var paymentDenied = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/entries",
            borrowerToken,
            new { entryType = "Payment", amount = 300m });
        var denied = await _client.SendAsync(paymentDenied);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal(
            ApplicationErrorCodes.PersonalUtangNotLedgerOwner,
            (await denied.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errorCode").GetString());

        // Stranger cannot record either.
        using var strangerEntry = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/entries",
            strangerToken,
            new { entryType = "Loan", amount = 10m, notes = "Nope" });
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(strangerEntry)).StatusCode);

        // Owner payment confirms immediately.
        using var paymentRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/entries",
            lenderToken,
            new { entryType = "Payment", amount = 300m });
        var payment = await contactResponse(await _client.SendAsync(paymentRequest));
        Assert.Equal("Confirmed", payment.GetProperty("status").GetString());
        Assert.Equal(700m, payment.GetProperty("balanceAfter").GetDecimal());

        // Dispute on Confirmed is rejected (legacy Pending-only path).
        using var disputeRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/entries/{entryId}/dispute",
            borrowerToken,
            new { reason = "Amount is incorrect." });
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.SendAsync(disputeRequest)).StatusCode);

        using var finalBalance = Authed(
            HttpMethod.Get,
            $"/api/v1/personal/utang/relationships/{relationshipId}/balance",
            lenderToken);
        Assert.Equal(700m, (await contactResponse(await _client.SendAsync(finalBalance)))
            .GetProperty("currentBalance").GetDecimal());
    }

    [Fact]
    public async Task Private_to_linked_preserves_balance_and_new_entries_confirmed()
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
        Assert.True(detail.GetProperty("isLedgerOwner").GetBoolean());

        using var historyRequest = Authed(
            HttpMethod.Get,
            $"/api/v1/personal/utang/relationships/{relationshipId}/history",
            inviteeToken);
        var history = await contactResponse(await _client.SendAsync(historyRequest));
        Assert.Equal(2, history.GetArrayLength());
        Assert.All(history.EnumerateArray(), e => Assert.Equal("Confirmed", e.GetProperty("status").GetString()));

        using var inviteeDetail = Authed(
            HttpMethod.Get,
            $"/api/v1/personal/utang/relationships/{relationshipId}",
            inviteeToken);
        var inviteeView = await contactResponse(await _client.SendAsync(inviteeDetail));
        Assert.False(inviteeView.GetProperty("isLedgerOwner").GetBoolean());

        using var postLinkLoan = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/entries",
            ownerToken,
            new
            {
                entryType = "Loan", amount = 400m, notes = "Test purpose",
                expectedVersion = detail.GetProperty("version").GetInt32()
            });
        var confirmedLoan = await contactResponse(await _client.SendAsync(postLinkLoan));
        Assert.Equal("Confirmed", confirmedLoan.GetProperty("status").GetString());
        Assert.Equal(1900m, confirmedLoan.GetProperty("balanceAfter").GetDecimal());

        using var balanceAfter = Authed(
            HttpMethod.Get,
            $"/api/v1/personal/utang/relationships/{relationshipId}/balance",
            ownerToken);
        Assert.Equal(1900m, (await contactResponse(await _client.SendAsync(balanceAfter)))
            .GetProperty("currentBalance").GetDecimal());
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
                initialLoanNotes = "Test purpose",
                shareWithCounterparty = true
            });
        var rel = await contactResponse(await _client.SendAsync(createRel));
        Assert.True(rel.GetProperty("isSharedLedger").GetBoolean());
        Assert.Equal("Shared", rel.GetProperty("shareOutcome").GetString());
        Assert.Equal(ownerId, rel.GetProperty("creditorUserIdentityId").GetGuid());
        Assert.Equal(targetId, rel.GetProperty("debtorUserIdentityId").GetGuid());
        Assert.Equal(ownerId, rel.GetProperty("ledgerOwnerUserIdentityId").GetGuid());
        Assert.True(rel.GetProperty("isLedgerOwner").GetBoolean());
        Assert.True(rel.TryGetProperty("debtorContactId", out var debtorContact)
            && debtorContact.ValueKind is JsonValueKind.Null);
        // Owner-model: Confirmed immediately — currentBalance is live.
        Assert.Equal(1000m, rel.GetProperty("currentBalance").GetDecimal());

        var relationshipId = rel.GetProperty("id").GetGuid();
        using var historyAsTarget = Authed(
            HttpMethod.Get,
            $"/api/v1/personal/utang/relationships/{relationshipId}/history",
            targetToken);
        var history = await contactResponse(await _client.SendAsync(historyAsTarget));
        Assert.Contains(
            history.EnumerateArray(),
            e => e.GetProperty("status").GetString() == "Confirmed"
                 && e.GetProperty("amount").GetDecimal() == 1000m);

        using var asTarget = Authed(
            HttpMethod.Get,
            $"/api/v1/personal/utang/relationships/{relationshipId}",
            targetToken);
        var targetView = await contactResponse(await _client.SendAsync(asTarget));
        Assert.False(targetView.GetProperty("isLedgerOwner").GetBoolean());
        Assert.Equal(ownerId, targetView.GetProperty("ledgerOwnerUserIdentityId").GetGuid());

        using var listLent = Authed(HttpMethod.Get, "/api/v1/personal/utang/relationships/lent", ownerToken);
        var lentList = await contactResponse(await _client.SendAsync(listLent));
        var lentRow = lentList.EnumerateArray().Single(r => r.GetProperty("id").GetGuid() == relationshipId);
        Assert.Equal(1000m, lentRow.GetProperty("currentBalance").GetDecimal());

        using var getRel = Authed(
            HttpMethod.Get,
            $"/api/v1/personal/utang/relationships/{relationshipId}",
            ownerToken);
        Assert.Equal(1000m, (await contactResponse(await _client.SendAsync(getRel)))
            .GetProperty("currentBalance").GetDecimal());

        using var ownerDash = Authed(HttpMethod.Get, "/api/v1/personal/dashboard", ownerToken);
        var ownerDashBody = await contactResponse(await _client.SendAsync(ownerDash));
        Assert.Equal(1000m, ownerDashBody.GetProperty("totalLentBalance").GetDecimal());
        Assert.Equal(0m, ownerDashBody.GetProperty("totalBorrowedBalance").GetDecimal());
        Assert.Equal(0, ownerDashBody.GetProperty("pendingConfirmationCount").GetInt32());

        using var targetDash = Authed(HttpMethod.Get, "/api/v1/personal/dashboard", targetToken);
        var targetDashBody = await contactResponse(await _client.SendAsync(targetDash));
        Assert.Equal(0m, targetDashBody.GetProperty("totalLentBalance").GetDecimal());
        // Shared-with-me must not inflate My Records money totals.
        Assert.Equal(0m, targetDashBody.GetProperty("totalBorrowedBalance").GetDecimal());
        Assert.Equal(1000m, targetDashBody.GetProperty("sharedWithMeBorrowedBalance").GetDecimal());
        Assert.Equal(1, targetDashBody.GetProperty("sharedWithMeActiveCount").GetInt32());
        Assert.Equal(0, targetDashBody.GetProperty("pendingConfirmationCount").GetInt32());
    }

    [Fact]
    public async Task Connected_contact_share_off_stays_private_owner_only()
    {
        var (ownerToken, ownerId) = await SeedPersonalUserAsync("prvA");
        var (targetToken, targetId, _) = await SeedPersonalUserWithEmailAsync("prvB");

        using var identityRequest = Authed(HttpMethod.Get, "/api/v1/me/public-identity", targetToken);
        var publicUserId = (await contactResponse(await _client.SendAsync(identityRequest)))
            .GetProperty("publicUserId").GetString()!;

        using var createContact = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/contacts",
            ownerToken,
            new { displayName = "Private B", linkedUserIdentityId = targetId, publicUserId });
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
                initialLoanNotes = "Private purpose",
                shareWithCounterparty = false
            });
        var rel = await contactResponse(await _client.SendAsync(createRel));
        Assert.False(rel.GetProperty("isSharedLedger").GetBoolean());
        Assert.True(rel.GetProperty("isPrivate").GetBoolean());
        Assert.Equal(1000m, rel.GetProperty("currentBalance").GetDecimal());
        Assert.Equal(contactId, rel.GetProperty("debtorContactId").GetGuid());
        Assert.True(rel.TryGetProperty("debtorUserIdentityId", out var debtorUser)
            && debtorUser.ValueKind is JsonValueKind.Null);

        var relationshipId = rel.GetProperty("id").GetGuid();
        using var asTarget = Authed(
            HttpMethod.Get,
            $"/api/v1/personal/utang/relationships/{relationshipId}",
            targetToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(asTarget)).StatusCode);

        using var borrowedAsTarget = Authed(
            HttpMethod.Get,
            "/api/v1/personal/utang/relationships/borrowed",
            targetToken);
        var borrowed = await contactResponse(await _client.SendAsync(borrowedAsTarget));
        Assert.DoesNotContain(
            borrowed.EnumerateArray(),
            e => e.GetProperty("id").GetGuid() == relationshipId);
    }

    [Fact]
    public async Task Share_on_with_receive_off_saves_privately()
    {
        var (ownerToken, ownerId) = await SeedPersonalUserAsync("rcvA");
        var (targetToken, targetId, _) = await SeedPersonalUserWithEmailAsync("rcvB");

        using var identityRequest = Authed(HttpMethod.Get, "/api/v1/me/public-identity", targetToken);
        var publicUserId = (await contactResponse(await _client.SendAsync(identityRequest)))
            .GetProperty("publicUserId").GetString()!;

        using var createContact = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/contacts",
            ownerToken,
            new { displayName = "Recv B", linkedUserIdentityId = targetId, publicUserId });
        var contactId = (await contactResponse(await _client.SendAsync(createContact)))
            .GetProperty("id").GetGuid();

        using var linkContact = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/contacts/{contactId}/link",
            ownerToken,
            new { linkedUserIdentityId = targetId, publicUserId });
        (await _client.SendAsync(linkContact)).EnsureSuccessStatusCode();

        using var ownerIdentity = Authed(HttpMethod.Get, "/api/v1/me/public-identity", ownerToken);
        var ownerPublicUserId = (await contactResponse(await _client.SendAsync(ownerIdentity)))
            .GetProperty("publicUserId").GetString()!;
        using var reverseContact = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/contacts",
            targetToken,
            new { displayName = "Recv A", linkedUserIdentityId = ownerId, publicUserId = ownerPublicUserId });
        var reverseContactId = (await contactResponse(await _client.SendAsync(reverseContact)))
            .GetProperty("id").GetGuid();
        using var reverseLink = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/contacts/{reverseContactId}/link",
            targetToken,
            new { linkedUserIdentityId = ownerId, publicUserId = ownerPublicUserId });
        (await _client.SendAsync(reverseLink)).EnsureSuccessStatusCode();

        // Recipient turns receive off for the owner (counterparty = sender).
        using var prefsPut = Authed(
            HttpMethod.Put,
            $"/api/v1/personal/shared-utang-preferences/{ownerId}",
            targetToken,
            new
            {
                receiveSharedUtang = false,
                autoAcceptSharedUtang = false,
                sharedUtangNotifications = true
            });
        (await _client.SendAsync(prefsPut)).EnsureSuccessStatusCode();

        using var createRel = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/relationships",
            ownerToken,
            new
            {
                creditorUserIdentityId = ownerId,
                debtorContactId = contactId,
                initialLoanAmount = 750m,
                initialLoanNotes = "Wanted to share",
                shareWithCounterparty = true
            });
        var rel = await contactResponse(await _client.SendAsync(createRel));
        Assert.False(rel.GetProperty("isSharedLedger").GetBoolean());
        Assert.Equal("PrivateNotReceiving", rel.GetProperty("shareOutcome").GetString());
        Assert.Equal(750m, rel.GetProperty("currentBalance").GetDecimal());

        var relationshipId = rel.GetProperty("id").GetGuid();
        using var asTarget = Authed(
            HttpMethod.Get,
            $"/api/v1/personal/utang/relationships/{relationshipId}",
            targetToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(asTarget)).StatusCode);
    }

    [Fact]
    public async Task Share_on_confirms_immediately_and_ignores_auto_accept_preference()
    {
        var (ownerToken, ownerId) = await SeedPersonalUserAsync("autA");
        var (targetToken, targetId, _) = await SeedPersonalUserWithEmailAsync("autB");

        using var identityRequest = Authed(HttpMethod.Get, "/api/v1/me/public-identity", targetToken);
        var publicUserId = (await contactResponse(await _client.SendAsync(identityRequest)))
            .GetProperty("publicUserId").GetString()!;

        using var createContact = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/contacts",
            ownerToken,
            new { displayName = "Auto B", linkedUserIdentityId = targetId, publicUserId });
        var contactId = (await contactResponse(await _client.SendAsync(createContact)))
            .GetProperty("id").GetGuid();

        using var linkContact = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/contacts/{contactId}/link",
            ownerToken,
            new { linkedUserIdentityId = targetId, publicUserId });
        (await _client.SendAsync(linkContact)).EnsureSuccessStatusCode();

        using var ownerIdentity = Authed(HttpMethod.Get, "/api/v1/me/public-identity", ownerToken);
        var ownerPublicUserId = (await contactResponse(await _client.SendAsync(ownerIdentity)))
            .GetProperty("publicUserId").GetString()!;
        using var reverseContact = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/contacts",
            targetToken,
            new { displayName = "Auto A", linkedUserIdentityId = ownerId, publicUserId = ownerPublicUserId });
        var reverseContactId = (await contactResponse(await _client.SendAsync(reverseContact)))
            .GetProperty("id").GetGuid();
        using var reverseLink = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/contacts/{reverseContactId}/link",
            targetToken,
            new { linkedUserIdentityId = ownerId, publicUserId = ownerPublicUserId });
        (await _client.SendAsync(reverseLink)).EnsureSuccessStatusCode();

        // AutoAccept is accepted for compatibility but forced false / ignored for new entries.
        using var prefsPut = Authed(
            HttpMethod.Put,
            $"/api/v1/personal/shared-utang-preferences/{ownerId}",
            targetToken,
            new
            {
                receiveSharedUtang = true,
                autoAcceptSharedUtang = true,
                sharedUtangNotifications = true
            });
        var prefsBody = await contactResponse(await _client.SendAsync(prefsPut));
        Assert.False(prefsBody.GetProperty("autoAcceptSharedUtang").GetBoolean());

        using var createRel = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/relationships",
            ownerToken,
            new
            {
                creditorUserIdentityId = ownerId,
                debtorContactId = contactId,
                initialLoanAmount = 500m,
                initialLoanNotes = "Trusted share",
                shareWithCounterparty = true
            });
        var rel = await contactResponse(await _client.SendAsync(createRel));
        Assert.True(rel.GetProperty("isSharedLedger").GetBoolean());
        Assert.Equal("Shared", rel.GetProperty("shareOutcome").GetString());
        Assert.Equal(500m, rel.GetProperty("currentBalance").GetDecimal());

        var relationshipId = rel.GetProperty("id").GetGuid();
        using var historyAsTarget = Authed(
            HttpMethod.Get,
            $"/api/v1/personal/utang/relationships/{relationshipId}/history",
            targetToken);
        var history = await contactResponse(await _client.SendAsync(historyAsTarget));
        var entry = Assert.Single(history.EnumerateArray());
        Assert.Equal("Confirmed", entry.GetProperty("status").GetString());
        Assert.False(entry.GetProperty("wasAutoSynced").GetBoolean());
    }

    [Fact]
    public async Task Non_owner_cannot_record_entry_on_shared_relationship()
    {
        var (ownerToken, ownerId) = await SeedPersonalUserAsync("nowA");
        var (targetToken, targetId, _) = await SeedPersonalUserWithEmailAsync("nowB");

        using var identityRequest = Authed(HttpMethod.Get, "/api/v1/me/public-identity", targetToken);
        var publicUserId = (await contactResponse(await _client.SendAsync(identityRequest)))
            .GetProperty("publicUserId").GetString()!;

        using var createContact = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/contacts",
            ownerToken,
            new { displayName = "NonOwner B", linkedUserIdentityId = targetId, publicUserId });
        var contactId = (await contactResponse(await _client.SendAsync(createContact)))
            .GetProperty("id").GetGuid();

        using var linkContact = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/contacts/{contactId}/link",
            ownerToken,
            new { linkedUserIdentityId = targetId, publicUserId });
        (await _client.SendAsync(linkContact)).EnsureSuccessStatusCode();

        using var createRel = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/relationships",
            ownerToken,
            new
            {
                creditorUserIdentityId = ownerId,
                debtorContactId = contactId,
                initialLoanAmount = 250m,
                initialLoanNotes = "Owner writes",
                shareWithCounterparty = true
            });
        var rel = await contactResponse(await _client.SendAsync(createRel));
        var relationshipId = rel.GetProperty("id").GetGuid();
        Assert.True(rel.GetProperty("isLedgerOwner").GetBoolean());

        using var denied = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/entries",
            targetToken,
            new
            {
                entryType = "Payment",
                amount = 50m,
                notes = "Counterparty attempt",
                expectedVersion = rel.GetProperty("version").GetInt32()
            });
        var response = await _client.SendAsync(denied);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(
            ApplicationErrorCodes.PersonalUtangNotLedgerOwner,
            body.GetProperty("errorCode").GetString());
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
                initialLoanNotes = "Test purpose",
                shareWithCounterparty = true
            });
        var rel = await contactResponse(await _client.SendAsync(createRel));
        Assert.True(rel.GetProperty("isSharedLedger").GetBoolean());
        Assert.Equal(targetId, rel.GetProperty("creditorUserIdentityId").GetGuid());
        Assert.Equal(ownerId, rel.GetProperty("debtorUserIdentityId").GetGuid());
    }

    [Fact]
    public async Task Link_existing_orphan_contact_keeps_private_relationship_private()
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

        using var identityRequest = Authed(HttpMethod.Get, "/api/v1/me/public-identity", targetToken);
        var publicUserId = (await contactResponse(await _client.SendAsync(identityRequest)))
            .GetProperty("publicUserId").GetString()!;

        using var linkRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/contacts/{contactId}/link",
            ownerToken,
            new { linkedUserIdentityId = targetId, publicUserId });
        var linked = await contactResponse(await _client.SendAsync(linkRequest));
        Assert.Equal(targetId, linked.GetProperty("linkedUserIdentityId").GetGuid());

        using var relAfterRequest = Authed(
            HttpMethod.Get,
            $"/api/v1/personal/utang/relationships/{relationshipId}",
            ownerToken);
        var relAfter = await contactResponse(await _client.SendAsync(relAfterRequest));
        Assert.Equal(relationshipId, relAfter.GetProperty("id").GetGuid());
        Assert.False(relAfter.GetProperty("isSharedLedger").GetBoolean());
        Assert.Equal(contactId, relAfter.GetProperty("debtorContactId").GetGuid());
        Assert.Equal(1500m, relAfter.GetProperty("currentBalance").GetDecimal());

        using var asTarget = Authed(
            HttpMethod.Get,
            $"/api/v1/personal/utang/relationships/{relationshipId}",
            targetToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(asTarget)).StatusCode);
    }

    [Fact]
    public async Task Personal_utang_client_ids_are_idempotent_across_replay()
    {
        var (token, userId) = await SeedPersonalUserAsync("idem");
        var contactId = Guid.NewGuid();
        var relationshipId = Guid.NewGuid();
        var loanEntryId = Guid.NewGuid();
        var paymentEntryId = Guid.NewGuid();

        using var contactRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/contacts",
            token,
            new { contactId, displayName = "Idem Friend", phone = "+639170000099" });
        var contactCreated = await _client.SendAsync(contactRequest);
        Assert.Equal(HttpStatusCode.Created, contactCreated.StatusCode);
        Assert.Equal(contactId, (await contactCreated.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid());

        using var contactReplay = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/contacts",
            token,
            new { contactId, displayName = "Idem Friend", phone = "+639170000099" });
        var contactReplayResponse = await _client.SendAsync(contactReplay);
        Assert.Equal(HttpStatusCode.Created, contactReplayResponse.StatusCode);
        Assert.Equal(contactId, (await contactReplayResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid());

        using var contactConflict = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/contacts",
            token,
            new { contactId, displayName = "Different Name", phone = "+639170000099" });
        var contactConflictResponse = await _client.SendAsync(contactConflict);
        Assert.Equal(HttpStatusCode.Conflict, contactConflictResponse.StatusCode);

        using var getContact = Authed(HttpMethod.Get, $"/api/v1/personal/utang/contacts/{contactId}", token);
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(getContact)).StatusCode);

        using var relationshipRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/relationships",
            token,
            new
            {
                relationshipId,
                initialLoanEntryId = loanEntryId,
                creditorUserIdentityId = userId,
                debtorContactId = contactId,
                currencyCode = "PHP",
                initialLoanAmount = 500m,
                initialLoanNotes = "idem loan"
            });
        var relationshipCreated = await _client.SendAsync(relationshipRequest);
        Assert.Equal(HttpStatusCode.Created, relationshipCreated.StatusCode);
        var relationship = await relationshipCreated.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(relationshipId, relationship.GetProperty("id").GetGuid());
        Assert.Equal(500m, relationship.GetProperty("currentBalance").GetDecimal());

        using var relationshipReplay = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/relationships",
            token,
            new
            {
                relationshipId,
                initialLoanEntryId = loanEntryId,
                creditorUserIdentityId = userId,
                debtorContactId = contactId,
                currencyCode = "PHP",
                initialLoanAmount = 500m,
                initialLoanNotes = "idem loan"
            });
        var relationshipReplayResponse = await _client.SendAsync(relationshipReplay);
        Assert.Equal(HttpStatusCode.Created, relationshipReplayResponse.StatusCode);
        Assert.Equal(
            500m,
            (await relationshipReplayResponse.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("currentBalance")
                .GetDecimal());

        using var relationshipConflict = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/relationships",
            token,
            new
            {
                relationshipId,
                initialLoanEntryId = loanEntryId,
                creditorUserIdentityId = userId,
                debtorContactId = contactId,
                currencyCode = "PHP",
                initialLoanAmount = 999m,
                initialLoanNotes = "different"
            });
        Assert.Equal(HttpStatusCode.Conflict, (await _client.SendAsync(relationshipConflict)).StatusCode);

        using var paymentRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/entries",
            token,
            new
            {
                entryId = paymentEntryId,
                entryType = "Payment",
                amount = 100m,
                notes = "partial"
            });
        var paymentCreated = await _client.SendAsync(paymentRequest);
        Assert.Equal(HttpStatusCode.Created, paymentCreated.StatusCode);
        Assert.Equal(paymentEntryId, (await paymentCreated.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid());

        using var paymentReplay = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/entries",
            token,
            new
            {
                entryId = paymentEntryId,
                entryType = "Payment",
                amount = 100m,
                notes = "partial"
            });
        var paymentReplayResponse = await _client.SendAsync(paymentReplay);
        Assert.Equal(HttpStatusCode.Created, paymentReplayResponse.StatusCode);
        Assert.Equal(
            paymentEntryId,
            (await paymentReplayResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid());

        using var paymentConflict = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/entries",
            token,
            new
            {
                entryId = paymentEntryId,
                entryType = "Payment",
                amount = 250m,
                notes = "different"
            });
        Assert.Equal(HttpStatusCode.Conflict, (await _client.SendAsync(paymentConflict)).StatusCode);

        using var getEntry = Authed(HttpMethod.Get, $"/api/v1/personal/utang/entries/{paymentEntryId}", token);
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(getEntry)).StatusCode);

        using var getLoanEntry = Authed(HttpMethod.Get, $"/api/v1/personal/utang/entries/{loanEntryId}", token);
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(getLoanEntry)).StatusCode);

        using var historyRequest = Authed(
            HttpMethod.Get,
            $"/api/v1/personal/utang/relationships/{relationshipId}/history",
            token);
        var historyResponse = await _client.SendAsync(historyRequest);
        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);
        var history = await historyResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, history.GetArrayLength());

        var (otherToken, _) = await SeedPersonalUserAsync("idemx");
        using var otherGet = Authed(HttpMethod.Get, $"/api/v1/personal/utang/entries/{paymentEntryId}", otherToken);
        var otherStatus = (await _client.SendAsync(otherGet)).StatusCode;
        Assert.NotEqual(HttpStatusCode.OK, otherStatus);
    }

    [Fact]
    public async Task Connected_pair_may_own_independent_ledgers_with_same_direction()
    {
        var (micaTok, micaId) = await SeedPersonalUserAsync("indm");
        var (paulTok, paulId) = await SeedPersonalUserAsync("indp");

        // Mica creates I Lent to Paul (shared).
        using var micaCreate = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/relationships",
            micaTok,
            new
            {
                creditorUserIdentityId = micaId,
                debtorUserIdentityId = paulId,
                currencyCode = "PHP",
                initialLoanAmount = 1000m,
                initialLoanNotes = "Mica book",
                shareWithCounterparty = true
            });
        var micaRel = await contactResponse(await _client.SendAsync(micaCreate));
        var micaRelId = micaRel.GetProperty("id").GetGuid();
        Assert.Equal(micaId, micaRel.GetProperty("ledgerOwnerUserIdentityId").GetGuid());
        Assert.True(micaRel.GetProperty("isLedgerOwner").GetBoolean());
        Assert.Equal(1000m, micaRel.GetProperty("currentBalance").GetDecimal());

        using var paulViewMica = Authed(
            HttpMethod.Get,
            $"/api/v1/personal/utang/relationships/{micaRelId}",
            paulTok);
        var paulSeesMica = await contactResponse(await _client.SendAsync(paulViewMica));
        Assert.Equal("Borrowed", paulSeesMica.GetProperty("perspective").GetString());
        Assert.False(paulSeesMica.GetProperty("isLedgerOwner").GetBoolean());

        // Paul creates his own I Borrowed from Mica (same direction, different owner).
        using var paulCreate = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/relationships",
            paulTok,
            new
            {
                creditorUserIdentityId = micaId,
                debtorUserIdentityId = paulId,
                currencyCode = "PHP",
                initialLoanAmount = 1000m,
                initialLoanNotes = "Paul book",
                shareWithCounterparty = true
            });
        var paulRel = await contactResponse(await _client.SendAsync(paulCreate));
        var paulRelId = paulRel.GetProperty("id").GetGuid();
        Assert.NotEqual(micaRelId, paulRelId);
        Assert.Equal(paulId, paulRel.GetProperty("ledgerOwnerUserIdentityId").GetGuid());
        Assert.True(paulRel.GetProperty("isLedgerOwner").GetBoolean());
        Assert.Equal("Borrowed", paulRel.GetProperty("perspective").GetString());
        Assert.Equal(1000m, paulRel.GetProperty("currentBalance").GetDecimal());

        // Cross-owner mutations denied.
        using var paulPayMica = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{micaRelId}/entries",
            paulTok,
            new { entryType = "Payment", amount = 200m });
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(paulPayMica)).StatusCode);

        using var micaPayPaul = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{paulRelId}/entries",
            micaTok,
            new { entryType = "Payment", amount = 200m });
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(micaPayPaul)).StatusCode);

        // Each owner pays only their own ledger; the other stays unchanged.
        using var micaPayOwn = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{micaRelId}/entries",
            micaTok,
            new { entryType = "Payment", amount = 200m, expectedVersion = micaRel.GetProperty("version").GetInt32() });
        await contactResponse(await _client.SendAsync(micaPayOwn));
        using var micaBal = Authed(HttpMethod.Get, $"/api/v1/personal/utang/relationships/{micaRelId}/balance", micaTok);
        Assert.Equal(800m, (await contactResponse(await _client.SendAsync(micaBal))).GetProperty("currentBalance").GetDecimal());
        using var paulBal = Authed(HttpMethod.Get, $"/api/v1/personal/utang/relationships/{paulRelId}/balance", paulTok);
        Assert.Equal(1000m, (await contactResponse(await _client.SendAsync(paulBal))).GetProperty("currentBalance").GetDecimal());

        // Both appear on Paul's borrowed list; ownership flags differ.
        using var paulList = Authed(HttpMethod.Get, "/api/v1/personal/utang/relationships/borrowed", paulTok);
        var borrowed = await contactResponse(await _client.SendAsync(paulList));
        var micaRow = borrowed.EnumerateArray().Single(r => r.GetProperty("id").GetGuid() == micaRelId);
        var paulRow = borrowed.EnumerateArray().Single(r => r.GetProperty("id").GetGuid() == paulRelId);
        Assert.False(micaRow.GetProperty("isLedgerOwner").GetBoolean());
        Assert.True(paulRow.GetProperty("isLedgerOwner").GetBoolean());

        // Dashboard: My Records for Paul = only Paul's ledger; shared-with-me is separate.
        using var paulDash = Authed(HttpMethod.Get, "/api/v1/personal/dashboard", paulTok);
        var dash = await contactResponse(await _client.SendAsync(paulDash));
        Assert.Equal(1000m, dash.GetProperty("totalBorrowedBalance").GetDecimal());
        Assert.Equal(800m, dash.GetProperty("sharedWithMeBorrowedBalance").GetDecimal());
        Assert.Equal(1, dash.GetProperty("sharedWithMeActiveCount").GetInt32());
        Assert.Equal(1, dash.GetProperty("activeRelationshipCount").GetInt32());
    }

    private static async Task<JsonElement> contactResponse(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}
