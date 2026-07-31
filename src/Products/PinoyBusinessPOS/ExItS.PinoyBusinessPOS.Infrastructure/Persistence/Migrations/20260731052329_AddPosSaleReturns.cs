using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPosSaleReturns : Migration
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
                name: "sale_return_number_sequences",
                schema: "pos",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    last_value = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sale_return_number_sequences", x => new { x.organization_id, x.business_date });
                    table.CheckConstraint("ck_sale_return_number_sequences_last_value_positive", "last_value > 0");
                });

            migrationBuilder.CreateTable(
                name: "sale_returns",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    return_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    sale_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cashier_shift_id = table.Column<Guid>(type: "uuid", nullable: true),
                    refund_method = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    return_date = table.Column<DateOnly>(type: "date", nullable: false),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    notes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    total_refund_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sale_returns", x => x.id);
                    table.CheckConstraint("ck_sale_returns_refund_method", "refund_method IN ('Cash', 'ManualGCash', 'Utang')");
                    table.CheckConstraint("ck_sale_returns_status", "status IN ('Completed')");
                    table.CheckConstraint("ck_sale_returns_total_refund_positive", "total_refund_amount > 0");
                    table.ForeignKey(
                        name: "fk_sale_returns_cashier_shifts",
                        column: x => x.cashier_shift_id,
                        principalSchema: "pos",
                        principalTable: "cashier_shifts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_returns_sales",
                        column: x => x.sale_id,
                        principalSchema: "pos",
                        principalTable: "sales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sale_return_lines",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sale_return_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sale_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_name_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    uom_snapshot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    quantity_returned = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    unit_price_snapshot = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    refund_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    restock_disposition = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    line_reason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    inventory_movement_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sale_return_lines", x => x.id);
                    table.CheckConstraint("ck_sale_return_lines_quantity_positive", "quantity_returned > 0");
                    table.CheckConstraint("ck_sale_return_lines_refund_positive", "refund_amount > 0");
                    table.CheckConstraint("ck_sale_return_lines_restock_disposition", "restock_disposition IN ('ReturnToStock', 'DoNotRestock')");
                    table.CheckConstraint("ck_sale_return_lines_uom", "uom_snapshot IN ('Piece', 'Pack', 'Box', 'Bottle', 'Can', 'Sachet', 'Kilogram', 'Gram', 'Liter', 'Milliliter', 'Meter')");
                    table.ForeignKey(
                        name: "fk_sale_return_lines_products",
                        column: x => x.product_id,
                        principalSchema: "pos",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_return_lines_returns",
                        column: x => x.sale_return_id,
                        principalSchema: "pos",
                        principalTable: "sale_returns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_return_lines_sale_lines",
                        column: x => x.sale_line_id,
                        principalSchema: "pos",
                        principalTable: "sale_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_stock_movements_sale_return_source",
                schema: "pos",
                table: "stock_movements",
                columns: new[] { "organization_id", "source_id", "product_id", "movement_type" },
                unique: true,
                filter: "source_type = 'SaleReturn' AND source_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movements_movement_type",
                schema: "pos",
                table: "stock_movements",
                sql: "movement_type IN ('OpeningStock', 'ManualIncrease', 'ManualDecrease', 'SaleDeduction', 'SaleVoidRestoration', 'PurchaseReceipt', 'StockCountVarianceIncrease', 'StockCountVarianceDecrease', 'SaleReturnRestock')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movements_source_type",
                schema: "pos",
                table: "stock_movements",
                sql: "source_type IN ('None', 'Sale', 'Manual', 'Opening', 'PurchaseReceipt', 'StockCount', 'SaleReturn')");

            migrationBuilder.CreateIndex(
                name: "ix_sale_return_lines_org_sale_line",
                schema: "pos",
                table: "sale_return_lines",
                columns: new[] { "organization_id", "sale_line_id" });

            migrationBuilder.CreateIndex(
                name: "IX_sale_return_lines_product_id",
                schema: "pos",
                table: "sale_return_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_sale_return_lines_sale_line_id",
                schema: "pos",
                table: "sale_return_lines",
                column: "sale_line_id");

            migrationBuilder.CreateIndex(
                name: "ux_sale_return_lines_return_sale_line",
                schema: "pos",
                table: "sale_return_lines",
                columns: new[] { "sale_return_id", "sale_line_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sale_returns_cashier_shift_id",
                schema: "pos",
                table: "sale_returns",
                column: "cashier_shift_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_returns_org_sale_created",
                schema: "pos",
                table: "sale_returns",
                columns: new[] { "organization_id", "sale_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_returns_org_shift",
                schema: "pos",
                table: "sale_returns",
                columns: new[] { "organization_id", "cashier_shift_id" },
                filter: "cashier_shift_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_sale_returns_sale_id",
                schema: "pos",
                table: "sale_returns",
                column: "sale_id");

            migrationBuilder.CreateIndex(
                name: "ux_sale_returns_org_return_number",
                schema: "pos",
                table: "sale_returns",
                columns: new[] { "organization_id", "return_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM pos.stock_movements
                WHERE source_type = 'SaleReturn'
                   OR movement_type = 'SaleReturnRestock';
                """);

            migrationBuilder.DropTable(
                name: "sale_return_lines",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "sale_return_number_sequences",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "sale_returns",
                schema: "pos");

            migrationBuilder.DropIndex(
                name: "ux_stock_movements_sale_return_source",
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
                sql: "movement_type IN ('OpeningStock', 'ManualIncrease', 'ManualDecrease', 'SaleDeduction', 'SaleVoidRestoration', 'PurchaseReceipt', 'StockCountVarianceIncrease', 'StockCountVarianceDecrease')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movements_source_type",
                schema: "pos",
                table: "stock_movements",
                sql: "source_type IN ('None', 'Sale', 'Manual', 'Opening', 'PurchaseReceipt', 'StockCount')");
        }
    }
}
