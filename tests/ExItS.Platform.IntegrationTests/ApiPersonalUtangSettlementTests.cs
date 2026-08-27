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
    public async Task Shared_settle_awaits_confirm_then_closes()
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
                initialLoanNotes = "Shared seed"
            });
        var relationship = await ReadOk(await _client.SendAsync(relationshipRequest));
        var relationshipId = relationship.GetProperty("id").GetGuid();

        using var historyRequest = Authed(
            HttpMethod.Get,
            $"/api/v1/personal/utang/relationships/{relationshipId}/history",
            lenderToken);
        var history = await ReadOk(await _client.SendAsync(historyRequest));
        var loanId = history.EnumerateArray().First().GetProperty("id").GetGuid();

        using var confirmLoan = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/entries/{loanId}/confirm",
            borrowerToken,
            new { expectedVersion = (int?)null });
        await ReadOk(await _client.SendAsync(confirmLoan));

        using var getRel = Authed(
            HttpMethod.Get,
            $"/api/v1/personal/utang/relationships/{relationshipId}",
            lenderToken);
        var active = await ReadOk(await _client.SendAsync(getRel));
        var version = active.GetProperty("version").GetInt32();
        Assert.Equal(1200m, active.GetProperty("currentBalance").GetDecimal());

        var settlementEntryId = Guid.NewGuid();
        using var settleRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/settle",
            borrowerToken,
            new { expectedVersion = version, settlementEntryId });
        var settled = await ReadOk(await _client.SendAsync(settleRequest));
        Assert.Equal("AwaitingCounterpartyConfirmation", settled.GetProperty("outcome").GetString());
        Assert.Equal("Active", settled.GetProperty("relationship").GetProperty("status").GetString());
        Assert.Equal(1200m, settled.GetProperty("relationship").GetProperty("currentBalance").GetDecimal());
        Assert.Equal("Pending", settled.GetProperty("settlementEntry").GetProperty("status").GetString());

        using var confirmSettlement = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/entries/{settlementEntryId}/confirm",
            lenderToken,
            new { expectedVersion = (int?)null });
        var confirmed = await ReadOk(await _client.SendAsync(confirmSettlement));
        Assert.Equal("Confirmed", confirmed.GetProperty("status").GetString());
        Assert.True(confirmed.GetProperty("isSettlement").GetBoolean());

        using var getClosed = Authed(
            HttpMethod.Get,
            $"/api/v1/personal/utang/relationships/{relationshipId}",
            lenderToken);
        var closed = await ReadOk(await _client.SendAsync(getClosed));
        Assert.Equal("Closed", closed.GetProperty("status").GetString());
        Assert.Equal(0m, closed.GetProperty("currentBalance").GetDecimal());
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
    public async Task Settle_blocked_when_pending_entry_exists()
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
                initialLoanNotes = "Pending seed"
            });
        var relationship = await ReadOk(await _client.SendAsync(relationshipRequest));
        var relationshipId = relationship.GetProperty("id").GetGuid();

        using var settleRequest = Authed(
            HttpMethod.Post,
            $"/api/v1/personal/utang/relationships/{relationshipId}/settle",
            lenderToken,
            new { expectedVersion = (int?)null });
        var response = await _client.SendAsync(settleRequest);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(
            "application.personal.utang.settlement.pending_entries",
            problem.GetProperty("errorCode").GetString());
    }
}
