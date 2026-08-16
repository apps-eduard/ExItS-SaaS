using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConnectedBuyerGlobalBlock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_blocked_from_connected_buyers",
                schema: "pos",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Intentional block = can_expose=false AND an exposure row exists for that product/org.
            // Unconfigured (can_expose=false, no exposure) stays not blocked → eligible after sync.
            migrationBuilder.Sql("""
                UPDATE pos.products AS p
                SET is_blocked_from_connected_buyers = TRUE
                WHERE p.can_expose_to_connected_buyers = FALSE
                  AND EXISTS (
                    SELECT 1 FROM pos.supplier_product_exposures e
                    WHERE e.product_id = p.id AND e.supplier_organization_id = p.organization_id);

                UPDATE pos.products
                SET can_expose_to_connected_buyers = NOT is_blocked_from_connected_buyers;
                """);

            migrationBuilder.AlterColumn<bool>(
                name: "can_expose_to_connected_buyers",
                schema: "pos",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse eligibility backfill: never-exposed products return to can_expose=false.
            // Intentional blocks already have can_expose=false; exposed+allowed stay true.
            migrationBuilder.Sql("""
                UPDATE pos.products AS p
                SET can_expose_to_connected_buyers = FALSE
                WHERE p.is_blocked_from_connected_buyers = FALSE
                  AND NOT EXISTS (
                    SELECT 1 FROM pos.supplier_product_exposures e
                    WHERE e.product_id = p.id AND e.supplier_organization_id = p.organization_id);
                """);

            migrationBuilder.DropColumn(
                name: "is_blocked_from_connected_buyers",
                schema: "pos",
                table: "products");

            migrationBuilder.AlterColumn<bool>(
                name: "can_expose_to_connected_buyers",
                schema: "pos",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);
        }
    }
}
