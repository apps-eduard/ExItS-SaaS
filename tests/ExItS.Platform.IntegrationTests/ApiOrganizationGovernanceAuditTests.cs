using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.Platform.IntegrationTests;

/// <summary>P28-WP15E: organization governance audit emission and org-scoped read authorization.</summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class ApiOrganizationGovernanceAuditTests(PostgreSqlFixture fixture) : IAsyncLifetime
{
    private SessionApiFactory _factory = null!;
    private HttpClient _client = null!;
    private HttpClient _admin = null!;

    public Task InitializeAsync()
    {
        _factory = new SessionApiFactory(fixture.ConnectionString);
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        _admin = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _admin.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private static string UniqueToken(string prefix) =>
        $"{prefix}{Guid.NewGuid():N}"[..Math.Min(24, prefix.Length + 32)].ToLowerInvariant();

    private async Task<Guid> CreateOrganizationAsync(string prefix)
    {
        var create = await _admin.PostAsJsonAsync(
            "/api/v1/platform/organizations",
            new { displayName = $"{prefix} Org", slug = UniqueToken(prefix) });
        create.EnsureSuccessStatusCode();
        return (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private async Task<(Guid UserId, string Email, string Password)> RegisterPersonalAsync(string prefix) =>
        await PlatformIntegrationTestUsers.RegisterPersonalWithPasswordAsync(_client, prefix);

    private async Task<string> LoginAsync(string email, string password)
    {
        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        return (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionToken").GetString()!;
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

    private async Task<(Guid OrganizationId, Guid OwnerUserId, string Token)> SeedOwnerSessionAsync(string prefix)
    {
        var (userId, email, password) = await RegisterPersonalAsync(prefix);
        var organizationId = await CreateOrganizationAsync(prefix);
        (await _admin.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{organizationId:D}/members",
            new { userId, role = "OrganizationOwner", reason = "integration-test-owner" }))
            .EnsureSuccessStatusCode();

        var token = await LoginAsync(email, password);
        return (organizationId, userId, token);
    }

    [Fact]
    public async Task Governance_mutation_emits_single_success_audit_with_correct_actor_and_org()
    {
        var (organizationId, ownerUserId, token) = await SeedOwnerSessionAsync("govaudit");

        using var update = Authed(
            HttpMethod.Put,
            $"/api/v1/platform/organizations/{organizationId:D}",
            token,
            new { displayName = "Audit Updated Org" });
        var updateResponse = await _client.SendAsync(update);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var auditRequest = Authed(
            HttpMethod.Get,
            $"/api/v1/platform/organizations/{organizationId:D}/audit?action={PlatformAuditActions.OrganizationUpdated}&outcome=Succeeded",
            token);
        var auditResponse = await _client.SendAsync(auditRequest);
        Assert.Equal(HttpStatusCode.OK, auditResponse.StatusCode);
        var page = await auditResponse.Content.ReadFromJsonAsync<JsonElement>();
        var items = page.GetProperty("items").EnumerateArray()
            .Where(i => string.Equals(i.GetProperty("targetId").GetString(), organizationId.ToString("D"), StringComparison.Ordinal))
            .ToList();
        Assert.Single(items);
        var row = items[0];
        Assert.Equal(organizationId, row.GetProperty("organizationId").GetGuid());
        Assert.Equal($"platform-user:{ownerUserId:D}", row.GetProperty("actorIdentifier").GetString());
        Assert.Equal("Succeeded", row.GetProperty("outcome").GetString());
    }

    [Fact]
    public async Task Failed_governance_mutation_does_not_emit_success_audit()
    {
        var (organizationId, _, token) = await SeedOwnerSessionAsync("govfail");
        var inviteEmail = $"{UniqueToken("dupinv")}@example.com";

        using var first = Authed(
            HttpMethod.Post,
            $"/api/v1/platform/organizations/{organizationId:D}/invitations",
            token,
            new { email = inviteEmail, role = "OrganizationMember" });
        Assert.Equal(HttpStatusCode.Created, (await _client.SendAsync(first)).StatusCode);

        using var duplicate = Authed(
            HttpMethod.Post,
            $"/api/v1/platform/organizations/{organizationId:D}/invitations",
            token,
            new { email = inviteEmail, role = "OrganizationMember" });
        var duplicateResponse = await _client.SendAsync(duplicate);
        Assert.NotEqual(HttpStatusCode.Created, duplicateResponse.StatusCode);

        using var auditRequest = Authed(
            HttpMethod.Get,
            $"/api/v1/platform/organizations/{organizationId:D}/audit?action={PlatformAuditActions.InvitationCreated}&outcome=Succeeded",
            token);
        var auditResponse = await _client.SendAsync(auditRequest);
        auditResponse.EnsureSuccessStatusCode();
        var page = await auditResponse.Content.ReadFromJsonAsync<JsonElement>();
        var successCount = page.GetProperty("items").EnumerateArray()
            .Count(i => string.Equals(i.GetProperty("actionCode").GetString(), PlatformAuditActions.InvitationCreated, StringComparison.Ordinal));
        Assert.Equal(1, successCount);
    }

    [Fact]
    public async Task Organization_member_cannot_read_governance_audit()
    {
        var (staffUserId, _, staffLogin, staffPassword, organizationId) =
            await PlatformIntegrationTestUsers.SeedOrgMemberViaInvitationAsync(_admin, _client, "govstaff");

        var staffToken = await LoginAsync(staffLogin, staffPassword);
        using var select = Authed(
            HttpMethod.Put,
            "/api/v1/platform/auth/organization-context",
            staffToken,
            new { organizationId });
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(select)).StatusCode);

        using var auditRequest = Authed(
            HttpMethod.Get,
            $"/api/v1/platform/organizations/{organizationId:D}/audit",
            staffToken);
        var auditResponse = await _client.SendAsync(auditRequest);
        Assert.Equal(HttpStatusCode.Forbidden, auditResponse.StatusCode);
        Assert.True(staffUserId != Guid.Empty);
    }

    [Fact]
    public async Task Cross_organization_audit_read_is_denied()
    {
        var (orgA, _, tokenA) = await SeedOwnerSessionAsync("orga");
        var orgB = await CreateOrganizationAsync("orgb");

        using var auditRequest = Authed(
            HttpMethod.Get,
            $"/api/v1/platform/organizations/{orgB:D}/audit",
            tokenA);
        var auditResponse = await _client.SendAsync(auditRequest);
        Assert.Equal(HttpStatusCode.Forbidden, auditResponse.StatusCode);
        Assert.NotEqual(orgA, orgB);
    }

    [Fact]
    public async Task Organization_audit_query_is_server_paged()
    {
        var (organizationId, _, token) = await SeedOwnerSessionAsync("govpage");
        for (var i = 0; i < 3; i++)
        {
            using var invite = Authed(
                HttpMethod.Post,
                $"/api/v1/platform/organizations/{organizationId:D}/invitations",
                token,
                new { email = $"{UniqueToken($"p{i}")}@example.com", role = "OrganizationMember" });
            (await _client.SendAsync(invite)).EnsureSuccessStatusCode();
        }

        using var page1 = Authed(
            HttpMethod.Get,
            $"/api/v1/platform/organizations/{organizationId:D}/audit?page=1&pageSize=2&outcome=Succeeded",
            token);
        var response1 = await _client.SendAsync(page1);
        response1.EnsureSuccessStatusCode();
        var body1 = await response1.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, body1.GetProperty("items").GetArrayLength());
        Assert.True(body1.GetProperty("totalCount").GetInt32() >= 3);
        Assert.Equal(1, body1.GetProperty("page").GetInt32());
        Assert.Equal(2, body1.GetProperty("pageSize").GetInt32());
    }

    [Fact]
    public async Task Invitation_create_audit_omits_invitee_email_from_summary()
    {
        var (organizationId, _, token) = await SeedOwnerSessionAsync("govinv");
        var inviteEmail = $"{UniqueToken("inv")}@example.com";

        using var invite = Authed(
            HttpMethod.Post,
            $"/api/v1/platform/organizations/{organizationId:D}/invitations",
            token,
            new { email = inviteEmail, role = "OrganizationMember" });
        var inviteResponse = await _client.SendAsync(invite);
        inviteResponse.EnsureSuccessStatusCode();
        var invitationId = (await inviteResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        using var auditRequest = Authed(
            HttpMethod.Get,
            $"/api/v1/platform/organizations/{organizationId:D}/audit?action={PlatformAuditActions.InvitationCreated}&outcome=Succeeded",
            token);
        var auditResponse = await _client.SendAsync(auditRequest);
        auditResponse.EnsureSuccessStatusCode();
        var page = await auditResponse.Content.ReadFromJsonAsync<JsonElement>();
        var match = page.GetProperty("items").EnumerateArray()
            .First(i => string.Equals(i.GetProperty("targetId").GetString(), invitationId.ToString("D"), StringComparison.Ordinal));
        var summary = match.TryGetProperty("summary", out var s) ? s.GetString() : null;
        Assert.NotNull(summary);
        Assert.DoesNotContain(inviteEmail, summary, StringComparison.OrdinalIgnoreCase);
    }
}
