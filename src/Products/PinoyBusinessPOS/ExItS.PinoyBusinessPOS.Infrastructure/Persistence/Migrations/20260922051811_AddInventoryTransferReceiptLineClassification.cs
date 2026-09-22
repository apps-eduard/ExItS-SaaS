using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryTransferReceiptLineClassification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_transfer_receipt_lines_qty_positive",
                schema: "pos",
                table: "inventory_transfer_receipt_lines");

            migrationBuilder.AddColumn<string>(
                name: "missing_disposition",
                schema: "pos",
                table: "inventory_transfer_receipt_lines",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "note",
                schema: "pos",
                table: "inventory_transfer_receipt_lines",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "quantity_damaged",
                schema: "pos",
                table: "inventory_transfer_receipt_lines",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "quantity_missing",
                schema: "pos",
                table: "inventory_transfer_receipt_lines",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_transfer_receipt_lines_qty_positive",
                schema: "pos",
                table: "inventory_transfer_receipt_lines",
                sql: "quantity_received >= 0 AND quantity_damaged >= 0 AND quantity_missing >= 0 AND (quantity_received + quantity_damaged + quantity_missing) > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_transfer_receipt_lines_qty_positive",
                schema: "pos",
                table: "inventory_transfer_receipt_lines");

            migrationBuilder.DropColumn(
                name: "missing_disposition",
                schema: "pos",
                table: "inventory_transfer_receipt_lines");

            migrationBuilder.DropColumn(
                name: "note",
                schema: "pos",
                table: "inventory_transfer_receipt_lines");

            migrationBuilder.DropColumn(
                name: "quantity_damaged",
                schema: "pos",
                table: "inventory_transfer_receipt_lines");

            migrationBuilder.DropColumn(
                name: "quantity_missing",
                schema: "pos",
                table: "inventory_transfer_receipt_lines");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_transfer_receipt_lines_qty_positive",
                schema: "pos",
                table: "inventory_transfer_receipt_lines",
                sql: "quantity_received > 0");
        }
    }
}
