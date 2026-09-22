using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryTransferDamageCustodyAndReplacementFamily : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_movements_movement_type",
                schema: "pos",
                table: "stock_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_branch_balances_reserved_not_over_on_hand",
                schema: "pos",
                table: "inventory_branch_balances");

            migrationBuilder.AlterColumn<string>(
                name: "transfer_number",
                schema: "pos",
                table: "inventory_transfers",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "damage_handling_policy",
                schema: "pos",
                table: "inventory_transfers",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "ReceiverMayDecide");

            migrationBuilder.AddColumn<string>(
                name: "replacement_reason",
                schema: "pos",
                table: "inventory_transfers",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "replacement_sequence",
                schema: "pos",
                table: "inventory_transfers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "root_transfer_id",
                schema: "pos",
                table: "inventory_transfers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "damaged_quantity",
                schema: "pos",
                table: "inventory_branch_balances",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "inspection_hold_quantity",
                schema: "pos",
                table: "inventory_branch_balances",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "inventory_transfer_damage_custodies",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transfer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    root_transfer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    receipt_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    decision = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    follow_up_intent = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    held_branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recovered_sellable_qty = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    confirmed_damaged_qty = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    waived_qty = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
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
                    table.PrimaryKey("PK_inventory_transfer_damage_custodies", x => x.id);
                    table.CheckConstraint("ck_itdc_decision", "decision IN ('KeepAtDestination', 'ReturnToSource')");
                    table.CheckConstraint("ck_itdc_follow_up", "follow_up_intent IN ('RequestReplacement', 'AcceptShortage')");
                    table.CheckConstraint("ck_itdc_inspection_split", "recovered_sellable_qty >= 0 AND confirmed_damaged_qty >= 0 AND waived_qty >= 0");
                    table.CheckConstraint("ck_itdc_quantity_positive", "quantity > 0");
                    table.CheckConstraint("ck_itdc_status", "status IN ('HeldAtDestination', 'AwaitingReturn', 'ReturnInTransit', 'ReceivedAtSource', 'AwaitingInspection', 'Inspected')");
                    table.ForeignKey(
                        name: "fk_itdc_receipt_line",
                        column: x => x.receipt_line_id,
                        principalSchema: "pos",
                        principalTable: "inventory_transfer_receipt_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_itdc_transfer",
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
                sql: "movement_type IN ('OpeningStock', 'ManualIncrease', 'ManualDecrease', 'SaleDeduction', 'SaleVoidRestoration', 'PurchaseReceipt', 'StockCountVarianceIncrease', 'StockCountVarianceDecrease', 'SaleReturnRestock', 'TransferOut', 'TransferIn', 'TransferCancelRestore', 'DirectPurchaseReceipt', 'ExpirationInitialization', 'StockUse', 'StockUseVoidRestoration', 'ProductionMaterialConsumption', 'ProductionMaterialRestoration', 'ProductionOutput', 'ProductionOutputReversal', 'WasteLoss', 'WasteLossVoidRestoration', 'PurchaseReceiptReversal', 'DirectPurchaseReceiptReversal', 'ConnectedPurchaseFulfillment', 'SaleReturnWriteOff', 'ConnectedPoReturnDispatch', 'ConnectedPoReturnRestock', 'ConnectedPoReturnWriteOff', 'ConnectedPurchaseFulfillmentReconciliation', 'TransferDamageHold', 'TransferDamageRecovery', 'TransferDamageReturnOut', 'TransferDamageReturnIn', 'TransferDamageWriteOff')");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_transfers_org_root",
                schema: "pos",
                table: "inventory_transfers",
                columns: new[] { "organization_id", "root_transfer_id" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_transfers_root_transfer_id",
                schema: "pos",
                table: "inventory_transfers",
                column: "root_transfer_id");

            migrationBuilder.CreateIndex(
                name: "ux_inventory_transfers_org_root_replacement_sequence",
                schema: "pos",
                table: "inventory_transfers",
                columns: new[] { "organization_id", "root_transfer_id", "replacement_sequence" },
                unique: true,
                filter: "root_transfer_id IS NOT NULL AND replacement_sequence IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_transfers_damage_handling_policy",
                schema: "pos",
                table: "inventory_transfers",
                sql: "damage_handling_policy IN ('ReceiverMayDecide', 'ReturnToSourceRequired', 'KeepAtDestination')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_transfers_replacement_shape",
                schema: "pos",
                table: "inventory_transfers",
                sql: "(root_transfer_id IS NULL AND replacement_sequence IS NULL) OR (root_transfer_id IS NOT NULL AND replacement_sequence IS NOT NULL AND replacement_sequence >= 1)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_branch_balances_buckets_not_over_on_hand",
                schema: "pos",
                table: "inventory_branch_balances",
                sql: "reserved_quantity + pending_return_quantity + inspection_hold_quantity + damaged_quantity <= on_hand_quantity");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_branch_balances_damaged_non_negative",
                schema: "pos",
                table: "inventory_branch_balances",
                sql: "damaged_quantity >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_branch_balances_inspection_hold_non_negative",
                schema: "pos",
                table: "inventory_branch_balances",
                sql: "inspection_hold_quantity >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_transfer_damage_custodies_receipt_line_id",
                schema: "pos",
                table: "inventory_transfer_damage_custodies",
                column: "receipt_line_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_transfer_damage_custodies_transfer_id",
                schema: "pos",
                table: "inventory_transfer_damage_custodies",
                column: "transfer_id");

            migrationBuilder.CreateIndex(
                name: "ix_itdc_org_root",
                schema: "pos",
                table: "inventory_transfer_damage_custodies",
                columns: new[] { "organization_id", "root_transfer_id" });

            migrationBuilder.CreateIndex(
                name: "ix_itdc_org_transfer",
                schema: "pos",
                table: "inventory_transfer_damage_custodies",
                columns: new[] { "organization_id", "transfer_id" });

            migrationBuilder.CreateIndex(
                name: "ux_itdc_org_receipt_line",
                schema: "pos",
                table: "inventory_transfer_damage_custodies",
                columns: new[] { "organization_id", "receipt_line_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_transfers_root_transfer",
                schema: "pos",
                table: "inventory_transfers",
                column: "root_transfer_id",
                principalSchema: "pos",
                principalTable: "inventory_transfers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_inventory_transfers_root_transfer",
                schema: "pos",
                table: "inventory_transfers");

            migrationBuilder.DropTable(
                name: "inventory_transfer_damage_custodies",
                schema: "pos");

            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_movements_movement_type",
                schema: "pos",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "ix_inventory_transfers_org_root",
                schema: "pos",
                table: "inventory_transfers");

            migrationBuilder.DropIndex(
                name: "IX_inventory_transfers_root_transfer_id",
                schema: "pos",
                table: "inventory_transfers");

            migrationBuilder.DropIndex(
                name: "ux_inventory_transfers_org_root_replacement_sequence",
                schema: "pos",
                table: "inventory_transfers");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_transfers_damage_handling_policy",
                schema: "pos",
                table: "inventory_transfers");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_transfers_replacement_shape",
                schema: "pos",
                table: "inventory_transfers");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_branch_balances_buckets_not_over_on_hand",
                schema: "pos",
                table: "inventory_branch_balances");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_branch_balances_damaged_non_negative",
                schema: "pos",
                table: "inventory_branch_balances");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_branch_balances_inspection_hold_non_negative",
                schema: "pos",
                table: "inventory_branch_balances");

            migrationBuilder.DropColumn(
                name: "damage_handling_policy",
                schema: "pos",
                table: "inventory_transfers");

            migrationBuilder.DropColumn(
                name: "replacement_reason",
                schema: "pos",
                table: "inventory_transfers");

            migrationBuilder.DropColumn(
                name: "replacement_sequence",
                schema: "pos",
                table: "inventory_transfers");

            migrationBuilder.DropColumn(
                name: "root_transfer_id",
                schema: "pos",
                table: "inventory_transfers");

            migrationBuilder.DropColumn(
                name: "damaged_quantity",
                schema: "pos",
                table: "inventory_branch_balances");

            migrationBuilder.DropColumn(
                name: "inspection_hold_quantity",
                schema: "pos",
                table: "inventory_branch_balances");

            migrationBuilder.AlterColumn<string>(
                name: "transfer_number",
                schema: "pos",
                table: "inventory_transfers",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movements_movement_type",
                schema: "pos",
                table: "stock_movements",
                sql: "movement_type IN ('OpeningStock', 'ManualIncrease', 'ManualDecrease', 'SaleDeduction', 'SaleVoidRestoration', 'PurchaseReceipt', 'StockCountVarianceIncrease', 'StockCountVarianceDecrease', 'SaleReturnRestock', 'TransferOut', 'TransferIn', 'TransferCancelRestore', 'DirectPurchaseReceipt', 'ExpirationInitialization', 'StockUse', 'StockUseVoidRestoration', 'ProductionMaterialConsumption', 'ProductionMaterialRestoration', 'ProductionOutput', 'ProductionOutputReversal', 'WasteLoss', 'WasteLossVoidRestoration', 'PurchaseReceiptReversal', 'DirectPurchaseReceiptReversal', 'ConnectedPurchaseFulfillment', 'SaleReturnWriteOff', 'ConnectedPoReturnDispatch', 'ConnectedPoReturnRestock', 'ConnectedPoReturnWriteOff', 'ConnectedPurchaseFulfillmentReconciliation')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_branch_balances_reserved_not_over_on_hand",
                schema: "pos",
                table: "inventory_branch_balances",
                sql: "reserved_quantity <= on_hand_quantity");
        }
    }
}
