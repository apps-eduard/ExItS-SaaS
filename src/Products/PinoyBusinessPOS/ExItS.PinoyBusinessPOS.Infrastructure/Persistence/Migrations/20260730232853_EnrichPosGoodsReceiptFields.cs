using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnrichPosGoodsReceiptFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "delivery_reference",
                schema: "pos",
                table: "goods_receipts",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "notes",
                schema: "pos",
                table: "goods_receipts",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "received_date",
                schema: "pos",
                table: "goods_receipts",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "supplier_id",
                schema: "pos",
                table: "goods_receipts",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE pos.goods_receipts AS gr
                SET supplier_id = po.supplier_id,
                    received_date = (gr.received_at_utc AT TIME ZONE 'UTC')::date
                FROM pos.purchase_orders AS po
                WHERE gr.purchase_order_id = po.id
                  AND (gr.supplier_id IS NULL OR gr.received_date IS NULL);
                """);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "received_date",
                schema: "pos",
                table: "goods_receipts",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "supplier_id",
                schema: "pos",
                table: "goods_receipts",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "inventory_movement_id",
                schema: "pos",
                table: "goods_receipt_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "line_total_snapshot",
                schema: "pos",
                table: "goods_receipt_lines",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "unit_purchase_cost_snapshot",
                schema: "pos",
                table: "goods_receipt_lines",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE pos.goods_receipt_lines AS gl
                SET unit_purchase_cost_snapshot = pol.unit_purchase_cost,
                    line_total_snapshot = ROUND(pol.unit_purchase_cost * gl.received_qty, 2)
                FROM pos.purchase_order_lines AS pol
                WHERE gl.purchase_order_line_id = pol.id
                  AND (gl.unit_purchase_cost_snapshot IS NULL OR gl.line_total_snapshot IS NULL);
                """);

            migrationBuilder.AlterColumn<decimal>(
                name: "line_total_snapshot",
                schema: "pos",
                table: "goods_receipt_lines",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "unit_purchase_cost_snapshot",
                schema: "pos",
                table: "goods_receipt_lines",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipts_supplier_id",
                schema: "pos",
                table: "goods_receipts",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ux_goods_receipt_lines_inventory_movement_id",
                schema: "pos",
                table: "goods_receipt_lines",
                column: "inventory_movement_id",
                unique: true,
                filter: "inventory_movement_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_goods_receipt_lines_line_total_non_negative",
                schema: "pos",
                table: "goods_receipt_lines",
                sql: "line_total_snapshot >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_goods_receipt_lines_unit_cost_non_negative",
                schema: "pos",
                table: "goods_receipt_lines",
                sql: "unit_purchase_cost_snapshot >= 0");

            migrationBuilder.AddForeignKey(
                name: "fk_goods_receipts_suppliers",
                schema: "pos",
                table: "goods_receipts",
                column: "supplier_id",
                principalSchema: "pos",
                principalTable: "suppliers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_goods_receipts_suppliers",
                schema: "pos",
                table: "goods_receipts");

            migrationBuilder.DropIndex(
                name: "IX_goods_receipts_supplier_id",
                schema: "pos",
                table: "goods_receipts");

            migrationBuilder.DropIndex(
                name: "ux_goods_receipt_lines_inventory_movement_id",
                schema: "pos",
                table: "goods_receipt_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_goods_receipt_lines_line_total_non_negative",
                schema: "pos",
                table: "goods_receipt_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_goods_receipt_lines_unit_cost_non_negative",
                schema: "pos",
                table: "goods_receipt_lines");

            migrationBuilder.DropColumn(
                name: "delivery_reference",
                schema: "pos",
                table: "goods_receipts");

            migrationBuilder.DropColumn(
                name: "notes",
                schema: "pos",
                table: "goods_receipts");

            migrationBuilder.DropColumn(
                name: "received_date",
                schema: "pos",
                table: "goods_receipts");

            migrationBuilder.DropColumn(
                name: "supplier_id",
                schema: "pos",
                table: "goods_receipts");

            migrationBuilder.DropColumn(
                name: "inventory_movement_id",
                schema: "pos",
                table: "goods_receipt_lines");

            migrationBuilder.DropColumn(
                name: "line_total_snapshot",
                schema: "pos",
                table: "goods_receipt_lines");

            migrationBuilder.DropColumn(
                name: "unit_purchase_cost_snapshot",
                schema: "pos",
                table: "goods_receipt_lines");
        }
    }
}
