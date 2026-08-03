using System.Net.Http.Json;
using System.Text.Json;

namespace ExItS.Platform.IntegrationTests.Support;

/// <summary>Shared helpers for POST /api/v1/platform/users (Platform Staff create requires PlatformRole).</summary>
internal static class PlatformIntegrationTestUsers
{
    internal static string Unique(string prefix) =>
        $"{prefix}{Guid.NewGuid():N}"[..Math.Min(24, prefix.Length + 32)].ToLowerInvariant();

    internal static async Task<(Guid UserId, string Username, string Password)> CreatePlatformStaffWithPasswordAsync(
        HttpClient admin,
        string prefix,
        string platformRole = "PlatformSupport")
    {
        var username = Unique(prefix);
        var password = "Correct-Horse-9!";
        var create = await admin.PostAsJsonAsync(
            "/api/v1/platform/users",
            new
            {
                username,
                firstName = "Test",
                lastName = "User",
                displayName = $"{prefix} User",
                email = $"{username}@example.com",
                platformRole
            });
        create.EnsureSuccessStatusCode();
        var userId = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await admin.PutAsJsonAsync(
            $"/api/v1/platform/users/{userId}/credentials/password",
            new { password })).EnsureSuccessStatusCode();
        return (userId, username, password);
    }

    internal static async Task<(Guid UserId, string Email, string Password)> RegisterPersonalWithPasswordAsync(
        HttpClient client,
        string prefix)
    {
        var emailLocal = Unique(prefix);
        var email = $"{emailLocal}@example.com";
        var password = "Correct-Horse-9!";
        var register = await client.PostAsJsonAsync(
            "/api/v1/platform/auth/register",
            new { displayName = "Personal User", email });
        register.EnsureSuccessStatusCode();
        var body = await register.Content.ReadFromJsonAsync<JsonElement>();
        var token = body.GetProperty("debugToken").GetString();
        Xunit.Assert.False(string.IsNullOrWhiteSpace(token));

        var activate = await client.PostAsJsonAsync(
            "/api/v1/platform/auth/activate-account",
            new { token, password });
        activate.EnsureSuccessStatusCode();

        var list = await client.GetAsync($"/api/v1/platform/users?search={emailLocal}&pageSize=5");
        list.EnsureSuccessStatusCode();
        var userId = (await list.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("items")[0]
            .GetProperty("id")
            .GetGuid();
        return (userId, email, password);
    }

    internal static async Task<(Guid UserId, string Email, string Password, Guid OrganizationId)> SeedOrgMemberViaInvitationAsync(
        HttpClient admin,
        HttpClient client,
        string prefix,
        string role = "OrganizationMember")
    {
        var slug = Unique(prefix);
        var org = await admin.PostAsJsonAsync(
            "/api/v1/platform/organizations",
            new { displayName = "Test Org", slug });
        org.EnsureSuccessStatusCode();
        var organizationId = (await org.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var emailLocal = Unique(prefix);
        var email = $"{emailLocal}@example.com";
        var invite = await admin.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/invitations",
            new { email, role, requireEmailVerification = false });
        invite.EnsureSuccessStatusCode();
        var inviteBody = await invite.Content.ReadFromJsonAsync<JsonElement>();
        var acceptToken = inviteBody.GetProperty("acceptToken").GetString();
        Xunit.Assert.False(string.IsNullOrWhiteSpace(acceptToken));

        var list = await admin.GetAsync($"/api/v1/platform/users?search={emailLocal}&pageSize=5");
        list.EnsureSuccessStatusCode();
        var userId = (await list.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("items")[0]
            .GetProperty("id")
            .GetGuid();

        var password = "Correct-Horse-9!";
        (await admin.PutAsJsonAsync(
            $"/api/v1/platform/users/{userId}/credentials/password",
            new { password })).EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = email, password });
        login.EnsureSuccessStatusCode();
        var sessionToken = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionToken").GetString();
        Xunit.Assert.False(string.IsNullOrWhiteSpace(sessionToken));

        using var acceptRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/platform/invitations/accept")
        {
            Content = JsonContent.Create(new { token = acceptToken })
        };
        acceptRequest.Headers.Add("X-ExItS-Session-Token", sessionToken);
        var accept = await client.SendAsync(acceptRequest);
        accept.EnsureSuccessStatusCode();

        return (userId, email, password, organizationId);
    }
}
