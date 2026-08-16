using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConnectedBuyerProductSharingAndPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "can_expose_to_connected_buyers",
                schema: "pos",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "default_connected_po_price",
                schema: "pos",
                table: "products",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "connected_buyer_product_shares",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    relationship_id = table.Column<Guid>(type: "uuid", nullable: false),
                    buyer_organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_shared = table.Column<bool>(type: "boolean", nullable: false),
                    buyer_specific_po_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    sync_version = table.Column<long>(type: "bigint", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_connected_buyer_product_shares", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_connected_buyer_product_shares_relationship_shared",
                schema: "pos",
                table: "connected_buyer_product_shares",
                columns: new[] { "relationship_id", "is_shared" });

            migrationBuilder.CreateIndex(
                name: "ix_connected_buyer_product_shares_supplier_product",
                schema: "pos",
                table: "connected_buyer_product_shares",
                columns: new[] { "supplier_organization_id", "supplier_product_id" });

            migrationBuilder.CreateIndex(
                name: "ux_connected_buyer_product_shares_relationship_product",
                schema: "pos",
                table: "connected_buyer_product_shares",
                columns: new[] { "relationship_id", "supplier_product_id" },
                unique: true);

            migrationBuilder.Sql("""
                UPDATE pos.products AS product
                SET can_expose_to_connected_buyers = TRUE,
                    default_connected_po_price = exposure.supplier_order_price
                FROM pos.supplier_product_exposures AS exposure
                WHERE exposure.product_id = product.id
                  AND exposure.supplier_organization_id = product.organization_id
                  AND exposure.is_exposed = TRUE;

                INSERT INTO pos.connected_buyer_product_shares (
                    id,
                    relationship_id,
                    buyer_organization_id,
                    supplier_organization_id,
                    supplier_product_id,
                    is_shared,
                    buyer_specific_po_price,
                    sync_version,
                    created_at_utc,
                    updated_at_utc)
                SELECT
                    gen_random_uuid(),
                    relationship.id,
                    relationship.buyer_organization_id,
                    relationship.supplier_organization_id,
                    exposure.product_id,
                    TRUE,
                    NULL,
                    1,
                    GREATEST(relationship.created_at_utc, exposure.created_at_utc),
                    GREATEST(relationship.updated_at_utc, exposure.updated_at_utc)
                FROM pos.connected_supplier_relationships AS relationship
                JOIN pos.supplier_product_exposures AS exposure
                  ON exposure.supplier_organization_id = relationship.supplier_organization_id
                 AND exposure.is_exposed = TRUE
                WHERE relationship.status = 1
                ON CONFLICT (relationship_id, supplier_product_id) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "connected_buyer_product_shares",
                schema: "pos");

            migrationBuilder.DropColumn(
                name: "can_expose_to_connected_buyers",
                schema: "pos",
                table: "products");

            migrationBuilder.DropColumn(
                name: "default_connected_po_price",
                schema: "pos",
                table: "products");
        }
    }
}
