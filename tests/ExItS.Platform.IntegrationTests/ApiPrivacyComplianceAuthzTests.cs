using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApiPrivacyComplianceAuthzTests(PostgreSqlFixture fixture) : IAsyncLifetime
{
    private const string DevUserHeader = "X-Dev-Platform-User-Id";

    private PrivacyComplianceApiFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new PrivacyComplianceApiFactory(fixture.ConnectionString);
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, Guid? actingUserId = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (actingUserId is not null)
        {
            request.Headers.Add(DevUserHeader, actingUserId.Value.ToString("D"));
        }

        return await _client.SendAsync(request);
    }

    private async Task AssignPlatformRoleAsync(Guid platformUserId, string role)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/platform/authorization/assignments",
            new { platformUserId, role, organizationId = (Guid?)null });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Privacy_overview_requires_view_permission()
    {
        var (userId, _, _) = await PlatformIntegrationTestUsers.CreatePlatformStaffWithPasswordAsync(_client, "pcv");
        var denied = await SendAsync(HttpMethod.Get, "/api/v1/platform/privacy-compliance/overview", userId);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);

        await AssignPlatformRoleAsync(userId, "PlatformAuditor");
        var allowed = await SendAsync(HttpMethod.Get, "/api/v1/platform/privacy-compliance/overview", userId);
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        var json = await allowed.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("totalRequirements").GetInt32() > 0);
    }

    [Fact]
    public async Task Privacy_ensure_catalog_requires_manage_permission()
    {
        var (auditorId, _, _) = await PlatformIntegrationTestUsers.CreatePlatformStaffWithPasswordAsync(_client, "pca");
        await AssignPlatformRoleAsync(auditorId, "PlatformAuditor");
        var denied = await SendAsync(HttpMethod.Post, "/api/v1/platform/privacy-compliance/ensure-catalog", auditorId);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);

        var (adminId, _, _) = await PlatformIntegrationTestUsers.CreatePlatformStaffWithPasswordAsync(_client, "pcm");
        await AssignPlatformRoleAsync(adminId, "PlatformAdministrator");
        var allowed = await SendAsync(HttpMethod.Post, "/api/v1/platform/privacy-compliance/ensure-catalog", adminId);
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    [Fact]
    public async Task Personal_account_cannot_access_privacy_compliance_overview()
    {
        var (userId, _, _) = await PlatformIntegrationTestUsers.RegisterPersonalWithPasswordAsync(_client, "pcpers");
        var denied = await SendAsync(HttpMethod.Get, "/api/v1/platform/privacy-compliance/overview", userId);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }

    [Fact]
    public async Task Privacy_export_pdf_is_available_to_viewer_and_contains_pdf_header()
    {
        var (adminId, _, _) = await PlatformIntegrationTestUsers.CreatePlatformStaffWithPasswordAsync(_client, "pcp");
        await AssignPlatformRoleAsync(adminId, "PlatformAdministrator");
        var list = await SendAsync(HttpMethod.Get, "/api/v1/platform/privacy-compliance/requirements", adminId);
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var items = await list.Content.ReadFromJsonAsync<JsonElement>();
        var id = items.EnumerateArray().First().GetProperty("id").GetGuid();

        var pdf = await SendAsync(
            HttpMethod.Get,
            $"/api/v1/platform/privacy-compliance/requirements/{id:D}/export.pdf",
            adminId);
        Assert.Equal(HttpStatusCode.OK, pdf.StatusCode);
        var bytes = await pdf.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 4);
        Assert.Equal((byte)'%', bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'D', bytes[2]);
        Assert.Equal((byte)'F', bytes[3]);
    }

    private sealed class PrivacyComplianceApiFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:PlatformDatabase", connectionString);
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PlatformDatabase"] = connectionString,
                    ["Security:EnforceHttps"] = "false",
                    ["PlatformAuthentication:External:TestingEndpointEnabled"] = "true",
                    ["PlatformAuthentication:Lifecycle:ExposeDebugTokens"] = "true"
                });
            });
        }
    }
}
