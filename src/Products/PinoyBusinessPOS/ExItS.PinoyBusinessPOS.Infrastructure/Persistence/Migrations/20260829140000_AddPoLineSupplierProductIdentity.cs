using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Allow connected PO lines to retain supplier product identity before buyer catalog mapping.
/// </summary>
[Migration("20260829140000_AddPoLineSupplierProductIdentity")]
public partial class AddPoLineSupplierProductIdentity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_purchase_order_lines_products",
            table: "purchase_order_lines");

        migrationBuilder.DropIndex(
            name: "ux_purchase_order_lines_po_product",
            table: "purchase_order_lines");

        migrationBuilder.AlterColumn<Guid>(
            name: "product_id",
            table: "purchase_order_lines",
            type: "uuid",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "uuid");

        migrationBuilder.AddColumn<Guid>(
            name: "supplier_product_id",
            table: "purchase_order_lines",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "sku_snapshot",
            table: "purchase_order_lines",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "ux_purchase_order_lines_po_product",
            table: "purchase_order_lines",
            columns: new[] { "purchase_order_id", "product_id" },
            unique: true,
            filter: "product_id IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "ux_purchase_order_lines_po_supplier_product",
            table: "purchase_order_lines",
            columns: new[] { "purchase_order_id", "supplier_product_id" },
            unique: true,
            filter: "supplier_product_id IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "ux_buyer_supplier_product_links_supplier_active",
            table: "buyer_supplier_product_links",
            columns: new[] { "relationship_id", "supplier_product_id" },
            unique: true,
            filter: "is_active");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ux_buyer_supplier_product_links_supplier_active",
            table: "buyer_supplier_product_links");

        migrationBuilder.DropIndex(
            name: "ux_purchase_order_lines_po_supplier_product",
            table: "purchase_order_lines");

        migrationBuilder.DropIndex(
            name: "ux_purchase_order_lines_po_product",
            table: "purchase_order_lines");

        migrationBuilder.DropColumn(
            name: "sku_snapshot",
            table: "purchase_order_lines");

        migrationBuilder.DropColumn(
            name: "supplier_product_id",
            table: "purchase_order_lines");

        migrationBuilder.AlterColumn<Guid>(
            name: "product_id",
            table: "purchase_order_lines",
            type: "uuid",
            nullable: false,
            defaultValue: Guid.Empty,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "ux_purchase_order_lines_po_product",
            table: "purchase_order_lines",
            columns: new[] { "purchase_order_id", "product_id" },
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "fk_purchase_order_lines_products",
            table: "purchase_order_lines",
            column: "product_id",
            principalTable: "catalog_products",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);
    }
}
