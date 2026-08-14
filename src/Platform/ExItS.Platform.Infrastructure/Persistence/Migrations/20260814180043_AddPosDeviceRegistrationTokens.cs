using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPosDeviceRegistrationTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent for Local Validation volumes that may partially apply schema outside EF history.
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS platform.pos_device_registration_tokens (
                    id uuid NOT NULL,
                    organization_id uuid NOT NULL,
                    token_hash character varying(64) NOT NULL,
                    created_by_user_id uuid NOT NULL,
                    created_at_utc timestamp with time zone NOT NULL,
                    expires_at_utc timestamp with time zone NOT NULL,
                    redeemed_at_utc timestamp with time zone NULL,
                    redeemed_by_installation_device_id character varying(128) NULL,
                    redeemed_pos_device_id uuid NULL,
                    status character varying(16) NOT NULL,
                    CONSTRAINT "PK_pos_device_registration_tokens" PRIMARY KEY (id),
                    CONSTRAINT "FK_pos_device_registration_tokens_organizations_organization_id"
                        FOREIGN KEY (organization_id) REFERENCES platform.organizations (id) ON DELETE RESTRICT,
                    CONSTRAINT "FK_pos_device_registration_tokens_platform_users_created_by_us~"
                        FOREIGN KEY (created_by_user_id) REFERENCES platform.platform_users (id) ON DELETE RESTRICT,
                    CONSTRAINT "FK_pos_device_registration_tokens_pos_devices_redeemed_pos_dev~"
                        FOREIGN KEY (redeemed_pos_device_id) REFERENCES platform.pos_devices (id) ON DELETE RESTRICT
                );

                CREATE INDEX IF NOT EXISTS "IX_pos_device_registration_tokens_created_by_user_id"
                    ON platform.pos_device_registration_tokens (created_by_user_id);

                CREATE INDEX IF NOT EXISTS ix_pos_device_registration_tokens_expires
                    ON platform.pos_device_registration_tokens (expires_at_utc);

                CREATE INDEX IF NOT EXISTS ix_pos_device_registration_tokens_org_status
                    ON platform.pos_device_registration_tokens (organization_id, status);

                CREATE INDEX IF NOT EXISTS "IX_pos_device_registration_tokens_redeemed_pos_device_id"
                    ON platform.pos_device_registration_tokens (redeemed_pos_device_id);

                CREATE UNIQUE INDEX IF NOT EXISTS ux_pos_device_registration_tokens_token_hash
                    ON platform.pos_device_registration_tokens (token_hash);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TABLE IF EXISTS platform.pos_device_registration_tokens;
                """);
        }
    }
}
