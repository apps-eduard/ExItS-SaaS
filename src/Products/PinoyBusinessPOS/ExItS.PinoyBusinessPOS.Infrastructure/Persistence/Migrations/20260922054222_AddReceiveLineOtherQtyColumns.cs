using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiveLineOtherQtyColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_transfer_receipt_lines_qty_positive",
                schema: "pos",
                table: "inventory_transfer_receipt_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_goods_receipt_lines_activity_positive",
                schema: "pos",
                table: "goods_receipt_lines");

            migrationBuilder.AddColumn<string>(
                name: "other_reason_code",
                schema: "pos",
                table: "inventory_transfer_receipt_lines",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "other_reason_note",
                schema: "pos",
                table: "inventory_transfer_receipt_lines",
                type: "character varying(280)",
                maxLength: 280,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "quantity_other",
                schema: "pos",
                table: "inventory_transfer_receipt_lines",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "other_qty",
                schema: "pos",
                table: "goods_receipt_lines",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "other_reason_code",
                schema: "pos",
                table: "goods_receipt_lines",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "other_reason_note",
                schema: "pos",
                table: "goods_receipt_lines",
                type: "character varying(280)",
                maxLength: 280,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_transfer_receipt_lines_qty_positive",
                schema: "pos",
                table: "inventory_transfer_receipt_lines",
                sql: "quantity_received >= 0 AND quantity_damaged >= 0 AND quantity_missing >= 0 AND quantity_other >= 0 AND (quantity_received + quantity_damaged + quantity_missing + quantity_other) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_goods_receipt_lines_activity_positive",
                schema: "pos",
                table: "goods_receipt_lines",
                sql: "(received_qty + damaged_qty + rejected_qty + other_qty + short_closed_qty) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_goods_receipt_lines_other_qty_nonnegative",
                schema: "pos",
                table: "goods_receipt_lines",
                sql: "other_qty >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_transfer_receipt_lines_qty_positive",
                schema: "pos",
                table: "inventory_transfer_receipt_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_goods_receipt_lines_activity_positive",
                schema: "pos",
                table: "goods_receipt_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_goods_receipt_lines_other_qty_nonnegative",
                schema: "pos",
                table: "goods_receipt_lines");

            migrationBuilder.DropColumn(
                name: "other_reason_code",
                schema: "pos",
                table: "inventory_transfer_receipt_lines");

            migrationBuilder.DropColumn(
                name: "other_reason_note",
                schema: "pos",
                table: "inventory_transfer_receipt_lines");

            migrationBuilder.DropColumn(
                name: "quantity_other",
                schema: "pos",
                table: "inventory_transfer_receipt_lines");

            migrationBuilder.DropColumn(
                name: "other_qty",
                schema: "pos",
                table: "goods_receipt_lines");

            migrationBuilder.DropColumn(
                name: "other_reason_code",
                schema: "pos",
                table: "goods_receipt_lines");

            migrationBuilder.DropColumn(
                name: "other_reason_note",
                schema: "pos",
                table: "goods_receipt_lines");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_transfer_receipt_lines_qty_positive",
                schema: "pos",
                table: "inventory_transfer_receipt_lines",
                sql: "quantity_received >= 0 AND quantity_damaged >= 0 AND quantity_missing >= 0 AND (quantity_received + quantity_damaged + quantity_missing) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_goods_receipt_lines_activity_positive",
                schema: "pos",
                table: "goods_receipt_lines",
                sql: "(received_qty + damaged_qty + rejected_qty + short_closed_qty) > 0");
        }
    }
}
