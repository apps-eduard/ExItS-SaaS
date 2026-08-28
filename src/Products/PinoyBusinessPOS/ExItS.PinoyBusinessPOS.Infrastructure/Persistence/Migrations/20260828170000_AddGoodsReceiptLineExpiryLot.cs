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
            // Idempotent: local DBs may already have these columns from a renamed
            // precursor migration (e.g. 20260828152338_AddGoodsReceiptLineExpiryLot).
            migrationBuilder.Sql(
                """
                ALTER TABLE pos.goods_receipt_lines
                    ADD COLUMN IF NOT EXISTS expiry_date date NULL;
                ALTER TABLE pos.goods_receipt_lines
                    ADD COLUMN IF NOT EXISTS lot_number character varying(64) NULL;
                """);
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
