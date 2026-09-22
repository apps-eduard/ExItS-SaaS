using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReturnBatchesAndPendingReturnQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_movements_movement_type",
                schema: "pos",
                table: "stock_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_returns_refund_method",
                schema: "pos",
                table: "sale_returns");

            migrationBuilder.AddColumn<decimal>(
                name: "damaged_quantity",
                schema: "pos",
                table: "sale_return_lines",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "sellable_quantity",
                schema: "pos",
                table: "sale_return_lines",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql(
                """
                UPDATE pos.sale_return_lines
                SET sellable_quantity = CASE
                        WHEN restock_disposition = 'ReturnToStock' THEN quantity_returned
                        ELSE 0
                    END,
                    damaged_quantity = CASE
                        WHEN restock_disposition = 'DoNotRestock' THEN quantity_returned
                        ELSE 0
                    END;
                """);

            migrationBuilder.AddColumn<decimal>(
                name: "pending_return_quantity",
                schema: "pos",
                table: "inventory_branch_balances",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "pending_return_quantity",
                schema: "pos",
                table: "inventory_accounts",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "check_settlement_status",
                schema: "pos",
                table: "sales",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE pos.sales
                SET check_settlement_status = 'Pending'
                WHERE payment_method = 'Check' AND check_settlement_status IS NULL;
                """);

            migrationBuilder.CreateTable(
                name: "return_batch_number_sequences",
                schema: "pos",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    last_value = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_return_batch_number_sequences", x => new { x.organization_id, x.business_date });
                    table.CheckConstraint("ck_return_batch_number_sequences_last_value_positive", "last_value > 0");
                });

            migrationBuilder.CreateTable(
                name: "return_batches",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sale_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    batch_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    refund_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    accepted_return_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    refund_due_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    refunded_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    sale_return_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    notes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    finalized_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    finalized_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_return_batches", x => x.id);
                    table.CheckConstraint("ck_return_batches_accepted_value_positive", "accepted_return_value > 0");
                    table.CheckConstraint("ck_return_batches_refund_due_non_negative", "refund_due_amount >= 0");
                    table.CheckConstraint("ck_return_batches_refund_status", "refund_status IN ('None', 'RefundDue', 'Refunded', 'ObligationReduced', 'CreditReduced')");
                    table.CheckConstraint("ck_return_batches_refunded_non_negative", "refunded_amount >= 0");
                    table.CheckConstraint("ck_return_batches_status", "status IN ('PendingInspection', 'ReadyForFinalize', 'Finalized')");
                    table.ForeignKey(
                        name: "fk_return_batches_sale_returns",
                        column: x => x.sale_return_id,
                        principalSchema: "pos",
                        principalTable: "sale_returns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_return_batches_sales",
                        column: x => x.sale_id,
                        principalSchema: "pos",
                        principalTable: "sales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "return_batch_lines",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    return_batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sale_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_name_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    uom_snapshot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    unit_price_snapshot = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    line_total_snapshot = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    accepted_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    refund_amount_snapshot = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    sellable_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    damaged_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    inspection_note = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    classified_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    classified_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_return_batch_lines", x => x.id);
                    table.CheckConstraint("ck_return_batch_lines_accepted_positive", "accepted_quantity > 0");
                    table.CheckConstraint("ck_return_batch_lines_refund_positive", "refund_amount_snapshot > 0");
                    table.CheckConstraint("ck_return_batch_lines_split_non_negative", "(sellable_quantity IS NULL OR sellable_quantity >= 0) AND (damaged_quantity IS NULL OR damaged_quantity >= 0)");
                    table.CheckConstraint("ck_return_batch_lines_split_sum", "(sellable_quantity IS NULL AND damaged_quantity IS NULL) OR sellable_quantity + damaged_quantity = accepted_quantity");
                    table.CheckConstraint("ck_return_batch_lines_uom", "uom_snapshot IN ('Piece', 'Pack', 'Box', 'Bottle', 'Can', 'Sachet', 'Kilogram', 'Gram', 'Liter', 'Milliliter', 'Meter')");
                    table.ForeignKey(
                        name: "fk_return_batch_lines_batches",
                        column: x => x.return_batch_id,
                        principalSchema: "pos",
                        principalTable: "return_batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_return_batch_lines_sale_lines",
                        column: x => x.sale_line_id,
                        principalSchema: "pos",
                        principalTable: "sale_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "return_batch_audit_events",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    return_batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    payload_json = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_return_batch_audit_events", x => x.id);
                    table.CheckConstraint("ck_return_batch_audit_events_event_type", "event_type IN ('Accepted', 'ClassificationSaved', 'Finalized', 'RefundRecorded')");
                    table.ForeignKey(
                        name: "fk_return_batch_audit_events_batches",
                        column: x => x.return_batch_id,
                        principalSchema: "pos",
                        principalTable: "return_batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "return_batch_refunds",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    return_batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    method = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    reference = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    note = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    client_refund_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_return_batch_refunds", x => x.id);
                    table.CheckConstraint("ck_return_batch_refunds_amount_positive", "amount > 0");
                    table.CheckConstraint("ck_return_batch_refunds_method", "method IN ('Cash', 'ManualGCash', 'Card', 'GCash', 'BankTransfer', 'Check', 'ManualMaya')");
                    table.ForeignKey(
                        name: "fk_return_batch_refunds_batches",
                        column: x => x.return_batch_id,
                        principalSchema: "pos",
                        principalTable: "return_batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movements_movement_type",
                schema: "pos",
                table: "stock_movements",
                sql: "movement_type IN ('OpeningStock', 'ManualIncrease', 'ManualDecrease', 'SaleDeduction', 'SaleVoidRestoration', 'PurchaseReceipt', 'StockCountVarianceIncrease', 'StockCountVarianceDecrease', 'SaleReturnRestock', 'TransferOut', 'TransferIn', 'TransferCancelRestore', 'DirectPurchaseReceipt', 'ExpirationInitialization', 'StockUse', 'StockUseVoidRestoration', 'ProductionMaterialConsumption', 'ProductionMaterialRestoration', 'ProductionOutput', 'ProductionOutputReversal', 'WasteLoss', 'WasteLossVoidRestoration', 'PurchaseReceiptReversal', 'DirectPurchaseReceiptReversal', 'ConnectedPurchaseFulfillment', 'SaleReturnWriteOff')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_returns_refund_method",
                schema: "pos",
                table: "sale_returns",
                sql: "refund_method IN ('Cash', 'ManualGCash', 'Utang', 'Card', 'GCash', 'BankTransfer', 'Check', 'ManualMaya')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_return_lines_split_quantities_match",
                schema: "pos",
                table: "sale_return_lines",
                sql: "sellable_quantity + damaged_quantity = quantity_returned");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_return_lines_split_quantities_non_negative",
                schema: "pos",
                table: "sale_return_lines",
                sql: "sellable_quantity >= 0 AND damaged_quantity >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_branch_balances_pending_return_non_negative",
                schema: "pos",
                table: "inventory_branch_balances",
                sql: "pending_return_quantity >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_accounts_pending_return_non_negative",
                schema: "pos",
                table: "inventory_accounts",
                sql: "pending_return_quantity >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_check_settlement_status",
                schema: "pos",
                table: "sales",
                sql: "(payment_method = 'Check' AND check_settlement_status IN ('Pending', 'Cleared', 'Bounced')) OR (payment_method <> 'Check' AND check_settlement_status IS NULL)");

            migrationBuilder.CreateIndex(
                name: "ix_return_batch_lines_org_batch",
                schema: "pos",
                table: "return_batch_lines",
                columns: new[] { "organization_id", "return_batch_id" });

            migrationBuilder.CreateIndex(
                name: "IX_return_batch_lines_return_batch_id",
                schema: "pos",
                table: "return_batch_lines",
                column: "return_batch_id");

            migrationBuilder.CreateIndex(
                name: "IX_return_batch_lines_sale_line_id",
                schema: "pos",
                table: "return_batch_lines",
                column: "sale_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_return_batch_audit_events_org_batch_created",
                schema: "pos",
                table: "return_batch_audit_events",
                columns: new[] { "organization_id", "return_batch_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_return_batch_audit_events_return_batch_id",
                schema: "pos",
                table: "return_batch_audit_events",
                column: "return_batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_return_batches_org_sale_created",
                schema: "pos",
                table: "return_batches",
                columns: new[] { "organization_id", "sale_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_return_batches_sale_id",
                schema: "pos",
                table: "return_batches",
                column: "sale_id");

            migrationBuilder.CreateIndex(
                name: "IX_return_batches_sale_return_id",
                schema: "pos",
                table: "return_batches",
                column: "sale_return_id");

            migrationBuilder.CreateIndex(
                name: "ux_return_batches_org_batch_number",
                schema: "pos",
                table: "return_batches",
                columns: new[] { "organization_id", "batch_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_return_batch_refunds_org_batch_created",
                schema: "pos",
                table: "return_batch_refunds",
                columns: new[] { "organization_id", "return_batch_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_return_batch_refunds_org_batch_client",
                schema: "pos",
                table: "return_batch_refunds",
                columns: new[] { "organization_id", "return_batch_id", "client_refund_id" },
                unique: true,
                filter: "client_refund_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_return_batch_refunds_return_batch_id",
                schema: "pos",
                table: "return_batch_refunds",
                column: "return_batch_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "return_batch_lines",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "return_batch_audit_events",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "return_batch_number_sequences",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "return_batch_refunds",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "return_batches",
                schema: "pos");

            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_movements_movement_type",
                schema: "pos",
                table: "stock_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_returns_refund_method",
                schema: "pos",
                table: "sale_returns");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_return_lines_split_quantities_match",
                schema: "pos",
                table: "sale_return_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_return_lines_split_quantities_non_negative",
                schema: "pos",
                table: "sale_return_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_branch_balances_pending_return_non_negative",
                schema: "pos",
                table: "inventory_branch_balances");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_accounts_pending_return_non_negative",
                schema: "pos",
                table: "inventory_accounts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_check_settlement_status",
                schema: "pos",
                table: "sales");

            migrationBuilder.DropColumn(
                name: "damaged_quantity",
                schema: "pos",
                table: "sale_return_lines");

            migrationBuilder.DropColumn(
                name: "sellable_quantity",
                schema: "pos",
                table: "sale_return_lines");

            migrationBuilder.DropColumn(
                name: "pending_return_quantity",
                schema: "pos",
                table: "inventory_branch_balances");

            migrationBuilder.DropColumn(
                name: "pending_return_quantity",
                schema: "pos",
                table: "inventory_accounts");

            migrationBuilder.DropColumn(
                name: "check_settlement_status",
                schema: "pos",
                table: "sales");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movements_movement_type",
                schema: "pos",
                table: "stock_movements",
                sql: "movement_type IN ('OpeningStock', 'ManualIncrease', 'ManualDecrease', 'SaleDeduction', 'SaleVoidRestoration', 'PurchaseReceipt', 'StockCountVarianceIncrease', 'StockCountVarianceDecrease', 'SaleReturnRestock', 'TransferOut', 'TransferIn', 'TransferCancelRestore', 'DirectPurchaseReceipt', 'ExpirationInitialization', 'StockUse', 'StockUseVoidRestoration', 'ProductionMaterialConsumption', 'ProductionMaterialRestoration', 'ProductionOutput', 'ProductionOutputReversal', 'WasteLoss', 'WasteLossVoidRestoration', 'PurchaseReceiptReversal', 'DirectPurchaseReceiptReversal', 'ConnectedPurchaseFulfillment')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_returns_refund_method",
                schema: "pos",
                table: "sale_returns",
                sql: "refund_method IN ('Cash', 'ManualGCash', 'Utang')");
        }
    }
}
