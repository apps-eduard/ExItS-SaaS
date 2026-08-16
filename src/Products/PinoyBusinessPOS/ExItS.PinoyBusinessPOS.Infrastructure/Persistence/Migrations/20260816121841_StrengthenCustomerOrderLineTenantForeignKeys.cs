using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StrengthenCustomerOrderLineTenantForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_customer_order_lines_orders",
                schema: "pos",
                table: "customer_order_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_customer_order_lines_products",
                schema: "pos",
                table: "customer_order_lines");

            migrationBuilder.DropIndex(
                name: "IX_customer_order_lines_product_id",
                schema: "pos",
                table: "customer_order_lines");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_products_id_organization_id",
                schema: "pos",
                table: "products",
                columns: new[] { "id", "organization_id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_customer_orders_id_seller_organization_id",
                schema: "pos",
                table: "customer_orders",
                columns: new[] { "id", "seller_organization_id" });

            migrationBuilder.CreateIndex(
                name: "IX_customer_order_lines_order_id_seller_organization_id",
                schema: "pos",
                table: "customer_order_lines",
                columns: new[] { "order_id", "seller_organization_id" });

            migrationBuilder.CreateIndex(
                name: "IX_customer_order_lines_product_id_seller_organization_id",
                schema: "pos",
                table: "customer_order_lines",
                columns: new[] { "product_id", "seller_organization_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_customer_order_lines_orders_tenant",
                schema: "pos",
                table: "customer_order_lines",
                columns: new[] { "order_id", "seller_organization_id" },
                principalSchema: "pos",
                principalTable: "customer_orders",
                principalColumns: new[] { "id", "seller_organization_id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_customer_order_lines_products_tenant",
                schema: "pos",
                table: "customer_order_lines",
                columns: new[] { "product_id", "seller_organization_id" },
                principalSchema: "pos",
                principalTable: "products",
                principalColumns: new[] { "id", "organization_id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_customer_order_lines_orders_tenant",
                schema: "pos",
                table: "customer_order_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_customer_order_lines_products_tenant",
                schema: "pos",
                table: "customer_order_lines");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_products_id_organization_id",
                schema: "pos",
                table: "products");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_customer_orders_id_seller_organization_id",
                schema: "pos",
                table: "customer_orders");

            migrationBuilder.DropIndex(
                name: "IX_customer_order_lines_order_id_seller_organization_id",
                schema: "pos",
                table: "customer_order_lines");

            migrationBuilder.DropIndex(
                name: "IX_customer_order_lines_product_id_seller_organization_id",
                schema: "pos",
                table: "customer_order_lines");

            migrationBuilder.CreateIndex(
                name: "IX_customer_order_lines_product_id",
                schema: "pos",
                table: "customer_order_lines",
                column: "product_id");

            migrationBuilder.AddForeignKey(
                name: "fk_customer_order_lines_orders",
                schema: "pos",
                table: "customer_order_lines",
                column: "order_id",
                principalSchema: "pos",
                principalTable: "customer_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_customer_order_lines_products",
                schema: "pos",
                table: "customer_order_lines",
                column: "product_id",
                principalSchema: "pos",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
