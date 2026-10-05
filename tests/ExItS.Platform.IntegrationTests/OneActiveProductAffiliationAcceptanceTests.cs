using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.GlobalCatalog;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;
using ExItS.Platform.Infrastructure;
using ExItS.Platform.Infrastructure.Persistence;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace ExItS.Platform.IntegrationTests;

/// <summary>
/// Live PostgreSQL proof for one active organization affiliation per user and product.
/// Races use independent HTTP clients, which open independent database sessions.
/// </summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class OneActiveProductAffiliationAcceptanceTests(PostgreSqlFixture fixture) : IAsyncLifetime
{
    private const string PreviousMigration = "20261004143000_OneOpenSubscriptionCheckout";
    private const string AffiliationMigration = "20261005130000_OneActiveProductAffiliationPerUser";

    private Wp11LocalValidationApiFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new Wp11LocalValidationApiFactory(fixture.ConnectionString);
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        _client.Timeout = TimeSpan.FromMinutes(3);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Active_partial_index_preserves_history_rejects_duplicates_and_aborts_conflicts()
    {
        var now = DateTimeOffset.UtcNow;
        var userId = Guid.NewGuid();
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();
        var orgLoan = Guid.NewGuid();
        var membershipA = Guid.NewGuid();
        var membershipB = Guid.NewGuid();
        var membershipLoan = Guid.NewGuid();
        var accessA = Guid.NewGuid();
        var accessB = Guid.NewGuid();
        var accessLoan = Guid.NewGuid();
        var slug = Guid.NewGuid().ToString("N")[..12];

        await using var setup = new NpgsqlConnection(fixture.ConnectionString);
        await setup.OpenAsync();
        try
        {
            await ExecAsync(
                setup,
                """
                INSERT INTO platform.products (id, code, display_name, status, created_at_utc, updated_at_utc)
                VALUES (@id, 'pinoy-loan-manager', 'Pinoy Loan Manager', 'Active', @now, @now)
                ON CONFLICT (code) DO NOTHING;
                """,
                ("id", Guid.NewGuid()),
                ("now", now));

            await ExecAsync(
                setup,
                """
                INSERT INTO platform.products (id, code, display_name, status, created_at_utc, updated_at_utc)
                VALUES (@id, 'pinoy-business-pos', 'Pinoy Business POS', 'Active', @now, @now)
                ON CONFLICT (code) DO NOTHING;
                """,
                ("id", Guid.NewGuid()),
                ("now", now));

            await ExecAsync(
                setup,
                """
                INSERT INTO platform.platform_users
                  (id, username, normalized_username, display_name, normalized_email, status, created_at_utc, updated_at_utc)
                VALUES
                  (@id, @username, @username, 'Affiliation User', @email, 'Active', @now, @now);
                """,
                ("id", userId),
                ("username", "aff" + slug),
                ("email", $"aff-{slug}@example.com"),
                ("now", now));

            await ExecAsync(
                setup,
                """
                INSERT INTO platform.organizations (id, display_name, slug, status, created_at_utc, updated_at_utc)
                VALUES
                  (@a, 'Hidden Org A', @slugA, 'Active', @now, @now),
                  (@b, 'Hidden Org B', @slugB, 'Active', @now, @now),
                  (@loan, 'Hidden Loan Org', @slugLoan, 'Active', @now, @now);
                """,
                ("a", orgA),
                ("b", orgB),
                ("loan", orgLoan),
                ("slugA", "a-" + slug),
                ("slugB", "b-" + slug),
                ("slugLoan", "l-" + slug),
                ("now", now));

            await ExecAsync(
                setup,
                """
                INSERT INTO platform.organization_memberships
                  (id, organization_id, user_id, role, status, branch_access_scope, is_business_contact, created_at_utc, updated_at_utc)
                VALUES
                  (@ma, @a, @user, 'OrganizationOwner', 'Active', 'AllActive', FALSE, @now, @now),
                  (@mb, @b, @user, 'OrganizationMember', 'Active', 'AllActive', FALSE, @now, @now),
                  (@ml, @loan, @user, 'OrganizationMember', 'Active', 'AllActive', FALSE, @now, @now);
                """,
                ("ma", membershipA),
                ("mb", membershipB),
                ("ml", membershipLoan),
                ("a", orgA),
                ("b", orgB),
                ("loan", orgLoan),
                ("user", userId),
                ("now", now));

            var indexDef = await ScalarAsync(
                setup,
                """
                SELECT indexdef
                FROM pg_indexes
                WHERE schemaname = 'platform'
                  AND indexname = 'ux_product_access_assignments_user_product_active';
                """);
            Assert.Contains("UNIQUE", indexDef, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("user_id", indexDef, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("product_code", indexDef, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Active", indexDef, StringComparison.Ordinal);
            Assert.Contains("WHERE", indexDef, StringComparison.OrdinalIgnoreCase);

            await InsertAccessAsync(setup, accessA, userId, orgA, membershipA, ProductCode.PinoyBusinessPos, "Active", now);

            await ExecAsync(
                setup,
                """
                UPDATE platform.product_access_assignments
                SET status = 'Revoked', revoked_at_utc = @now, revoked_by_actor = 'test', updated_at_utc = @now
                WHERE id = @id;
                """,
                ("id", accessA),
                ("now", now));

            await InsertAccessAsync(setup, accessB, userId, orgB, membershipB, ProductCode.PinoyBusinessPos, "Active", now);
            await InsertAccessAsync(setup, accessLoan, userId, orgLoan, membershipLoan, ProductCode.PinoyLoanManager, "Active", now);
            var blocked = await Assert.ThrowsAsync<PostgresException>(() => InsertAccessAsync(
                setup,
                Guid.NewGuid(),
                userId,
                orgA,
                membershipA,
                ProductCode.PinoyBusinessPos,
                "Active",
                now));
            Assert.Equal("23505", blocked.SqlState);

            await ExecAsync(
                setup,
                """
                UPDATE platform.product_access_assignments
                SET status = 'Revoked', revoked_at_utc = @now, revoked_by_actor = 'test', updated_at_utc = @now
                WHERE id = @id;
                """,
                ("id", accessB),
                ("now", now));

            await using var racingA = new NpgsqlConnection(fixture.ConnectionString);
            await using var racingB = new NpgsqlConnection(fixture.ConnectionString);
            await racingA.OpenAsync();
            await racingB.OpenAsync();
            var duplicateA = Guid.NewGuid();
            var duplicateB = Guid.NewGuid();
            var firstInsert = InsertAccessAsync(racingA, duplicateA, userId, orgA, membershipA, ProductCode.PinoyBusinessPos, "Active", now);
            var secondInsert = InsertAccessAsync(racingB, duplicateB, userId, orgB, membershipB, ProductCode.PinoyBusinessPos, "Active", now);
            var outcomes = await Task.WhenAll(
                CaptureAsync(firstInsert),
                CaptureAsync(secondInsert));
            Assert.Single(outcomes, outcome => outcome is null);
            var rejected = Assert.Single(outcomes, outcome => outcome is not null);
            Assert.Equal("23505", rejected!.SqlState);

            var activePos = await CountAsync(
                setup,
                """
                SELECT COUNT(*)
                FROM platform.product_access_assignments
                WHERE user_id = @user AND product_code = 'pinoy-business-pos' AND status = 'Active';
                """,
                ("user", userId));
            Assert.Equal(1, activePos);

            var revoked = await CountAsync(
                setup,
                """
                SELECT COUNT(*)
                FROM platform.product_access_assignments
                WHERE id = @id AND status = 'Revoked';
                """,
                ("id", accessA));
            Assert.Equal(1, revoked);

            await ExecAsync(setup, "DROP INDEX platform.ux_product_access_assignments_user_product_active;");
            await ExecAsync(
                setup,
                """
                UPDATE platform.product_access_assignments
                SET status = 'Revoked', revoked_at_utc = @now, revoked_by_actor = 'test', updated_at_utc = @now
                WHERE user_id = @user AND product_code = 'pinoy-business-pos' AND status = 'Active';
                """,
                ("user", userId),
                ("now", now));
            var conflictA = Guid.NewGuid();
            var conflictB = Guid.NewGuid();
            await InsertAccessAsync(setup, conflictA, userId, orgA, membershipA, ProductCode.PinoyBusinessPos, "Active", now);
            await InsertAccessAsync(setup, conflictB, userId, orgB, membershipB, ProductCode.PinoyBusinessPos, "Active", now);

            var before = await CountAsync(
                setup,
                "SELECT COUNT(*) FROM platform.product_access_assignments WHERE user_id = @user;",
                ("user", userId));
            var conflict = await Assert.ThrowsAsync<PostgresException>(() => ExecAsync(
                setup,
                """
                DO $$
                DECLARE
                  conflict_count integer;
                BEGIN
                  SELECT COUNT(*) INTO conflict_count
                  FROM (
                    SELECT user_id, product_code
                    FROM platform.product_access_assignments
                    WHERE status = 'Active'
                    GROUP BY user_id, product_code
                    HAVING COUNT(DISTINCT organization_id) > 1
                  ) conflicts;
                  IF conflict_count > 0 THEN
                    RAISE EXCEPTION
                      'Cannot enforce one active organization affiliation per user and product. % user/product pairs already have more than one active organization. Do not delete memberships. End one active affiliation per pair, then retry this migration.',
                      conflict_count;
                  END IF;
                END $$;
                """));
            Assert.Contains("one active organization affiliation", conflict.MessageText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("1 user/product", conflict.MessageText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Hidden Org", conflict.MessageText, StringComparison.Ordinal);
            Assert.DoesNotContain(orgA.ToString(), conflict.MessageText, StringComparison.OrdinalIgnoreCase);

            var after = await CountAsync(
                setup,
                "SELECT COUNT(*) FROM platform.product_access_assignments WHERE user_id = @user;",
                ("user", userId));
            Assert.Equal(before, after);

            await ExecAsync(
                setup,
                """
                DELETE FROM platform.product_access_assignments
                WHERE user_id = @user
                  AND product_code = 'pinoy-business-pos'
                  AND status = 'Active'
                  AND id <> @keep;
                """,
                ("user", userId),
                ("keep", accessB));
            await ExecAsync(
                setup,
                """
                CREATE UNIQUE INDEX IF NOT EXISTS ux_product_access_assignments_user_product_active
                  ON platform.product_access_assignments (user_id, product_code)
                  WHERE status = 'Active';
                """);

            var options = new DbContextOptionsBuilder<PlatformDbContext>()
                .UseNpgsql(fixture.ConnectionString)
                .Options;
            await using (var context = new PlatformDbContext(options))
            {
                var rowCount = await CountAsync(setup, "SELECT COUNT(*) FROM platform.product_access_assignments;");
                var migrator = context.GetService<IMigrator>();
                await migrator.MigrateAsync(PreviousMigration);
                var indexAfterDown = await ScalarAsync(
                    setup,
                    """
                    SELECT COUNT(*)::text
                    FROM pg_indexes
                    WHERE schemaname = 'platform'
                      AND indexname = 'ux_product_access_assignments_user_product_active';
                    """);
                Assert.Equal("0", indexAfterDown);
                await migrator.MigrateAsync(AffiliationMigration);
                var rowCountAfter = await CountAsync(setup, "SELECT COUNT(*) FROM platform.product_access_assignments;");
                Assert.Equal(rowCount, rowCountAfter);
            }

            var restored = await ScalarAsync(
                setup,
                """
                SELECT indexdef
                FROM pg_indexes
                WHERE schemaname = 'platform'
                  AND indexname = 'ux_product_access_assignments_user_product_active';
                """);
            Assert.Contains("Active", restored, StringComparison.Ordinal);
            Assert.Contains("WHERE", restored, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await using var cleanup = new NpgsqlConnection(fixture.ConnectionString);
            await cleanup.OpenAsync();
            await ExecAsync(
                cleanup,
                """
                CREATE UNIQUE INDEX IF NOT EXISTS ux_product_access_assignments_user_product_active
                  ON platform.product_access_assignments (user_id, product_code)
                  WHERE status = 'Active';
                """);
            await ExecAsync(
                cleanup,
                "DELETE FROM platform.product_access_assignments WHERE user_id = @user;",
                ("user", userId));
            await ExecAsync(
                cleanup,
                "DELETE FROM platform.organization_memberships WHERE user_id = @user;",
                ("user", userId));
            await ExecAsync(
                cleanup,
                "DELETE FROM platform.platform_users WHERE id = @user;",
                ("user", userId));
            await ExecAsync(
                cleanup,
                "DELETE FROM platform.organizations WHERE id IN (@a, @b, @loan);",
                ("a", orgA),
                ("b", orgB),
                ("loan", orgLoan));
        }
    }

    [Fact]
    public async Task Concurrent_start_business_keeps_one_pos_affiliation()
    {
        await EnsureMvpCatalogAsync();
        var (token, userId, _, _) = await SeedPersonalUserAsync("racea");
        var businessTypeId = await ResolvePrimaryBusinessTypeIdAsync(token);
        using var clientA = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        using var clientB = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        clientA.Timeout = TimeSpan.FromMinutes(3);
        clientB.Timeout = TimeSpan.FromMinutes(3);

        var first = SendStartAsync(clientA, token, businessTypeId, "racea1");
        var second = SendStartAsync(clientB, token, businessTypeId, "racea2");
        await Task.WhenAll(first, second);
        using var responseA = await first;
        using var responseB = await second;

        var active = await CountAsync(
            """
            SELECT COUNT(*)
            FROM platform.product_access_assignments
            WHERE user_id = @user AND product_code = 'pinoy-business-pos' AND status = 'Active';
            """,
            ("user", userId));
        var owners = await CountAsync(
            """
            SELECT COUNT(*)
            FROM platform.organization_memberships
            WHERE user_id = @user AND role = 'OrganizationOwner' AND status = 'Active';
            """,
            ("user", userId));

        Assert.Equal(1, active);
        Assert.Equal(1, owners);
        var bodyA = await responseA.Content.ReadAsStringAsync();
        var bodyB = await responseB.Content.ReadAsStringAsync();
        Assert.True(
            responseA.IsSuccessStatusCode || responseB.IsSuccessStatusCode,
            $"Both start-business calls failed: {responseA.StatusCode} {bodyA} | {responseB.StatusCode} {bodyB}");
    }

    [Fact]
    public async Task Concurrent_invitation_accept_keeps_one_pos_affiliation()
    {
        await EnsureMvpCatalogAsync();
        var orgA = await StartOwnerAsync("invA");
        var orgB = await StartOwnerAsync("invB");
        var contactEmail = $"{Guid.NewGuid():N}"[..12] + "@example.com";
        var tokenA = await InviteStaffAsync(orgA.Token, orgA.OrganizationId, contactEmail);
        var tokenB = await InviteStaffAsync(orgB.Token, orgB.OrganizationId, contactEmail);

        using var clientA = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        using var clientB = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var acceptA = clientA.PostAsJsonAsync(
            "/api/v1/platform/invitations/accept",
            new { token = tokenA, password = "Correct-Horse-9!" });
        var acceptB = clientB.PostAsJsonAsync(
            "/api/v1/platform/invitations/accept",
            new { token = tokenB, password = "Correct-Horse-9!" });
        await Task.WhenAll(acceptA, acceptB);
        using var responseA = await acceptA;
        using var responseB = await acceptB;
        var bodyA = await responseA.Content.ReadAsStringAsync();
        var bodyB = await responseB.Content.ReadAsStringAsync();
        var statuses = new[] { responseA.StatusCode, responseB.StatusCode };
        Assert.Contains(HttpStatusCode.OK, statuses);
        Assert.Single(statuses, status => status == HttpStatusCode.OK);

        var loser = responseA.StatusCode == HttpStatusCode.OK ? bodyB : bodyA;
        using var loserJson = JsonDocument.Parse(loser);
        Assert.Equal(
            ApplicationErrorCodes.ProductAffiliationConflict,
            loserJson.RootElement.GetProperty("errorCode").GetString());
        Assert.DoesNotContain(orgA.DisplayName, loser, StringComparison.Ordinal);
        Assert.DoesNotContain(orgB.DisplayName, loser, StringComparison.Ordinal);
        Assert.DoesNotContain(orgA.OrganizationId.ToString(), loser, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(orgB.OrganizationId.ToString(), loser, StringComparison.OrdinalIgnoreCase);

        var staffUserId = await StaffUserIdByContactEmailAsync(contactEmail);
        var active = await CountAsync(
            """
            SELECT COUNT(*)
            FROM platform.product_access_assignments
            WHERE user_id = @user AND product_code = 'pinoy-business-pos' AND status = 'Active';
            """,
            ("user", staffUserId));
        Assert.Equal(1, active);
    }

    [Fact]
    public async Task Concurrent_staff_accept_stops_at_one_remaining_seat()
    {
        await EnsureMvpCatalogAsync();
        var org = await StartOwnerAsync("seat");
        var updated = await ExecCountAsync(
            """
            UPDATE platform.entitlement_snapshot_grants
            SET numeric_limit = 1
            WHERE feature_code = 'plan-max-active-staff'
              AND snapshot_id IN (
                SELECT id FROM platform.entitlement_snapshots WHERE organization_id = @org
              );
            """,
            ("org", org.OrganizationId));
        Assert.True(updated >= 1);

        var emailA = $"{Guid.NewGuid():N}"[..12] + "@example.com";
        var emailB = $"{Guid.NewGuid():N}"[..12] + "@example.com";
        var tokenA = await InviteStaffAsync(org.Token, org.OrganizationId, emailA);
        var tokenB = await InviteStaffAsync(org.Token, org.OrganizationId, emailB);

        using var clientA = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        using var clientB = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var acceptA = clientA.PostAsJsonAsync(
            "/api/v1/platform/invitations/accept",
            new { token = tokenA, password = "Correct-Horse-9!" });
        var acceptB = clientB.PostAsJsonAsync(
            "/api/v1/platform/invitations/accept",
            new { token = tokenB, password = "Correct-Horse-9!" });
        await Task.WhenAll(acceptA, acceptB);
        using var responseA = await acceptA;
        using var responseB = await acceptB;
        var bodyA = await responseA.Content.ReadAsStringAsync();
        var bodyB = await responseB.Content.ReadAsStringAsync();
        var statuses = new[] { responseA.StatusCode, responseB.StatusCode };
        Assert.Contains(HttpStatusCode.OK, statuses);
        Assert.Single(statuses, status => status == HttpStatusCode.OK);

        var loser = responseA.StatusCode == HttpStatusCode.OK ? bodyB : bodyA;
        using var loserJson = JsonDocument.Parse(loser);
        Assert.Equal(
            ApplicationErrorCodes.StaffSeatLimitReached,
            loserJson.RootElement.GetProperty("errorCode").GetString());

        var staff = await CountAsync(
            """
            SELECT COUNT(*)
            FROM platform.organization_memberships
            WHERE organization_id = @org AND role <> 'OrganizationOwner' AND status = 'Active';
            """,
            ("org", org.OrganizationId));
        Assert.Equal(1, staff);
    }

    [Fact]
    public async Task Same_email_cannot_cross_pos_owner_and_staff_roles()
    {
        await EnsureMvpCatalogAsync();
        var org = await StartOwnerAsync("cross");
        var contactEmail = $"{Guid.NewGuid():N}"[..12] + "@example.com";
        var staffToken = await InviteStaffAsync(org.Token, org.OrganizationId, contactEmail);
        var accepted = await _client.PostAsJsonAsync(
            "/api/v1/platform/invitations/accept",
            new { token = staffToken, password = "Correct-Horse-9!" });
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);

        var (personalToken, personalUserId, _, _) = await SeedPersonalUserAsync("same", contactEmail);
        var businessTypeId = await ResolvePrimaryBusinessTypeIdAsync(personalToken);
        using var start = Authed(
            HttpMethod.Post,
            "/api/v1/personal/start-business",
            personalToken,
            StartBody(businessTypeId, "samepos"));
        var startResponse = await _client.SendAsync(start);
        var startBody = await startResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Conflict, startResponse.StatusCode);
        using var startJson = JsonDocument.Parse(startBody);
        Assert.Equal(
            ApplicationErrorCodes.ProductAffiliationConflict,
            startJson.RootElement.GetProperty("errorCode").GetString());
        Assert.DoesNotContain(org.DisplayName, startBody, StringComparison.Ordinal);
        Assert.DoesNotContain(org.OrganizationId.ToString(), startBody, StringComparison.OrdinalIgnoreCase);

        var personalActive = await CountAsync(
            """
            SELECT COUNT(*)
            FROM platform.product_access_assignments
            WHERE user_id = @user AND product_code = 'pinoy-business-pos' AND status = 'Active';
            """,
            ("user", personalUserId));
        Assert.Equal(0, personalActive);

        var owner = await StartOwnerAsync("own2");
        var ownerInvite = await InviteStaffAsync(owner.Token, owner.OrganizationId, org.Email);
        var personalSession = await EnsurePersonalSessionAsync(org.Email, org.Password);
        using var acceptOwner = Authed(
            HttpMethod.Post,
            "/api/v1/platform/invitations/accept-as-personal",
            personalSession,
            new { token = ownerInvite, password = "Correct-Horse-9!" });
        var acceptResponse = await _client.SendAsync(acceptOwner);
        var acceptBody = await acceptResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Conflict, acceptResponse.StatusCode);
        using var acceptJson = JsonDocument.Parse(acceptBody);
        Assert.Equal(
            ApplicationErrorCodes.ProductAffiliationConflict,
            acceptJson.RootElement.GetProperty("errorCode").GetString());
        Assert.DoesNotContain(owner.DisplayName, acceptBody, StringComparison.Ordinal);
        Assert.DoesNotContain(org.DisplayName, acceptBody, StringComparison.Ordinal);
    }

    private async Task<(Guid OrganizationId, string Token, string Email, string Password, string DisplayName)> StartOwnerAsync(
        string prefix)
    {
        var (token, _, email, password) = await SeedPersonalUserAsync(prefix);
        var businessTypeId = await ResolvePrimaryBusinessTypeIdAsync(token);
        var displayName = "Org " + prefix + Guid.NewGuid().ToString("N")[..6];
        using var start = Authed(
            HttpMethod.Post,
            "/api/v1/personal/start-business",
            token,
            StartBody(businessTypeId, prefix, displayName));
        var response = await _client.SendAsync(start);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Created, body);
        using var json = JsonDocument.Parse(body);
        return (
            json.RootElement.GetProperty("organizationId").GetGuid(),
            json.RootElement.GetProperty("sessionToken").GetString()!,
            email,
            password,
            displayName);
    }

    private async Task<string> InviteStaffAsync(string ownerToken, Guid organizationId, string email)
    {
        using var invite = Authed(
            HttpMethod.Post,
            $"/api/v1/organizations/{organizationId}/staff-invitations",
            ownerToken,
            new { email, role = "OrganizationMember", productRole = "Cashier" });
        var response = await _client.SendAsync(invite);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Created, body);
        using var json = JsonDocument.Parse(body);
        return json.RootElement.GetProperty("acceptToken").GetString()!;
    }

    private async Task<Guid> StaffUserIdByLoginEmailAsync(string email)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT id
            FROM platform.platform_users
            WHERE normalized_email = @email
            LIMIT 1;
            """,
            connection);
        command.Parameters.AddWithValue("email", email.Trim().ToLowerInvariant());
        var result = await command.ExecuteScalarAsync();
        return (Guid)result!;
    }

    private async Task<Guid> StaffUserIdByContactEmailAsync(string email)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT id
            FROM platform.platform_users
            WHERE normalized_contact_email = @email
            LIMIT 1;
            """,
            connection);
        command.Parameters.AddWithValue("email", email.Trim().ToLowerInvariant());
        var result = await command.ExecuteScalarAsync();
        return (Guid)result!;
    }

    private Task<HttpResponseMessage> SendStartAsync(HttpClient client, string token, Guid businessTypeId, string prefix)
    {
        var request = Authed(
            HttpMethod.Post,
            "/api/v1/personal/start-business",
            token,
            StartBody(businessTypeId, prefix));
        return client.SendAsync(request);
    }

    private static object StartBody(Guid businessTypeId, string prefix, string? displayName = null) => new
    {
        displayName = displayName ?? ("Race " + prefix),
        slug = (prefix + Guid.NewGuid().ToString("N"))[..16],
        primaryBusinessTypeId = businessTypeId,
        productCode = ProductCode.PinoyBusinessPos,
        planKey = MvpPosPlanCodes.Starter,
        billingCycle = BillingCycle.Monthly,
        startAsTrial = true,
        payNow = false,
        activatePosEntitlement = true,
        activateProductAccess = true,
        assignPosOwnerRole = true
    };

    private async Task<(string Token, Guid UserId, string Email, string Password)> SeedPersonalUserAsync(
        string prefix,
        string? email = null)
    {
        if (email is null)
        {
            var (userId, registeredEmail, password) =
                await PlatformIntegrationTestUsers.RegisterPersonalWithPasswordAsync(_client, prefix);
            var login = await _client.PostAsJsonAsync(
                "/api/v1/platform/auth/login",
                new { usernameOrEmail = registeredEmail, password });
            login.EnsureSuccessStatusCode();
            var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionToken").GetString()!;
            return (token, userId, registeredEmail, password);
        }

        var chosenPassword = "Correct-Horse-9!";
        var register = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/register",
            new { displayName = "Personal User", email });
        register.EnsureSuccessStatusCode();
        var debugToken = (await register.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("debugToken").GetString();
        var activate = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/activate-account",
            new { token = debugToken, password = chosenPassword });
        activate.EnsureSuccessStatusCode();
        var loginExisting = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = email, password = chosenPassword });
        loginExisting.EnsureSuccessStatusCode();
        var loginBody = await loginExisting.Content.ReadFromJsonAsync<JsonElement>();
        var user = await StaffUserIdByLoginEmailAsync(email);
        return (loginBody.GetProperty("sessionToken").GetString()!, user, email, chosenPassword);
    }

    private async Task<string> EnsurePersonalSessionAsync(string email, string password)
    {
        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = email, password });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionToken").GetString()!;
        using var profilesRequest = Authed(HttpMethod.Get, "/api/v1/platform/auth/account-profiles", token);
        var profilesResponse = await _client.SendAsync(profilesRequest);
        profilesResponse.EnsureSuccessStatusCode();
        var personalProfileId = (await profilesResponse.Content.ReadFromJsonAsync<JsonElement>())
            .EnumerateArray()
            .First(profile => profile.GetProperty("accountClass").GetString() == "Personal")
            .GetProperty("id")
            .GetGuid();
        using var selectRequest = Authed(
            HttpMethod.Post,
            "/api/v1/platform/auth/account-profiles/select",
            token,
            new { accountProfileId = personalProfileId });
        var selectResponse = await _client.SendAsync(selectRequest);
        selectResponse.EnsureSuccessStatusCode();
        return (await selectResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionToken").GetString()!;
    }

    private async Task<Guid> ResolvePrimaryBusinessTypeIdAsync(string personalToken)
    {
        using var request = Authed(HttpMethod.Get, "/api/v1/personal/onboarding/business-types", personalToken);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var businessTypes = await response.Content.ReadFromJsonAsync<JsonElement>();
        return businessTypes.EnumerateArray().First().GetProperty("id").GetGuid();
    }

    private async Task EnsureMvpCatalogAsync()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PlatformDatabase"] = fixture.ConnectionString
            })
            .Build();
        var services = new ServiceCollection();
        services.AddPlatformPersistence(configuration);
        services.AddLogging();
        services.AddScoped<CreateProduct>();
        services.AddScoped<CreateFeatureDefinition>();
        services.AddScoped<CreatePlan>();
        services.AddScoped<ActivatePlan>();
        services.AddScoped<UpdatePlanCommercialPackage>();
        services.AddScoped<CreateDraftPlanVersion>();
        services.AddScoped<PublishExistingPlanVersion>();
        services.AddScoped<CreateTrialDefinition>();
        services.AddScoped<RetirePlan>();
        services.AddScoped<EnsureMvpPosPlans>();
        services.AddScoped<EnsurePhilippinePosStarterCatalog>();
        await using var provider = services.BuildServiceProvider();
        var createProduct = provider.GetRequiredService<CreateProduct>();
        var productResult = await createProduct.ExecuteAsync(ProductCode.PinoyBusinessPos, "Pinoy Business POS");
        if (!productResult.IsSuccess && productResult.ErrorCode != ApplicationErrorCodes.DuplicateProductCode)
        {
            throw new InvalidOperationException(productResult.ErrorMessage);
        }

        await provider.GetRequiredService<EnsureMvpPosPlans>().ExecuteAsync();
        await provider.GetRequiredService<EnsurePhilippinePosStarterCatalog>().ExecuteAsync();
    }

    private async Task<int> CountAsync(string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        return await CountAsync(connection, sql, parameters);
    }

    private async Task<int> ExecCountAsync(string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        return await command.ExecuteNonQueryAsync();
    }

    private static async Task<int> CountAsync(
        NpgsqlConnection connection,
        string sql,
        params (string Name, object Value)[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        return Convert.ToInt32(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task<string> ScalarAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return (string)(await command.ExecuteScalarAsync())!;
    }

    private static async Task InsertAccessAsync(
        NpgsqlConnection connection,
        Guid id,
        Guid userId,
        Guid organizationId,
        Guid membershipId,
        string productCode,
        string status,
        DateTimeOffset now)
    {
        await ExecAsync(
            connection,
            """
            INSERT INTO platform.product_access_assignments
              (id, user_id, organization_id, membership_id, product_code, status, granted_at_utc, granted_by_actor, created_at_utc, updated_at_utc)
            VALUES
              (@id, @user, @org, @membership, @product, @status, @now, 'test', @now, @now);
            """,
            ("id", id),
            ("user", userId),
            ("org", organizationId),
            ("membership", membershipId),
            ("product", productCode),
            ("status", status),
            ("now", now));
    }

    private static async Task ExecAsync(
        NpgsqlConnection connection,
        string sql,
        params (string Name, object Value)[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        await command.ExecuteNonQueryAsync();
    }

    private static async Task<PostgresException?> CaptureAsync(Task insert)
    {
        try
        {
            await insert;
            return null;
        }
        catch (PostgresException exception)
        {
            return exception;
        }
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
}
