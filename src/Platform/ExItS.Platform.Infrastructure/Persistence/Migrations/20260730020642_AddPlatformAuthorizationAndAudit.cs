using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformAuthorizationAndAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_records",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actor_identifier = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    actor_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    action_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    target_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    target_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    product_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    outcome = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_records", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "platform_role_assignments",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    platform_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    granted_by_actor = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    granted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    revoked_by_actor = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoke_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_role_assignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_platform_role_assignments_organizations_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "platform",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_platform_role_assignments_platform_users_platform_user_id",
                        column: x => x.platform_user_id,
                        principalSchema: "platform",
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_records_action_code",
                schema: "platform",
                table: "audit_records",
                column: "action_code");

            migrationBuilder.CreateIndex(
                name: "ix_audit_records_actor_identifier",
                schema: "platform",
                table: "audit_records",
                column: "actor_identifier");

            migrationBuilder.CreateIndex(
                name: "ix_audit_records_occurred_at_utc",
                schema: "platform",
                table: "audit_records",
                column: "occurred_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_audit_records_organization_id",
                schema: "platform",
                table: "audit_records",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_records_outcome",
                schema: "platform",
                table: "audit_records",
                column: "outcome");

            migrationBuilder.CreateIndex(
                name: "ix_platform_role_assignments_organization_id",
                schema: "platform",
                table: "platform_role_assignments",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ux_platform_role_assignments_org_scoped_active",
                schema: "platform",
                table: "platform_role_assignments",
                columns: new[] { "platform_user_id", "role", "organization_id" },
                unique: true,
                filter: "status = 'Active' AND organization_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_platform_role_assignments_platform_wide_active",
                schema: "platform",
                table: "platform_role_assignments",
                columns: new[] { "platform_user_id", "role" },
                unique: true,
                filter: "status = 'Active' AND organization_id IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_records",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "platform_role_assignments",
                schema: "platform");
        }
    }
}
