using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApiCredentialPersistenceTests(PostgreSqlFixture fixture) : IAsyncLifetime
{
    private const string TestBootstrapSecret = "integration-test-bootstrap-secret-32b!";

    private CredentialApiFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new CredentialApiFactory(fixture.ConnectionString);
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
    public async Task Set_password_status_and_email_verified_without_login_session()
    {
        var username = UniqueToken("cred");
        var create = await _client.PostAsJsonAsync(
            "/api/v1/platform/users",
            new
            {
                username,
                firstName = "Cred",
                lastName = "User",
                displayName = "Cred User",
                email = $"{username}@example.com",
                platformRole = "PlatformSupport"
            });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var userId = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var before = await _client.GetAsync($"/api/v1/platform/users/{userId}/credentials");
        Assert.Equal(HttpStatusCode.OK, before.StatusCode);
        Assert.False((await before.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("hasPassword").GetBoolean());

        var set = await _client.PutAsJsonAsync(
            $"/api/v1/platform/users/{userId}/credentials/password",
            new { password = "Correct-Horse-9!" });
        Assert.Equal(HttpStatusCode.OK, set.StatusCode);
        var status = await set.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(status.GetProperty("hasPassword").GetBoolean());
        Assert.False(status.GetProperty("emailVerified").GetBoolean());
        Assert.False(status.TryGetProperty("passwordHash", out _));
        Assert.False(status.TryGetProperty("password", out _));

        var mark = await _client.PostAsync($"/api/v1/platform/users/{userId}/credentials/email-verified", null);
        Assert.Equal(HttpStatusCode.OK, mark.StatusCode);
        Assert.True((await mark.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("emailVerified").GetBoolean());
    }

    [Fact]
    public async Task Bootstrap_creates_first_admin_once_when_enabled_with_shared_secret()
    {
        var username = UniqueToken("boot");
        using var factory = new CredentialApiFactory(
            fixture.ConnectionString,
            bootstrapEnabled: true,
            bootstrapUsername: username,
            bootstrapEmail: $"{username}@example.com",
            bootstrapPassword: "Correct-Horse-9!",
            bootstrapSharedSecret: TestBootstrapSecret);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(PlatformAuthBootstrapOptions.SharedSecretHeaderName, TestBootstrapSecret);

        var first = await client.PostAsync("/api/v1/platform/auth/bootstrap", null);
        Assert.True(
            first.StatusCode is HttpStatusCode.Created or HttpStatusCode.Conflict,
            $"Unexpected bootstrap status {first.StatusCode}");

        if (first.StatusCode == HttpStatusCode.Created)
        {
            var body = await first.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(username, body.GetProperty("username").GetString());
        }

        var second = await client.PostAsync("/api/v1/platform/auth/bootstrap", null);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Bootstrap_rejects_missing_or_wrong_shared_secret()
    {
        var username = UniqueToken("sec");
        using var factory = new CredentialApiFactory(
            fixture.ConnectionString,
            bootstrapEnabled: true,
            bootstrapUsername: username,
            bootstrapEmail: $"{username}@example.com",
            bootstrapPassword: "Correct-Horse-9!",
            bootstrapSharedSecret: TestBootstrapSecret);
        using var client = factory.CreateClient();

        var missing = await client.PostAsync("/api/v1/platform/auth/bootstrap", null);
        Assert.Equal(HttpStatusCode.Forbidden, missing.StatusCode);

        client.DefaultRequestHeaders.Add(PlatformAuthBootstrapOptions.SharedSecretHeaderName, "wrong-secret-not-matching-config!!");
        var wrong = await client.PostAsync("/api/v1/platform/auth/bootstrap", null);
        Assert.Equal(HttpStatusCode.Forbidden, wrong.StatusCode);
    }

    [Fact]
    public async Task Bootstrap_fails_when_disabled()
    {
        var response = await _client.PostAsync("/api/v1/platform/auth/bootstrap", null);
        Assert.True(
            response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict or HttpStatusCode.Forbidden,
            $"Unexpected status {response.StatusCode}");
    }

    [Fact]
    public async Task Bootstrap_is_forbidden_in_production_environment()
    {
        var username = UniqueToken("prod");
        using var factory = new CredentialApiFactory(
            fixture.ConnectionString,
            bootstrapEnabled: false,
            bootstrapUsername: username,
            bootstrapEmail: $"{username}@example.com",
            bootstrapPassword: "Correct-Horse-9!",
            bootstrapSharedSecret: TestBootstrapSecret,
            environmentName: "Production",
            allowedHosts: "localhost");
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(PlatformAuthBootstrapOptions.SharedSecretHeaderName, TestBootstrapSecret);

        var response = await client.PostAsync("/api/v1/platform/auth/bootstrap", null);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public void Production_startup_rejects_bootstrap_enabled_configuration()
    {
        using var factory = new CredentialApiFactory(
            fixture.ConnectionString,
            bootstrapEnabled: true,
            bootstrapUsername: "should-not-start",
            bootstrapEmail: "should-not-start@example.com",
            bootstrapPassword: "Correct-Horse-9!",
            bootstrapSharedSecret: TestBootstrapSecret,
            environmentName: "Production",
            allowedHosts: "localhost");

        var ex = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("Bootstrap", ex.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}

[Collection(PostgreSqlCollection.Name)]
public sealed class CredentialMigrationTests(PostgreSqlFixture fixture)
{
    private const string TargetMigration = "AddPlatformUserCredentials";
    private const string PreviousMigration = "AddPlatformAuthorizationAndAudit";

    [Fact]
    public async Task Credential_migration_applies_rolls_back_and_reapplies()
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        await using (var context = new PlatformDbContext(options))
        {
            await context.Database.MigrateAsync();
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.Contains(applied, m => m.Contains(TargetMigration, StringComparison.Ordinal));
        }

        await AssertTablePresentAsync("platform_user_credentials");

        await using (var context = new PlatformDbContext(options))
        {
            await context.Database.MigrateAsync(PreviousMigration);
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.DoesNotContain(applied, m => m.Contains(TargetMigration, StringComparison.Ordinal));
        }

        await AssertTableAbsentAsync("platform_user_credentials");
        await AssertTablePresentAsync("platform_users");
        await AssertTablePresentAsync("audit_records");

        await using (var context = new PlatformDbContext(options))
        {
            await context.Database.MigrateAsync();
        }

        await AssertTablePresentAsync("platform_user_credentials");
    }

    private async Task AssertTablePresentAsync(string table)
    {
        var tables = await QueryPlatformTablesAsync();
        Assert.Contains(table, tables);
    }

    private async Task AssertTableAbsentAsync(string table)
    {
        var tables = await QueryPlatformTablesAsync();
        Assert.DoesNotContain(table, tables);
    }

    private async Task<HashSet<string>> QueryPlatformTablesAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'platform'
              AND table_type = 'BASE TABLE'
            """,
            connection);

        var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }
}

internal sealed class CredentialApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly bool _bootstrapEnabled;
    private readonly string _bootstrapUsername;
    private readonly string _bootstrapEmail;
    private readonly string _bootstrapPassword;
    private readonly string _bootstrapSharedSecret;
    private readonly string _environmentName;
    private readonly string? _allowedHosts;

    public CredentialApiFactory(
        string connectionString,
        bool bootstrapEnabled = false,
        string bootstrapUsername = "",
        string bootstrapEmail = "",
        string bootstrapPassword = "",
        string bootstrapSharedSecret = "",
        string environmentName = "Testing",
        string? allowedHosts = null)
    {
        _connectionString = connectionString;
        _bootstrapEnabled = bootstrapEnabled;
        _bootstrapUsername = bootstrapUsername;
        _bootstrapEmail = bootstrapEmail;
        _bootstrapPassword = bootstrapPassword;
        _bootstrapSharedSecret = bootstrapSharedSecret;
        _environmentName = environmentName;
        _allowedHosts = allowedHosts;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environmentName);
        builder.UseSetting("ConnectionStrings:PlatformDatabase", _connectionString);
        builder.UseSetting("Security:EnforceHttps", "false");
        builder.UseSetting("PlatformAuthentication:Bootstrap:Enabled", _bootstrapEnabled ? "true" : "false");
        builder.UseSetting("PlatformAuthentication:Bootstrap:SharedSecret", _bootstrapSharedSecret);
        builder.UseSetting("PlatformAuthentication:Bootstrap:Username", _bootstrapUsername);
        builder.UseSetting("PlatformAuthentication:Bootstrap:DisplayName", "Bootstrap Admin");
        builder.UseSetting("PlatformAuthentication:Bootstrap:Email", _bootstrapEmail);
        builder.UseSetting("PlatformAuthentication:Bootstrap:Password", _bootstrapPassword);

        if (_allowedHosts is not null)
        {
            builder.UseSetting("AllowedHosts", _allowedHosts);
        }

        builder.ConfigureAppConfiguration((_, config) =>
        {
            var values = new Dictionary<string, string?>
            {
                ["ConnectionStrings:PlatformDatabase"] = _connectionString,
                ["PlatformAuthentication:Bootstrap:Enabled"] = _bootstrapEnabled ? "true" : "false",
                ["PlatformAuthentication:Bootstrap:SharedSecret"] = _bootstrapSharedSecret,
                ["PlatformAuthentication:Bootstrap:Username"] = _bootstrapUsername,
                ["PlatformAuthentication:Bootstrap:DisplayName"] = "Bootstrap Admin",
                ["PlatformAuthentication:Bootstrap:Email"] = _bootstrapEmail,
                ["PlatformAuthentication:Bootstrap:Password"] = _bootstrapPassword,
                ["Security:EnforceHttps"] = "false"
            };

            if (_allowedHosts is not null)
            {
                values["AllowedHosts"] = _allowedHosts;
            }

            config.AddInMemoryCollection(values);
        });
    }
}
