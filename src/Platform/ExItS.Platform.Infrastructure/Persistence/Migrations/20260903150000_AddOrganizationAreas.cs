using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationAreas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Areas group branches for access, navigation, and reporting only. No table here owns
            // stock, reservations, registers, shifts, or documents — that stays on branches.
            // Existing branches keep area_id NULL and no default Area rows are created.
            migrationBuilder.AddColumn<int>(
                name: "max_areas",
                schema: "platform",
                table: "plans",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<Guid>(
                name: "area_id",
                schema: "platform",
                table: "organization_branches",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "organization_areas",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_areas", x => x.id);
                    table.UniqueConstraint("AK_organization_areas_id_organization_id", x => new { x.id, x.organization_id });
                    table.ForeignKey(
                        name: "FK_organization_areas_organizations_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "platform",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "organization_membership_area_assignments",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    membership_id = table.Column<Guid>(type: "uuid", nullable: false),
                    area_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actor_reference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_membership_area_assignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_organization_membership_area_assignments_organization_areas~",
                        column: x => x.area_id,
                        principalSchema: "platform",
                        principalTable: "organization_areas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_organization_membership_area_assignments_organization_membe~",
                        column: x => x.membership_id,
                        principalSchema: "platform",
                        principalTable: "organization_memberships",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_organization_membership_area_assignments_organizations_orga~",
                        column: x => x.organization_id,
                        principalSchema: "platform",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_organization_branches_area_id",
                schema: "platform",
                table: "organization_branches",
                column: "area_id");

            migrationBuilder.CreateIndex(
                name: "ix_organization_areas_organization_id",
                schema: "platform",
                table: "organization_areas",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ux_organization_areas_organization_code",
                schema: "platform",
                table: "organization_areas",
                columns: new[] { "organization_id", "code" },
                unique: true,
                filter: "code IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_org_membership_area_assignments_area_id",
                schema: "platform",
                table: "organization_membership_area_assignments",
                column: "area_id");

            migrationBuilder.CreateIndex(
                name: "ix_org_membership_area_assignments_organization_id",
                schema: "platform",
                table: "organization_membership_area_assignments",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ux_org_membership_area_assignments_membership_area",
                schema: "platform",
                table: "organization_membership_area_assignments",
                columns: new[] { "membership_id", "area_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_organization_branches_organization_areas_area_id",
                schema: "platform",
                table: "organization_branches",
                column: "area_id",
                principalSchema: "platform",
                principalTable: "organization_areas",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_organization_branches_organization_areas_area_id",
                schema: "platform",
                table: "organization_branches");

            migrationBuilder.DropTable(
                name: "organization_membership_area_assignments",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "organization_areas",
                schema: "platform");

            migrationBuilder.DropIndex(
                name: "ix_organization_branches_area_id",
                schema: "platform",
                table: "organization_branches");

            migrationBuilder.DropColumn(
                name: "max_areas",
                schema: "platform",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "area_id",
                schema: "platform",
                table: "organization_branches");
        }
    }
}
