using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationBranchesAndPosDevices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "max_active_pos_devices",
                schema: "platform",
                table: "plans",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<Guid>(
                name: "primary_business_type_id",
                schema: "platform",
                table: "organizations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "organization_branches",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    address_line1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    address_line2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    region = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    postal_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_branches", x => x.id);
                    table.ForeignKey(
                        name: "FK_organization_branches_organizations_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "platform",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pos_devices",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    installation_device_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    friendly_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    platform = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    model = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    app_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    registered_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_seen_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pos_devices", x => x.id);
                    table.ForeignKey(
                        name: "FK_pos_devices_organization_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "platform",
                        principalTable: "organization_branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pos_devices_organizations_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "platform",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pos_devices_platform_users_revoked_by_user_id",
                        column: x => x.revoked_by_user_id,
                        principalSchema: "platform",
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_organizations_primary_business_type_id",
                schema: "platform",
                table: "organizations",
                column: "primary_business_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_organization_branches_organization_id_code",
                schema: "platform",
                table: "organization_branches",
                columns: new[] { "organization_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_organization_branches_status",
                schema: "platform",
                table: "organization_branches",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_pos_devices_branch_id",
                schema: "platform",
                table: "pos_devices",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_pos_devices_organization_id_installation_device_id",
                schema: "platform",
                table: "pos_devices",
                columns: new[] { "organization_id", "installation_device_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pos_devices_revoked_by_user_id",
                schema: "platform",
                table: "pos_devices",
                column: "revoked_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_pos_devices_status",
                schema: "platform",
                table: "pos_devices",
                column: "status");

            migrationBuilder.AddForeignKey(
                name: "FK_organizations_business_types_primary_business_type_id",
                schema: "platform",
                table: "organizations",
                column: "primary_business_type_id",
                principalSchema: "catalog",
                principalTable: "business_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_organizations_business_types_primary_business_type_id",
                schema: "platform",
                table: "organizations");

            migrationBuilder.DropTable(
                name: "pos_devices",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "organization_branches",
                schema: "platform");

            migrationBuilder.DropIndex(
                name: "ix_organizations_primary_business_type_id",
                schema: "platform",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "max_active_pos_devices",
                schema: "platform",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "primary_business_type_id",
                schema: "platform",
                table: "organizations");
        }
    }
}
