using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Infrastructure.Persistence;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApiDeviceRecoveryCredentialTests(PostgreSqlFixture fixture) : IAsyncLifetime
{
    private SessionApiFactory _factory = null!;
    private HttpClient _client = null!;

    private const string DeviceHeader = "X-Pos-Installation-Device-Id";

    public Task InitializeAsync()
    {
        _factory = new SessionApiFactory(fixture.ConnectionString);
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private static string UniqueDeviceId() => $"dev-{Guid.NewGuid():N}"[..20];

    private async Task<(Guid UserId, string AccessToken, string DeviceId, string RecoveryCredential)> EnrollRecoveryAsync()
    {
        var admin = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var (_, username, password) = await PlatformIntegrationTestUsers.CreatePlatformStaffWithPasswordAsync(admin, "rcv");
        admin.Dispose();

        var issue = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/token",
            new { grantType = "password", usernameOrEmail = username, password });
        issue.EnsureSuccessStatusCode();
        var tokenBody = await issue.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = tokenBody.GetProperty("accessToken").GetString()!;
        var userId = tokenBody.GetProperty("userId").GetGuid();

        var deviceId = UniqueDeviceId();
        using var enroll = new HttpRequestMessage(HttpMethod.Post, "/api/v1/platform/auth/recovery/enroll");
        enroll.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        enroll.Headers.Add(DeviceHeader, deviceId);
        var enrolled = await _client.SendAsync(enroll);
        enrolled.EnsureSuccessStatusCode();
        var enrollBody = await enrolled.Content.ReadFromJsonAsync<JsonElement>();
        var recoveryCredential = enrollBody.GetProperty("recoveryCredential").GetString();
        Assert.False(string.IsNullOrWhiteSpace(recoveryCredential));

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            var hash = await db.PlatformDeviceRecoveryCredentials.AsNoTracking()
                .Where(c => c.UserId == userId)
                .Select(c => c.TokenHash)
                .SingleAsync();
            Assert.DoesNotContain(recoveryCredential!, hash, StringComparison.Ordinal);
        }

        return (userId, accessToken, deviceId, recoveryCredential!);
    }

    [Fact]
    public async Task Enroll_and_exchange_issues_sixty_minute_access_token_and_rotates_credential()
    {
        var seeded = await EnrollRecoveryAsync();

        using var exchange = new HttpRequestMessage(HttpMethod.Post, "/api/v1/platform/auth/recovery/exchange");
        exchange.Headers.Add(DeviceHeader, seeded.DeviceId);
        exchange.Content = JsonContent.Create(new { recoveryCredential = seeded.RecoveryCredential });
        var exchanged = await _client.SendAsync(exchange);
        Assert.Equal(HttpStatusCode.OK, exchanged.StatusCode);
        var body = await exchanged.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = body.GetProperty("accessToken").GetProperty("accessToken").GetString();
        var rotated = body.GetProperty("recoveryCredential").GetString();
        Assert.False(string.IsNullOrWhiteSpace(accessToken));
        Assert.False(string.IsNullOrWhiteSpace(rotated));
        Assert.NotEqual(seeded.RecoveryCredential, rotated);

        var expiresAt = body.GetProperty("accessToken").GetProperty("expiresAtUtc").GetDateTimeOffset();
        Assert.InRange((expiresAt - DateTimeOffset.UtcNow).TotalMinutes, 55, 65);

        using var reuse = new HttpRequestMessage(HttpMethod.Post, "/api/v1/platform/auth/recovery/exchange");
        reuse.Headers.Add(DeviceHeader, seeded.DeviceId);
        reuse.Content = JsonContent.Create(new { recoveryCredential = seeded.RecoveryCredential });
        var reused = await _client.SendAsync(reuse);
        Assert.Equal(HttpStatusCode.BadRequest, reused.StatusCode);
    }

    [Fact]
    public async Task Exchange_rejects_wrong_device()
    {
        var seeded = await EnrollRecoveryAsync();

        using var exchange = new HttpRequestMessage(HttpMethod.Post, "/api/v1/platform/auth/recovery/exchange");
        exchange.Headers.Add(DeviceHeader, UniqueDeviceId());
        exchange.Content = JsonContent.Create(new { recoveryCredential = seeded.RecoveryCredential });
        var result = await _client.SendAsync(exchange);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task Paul_credential_cannot_issue_mica_token()
    {
        var mica = await EnrollRecoveryAsync();
        var paul = await EnrollRecoveryAsync();

        using var exchange = new HttpRequestMessage(HttpMethod.Post, "/api/v1/platform/auth/recovery/exchange");
        exchange.Headers.Add(DeviceHeader, mica.DeviceId);
        exchange.Content = JsonContent.Create(new { recoveryCredential = paul.RecoveryCredential });
        var result = await _client.SendAsync(exchange);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }
}
