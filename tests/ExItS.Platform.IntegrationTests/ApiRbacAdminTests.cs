using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Organizations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApiRbacAdminTests(PostgreSqlFixture fixture) : IAsyncLifetime
{
    private AuthorizationAuditApiFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new AuthorizationAuditApiFactory(fixture.ConnectionString);
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private static string UniqueToken(string prefix) =>
        $"{prefix}{Guid.NewGuid():N}"[..Math.Min(24, prefix.Length + 32)].ToLowerInvariant();

    [Fact]
    public async Task Platform_role_definition_lifecycle_and_custom_assignment_effective_permissions()
    {
        var create = await _client.PostAsJsonAsync("/api/v1/platform/authorization/role-definitions", new
        {
            code = "OpsViewer_" + Guid.NewGuid().ToString("N")[..8],
            name = "Ops Viewer",
            description = "View portfolio and audit",
            permissions = new[]
            {
                PlatformPermission.ViewPortfolio,
                PlatformPermission.ViewAuditRecords
            }
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<JsonElement>();
        var roleId = created.GetProperty("id").GetGuid();
        var version = created.GetProperty("version").GetInt32();

        // Seed built-ins via list, then attempt protected retire.
        _ = await _client.GetAsync("/api/v1/platform/authorization/role-definitions?pageSize=10");
        var retireBuiltIn = await _client.PostAsJsonAsync(
            $"/api/v1/platform/authorization/role-definitions/{BuiltInPlatformRoleDefinitions.PlatformAdministratorId}/retire",
            new { expectedVersion = 1 });
        Assert.Equal(HttpStatusCode.Conflict, retireBuiltIn.StatusCode);

        var userId = await CreatePlatformUserAsync("rbac");
        var assign = await _client.PostAsJsonAsync("/api/v1/platform/authorization/custom-assignments", new
        {
            platformUserId = userId,
            roleDefinitionId = roleId,
            reason = "grant"
        });
        Assert.Equal(HttpStatusCode.Created, assign.StatusCode);

        var effective = await _client.GetFromJsonAsync<JsonElement>(
            $"/api/v1/platform/authorization/users/{userId}/effective-permissions");
        var permissions = effective.GetProperty("permissions").EnumerateArray().Select(p => p.GetString()).ToHashSet();
        Assert.Contains(PlatformPermission.ViewPortfolio, permissions);
        Assert.DoesNotContain(PlatformPermission.ManagePlatformUsers, permissions);

        var deactivate = await _client.PostAsJsonAsync(
            $"/api/v1/platform/authorization/role-definitions/{roleId}/deactivate",
            new { expectedVersion = version });
        Assert.True(deactivate.IsSuccessStatusCode);
        var afterDeactivate = await deactivate.Content.ReadFromJsonAsync<JsonElement>();
        _ = afterDeactivate.GetProperty("version").GetInt32();

        effective = await _client.GetFromJsonAsync<JsonElement>(
            $"/api/v1/platform/authorization/users/{userId}/effective-permissions");
        permissions = effective.GetProperty("permissions").EnumerateArray().Select(p => p.GetString()).ToHashSet();
        Assert.DoesNotContain(PlatformPermission.ViewPortfolio, permissions);

        var stale = await _client.PostAsJsonAsync(
            $"/api/v1/platform/authorization/role-definitions/{roleId}/activate",
            new { expectedVersion = 1 });
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);

        var unknown = await _client.PostAsJsonAsync("/api/v1/platform/authorization/role-definitions", new
        {
            code = "BadPerm_" + Guid.NewGuid().ToString("N")[..6],
            name = "Bad",
            permissions = new[] { "pos.role.cashier" }
        });
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
    }

    [Fact]
    public async Task User_directory_filters_and_last_platform_administrator_protected()
    {
        var unassignedUser = await CreatePlatformUserAsync("unasg");
        var directory = await _client.GetFromJsonAsync<JsonElement>(
            "/api/v1/platform/users?directory=Unassigned&pageSize=100");
        var ids = directory.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetGuid()).ToHashSet();
        Assert.Contains(unassignedUser, ids);

        var staffUser = await CreatePlatformUserAsync("staff");
        var assign = await _client.PostAsJsonAsync("/api/v1/platform/authorization/assignments", new
        {
            platformUserId = staffUser,
            role = nameof(PlatformSystemRole.PlatformSupport)
        });
        Assert.Equal(HttpStatusCode.Created, assign.StatusCode);

        var staff = await _client.GetFromJsonAsync<JsonElement>(
            "/api/v1/platform/users?directory=PlatformStaff&pageSize=100");
        var staffIds = staff.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetGuid()).ToHashSet();
        Assert.Contains(staffUser, staffIds);

        // Isolate last-admin: ensure at least one PlatformAdministrator, then assert cannot revoke the final one.
        var tempAdmin = await CreatePlatformUserAsync("tmpadm");
        var grant = await _client.PostAsJsonAsync("/api/v1/platform/authorization/assignments", new
        {
            platformUserId = tempAdmin,
            role = nameof(PlatformSystemRole.PlatformAdministrator)
        });
        Assert.Equal(HttpStatusCode.Created, grant.StatusCode);

        var assignments = await _client.GetFromJsonAsync<JsonElement>(
            "/api/v1/platform/authorization/assignments?role=PlatformAdministrator&status=Active&pageSize=50");
        var adminAssignments = assignments.GetProperty("items").EnumerateArray().ToList();
        Assert.NotEmpty(adminAssignments);

        // Revoke extras until one remains.
        foreach (var item in adminAssignments.Skip(1))
        {
            var id = item.GetProperty("id").GetGuid();
            var revokeExtra = await _client.PostAsJsonAsync(
                $"/api/v1/platform/authorization/assignments/{id}/revoke",
                new { reason = "leave one" });
            Assert.True(revokeExtra.IsSuccessStatusCode);
        }

        assignments = await _client.GetFromJsonAsync<JsonElement>(
            "/api/v1/platform/authorization/assignments?role=PlatformAdministrator&status=Active&pageSize=50");
        adminAssignments = assignments.GetProperty("items").EnumerateArray().ToList();
        Assert.Single(adminAssignments);
        var onlyId = adminAssignments[0].GetProperty("id").GetGuid();
        var revoke = await _client.PostAsJsonAsync(
            $"/api/v1/platform/authorization/assignments/{onlyId}/revoke",
            new { reason = "should fail" });
        Assert.Equal(HttpStatusCode.Conflict, revoke.StatusCode);
    }

    [Fact]
    public async Task Organization_custom_role_isolated_from_other_org_and_platform_catalog()
    {
        var orgA = await CreateOrganizationAsync("rbaca");
        var orgB = await CreateOrganizationAsync("rbacb");
        var member = await CreatePlatformUserAsync("orgm");

        (await _client.PostAsJsonAsync($"/api/v1/platform/organizations/{orgA}/members", new
        {
            userId = member,
            role = nameof(OrganizationRole.OrganizationMember)
        })).EnsureSuccessStatusCode();

        var create = await _client.PostAsJsonAsync($"/api/v1/platform/organizations/{orgA}/role-definitions", new
        {
            code = "BillingClerk",
            name = "Billing Clerk",
            permissions = new[] { OrganizationPermission.ViewCommercial, OrganizationPermission.ViewOrganization }
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var roleId = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var cross = await _client.PostAsJsonAsync($"/api/v1/platform/organizations/{orgB}/role-assignments", new
        {
            platformUserId = member,
            roleDefinitionId = roleId
        });
        Assert.True(
            cross.StatusCode is HttpStatusCode.NotFound
                or HttpStatusCode.Conflict
                or HttpStatusCode.BadRequest
                or HttpStatusCode.Forbidden);

        var assign = await _client.PostAsJsonAsync($"/api/v1/platform/organizations/{orgA}/role-assignments", new
        {
            platformUserId = member,
            roleDefinitionId = roleId
        });
        Assert.Equal(HttpStatusCode.Created, assign.StatusCode);

        var effective = await _client.GetFromJsonAsync<JsonElement>(
            $"/api/v1/platform/organizations/{orgA}/members/{member}/effective-permissions");
        var perms = effective.GetProperty("permissions").EnumerateArray().Select(p => p.GetString()).ToHashSet();
        Assert.Contains(OrganizationPermission.ViewCommercial, perms);
        Assert.DoesNotContain(PlatformPermission.ManagePlatformUsers, perms);
    }

    private async Task<Guid> CreatePlatformUserAsync(string prefix)
    {
        var username = UniqueToken(prefix);
        var response = await _client.PostAsJsonAsync(
            "/api/v1/platform/users",
            new { username, displayName = $"{prefix} User", email = $"{username}@example.com" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private async Task<Guid> CreateOrganizationAsync(string prefix)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/platform/organizations",
            new { displayName = $"{prefix} Org", slug = UniqueToken(prefix) });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private sealed class AuthorizationAuditApiFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:PlatformDatabase", connectionString);
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PlatformDatabase"] = connectionString
                });
            });
        }
    }
}
