using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationStaffIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "address_line_1",
                schema: "platform",
                table: "organization_memberships",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "address_line_2",
                schema: "platform",
                table: "organization_memberships",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "barangay",
                schema: "platform",
                table: "organization_memberships",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "city_municipality",
                schema: "platform",
                table: "organization_memberships",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "country",
                schema: "platform",
                table: "organization_memberships",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "postal_code",
                schema: "platform",
                table: "organization_memberships",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "province_state",
                schema: "platform",
                table: "organization_memberships",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "staff_id",
                schema: "platform",
                table: "organization_memberships",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "organization_staff_id_settings",
                schema: "platform",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    prefix = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    next_number = table.Column<int>(type: "integer", nullable: false),
                    pad_digits = table.Column<int>(type: "integer", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_staff_id_settings", x => x.organization_id);
                    table.ForeignKey(
                        name: "FK_organization_staff_id_settings_organizations_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "platform",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_organization_memberships_staff_id",
                schema: "platform",
                table: "organization_memberships",
                columns: new[] { "organization_id", "staff_id" },
                unique: true,
                filter: "staff_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "organization_staff_id_settings",
                schema: "platform");

            migrationBuilder.DropIndex(
                name: "ux_organization_memberships_staff_id",
                schema: "platform",
                table: "organization_memberships");

            migrationBuilder.DropColumn(
                name: "address_line_1",
                schema: "platform",
                table: "organization_memberships");

            migrationBuilder.DropColumn(
                name: "address_line_2",
                schema: "platform",
                table: "organization_memberships");

            migrationBuilder.DropColumn(
                name: "barangay",
                schema: "platform",
                table: "organization_memberships");

            migrationBuilder.DropColumn(
                name: "city_municipality",
                schema: "platform",
                table: "organization_memberships");

            migrationBuilder.DropColumn(
                name: "country",
                schema: "platform",
                table: "organization_memberships");

            migrationBuilder.DropColumn(
                name: "postal_code",
                schema: "platform",
                table: "organization_memberships");

            migrationBuilder.DropColumn(
                name: "province_state",
                schema: "platform",
                table: "organization_memberships");

            migrationBuilder.DropColumn(
                name: "staff_id",
                schema: "platform",
                table: "organization_memberships");
        }
    }
}
