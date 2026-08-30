using System;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(PosDbContext))]
    [Migration("20260830120000_AddPurchaseReceiptReversal")]
    public partial class AddPurchaseReceiptReversal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_movements_movement_type",
                schema: "pos",
                table: "stock_movements");

            migrationBuilder.AddColumn<string>(
                name: "status",
                schema: "pos",
                table: "goods_receipts",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Posted");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "voided_at_utc",
                schema: "pos",
                table: "goods_receipts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "voided_by_user_id",
                schema: "pos",
                table: "goods_receipts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "void_reason",
                schema: "pos",
                table: "goods_receipts",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.Sql("UPDATE pos.goods_receipts SET status = 'Posted' WHERE status IS NULL OR status = '';");

            migrationBuilder.AddCheckConstraint(
                name: "ck_goods_receipts_status",
                schema: "pos",
                table: "goods_receipts",
                sql: "status IN ('Posted', 'Voided')");

            migrationBuilder.CreateIndex(
                name: "ix_goods_receipts_org_status",
                schema: "pos",
                table: "goods_receipts",
                columns: new[] { "organization_id", "status" });

            migrationBuilder.AddColumn<string>(
                name: "status",
                schema: "pos",
                table: "direct_purchase_receipts",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Posted");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "voided_at_utc",
                schema: "pos",
                table: "direct_purchase_receipts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "voided_by_user_id",
                schema: "pos",
                table: "direct_purchase_receipts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "void_reason",
                schema: "pos",
                table: "direct_purchase_receipts",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE pos.direct_purchase_receipts SET status = 'Posted' WHERE status IS NULL OR status = '';");

            migrationBuilder.AddCheckConstraint(
                name: "ck_direct_purchase_receipts_status",
                schema: "pos",
                table: "direct_purchase_receipts",
                sql: "status IN ('Posted', 'Voided')");

            migrationBuilder.CreateIndex(
                name: "ix_direct_purchase_receipts_org_status",
                schema: "pos",
                table: "direct_purchase_receipts",
                columns: new[] { "organization_id", "status" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movements_movement_type",
                schema: "pos",
                table: "stock_movements",
                sql: "movement_type IN ('OpeningStock', 'ManualIncrease', 'ManualDecrease', 'SaleDeduction', 'SaleVoidRestoration', 'PurchaseReceipt', 'StockCountVarianceIncrease', 'StockCountVarianceDecrease', 'SaleReturnRestock', 'TransferOut', 'TransferIn', 'TransferCancelRestore', 'DirectPurchaseReceipt', 'ExpirationInitialization', 'StockUse', 'StockUseVoidRestoration', 'ProductionMaterialConsumption', 'ProductionMaterialRestoration', 'ProductionOutput', 'ProductionOutputReversal', 'WasteLoss', 'WasteLossVoidRestoration', 'PurchaseReceiptReversal', 'DirectPurchaseReceiptReversal')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_movements_movement_type",
                schema: "pos",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "ix_goods_receipts_org_status",
                schema: "pos",
                table: "goods_receipts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_goods_receipts_status",
                schema: "pos",
                table: "goods_receipts");

            migrationBuilder.DropColumn(name: "void_reason", schema: "pos", table: "goods_receipts");
            migrationBuilder.DropColumn(name: "voided_by_user_id", schema: "pos", table: "goods_receipts");
            migrationBuilder.DropColumn(name: "voided_at_utc", schema: "pos", table: "goods_receipts");
            migrationBuilder.DropColumn(name: "status", schema: "pos", table: "goods_receipts");

            migrationBuilder.DropIndex(
                name: "ix_direct_purchase_receipts_org_status",
                schema: "pos",
                table: "direct_purchase_receipts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_direct_purchase_receipts_status",
                schema: "pos",
                table: "direct_purchase_receipts");

            migrationBuilder.DropColumn(name: "void_reason", schema: "pos", table: "direct_purchase_receipts");
            migrationBuilder.DropColumn(name: "voided_by_user_id", schema: "pos", table: "direct_purchase_receipts");
            migrationBuilder.DropColumn(name: "voided_at_utc", schema: "pos", table: "direct_purchase_receipts");
            migrationBuilder.DropColumn(name: "status", schema: "pos", table: "direct_purchase_receipts");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movements_movement_type",
                schema: "pos",
                table: "stock_movements",
                sql: "movement_type IN ('OpeningStock', 'ManualIncrease', 'ManualDecrease', 'SaleDeduction', 'SaleVoidRestoration', 'PurchaseReceipt', 'StockCountVarianceIncrease', 'StockCountVarianceDecrease', 'SaleReturnRestock', 'TransferOut', 'TransferIn', 'TransferCancelRestore', 'DirectPurchaseReceipt', 'ExpirationInitialization', 'StockUse', 'StockUseVoidRestoration', 'ProductionMaterialConsumption', 'ProductionMaterialRestoration', 'ProductionOutput', 'ProductionOutputReversal', 'WasteLoss', 'WasteLossVoidRestoration')");
        }
    }
}
