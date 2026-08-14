using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationOwnershipTransfers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent for Local Validation volumes that may partially apply schema outside EF history.
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS platform.organization_ownership_transfers (
                    id uuid NOT NULL,
                    organization_id uuid NOT NULL,
                    from_owner_user_id uuid NOT NULL,
                    to_user_id uuid NOT NULL,
                    status character varying(32) NOT NULL,
                    created_at_utc timestamp with time zone NOT NULL,
                    expires_at_utc timestamp with time zone NOT NULL,
                    accepted_at_utc timestamp with time zone NULL,
                    declined_at_utc timestamp with time zone NULL,
                    cancelled_at_utc timestamp with time zone NULL,
                    completed_at_utc timestamp with time zone NULL,
                    updated_at_utc timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_organization_ownership_transfers" PRIMARY KEY (id),
                    CONSTRAINT "FK_organization_ownership_transfers_organizations_organization~"
                        FOREIGN KEY (organization_id) REFERENCES platform.organizations (id) ON DELETE RESTRICT,
                    CONSTRAINT "FK_organization_ownership_transfers_platform_users_from_owner_~"
                        FOREIGN KEY (from_owner_user_id) REFERENCES platform.platform_users (id) ON DELETE RESTRICT,
                    CONSTRAINT "FK_organization_ownership_transfers_platform_users_to_user_id"
                        FOREIGN KEY (to_user_id) REFERENCES platform.platform_users (id) ON DELETE RESTRICT
                );

                CREATE INDEX IF NOT EXISTS "IX_organization_ownership_transfers_from_owner_user_id"
                    ON platform.organization_ownership_transfers (from_owner_user_id);

                CREATE INDEX IF NOT EXISTS ix_organization_ownership_transfers_to_user_status
                    ON platform.organization_ownership_transfers (to_user_id, status);

                CREATE UNIQUE INDEX IF NOT EXISTS ux_organization_ownership_transfers_pending_org
                    ON platform.organization_ownership_transfers (organization_id)
                    WHERE status = 'Pending';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TABLE IF EXISTS platform.organization_ownership_transfers;
                """);
        }
    }
}
