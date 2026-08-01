using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformAndOrganizationRbac : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "organization_role_definitions",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    permissions_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_role_definitions", x => x.id);
                    table.ForeignKey(
                        name: "FK_organization_role_definitions_organizations_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "platform",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "platform_role_definitions",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    permissions_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_role_definitions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "organization_custom_role_assignments",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    platform_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    granted_by_actor = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    granted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    revoked_by_actor = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoke_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_custom_role_assignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_organization_custom_role_assignments_organization_role_defi~",
                        column: x => x.role_definition_id,
                        principalSchema: "platform",
                        principalTable: "organization_role_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_organization_custom_role_assignments_organizations_organiza~",
                        column: x => x.organization_id,
                        principalSchema: "platform",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_organization_custom_role_assignments_platform_users_platfor~",
                        column: x => x.platform_user_id,
                        principalSchema: "platform",
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "platform_custom_role_assignments",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    platform_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    granted_by_actor = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    granted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    revoked_by_actor = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoke_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_custom_role_assignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_platform_custom_role_assignments_platform_role_definitions_~",
                        column: x => x.role_definition_id,
                        principalSchema: "platform",
                        principalTable: "platform_role_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_platform_custom_role_assignments_platform_users_platform_us~",
                        column: x => x.platform_user_id,
                        principalSchema: "platform",
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_organization_custom_role_assignments_platform_user_id",
                schema: "platform",
                table: "organization_custom_role_assignments",
                column: "platform_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_organization_custom_role_assignments_role_definition_id",
                schema: "platform",
                table: "organization_custom_role_assignments",
                column: "role_definition_id");

            migrationBuilder.CreateIndex(
                name: "ux_organization_custom_role_assignments_active",
                schema: "platform",
                table: "organization_custom_role_assignments",
                columns: new[] { "organization_id", "platform_user_id", "role_definition_id" },
                unique: true,
                filter: "status = 'Active'");

            migrationBuilder.CreateIndex(
                name: "ux_organization_role_definitions_org_code",
                schema: "platform",
                table: "organization_role_definitions",
                columns: new[] { "organization_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_platform_custom_role_assignments_role_definition_id",
                schema: "platform",
                table: "platform_custom_role_assignments",
                column: "role_definition_id");

            migrationBuilder.CreateIndex(
                name: "ux_platform_custom_role_assignments_active",
                schema: "platform",
                table: "platform_custom_role_assignments",
                columns: new[] { "platform_user_id", "role_definition_id" },
                unique: true,
                filter: "status = 'Active'");

            migrationBuilder.CreateIndex(
                name: "ux_platform_role_definitions_code",
                schema: "platform",
                table: "platform_role_definitions",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "organization_custom_role_assignments",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "platform_custom_role_assignments",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "organization_role_definitions",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "platform_role_definitions",
                schema: "platform");
        }
    }
}
