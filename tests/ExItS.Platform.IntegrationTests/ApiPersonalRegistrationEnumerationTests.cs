using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApiPersonalRegistrationEnumerationTests(PostgreSqlFixture fixture) : IAsyncLifetime
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

    private static string UniqueEmail(string prefix) =>
        $"{prefix}{Guid.NewGuid():N}"[..Math.Min(32, prefix.Length + 32)].ToLowerInvariant() + "@example.test";

    private static async Task<(HttpStatusCode Status, JsonElement Body)> RegisterAsync(
        HttpClient client,
        string displayName,
        string email)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/platform/auth/register",
            new { displayName, email });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return (response.StatusCode, body);
    }

    [Fact]
    public async Task Public_register_returns_same_generic_ack_for_new_active_and_pending_emails()
    {
        var newEmail = UniqueEmail("new");
        var pendingEmail = UniqueEmail("pending");

        var (_, activeEmail, _) = await PlatformIntegrationTestUsers.RegisterPersonalWithPasswordAsync(
            _client,
            "enum-active");

        var (freshStatus, freshBody) = await RegisterAsync(_client, "Fresh Enum", newEmail);
        var (activeStatus, activeBody) = await RegisterAsync(_client, "Dup Active", activeEmail);
        var (pendingStatus, pendingBody) = await RegisterAsync(_client, "Pending Enum", pendingEmail);
        var (pendingDupStatus, pendingDupBody) = await RegisterAsync(_client, "Pending Enum", pendingEmail);

        Assert.Equal(HttpStatusCode.OK, freshStatus);
        Assert.Equal(HttpStatusCode.OK, activeStatus);
        Assert.Equal(HttpStatusCode.OK, pendingStatus);
        Assert.Equal(HttpStatusCode.OK, pendingDupStatus);

        var freshMessage = freshBody.GetProperty("message").GetString();
        Assert.Equal(RegisterPersonalAccount.GenericAcknowledgement, freshMessage);
        Assert.Equal(freshMessage, activeBody.GetProperty("message").GetString());
        Assert.Equal(freshMessage, pendingBody.GetProperty("message").GetString());
        Assert.Equal(freshMessage, pendingDupBody.GetProperty("message").GetString());

        Assert.False(freshBody.TryGetProperty("errorCode", out _));
        Assert.False(activeBody.TryGetProperty("errorCode", out _));
        Assert.False(pendingBody.TryGetProperty("errorCode", out _));
        Assert.False(pendingDupBody.TryGetProperty("errorCode", out _));

        var pendingUsers = await _client.GetAsync(
            $"/api/v1/platform/users?search={pendingEmail.Split('@')[0]}&pageSize=5");
        pendingUsers.EnsureSuccessStatusCode();
        Assert.Equal(1, (await pendingUsers.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("items").GetArrayLength());

        var firstToken = pendingBody.GetProperty("debugToken").GetString();
        var secondToken = pendingDupBody.GetProperty("debugToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(firstToken));
        Assert.False(string.IsNullOrWhiteSpace(secondToken));
        Assert.NotEqual(firstToken, secondToken);

        var staleActivate = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/activate-account",
            new { token = firstToken, password = "Correct-Horse-9!" });
        Assert.Equal(HttpStatusCode.Unauthorized, staleActivate.StatusCode);

        var activate = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/activate-account",
            new { token = secondToken, password = "Correct-Horse-9!" });
        activate.EnsureSuccessStatusCode();
    }
}
