using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApiPersonalUtangSettlementTests(PostgreSqlFixture fixture) : IAsyncLifetime
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

    private static async Task<JsonElement> ReadOk(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task Private_settle_completes_and_close_is_idempotent()
    {
        var (token, userId) = await SeedPersonalUserAsync("setl");

        using var contactRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/contacts",
            token,
            new { displayName = "Private Friend", phone = "+639170009901" });
        var contactId = (await ReadOk(await _client.SendAsync(contactRequest))).GetProperty("id").GetGuid();

        using var relationshipRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/relationships",
            token,
            new
            {
                creditorUserIdentityId = userId,
                debtorContactId = contactId,
                currencyCode = "PHP",
                initialLoanAmount = 750m,
                initialLoanNotes = "Seed loan"
            });
        var relationship = await ReadOk(await _client.SendAsync(relationshipRequest));
        var relationshipId = relationship.GetProperty("id").GetGuid();
        var version = relationship.GetProperty("version").GetInt32();
        var settlementEntryId = Guid.NewGuid();

        using var settleRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/settle",
            token,
            new { expectedVersion = version, settlementEntryId });
        var settled = await ReadOk(await _client.SendAsync(settleRequest));
        Assert.Equal("Completed", settled.GetProperty("outcome").GetString());
        Assert.Equal("Closed", settled.GetProperty("relationship").GetProperty("status").GetString());
        Assert.Equal(0m, settled.GetProperty("relationship").GetProperty("currentBalance").GetDecimal());
        Assert.True(settled.GetProperty("settlementEntry").GetProperty("isSettlement").GetBoolean());
        Assert.Equal("Settlement", settled.GetProperty("settlementEntry").GetProperty("intent").GetString());
        Assert.Equal(settlementEntryId, settled.GetProperty("settlementEntry").GetProperty("id").GetGuid());

        using var settleRetry = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/settle",
            token,
            new { expectedVersion = (int?)null, settlementEntryId });
        var retry = await ReadOk(await _client.SendAsync(settleRetry));
        Assert.Equal("Completed", retry.GetProperty("outcome").GetString());
        Assert.Equal(settlementEntryId, retry.GetProperty("settlementEntry").GetProperty("id").GetGuid());

        using var closeRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/close",
            token,
            new { expectedVersion = (int?)null });
        var closed = await ReadOk(await _client.SendAsync(closeRequest));
        Assert.Equal("AlreadySettled", closed.GetProperty("outcome").GetString());
        Assert.Equal("Closed", closed.GetProperty("relationship").GetProperty("status").GetString());
    }

    [Fact]
    public async Task Shared_owner_settle_completes_immediately_and_non_owner_denied()
    {
        var (lenderToken, lenderId) = await SeedPersonalUserAsync("slnd");
        var (borrowerToken, borrowerId) = await SeedPersonalUserAsync("sbor");

        using var relationshipRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/relationships",
            lenderToken,
            new
            {
                creditorUserIdentityId = lenderId,
                debtorUserIdentityId = borrowerId,
                currencyCode = "PHP",
                initialLoanAmount = 1200m,
                initialLoanNotes = "Shared seed",
                shareWithCounterparty = true
            });
        var relationship = await ReadOk(await _client.SendAsync(relationshipRequest));
        var relationshipId = relationship.GetProperty("id").GetGuid();
        var version = relationship.GetProperty("version").GetInt32();
        Assert.Equal(1200m, relationship.GetProperty("currentBalance").GetDecimal());
        Assert.True(relationship.GetProperty("isLedgerOwner").GetBoolean());

        using var borrowerSettle = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/settle",
            borrowerToken,
            new { expectedVersion = version });
        var denied = await _client.SendAsync(borrowerSettle);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal(
            "application.personal.utang.not_ledger_owner",
            (await denied.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errorCode").GetString());

        var settlementEntryId = Guid.NewGuid();
        using var settleRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/settle",
            lenderToken,
            new { expectedVersion = version, settlementEntryId });
        var settled = await ReadOk(await _client.SendAsync(settleRequest));
        Assert.Equal("Completed", settled.GetProperty("outcome").GetString());
        Assert.Equal("Closed", settled.GetProperty("relationship").GetProperty("status").GetString());
        Assert.Equal(0m, settled.GetProperty("relationship").GetProperty("currentBalance").GetDecimal());
        Assert.Equal("Confirmed", settled.GetProperty("settlementEntry").GetProperty("status").GetString());
        Assert.True(settled.GetProperty("settlementEntry").GetProperty("isSettlement").GetBoolean());

        using var getClosed = Authed(
            HttpMethod.Get,
            $"/api/v1/personal/utang/relationships/{relationshipId}",
            borrowerToken);
        var closed = await ReadOk(await _client.SendAsync(getClosed));
        Assert.Equal("Closed", closed.GetProperty("status").GetString());
        Assert.Equal(0m, closed.GetProperty("currentBalance").GetDecimal());
        Assert.False(closed.GetProperty("isLedgerOwner").GetBoolean());
    }

    [Fact]
    public async Task Close_zero_balance_private_relationship()
    {
        var (token, userId) = await SeedPersonalUserAsync("clze");

        using var contactRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/contacts",
            token,
            new { displayName = "Zero Friend", phone = "+639170009902" });
        var contactId = (await ReadOk(await _client.SendAsync(contactRequest))).GetProperty("id").GetGuid();

        using var relationshipRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/relationships",
            token,
            new
            {
                creditorUserIdentityId = userId,
                debtorContactId = contactId,
                currencyCode = "PHP",
                initialLoanAmount = 200m,
                initialLoanNotes = "To repay"
            });
        var relationship = await ReadOk(await _client.SendAsync(relationshipRequest));
        var relationshipId = relationship.GetProperty("id").GetGuid();
        var version = relationship.GetProperty("version").GetInt32();

        using var paymentRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/entries",
            token,
            new { entryType = "Payment", amount = 200m, expectedVersion = version });
        await ReadOk(await _client.SendAsync(paymentRequest));

        using var closeRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/close",
            token,
            new { expectedVersion = (int?)null });
        var closed = await ReadOk(await _client.SendAsync(closeRequest));
        Assert.Equal("Closed", closed.GetProperty("outcome").GetString());
        Assert.Equal("Closed", closed.GetProperty("relationship").GetProperty("status").GetString());
    }

    [Fact]
    public async Task Non_owner_cannot_close_shared_relationship()
    {
        var (lenderToken, lenderId) = await SeedPersonalUserAsync("spnd");
        var (borrowerToken, borrowerId) = await SeedPersonalUserAsync("bpnd");

        using var relationshipRequest = Authed(
            HttpMethod.Post,
            "/api/v1/personal/utang/relationships",
            lenderToken,
            new
            {
                creditorUserIdentityId = lenderId,
                debtorUserIdentityId = borrowerId,
                currencyCode = "PHP",
                initialLoanAmount = 500m,
                initialLoanNotes = "Owner seed",
                shareWithCounterparty = true
            });
        var relationship = await ReadOk(await _client.SendAsync(relationshipRequest));
        var relationshipId = relationship.GetProperty("id").GetGuid();
        var version = relationship.GetProperty("version").GetInt32();

        using var pay = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/entries",
            lenderToken,
            new { entryType = "Payment", amount = 500m, expectedVersion = version });
        await ReadOk(await _client.SendAsync(pay));

        using var closeDenied = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/close",
            borrowerToken,
            new { expectedVersion = (int?)null });
        var response = await _client.SendAsync(closeDenied);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(
            "application.personal.utang.not_ledger_owner",
            (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errorCode").GetString());
    }
}
