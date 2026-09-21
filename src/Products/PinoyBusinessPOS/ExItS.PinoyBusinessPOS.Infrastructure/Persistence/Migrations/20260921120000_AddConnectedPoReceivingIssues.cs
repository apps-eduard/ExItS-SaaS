using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Seller receiving-issue review aggregate + ConnectedPurchaseFulfillmentReconciliation movement type.
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260921120000_AddConnectedPoReceivingIssues")]
public partial class AddConnectedPoReceivingIssues : Migration
{
    private const string PreviousMovementTypeSql =
        "movement_type IN ('OpeningStock', 'ManualIncrease', 'ManualDecrease', 'SaleDeduction', 'SaleVoidRestoration', 'PurchaseReceipt', 'StockCountVarianceIncrease', 'StockCountVarianceDecrease', 'SaleReturnRestock', 'TransferOut', 'TransferIn', 'TransferCancelRestore', 'DirectPurchaseReceipt', 'ExpirationInitialization', 'StockUse', 'StockUseVoidRestoration', 'ProductionMaterialConsumption', 'ProductionMaterialRestoration', 'ProductionOutput', 'ProductionOutputReversal', 'WasteLoss', 'WasteLossVoidRestoration', 'PurchaseReceiptReversal', 'DirectPurchaseReceiptReversal', 'ConnectedPurchaseFulfillment', 'SaleReturnWriteOff', 'ConnectedPoReturnDispatch', 'ConnectedPoReturnRestock', 'ConnectedPoReturnWriteOff')";

    private const string MovementTypeSql =
        "movement_type IN ('OpeningStock', 'ManualIncrease', 'ManualDecrease', 'SaleDeduction', 'SaleVoidRestoration', 'PurchaseReceipt', 'StockCountVarianceIncrease', 'StockCountVarianceDecrease', 'SaleReturnRestock', 'TransferOut', 'TransferIn', 'TransferCancelRestore', 'DirectPurchaseReceipt', 'ExpirationInitialization', 'StockUse', 'StockUseVoidRestoration', 'ProductionMaterialConsumption', 'ProductionMaterialRestoration', 'ProductionOutput', 'ProductionOutputReversal', 'WasteLoss', 'WasteLossVoidRestoration', 'PurchaseReceiptReversal', 'DirectPurchaseReceiptReversal', 'ConnectedPurchaseFulfillment', 'SaleReturnWriteOff', 'ConnectedPoReturnDispatch', 'ConnectedPoReturnRestock', 'ConnectedPoReturnWriteOff', 'ConnectedPurchaseFulfillmentReconciliation')";

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "movement_type",
            schema: "pos",
            table: "stock_movements",
            type: "character varying(48)",
            maxLength: 48,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(32)",
            oldMaxLength: 32);

        migrationBuilder.DropCheckConstraint(
            name: "ck_stock_movements_movement_type",
            schema: "pos",
            table: "stock_movements");

        migrationBuilder.AddCheckConstraint(
            name: "ck_stock_movements_movement_type",
            schema: "pos",
            table: "stock_movements",
            sql: MovementTypeSql);

        migrationBuilder.CreateTable(
            name: "connected_po_receiving_issues",
            schema: "pos",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                connected_purchase_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                purchase_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                goods_receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                buyer_organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                seller_organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                fulfillment_source_id = table.Column<Guid>(type: "uuid", nullable: false),
                status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                resolved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                resolved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                seller_notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_connected_po_receiving_issues", x => x.id);
                table.CheckConstraint(
                    "ck_cpo_receiving_issue_status",
                    "status IN ('PendingSellerReview', 'Resolved')");
            });

        migrationBuilder.CreateTable(
            name: "connected_po_receiving_issue_lines",
            schema: "pos",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                receiving_issue_id = table.Column<Guid>(type: "uuid", nullable: false),
                seller_organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                goods_receipt_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                purchase_order_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                supplier_product_id = table.Column<Guid>(type: "uuid", nullable: false),
                buyer_product_id = table.Column<Guid>(type: "uuid", nullable: true),
                name_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                uom_snapshot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                fulfillment_source_id = table.Column<Guid>(type: "uuid", nullable: false),
                shipped_qty = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                good_qty = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                damaged_qty = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                missing_qty = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                line_kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                buyer_discrepancy_kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                buyer_discrepancy_note = table.Column<string>(type: "character varying(280)", maxLength: 280, nullable: true),
                missing_resolution = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                damaged_resolution = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                resolution_qty = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                seller_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                inventory_movement_id = table.Column<Guid>(type: "uuid", nullable: true),
                return_batch_id = table.Column<Guid>(type: "uuid", nullable: true),
                resolved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                resolved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_connected_po_receiving_issue_lines", x => x.id);
                table.CheckConstraint(
                    "ck_cpo_receiving_issue_line_kind",
                    "line_kind IN ('Missing', 'Damaged')");
                table.CheckConstraint(
                    "ck_cpo_receiving_issue_missing_resolution",
                    "missing_resolution IS NULL OR missing_resolution IN ('FoundAtSeller', 'NeverShipped', 'LostInTransit', 'DeliveredDisputed', 'ReplacementPlanned', 'Other')");
                table.CheckConstraint(
                    "ck_cpo_receiving_issue_damaged_resolution",
                    "damaged_resolution IS NULL OR damaged_resolution IN ('AcceptedNoReturn', 'ReturnRequested', 'ReplacementApproved', 'Disputed', 'Other')");
                table.CheckConstraint(
                    "ck_cpo_receiving_issue_resolution_qty",
                    "resolution_qty >= 0");
                table.ForeignKey(
                    name: "fk_cpo_receiving_issue_lines_issue",
                    column: x => x.receiving_issue_id,
                    principalSchema: "pos",
                    principalTable: "connected_po_receiving_issues",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ux_cpo_receiving_issues_goods_receipt",
            schema: "pos",
            table: "connected_po_receiving_issues",
            column: "goods_receipt_id",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_cpo_receiving_issues_order_status",
            schema: "pos",
            table: "connected_po_receiving_issues",
            columns: new[] { "connected_purchase_order_id", "status" });

        migrationBuilder.CreateIndex(
            name: "ix_cpo_receiving_issues_seller_status",
            schema: "pos",
            table: "connected_po_receiving_issues",
            columns: new[] { "seller_organization_id", "status", "created_at_utc" });

        migrationBuilder.CreateIndex(
            name: "ux_cpo_receiving_issue_lines_grn_kind",
            schema: "pos",
            table: "connected_po_receiving_issue_lines",
            columns: new[] { "receiving_issue_id", "goods_receipt_line_id", "line_kind" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_cpo_receiving_issue_lines_seller_resolved",
            schema: "pos",
            table: "connected_po_receiving_issue_lines",
            columns: new[] { "seller_organization_id", "resolved_at_utc" });

        migrationBuilder.CreateIndex(
            name: "IX_connected_po_receiving_issue_lines_receiving_issue_id",
            schema: "pos",
            table: "connected_po_receiving_issue_lines",
            column: "receiving_issue_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "connected_po_receiving_issue_lines",
            schema: "pos");

        migrationBuilder.DropTable(
            name: "connected_po_receiving_issues",
            schema: "pos");

        migrationBuilder.DropCheckConstraint(
            name: "ck_stock_movements_movement_type",
            schema: "pos",
            table: "stock_movements");

        migrationBuilder.AddCheckConstraint(
            name: "ck_stock_movements_movement_type",
            schema: "pos",
            table: "stock_movements",
            sql: PreviousMovementTypeSql);

        migrationBuilder.AlterColumn<string>(
            name: "movement_type",
            schema: "pos",
            table: "stock_movements",
            type: "character varying(32)",
            maxLength: 32,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(48)",
            oldMaxLength: 48);
    }
}
