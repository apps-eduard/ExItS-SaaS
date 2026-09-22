using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConnectedPoReturnBatchSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Local Validation / partial applies: AddReturnBatches may be recorded without
            // audit/refund tables. Recreate them before altering event_type / status constraints.
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS pos.return_batch_audit_events (
                    id uuid NOT NULL,
                    return_batch_id uuid NOT NULL,
                    organization_id uuid NOT NULL,
                    event_type character varying(64) NOT NULL,
                    payload_json character varying(2048) NOT NULL,
                    created_at_utc timestamp with time zone NOT NULL,
                    created_by uuid NOT NULL,
                    CONSTRAINT "PK_return_batch_audit_events" PRIMARY KEY (id),
                    CONSTRAINT ck_return_batch_audit_events_event_type
                        CHECK (event_type IN ('Accepted', 'ClassificationSaved', 'Finalized', 'RefundRecorded')),
                    CONSTRAINT fk_return_batch_audit_events_batches
                        FOREIGN KEY (return_batch_id) REFERENCES pos.return_batches (id) ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS ix_return_batch_audit_events_org_batch_created
                    ON pos.return_batch_audit_events (organization_id, return_batch_id, created_at_utc);
                CREATE INDEX IF NOT EXISTS "IX_return_batch_audit_events_return_batch_id"
                    ON pos.return_batch_audit_events (return_batch_id);

                CREATE TABLE IF NOT EXISTS pos.return_batch_refunds (
                    id uuid NOT NULL,
                    return_batch_id uuid NOT NULL,
                    organization_id uuid NOT NULL,
                    amount numeric(18,2) NOT NULL,
                    method character varying(32) NOT NULL,
                    reference character varying(64) NULL,
                    note character varying(512) NULL,
                    client_refund_id character varying(64) NULL,
                    created_at_utc timestamp with time zone NOT NULL,
                    created_by uuid NOT NULL,
                    CONSTRAINT "PK_return_batch_refunds" PRIMARY KEY (id),
                    CONSTRAINT ck_return_batch_refunds_amount_positive CHECK (amount > 0),
                    CONSTRAINT ck_return_batch_refunds_method
                        CHECK (method IN ('Cash', 'ManualGCash', 'Card', 'GCash', 'BankTransfer', 'Check', 'ManualMaya')),
                    CONSTRAINT fk_return_batch_refunds_batches
                        FOREIGN KEY (return_batch_id) REFERENCES pos.return_batches (id) ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS ix_return_batch_refunds_org_batch_created
                    ON pos.return_batch_refunds (organization_id, return_batch_id, created_at_utc);
                CREATE UNIQUE INDEX IF NOT EXISTS ux_return_batch_refunds_org_client_refund_id
                    ON pos.return_batch_refunds (organization_id, client_refund_id)
                    WHERE client_refund_id IS NOT NULL;
                CREATE INDEX IF NOT EXISTS "IX_return_batch_refunds_return_batch_id"
                    ON pos.return_batch_refunds (return_batch_id);
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE pos.stock_movements DROP CONSTRAINT IF EXISTS ck_stock_movements_movement_type;
                ALTER TABLE pos.stock_movements DROP CONSTRAINT IF EXISTS ck_stock_movements_source_type;
                ALTER TABLE pos.return_batches DROP CONSTRAINT IF EXISTS ck_return_batches_status;
                ALTER TABLE pos.return_batch_audit_events DROP CONSTRAINT IF EXISTS ck_return_batch_audit_events_event_type;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "sale_id",
                schema: "pos",
                table: "return_batches",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "buyer_branch_id",
                schema: "pos",
                table: "return_batches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "buyer_organization_id",
                schema: "pos",
                table: "return_batches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "connected_purchase_order_id",
                schema: "pos",
                table: "return_batches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payment_timing",
                schema: "pos",
                table: "return_batches",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "po_number_snapshot",
                schema: "pos",
                table: "return_batches",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "purchase_order_id",
                schema: "pos",
                table: "return_batches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "seller_branch_id",
                schema: "pos",
                table: "return_batches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "seller_organization_id",
                schema: "pos",
                table: "return_batches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "seller_received_at_utc",
                schema: "pos",
                table: "return_batches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "seller_received_by",
                schema: "pos",
                table: "return_batches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_type",
                schema: "pos",
                table: "return_batches",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            // Every pre-existing return batch originated from a sale.
            migrationBuilder.Sql("UPDATE pos.return_batches SET source_type = 'Sale' WHERE source_type IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "source_type",
                schema: "pos",
                table: "return_batches",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "sale_line_id",
                schema: "pos",
                table: "return_batch_lines",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "purchase_order_line_id",
                schema: "pos",
                table: "return_batch_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "supplier_product_id",
                schema: "pos",
                table: "return_batch_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_return_batch_audit_events_event_type",
                schema: "pos",
                table: "return_batch_audit_events",
                sql: "event_type IN ('Accepted', 'ClassificationSaved', 'Finalized', 'RefundRecorded', 'ReceivedBySeller')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movements_movement_type",
                schema: "pos",
                table: "stock_movements",
                sql: "movement_type IN ('OpeningStock', 'ManualIncrease', 'ManualDecrease', 'SaleDeduction', 'SaleVoidRestoration', 'PurchaseReceipt', 'StockCountVarianceIncrease', 'StockCountVarianceDecrease', 'SaleReturnRestock', 'TransferOut', 'TransferIn', 'TransferCancelRestore', 'DirectPurchaseReceipt', 'ExpirationInitialization', 'StockUse', 'StockUseVoidRestoration', 'ProductionMaterialConsumption', 'ProductionMaterialRestoration', 'ProductionOutput', 'ProductionOutputReversal', 'WasteLoss', 'WasteLossVoidRestoration', 'PurchaseReceiptReversal', 'DirectPurchaseReceiptReversal', 'ConnectedPurchaseFulfillment', 'SaleReturnWriteOff', 'ConnectedPoReturnDispatch', 'ConnectedPoReturnRestock', 'ConnectedPoReturnWriteOff')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movements_source_type",
                schema: "pos",
                table: "stock_movements",
                sql: "source_type IN ('None', 'Sale', 'Manual', 'Opening', 'PurchaseReceipt', 'StockCount', 'SaleReturn', 'InventoryTransfer', 'CustomerOrder', 'DirectPurchase', 'StockUse', 'Production', 'WasteLoss', 'ConnectedPurchaseOrder', 'ReturnBatch')");

            migrationBuilder.CreateIndex(
                name: "ix_return_batches_buyer_org_created",
                schema: "pos",
                table: "return_batches",
                columns: new[] { "buyer_organization_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_return_batches_connected_po_created",
                schema: "pos",
                table: "return_batches",
                columns: new[] { "connected_purchase_order_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_return_batches_purchase_order_created",
                schema: "pos",
                table: "return_batches",
                columns: new[] { "purchase_order_id", "created_at_utc" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_return_batches_source_identity",
                schema: "pos",
                table: "return_batches",
                sql: "(source_type = 'Sale' AND sale_id IS NOT NULL AND purchase_order_id IS NULL) OR (source_type = 'ConnectedPurchaseOrder' AND sale_id IS NULL AND purchase_order_id IS NOT NULL AND buyer_organization_id IS NOT NULL AND seller_organization_id IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_return_batches_source_type",
                schema: "pos",
                table: "return_batches",
                sql: "source_type IN ('Sale', 'ConnectedPurchaseOrder')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_return_batches_status",
                schema: "pos",
                table: "return_batches",
                sql: "status IN ('PendingInspection', 'ReadyForFinalize', 'Finalized', 'AwaitingSellerReceipt')");

            migrationBuilder.CreateIndex(
                name: "IX_return_batch_lines_purchase_order_line_id",
                schema: "pos",
                table: "return_batch_lines",
                column: "purchase_order_line_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_return_batch_lines_source_identity",
                schema: "pos",
                table: "return_batch_lines",
                sql: "(sale_line_id IS NOT NULL AND purchase_order_line_id IS NULL) OR (sale_line_id IS NULL AND purchase_order_line_id IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "fk_return_batch_lines_purchase_order_lines",
                schema: "pos",
                table: "return_batch_lines",
                column: "purchase_order_line_id",
                principalSchema: "pos",
                principalTable: "purchase_order_lines",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_return_batches_purchase_orders",
                schema: "pos",
                table: "return_batches",
                column: "purchase_order_id",
                principalSchema: "pos",
                principalTable: "purchase_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_return_batch_lines_purchase_order_lines",
                schema: "pos",
                table: "return_batch_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_return_batches_purchase_orders",
                schema: "pos",
                table: "return_batches");

            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_movements_movement_type",
                schema: "pos",
                table: "stock_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_movements_source_type",
                schema: "pos",
                table: "stock_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_return_batch_audit_events_event_type",
                schema: "pos",
                table: "return_batch_audit_events");

            migrationBuilder.DropIndex(
                name: "ix_return_batches_buyer_org_created",
                schema: "pos",
                table: "return_batches");

            migrationBuilder.DropIndex(
                name: "ix_return_batches_connected_po_created",
                schema: "pos",
                table: "return_batches");

            migrationBuilder.DropIndex(
                name: "ix_return_batches_purchase_order_created",
                schema: "pos",
                table: "return_batches");

            migrationBuilder.DropCheckConstraint(
                name: "ck_return_batches_source_identity",
                schema: "pos",
                table: "return_batches");

            migrationBuilder.DropCheckConstraint(
                name: "ck_return_batches_source_type",
                schema: "pos",
                table: "return_batches");

            migrationBuilder.DropCheckConstraint(
                name: "ck_return_batches_status",
                schema: "pos",
                table: "return_batches");

            migrationBuilder.DropIndex(
                name: "IX_return_batch_lines_purchase_order_line_id",
                schema: "pos",
                table: "return_batch_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_return_batch_lines_source_identity",
                schema: "pos",
                table: "return_batch_lines");

            // Connected-PO sourced rows cannot be represented by the sale-only shape.
            migrationBuilder.Sql(
                "DELETE FROM pos.return_batch_lines WHERE return_batch_id IN (SELECT id FROM pos.return_batches WHERE source_type = 'ConnectedPurchaseOrder');");
            migrationBuilder.Sql(
                "DELETE FROM pos.return_batch_refunds WHERE return_batch_id IN (SELECT id FROM pos.return_batches WHERE source_type = 'ConnectedPurchaseOrder');");
            migrationBuilder.Sql(
                "DELETE FROM pos.return_batch_audit_events WHERE return_batch_id IN (SELECT id FROM pos.return_batches WHERE source_type = 'ConnectedPurchaseOrder');");
            migrationBuilder.Sql(
                "DELETE FROM pos.return_batches WHERE source_type = 'ConnectedPurchaseOrder';");

            migrationBuilder.DropColumn(
                name: "buyer_branch_id",
                schema: "pos",
                table: "return_batches");

            migrationBuilder.DropColumn(
                name: "buyer_organization_id",
                schema: "pos",
                table: "return_batches");

            migrationBuilder.DropColumn(
                name: "connected_purchase_order_id",
                schema: "pos",
                table: "return_batches");

            migrationBuilder.DropColumn(
                name: "payment_timing",
                schema: "pos",
                table: "return_batches");

            migrationBuilder.DropColumn(
                name: "po_number_snapshot",
                schema: "pos",
                table: "return_batches");

            migrationBuilder.DropColumn(
                name: "purchase_order_id",
                schema: "pos",
                table: "return_batches");

            migrationBuilder.DropColumn(
                name: "seller_branch_id",
                schema: "pos",
                table: "return_batches");

            migrationBuilder.DropColumn(
                name: "seller_organization_id",
                schema: "pos",
                table: "return_batches");

            migrationBuilder.DropColumn(
                name: "seller_received_at_utc",
                schema: "pos",
                table: "return_batches");

            migrationBuilder.DropColumn(
                name: "seller_received_by",
                schema: "pos",
                table: "return_batches");

            migrationBuilder.DropColumn(
                name: "source_type",
                schema: "pos",
                table: "return_batches");

            migrationBuilder.DropColumn(
                name: "purchase_order_line_id",
                schema: "pos",
                table: "return_batch_lines");

            migrationBuilder.DropColumn(
                name: "supplier_product_id",
                schema: "pos",
                table: "return_batch_lines");

            migrationBuilder.AlterColumn<Guid>(
                name: "sale_id",
                schema: "pos",
                table: "return_batches",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "sale_line_id",
                schema: "pos",
                table: "return_batch_lines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_return_batch_audit_events_event_type",
                schema: "pos",
                table: "return_batch_audit_events",
                sql: "event_type IN ('Accepted', 'ClassificationSaved', 'Finalized', 'RefundRecorded')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movements_movement_type",
                schema: "pos",
                table: "stock_movements",
                sql: "movement_type IN ('OpeningStock', 'ManualIncrease', 'ManualDecrease', 'SaleDeduction', 'SaleVoidRestoration', 'PurchaseReceipt', 'StockCountVarianceIncrease', 'StockCountVarianceDecrease', 'SaleReturnRestock', 'TransferOut', 'TransferIn', 'TransferCancelRestore', 'DirectPurchaseReceipt', 'ExpirationInitialization', 'StockUse', 'StockUseVoidRestoration', 'ProductionMaterialConsumption', 'ProductionMaterialRestoration', 'ProductionOutput', 'ProductionOutputReversal', 'WasteLoss', 'WasteLossVoidRestoration', 'PurchaseReceiptReversal', 'DirectPurchaseReceiptReversal', 'ConnectedPurchaseFulfillment', 'SaleReturnWriteOff')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movements_source_type",
                schema: "pos",
                table: "stock_movements",
                sql: "source_type IN ('None', 'Sale', 'Manual', 'Opening', 'PurchaseReceipt', 'StockCount', 'SaleReturn', 'InventoryTransfer', 'CustomerOrder', 'DirectPurchase', 'StockUse', 'Production', 'WasteLoss', 'ConnectedPurchaseOrder')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_return_batches_status",
                schema: "pos",
                table: "return_batches",
                sql: "status IN ('PendingInspection', 'ReadyForFinalize', 'Finalized')");
        }
    }
}
