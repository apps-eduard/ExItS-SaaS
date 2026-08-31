using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefineBranchDeliveryServiceAreaPsgcUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_branch_delivery_service_areas_active_city",
                schema: "platform",
                table: "branch_delivery_service_areas");

            migrationBuilder.CreateIndex(
                name: "ix_branch_delivery_service_areas_branch_city",
                schema: "platform",
                table: "branch_delivery_service_areas",
                columns: new[] { "branch_id", "normalized_city_municipality_name" });

            migrationBuilder.CreateIndex(
                name: "ux_branch_delivery_service_areas_active_psgc",
                schema: "platform",
                table: "branch_delivery_service_areas",
                columns: new[] { "branch_id", "external_area_code" },
                unique: true,
                filter: "is_active = TRUE AND external_area_code IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_branch_delivery_service_areas_branch_city",
                schema: "platform",
                table: "branch_delivery_service_areas");

            migrationBuilder.DropIndex(
                name: "ux_branch_delivery_service_areas_active_psgc",
                schema: "platform",
                table: "branch_delivery_service_areas");

            migrationBuilder.CreateIndex(
                name: "ux_branch_delivery_service_areas_active_city",
                schema: "platform",
                table: "branch_delivery_service_areas",
                columns: new[] { "branch_id", "normalized_city_municipality_name" },
                unique: true,
                filter: "is_active = TRUE");
        }
    }
}
