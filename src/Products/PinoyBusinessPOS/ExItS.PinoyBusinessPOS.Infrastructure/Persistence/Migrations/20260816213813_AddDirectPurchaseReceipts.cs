using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDirectPurchaseReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_movements_movement_type",
                schema: "pos",
                table: "stock_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_movements_source_type",
                schema: "pos",
                table: "stock_movements");

            migrationBuilder.CreateTable(
                name: "direct_purchase_receipt_number_sequences",
                schema: "pos",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    last_value = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_direct_purchase_receipt_number_sequences", x => new { x.organization_id, x.business_date });
                    table.CheckConstraint("ck_direct_purchase_receipt_number_sequences_last_value_positive", "last_value > 0");
                });

            migrationBuilder.CreateTable(
                name: "direct_purchase_receipts",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    receipt_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    purchase_date = table.Column<DateOnly>(type: "date", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_name_snapshot = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    reference_number = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    notes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    total_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_direct_purchase_receipts", x => x.id);
                    table.CheckConstraint("ck_direct_purchase_receipts_total_cost_non_negative", "total_cost >= 0");
                    table.ForeignKey(
                        name: "fk_direct_purchase_receipts_suppliers",
                        column: x => x.supplier_id,
                        principalSchema: "pos",
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "direct_purchase_receipt_lines",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    product_name_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    sku_snapshot = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    unit_of_measure_snapshot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    line_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: true),
                    lot_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    inventory_movement_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_direct_purchase_receipt_lines", x => x.id);
                    table.CheckConstraint("ck_direct_purchase_receipt_lines_line_total_non_negative", "line_total >= 0");
                    table.CheckConstraint("ck_direct_purchase_receipt_lines_quantity_positive", "quantity > 0");
                    table.CheckConstraint("ck_direct_purchase_receipt_lines_unit_cost_positive", "unit_cost > 0");
                    table.ForeignKey(
                        name: "fk_direct_purchase_receipt_lines_products",
                        column: x => x.product_id,
                        principalSchema: "pos",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_direct_purchase_receipt_lines_receipts",
                        column: x => x.receipt_id,
                        principalSchema: "pos",
                        principalTable: "direct_purchase_receipts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movements_movement_type",
                schema: "pos",
                table: "stock_movements",
                sql: "movement_type IN ('OpeningStock', 'ManualIncrease', 'ManualDecrease', 'SaleDeduction', 'SaleVoidRestoration', 'PurchaseReceipt', 'StockCountVarianceIncrease', 'StockCountVarianceDecrease', 'SaleReturnRestock', 'TransferOut', 'TransferIn', 'TransferCancelRestore', 'DirectPurchaseReceipt')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movements_source_type",
                schema: "pos",
                table: "stock_movements",
                sql: "source_type IN ('None', 'Sale', 'Manual', 'Opening', 'PurchaseReceipt', 'StockCount', 'SaleReturn', 'InventoryTransfer', 'CustomerOrder', 'DirectPurchase')");

            migrationBuilder.CreateIndex(
                name: "IX_direct_purchase_receipt_lines_product_id",
                schema: "pos",
                table: "direct_purchase_receipt_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ux_direct_purchase_receipt_lines_inventory_movement_id",
                schema: "pos",
                table: "direct_purchase_receipt_lines",
                column: "inventory_movement_id",
                unique: true,
                filter: "inventory_movement_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_direct_purchase_receipt_lines_receipt_line_number",
                schema: "pos",
                table: "direct_purchase_receipt_lines",
                columns: new[] { "receipt_id", "line_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_direct_purchase_receipts_org_created_at",
                schema: "pos",
                table: "direct_purchase_receipts",
                columns: new[] { "organization_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_direct_purchase_receipts_org_purchase_date",
                schema: "pos",
                table: "direct_purchase_receipts",
                columns: new[] { "organization_id", "purchase_date" });

            migrationBuilder.CreateIndex(
                name: "ix_direct_purchase_receipts_org_reference",
                schema: "pos",
                table: "direct_purchase_receipts",
                columns: new[] { "organization_id", "reference_number" });

            migrationBuilder.CreateIndex(
                name: "ix_direct_purchase_receipts_org_supplier_id",
                schema: "pos",
                table: "direct_purchase_receipts",
                columns: new[] { "organization_id", "supplier_id" });

            migrationBuilder.CreateIndex(
                name: "IX_direct_purchase_receipts_supplier_id",
                schema: "pos",
                table: "direct_purchase_receipts",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ux_direct_purchase_receipts_org_idempotency_key",
                schema: "pos",
                table: "direct_purchase_receipts",
                columns: new[] { "organization_id", "idempotency_key" },
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_direct_purchase_receipts_org_receipt_number",
                schema: "pos",
                table: "direct_purchase_receipts",
                columns: new[] { "organization_id", "receipt_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "direct_purchase_receipt_lines",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "direct_purchase_receipt_number_sequences",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "direct_purchase_receipts",
                schema: "pos");

            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_movements_movement_type",
                schema: "pos",
                table: "stock_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_movements_source_type",
                schema: "pos",
                table: "stock_movements");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movements_movement_type",
                schema: "pos",
                table: "stock_movements",
                sql: "movement_type IN ('OpeningStock', 'ManualIncrease', 'ManualDecrease', 'SaleDeduction', 'SaleVoidRestoration', 'PurchaseReceipt', 'StockCountVarianceIncrease', 'StockCountVarianceDecrease', 'SaleReturnRestock', 'TransferOut', 'TransferIn', 'TransferCancelRestore')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movements_source_type",
                schema: "pos",
                table: "stock_movements",
                sql: "source_type IN ('None', 'Sale', 'Manual', 'Opening', 'PurchaseReceipt', 'StockCount', 'SaleReturn', 'InventoryTransfer', 'CustomerOrder')");
        }
    }
}
