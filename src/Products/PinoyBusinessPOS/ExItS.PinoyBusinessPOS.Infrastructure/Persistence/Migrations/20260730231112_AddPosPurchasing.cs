using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPosPurchasing : Migration
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
                name: "grn_number_sequences",
                schema: "pos",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    last_value = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_grn_number_sequences", x => new { x.organization_id, x.business_date });
                    table.CheckConstraint("ck_grn_number_sequences_last_value_positive", "last_value > 0");
                });

            migrationBuilder.CreateTable(
                name: "purchase_order_number_sequences",
                schema: "pos",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    last_value = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_order_number_sequences", x => new { x.organization_id, x.business_date });
                    table.CheckConstraint("ck_po_number_sequences_last_value_positive", "last_value > 0");
                });

            migrationBuilder.CreateTable(
                name: "purchase_orders",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    po_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    order_date = table.Column<DateOnly>(type: "date", nullable: false),
                    expected_delivery_date = table.Column<DateOnly>(type: "date", nullable: true),
                    supplier_reference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    notes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ordered_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ordered_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_orders", x => x.id);
                    table.CheckConstraint("ck_purchase_orders_status", "status IN ('Draft', 'Ordered', 'PartiallyReceived', 'Received', 'Cancelled')");
                    table.ForeignKey(
                        name: "fk_purchase_orders_suppliers",
                        column: x => x.supplier_id,
                        principalSchema: "pos",
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "goods_receipts",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    grn_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    received_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    received_by = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_goods_receipts", x => x.id);
                    table.ForeignKey(
                        name: "fk_goods_receipts_purchase_orders",
                        column: x => x.purchase_order_id,
                        principalSchema: "pos",
                        principalTable: "purchase_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_order_lines",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    name_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    uom_snapshot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ordered_qty = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    unit_purchase_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    line_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    received_qty = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    line_notes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_order_lines", x => x.id);
                    table.CheckConstraint("ck_purchase_order_lines_ordered_qty_positive", "ordered_qty > 0");
                    table.CheckConstraint("ck_purchase_order_lines_received_qty_nonnegative", "received_qty >= 0");
                    table.CheckConstraint("ck_purchase_order_lines_unit_cost_nonnegative", "unit_purchase_cost >= 0");
                    table.ForeignKey(
                        name: "fk_purchase_order_lines_products",
                        column: x => x.product_id,
                        principalSchema: "pos",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_order_lines_purchase_orders",
                        column: x => x.purchase_order_id,
                        principalSchema: "pos",
                        principalTable: "purchase_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "goods_receipt_lines",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    goods_receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_order_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    name_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    uom_snapshot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    received_qty = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_goods_receipt_lines", x => x.id);
                    table.CheckConstraint("ck_goods_receipt_lines_received_qty_positive", "received_qty > 0");
                    table.ForeignKey(
                        name: "fk_goods_receipt_lines_goods_receipts",
                        column: x => x.goods_receipt_id,
                        principalSchema: "pos",
                        principalTable: "goods_receipts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_goods_receipt_lines_po_lines",
                        column: x => x.purchase_order_line_id,
                        principalSchema: "pos",
                        principalTable: "purchase_order_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_stock_movements_purchase_receipt_source",
                schema: "pos",
                table: "stock_movements",
                columns: new[] { "organization_id", "source_id", "product_id", "movement_type" },
                unique: true,
                filter: "source_type = 'PurchaseReceipt' AND source_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movements_movement_type",
                schema: "pos",
                table: "stock_movements",
                sql: "movement_type IN ('OpeningStock', 'ManualIncrease', 'ManualDecrease', 'SaleDeduction', 'SaleVoidRestoration', 'PurchaseReceipt')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movements_source_type",
                schema: "pos",
                table: "stock_movements",
                sql: "source_type IN ('None', 'Sale', 'Manual', 'Opening', 'PurchaseReceipt')");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_lines_purchase_order_line_id",
                schema: "pos",
                table: "goods_receipt_lines",
                column: "purchase_order_line_id");

            migrationBuilder.CreateIndex(
                name: "ux_goods_receipt_lines_grn_line_number",
                schema: "pos",
                table: "goods_receipt_lines",
                columns: new[] { "goods_receipt_id", "line_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_goods_receipts_org_po",
                schema: "pos",
                table: "goods_receipts",
                columns: new[] { "organization_id", "purchase_order_id" });

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipts_purchase_order_id",
                schema: "pos",
                table: "goods_receipts",
                column: "purchase_order_id");

            migrationBuilder.CreateIndex(
                name: "ux_goods_receipts_org_grn_number",
                schema: "pos",
                table: "goods_receipts",
                columns: new[] { "organization_id", "grn_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_lines_product_id",
                schema: "pos",
                table: "purchase_order_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ux_purchase_order_lines_po_line_number",
                schema: "pos",
                table: "purchase_order_lines",
                columns: new[] { "purchase_order_id", "line_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_purchase_order_lines_po_product",
                schema: "pos",
                table: "purchase_order_lines",
                columns: new[] { "purchase_order_id", "product_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_org_order_date",
                schema: "pos",
                table: "purchase_orders",
                columns: new[] { "organization_id", "order_date" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_org_status",
                schema: "pos",
                table: "purchase_orders",
                columns: new[] { "organization_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_org_supplier",
                schema: "pos",
                table: "purchase_orders",
                columns: new[] { "organization_id", "supplier_id" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_supplier_id",
                schema: "pos",
                table: "purchase_orders",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ux_purchase_orders_org_po_number",
                schema: "pos",
                table: "purchase_orders",
                columns: new[] { "organization_id", "po_number" },
                unique: true,
                filter: "po_number IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "goods_receipt_lines",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "grn_number_sequences",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "purchase_order_number_sequences",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "goods_receipts",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "purchase_order_lines",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "purchase_orders",
                schema: "pos");

            migrationBuilder.DropIndex(
                name: "ux_stock_movements_purchase_receipt_source",
                schema: "pos",
                table: "stock_movements");

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
                sql: "movement_type IN ('OpeningStock', 'ManualIncrease', 'ManualDecrease', 'SaleDeduction', 'SaleVoidRestoration')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movements_source_type",
                schema: "pos",
                table: "stock_movements",
                sql: "source_type IN ('None', 'Sale', 'Manual', 'Opening')");
        }
    }
}
