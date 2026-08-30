using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchDeliveryServiceAreas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "branch_delivery_service_areas",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    region_or_province_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    city_municipality_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    normalized_city_municipality_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    external_area_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branch_delivery_service_areas", x => x.id);
                    table.ForeignKey(
                        name: "FK_branch_delivery_service_areas_organization_branches_branch_~",
                        columns: x => new { x.branch_id, x.organization_id },
                        principalSchema: "platform",
                        principalTable: "organization_branches",
                        principalColumns: new[] { "id", "organization_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_branch_delivery_service_areas_branch_id",
                schema: "platform",
                table: "branch_delivery_service_areas",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_branch_delivery_service_areas_branch_id_organization_id",
                schema: "platform",
                table: "branch_delivery_service_areas",
                columns: new[] { "branch_id", "organization_id" });

            migrationBuilder.CreateIndex(
                name: "IX_branch_delivery_service_areas_organization_id",
                schema: "platform",
                table: "branch_delivery_service_areas",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ux_branch_delivery_service_areas_active_city",
                schema: "platform",
                table: "branch_delivery_service_areas",
                columns: new[] { "branch_id", "normalized_city_municipality_name" },
                unique: true,
                filter: "is_active = TRUE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "branch_delivery_service_areas",
                schema: "platform");
        }
    }
}
