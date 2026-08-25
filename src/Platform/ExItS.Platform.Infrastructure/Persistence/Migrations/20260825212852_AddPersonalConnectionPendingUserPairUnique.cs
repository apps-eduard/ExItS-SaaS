using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddPersonalConnectionPendingUserPairUnique : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            WITH ranked AS (
                SELECT id,
                       row_number() OVER (
                           PARTITION BY LEAST(requester_user_identity_id, target_user_identity_id),
                                        GREATEST(requester_user_identity_id, target_user_identity_id)
                           ORDER BY created_at_utc ASC, id ASC) AS rn
                FROM platform.personal_connection_requests
                WHERE status = 'Pending'
            )
            UPDATE platform.personal_connection_requests AS r
            SET status = 'Revoked',
                revoked_at_utc = now() AT TIME ZONE 'utc',
                updated_at_utc = now() AT TIME ZONE 'utc'
            FROM ranked
            WHERE r.id = ranked.id
              AND ranked.rn > 1;
            """);

        migrationBuilder.Sql(
            """
            CREATE UNIQUE INDEX ux_personal_connection_requests_pending_user_pair
            ON platform.personal_connection_requests (
                LEAST(requester_user_identity_id, target_user_identity_id),
                GREATEST(requester_user_identity_id, target_user_identity_id))
            WHERE status = 'Pending';
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP INDEX IF EXISTS platform.ux_personal_connection_requests_pending_user_pair;
            """);
    }
}
