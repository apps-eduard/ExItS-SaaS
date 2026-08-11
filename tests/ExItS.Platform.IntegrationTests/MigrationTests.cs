using Npgsql;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class MigrationTests(PostgreSqlFixture fixture)
{
    private static readonly string[] ExpectedTables =
    [
        "products",
        "feature_definitions",
        "plans",
        "plan_versions",
        "plan_version_feature_grants",
        "plan_version_business_type_grants",
        "trial_definitions",
        "trial_definition_feature_grants",
        "organizations",
        "organization_business_type_activations",
        "subscriptions",
        "saas_payments",
        "feature_overrides",
        "entitlement_snapshots",
        "entitlement_snapshot_grants",
        "platform_users",
        "organization_memberships",
        "product_access_assignments"
    ];

    private static readonly string[] ForbiddenTables =
    [
        "users",
        "memberships",
        "payments",
        "invoices",
        "hangfire",
        "gcash_clients",
        "patients",
        "AspNetUsers",
        "AspNetRoles",
        "password_hashes",
        "refresh_tokens",
        "mfa_secrets"
    ];

    [Fact]
    public async Task Platform_migrations_create_expected_schema_tables_only()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        var tables = await QueryPlatformTablesAsync(connection);

        foreach (var expected in ExpectedTables)
        {
            Assert.Contains(expected, tables);
        }

        foreach (var forbidden in ForbiddenTables)
        {
            Assert.DoesNotContain(forbidden, tables);
        }
    }

    private static async Task<HashSet<string>> QueryPlatformTablesAsync(NpgsqlConnection connection)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'platform'
              AND table_type = 'BASE TABLE'
            ORDER BY table_name
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
