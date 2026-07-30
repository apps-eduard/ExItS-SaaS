using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPosPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_org_recorded",
                schema: "pos",
                table: "stock_movements",
                columns: new[] { "organization_id", "recorded_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_lines_org_product",
                schema: "pos",
                table: "sale_lines",
                columns: new[] { "organization_id", "product_id" });

            migrationBuilder.CreateIndex(
                name: "ix_customers_org_updated",
                schema: "pos",
                table: "customers",
                columns: new[] { "organization_id", "updated_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_stock_movements_org_recorded",
                schema: "pos",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "ix_sale_lines_org_product",
                schema: "pos",
                table: "sale_lines");

            migrationBuilder.DropIndex(
                name: "ix_customers_org_updated",
                schema: "pos",
                table: "customers");
        }
    }
}
