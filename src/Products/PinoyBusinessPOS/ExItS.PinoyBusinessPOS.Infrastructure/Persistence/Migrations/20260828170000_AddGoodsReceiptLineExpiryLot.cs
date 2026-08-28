using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGoodsReceiptLineExpiryLot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "expiry_date",
                schema: "pos",
                table: "goods_receipt_lines",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "lot_number",
                schema: "pos",
                table: "goods_receipt_lines",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "expiry_date",
                schema: "pos",
                table: "goods_receipt_lines");

            migrationBuilder.DropColumn(
                name: "lot_number",
                schema: "pos",
                table: "goods_receipt_lines");
        }
    }
}
