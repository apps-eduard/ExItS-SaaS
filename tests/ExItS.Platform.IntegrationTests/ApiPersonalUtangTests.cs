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
                initialLoanAmount = 250m
            });
        var linkedResponse = await _client.SendAsync(linkedRelationshipRequest);
        Assert.Equal(HttpStatusCode.Created, linkedResponse.StatusCode);

        using var borrowedRequest = Authed(HttpMethod.Get, "/api/v1/personal/utang/relationships/borrowed", borrowerToken);
        var borrowedList = await _client.SendAsync(borrowedRequest);
        Assert.Equal(HttpStatusCode.OK, borrowedList.StatusCode);
        Assert.Equal(1, (await borrowedList.Content.ReadFromJsonAsync<JsonElement>()).GetArrayLength());
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
                initialLoanAmount = 100m
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
                initialLoanAmount = 50m
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

    private static async Task<JsonElement> contactResponse(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}
