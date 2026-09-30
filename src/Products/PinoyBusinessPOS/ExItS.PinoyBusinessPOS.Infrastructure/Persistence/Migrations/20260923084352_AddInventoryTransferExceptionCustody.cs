using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryTransferExceptionCustody : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_movements_movement_type",
                schema: "pos",
                table: "stock_movements");

            migrationBuilder.AddColumn<Guid>(
                name: "actual_received_product_id",
                schema: "pos",
                table: "inventory_transfer_receipt_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "other_custody_decision",
                schema: "pos",
                table: "inventory_transfer_receipt_lines",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "inventory_transfer_exception_custodies",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transfer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    root_transfer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    receipt_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expected_product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actual_product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    reason_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    decision = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    follow_up_intent = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    held_branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recovered_sellable_qty = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    confirmed_non_sellable_qty = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    return_dispatched_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    return_dispatched_by = table.Column<Guid>(type: "uuid", nullable: true),
                    return_received_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    return_received_by = table.Column<Guid>(type: "uuid", nullable: true),
                    inspected_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    inspected_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_transfer_exception_custodies", x => x.id);
                    table.CheckConstraint("ck_itec_decision", "decision IN ('KeepAtDestination', 'ReturnToSource')");
                    table.CheckConstraint("ck_itec_follow_up", "follow_up_intent IN ('RequestReplacement', 'AcceptShortage')");
                    table.CheckConstraint("ck_itec_inspection_split", "recovered_sellable_qty >= 0 AND confirmed_non_sellable_qty >= 0");
                    table.CheckConstraint("ck_itec_quantity_positive", "quantity > 0");
                    table.CheckConstraint("ck_itec_status", "status IN ('HeldAtDestination', 'AwaitingReturn', 'ReturnInTransit', 'ReceivedAtSource', 'AwaitingInspection', 'Inspected')");
                    table.ForeignKey(
                        name: "fk_itec_receipt_line",
                        column: x => x.receipt_line_id,
                        principalSchema: "pos",
                        principalTable: "inventory_transfer_receipt_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_itec_transfer",
                        column: x => x.transfer_id,
                        principalSchema: "pos",
                        principalTable: "inventory_transfers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movements_movement_type",
                schema: "pos",
                table: "stock_movements",
                sql: "movement_type IN ('OpeningStock', 'ManualIncrease', 'ManualDecrease', 'SaleDeduction', 'SaleVoidRestoration', 'PurchaseReceipt', 'StockCountVarianceIncrease', 'StockCountVarianceDecrease', 'SaleReturnRestock', 'TransferOut', 'TransferIn', 'TransferCancelRestore', 'DirectPurchaseReceipt', 'ExpirationInitialization', 'StockUse', 'StockUseVoidRestoration', 'ProductionMaterialConsumption', 'ProductionMaterialRestoration', 'ProductionOutput', 'ProductionOutputReversal', 'WasteLoss', 'WasteLossVoidRestoration', 'PurchaseReceiptReversal', 'DirectPurchaseReceiptReversal', 'ConnectedPurchaseFulfillment', 'SaleReturnWriteOff', 'ConnectedPoReturnDispatch', 'ConnectedPoReturnRestock', 'ConnectedPoReturnWriteOff', 'ConnectedPurchaseFulfillmentReconciliation', 'TransferDamageHold', 'TransferDamageRecovery', 'TransferDamageReturnOut', 'TransferDamageReturnIn', 'TransferDamageWriteOff', 'TransferExceptionHold', 'TransferExceptionExpectedRestore', 'TransferExceptionActualOut', 'TransferExceptionReturnOut', 'TransferExceptionReturnIn', 'TransferExceptionRecovery', 'TransferExceptionWriteOff', 'TransferExceptionReturnRestock')");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_transfer_exception_custodies_receipt_line_id",
                schema: "pos",
                table: "inventory_transfer_exception_custodies",
                column: "receipt_line_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_transfer_exception_custodies_transfer_id",
                schema: "pos",
                table: "inventory_transfer_exception_custodies",
                column: "transfer_id");

            migrationBuilder.CreateIndex(
                name: "ix_itec_org_root",
                schema: "pos",
                table: "inventory_transfer_exception_custodies",
                columns: new[] { "organization_id", "root_transfer_id" });

            migrationBuilder.CreateIndex(
                name: "ix_itec_org_transfer",
                schema: "pos",
                table: "inventory_transfer_exception_custodies",
                columns: new[] { "organization_id", "transfer_id" });

            migrationBuilder.CreateIndex(
                name: "ux_itec_org_receipt_line",
                schema: "pos",
                table: "inventory_transfer_exception_custodies",
                columns: new[] { "organization_id", "receipt_line_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_transfer_exception_custodies",
                schema: "pos");

            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_movements_movement_type",
                schema: "pos",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "actual_received_product_id",
                schema: "pos",
                table: "inventory_transfer_receipt_lines");

            migrationBuilder.DropColumn(
                name: "other_custody_decision",
                schema: "pos",
                table: "inventory_transfer_receipt_lines");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movements_movement_type",
                schema: "pos",
                table: "stock_movements",
                sql: "movement_type IN ('OpeningStock', 'ManualIncrease', 'ManualDecrease', 'SaleDeduction', 'SaleVoidRestoration', 'PurchaseReceipt', 'StockCountVarianceIncrease', 'StockCountVarianceDecrease', 'SaleReturnRestock', 'TransferOut', 'TransferIn', 'TransferCancelRestore', 'DirectPurchaseReceipt', 'ExpirationInitialization', 'StockUse', 'StockUseVoidRestoration', 'ProductionMaterialConsumption', 'ProductionMaterialRestoration', 'ProductionOutput', 'ProductionOutputReversal', 'WasteLoss', 'WasteLossVoidRestoration', 'PurchaseReceiptReversal', 'DirectPurchaseReceiptReversal', 'ConnectedPurchaseFulfillment', 'SaleReturnWriteOff', 'ConnectedPoReturnDispatch', 'ConnectedPoReturnRestock', 'ConnectedPoReturnWriteOff', 'ConnectedPurchaseFulfillmentReconciliation', 'TransferDamageHold', 'TransferDamageRecovery', 'TransferDamageReturnOut', 'TransferDamageReturnIn', 'TransferDamageWriteOff')");
        }
    }
}
