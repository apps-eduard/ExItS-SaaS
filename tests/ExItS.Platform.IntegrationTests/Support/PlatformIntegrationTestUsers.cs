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

    /// <summary>
    /// Revokes every active platform role assignment. Staff create always seeds a role; use this
    /// when a test needs an unprivileged actor or an exclusive replacement role.
    /// </summary>
    internal static async Task RevokeAllActivePlatformRolesAsync(
        HttpClient admin,
        Guid platformUserId,
        string reason = "test isolation")
    {
        var assignments = await admin.GetFromJsonAsync<JsonElement>(
            $"/api/v1/platform/authorization/assignments?platformUserId={platformUserId:D}&status=Active&pageSize=50");
        foreach (var item in assignments.GetProperty("items").EnumerateArray())
        {
            var id = item.GetProperty("id").GetGuid();
            (await admin.PostAsJsonAsync(
                $"/api/v1/platform/authorization/assignments/{id}/revoke",
                new { reason })).EnsureSuccessStatusCode();
        }
    }

    /// <summary>
    /// Ensures <paramref name="role"/> is active at the given organization scope (null = global).
    /// No-ops when already assigned; creates when missing. Use instead of blind assign after staff create.
    /// </summary>
    internal static async Task EnsurePlatformRoleAsync(
        HttpClient admin,
        Guid platformUserId,
        string role,
        Guid? organizationId = null)
    {
        var assignments = await admin.GetFromJsonAsync<JsonElement>(
            $"/api/v1/platform/authorization/assignments?platformUserId={platformUserId:D}&status=Active&pageSize=50");
        foreach (var item in assignments.GetProperty("items").EnumerateArray())
        {
            if (!string.Equals(item.GetProperty("role").GetString(), role, StringComparison.Ordinal))
            {
                continue;
            }

            Guid? existingOrg = item.TryGetProperty("organizationId", out var orgEl) && orgEl.ValueKind != JsonValueKind.Null
                ? orgEl.GetGuid()
                : null;
            if (existingOrg == organizationId)
            {
                return;
            }
        }

        var response = await admin.PostAsJsonAsync(
            "/api/v1/platform/authorization/assignments",
            new { platformUserId, role, organizationId });
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Replaces all active platform roles with exactly <paramref name="role"/> (global unless scoped).
    /// </summary>
    internal static async Task ReplaceWithPlatformRoleAsync(
        HttpClient admin,
        Guid platformUserId,
        string role,
        Guid? organizationId = null)
    {
        await RevokeAllActivePlatformRolesAsync(admin, platformUserId).ConfigureAwait(false);
        var response = await admin.PostAsJsonAsync(
            "/api/v1/platform/authorization/assignments",
            new { platformUserId, role, organizationId });
        response.EnsureSuccessStatusCode();
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

    /// <summary>
    /// Accepts a staff invite into an existing organization via token + password,
    /// yielding a distinct org-scoped staff identity (never attaches to a Personal user).
    /// </summary>
    internal static async Task<(Guid UserId, string ContactEmail, string StaffLogin, string Password, Guid MembershipId)> SeedStaffViaInvitationToOrgAsync(
        HttpClient admin,
        HttpClient client,
        Guid organizationId,
        string prefix,
        string role = "OrganizationMember")
    {
        var emailLocal = Unique(prefix);
        var contactEmail = $"{emailLocal}@example.com";
        var invite = await admin.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId}/invitations",
            new { email = contactEmail, role, requireEmailVerification = false });
        invite.EnsureSuccessStatusCode();
        var inviteBody = await invite.Content.ReadFromJsonAsync<JsonElement>();
        var acceptToken = inviteBody.GetProperty("acceptToken").GetString();
        Xunit.Assert.False(string.IsNullOrWhiteSpace(acceptToken));

        // Invite must not provision a PlatformUser before accept.
        var preAcceptUsers = await admin.GetAsync($"/api/v1/platform/users?search={emailLocal}&pageSize=5");
        preAcceptUsers.EnsureSuccessStatusCode();
        Xunit.Assert.Equal(0, (await preAcceptUsers.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("items").GetArrayLength());

        var password = "Correct-Horse-9!";
        var accept = await client.PostAsJsonAsync(
            "/api/v1/platform/invitations/accept",
            new { token = acceptToken, password });
        accept.EnsureSuccessStatusCode();
        var acceptBody = await accept.Content.ReadFromJsonAsync<JsonElement>();
        var userId = acceptBody.GetProperty("userId").GetGuid();
        var staffLogin = acceptBody.GetProperty("staffLogin").GetString();
        Xunit.Assert.False(string.IsNullOrWhiteSpace(staffLogin));
        Xunit.Assert.Contains("@ORG", staffLogin, StringComparison.OrdinalIgnoreCase);

        var members = await admin.GetAsync(
            $"/api/v1/platform/organizations/{organizationId}/members?status=Active&pageSize=50");
        members.EnsureSuccessStatusCode();
        var membershipId = (await members.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("items")
            .EnumerateArray()
            .First(m => m.GetProperty("userId").GetGuid() == userId)
            .GetProperty("id")
            .GetGuid();

        return (userId, contactEmail, staffLogin!, password, membershipId);
    }

    /// <summary>
    /// Creates an organization and accepts a staff invite via token + password,
    /// yielding a distinct org-scoped staff identity (never attaches to a Personal user).
    /// </summary>
    internal static async Task<(Guid UserId, string ContactEmail, string StaffLogin, string Password, Guid OrganizationId)> SeedOrgMemberViaInvitationAsync(
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

        var (userId, contactEmail, staffLogin, password, _) =
            await SeedStaffViaInvitationToOrgAsync(admin, client, organizationId, prefix, role);
        return (userId, contactEmail, staffLogin, password, organizationId);
    }
}
