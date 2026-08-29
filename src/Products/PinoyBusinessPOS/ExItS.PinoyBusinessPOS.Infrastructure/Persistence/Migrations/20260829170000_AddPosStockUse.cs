using System;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(PosDbContext))]
    [Migration("20260829170000_AddPosStockUse")]
    public partial class AddPosStockUse : Migration
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
                name: "stock_use_number_sequences",
                schema: "pos",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    last_value = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_use_number_sequences", x => new { x.organization_id, x.business_date });
                    table.CheckConstraint("ck_stock_use_number_sequences_last_value_positive", "last_value > 0");
                });

            migrationBuilder.CreateTable(
                name: "stock_uses",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    stock_use_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    reference_number = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    notes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    voided_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    voided_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_uses", x => x.id);
                    table.CheckConstraint(
                        "ck_stock_uses_reason",
                        "reason IN ('InternalOperations', 'StaffUse', 'SampleOrTesting', 'Other')");
                    table.CheckConstraint(
                        "ck_stock_uses_status",
                        "status IN ('Posted', 'Voided')");
                });

            migrationBuilder.CreateTable(
                name: "stock_use_lines",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_use_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    quantity_entered = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    multiplier_to_base = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    base_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    name_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    unit_label_snapshot = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    unit_cost_snapshot = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    line_cost_snapshot = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    inventory_movement_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_use_lines", x => x.id);
                    table.CheckConstraint("ck_stock_use_lines_quantity_entered_positive", "quantity_entered > 0");
                    table.CheckConstraint("ck_stock_use_lines_multiplier_positive", "multiplier_to_base > 0");
                    table.CheckConstraint("ck_stock_use_lines_base_quantity_positive", "base_quantity > 0");
                    table.ForeignKey(
                        name: "fk_stock_use_lines_products",
                        column: x => x.product_id,
                        principalSchema: "pos",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_use_lines_stock_uses",
                        column: x => x.stock_use_id,
                        principalSchema: "pos",
                        principalTable: "stock_uses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_stock_uses_org_branch_id",
                schema: "pos",
                table: "stock_uses",
                columns: new[] { "organization_id", "branch_id" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_uses_org_occurred_at",
                schema: "pos",
                table: "stock_uses",
                columns: new[] { "organization_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_uses_org_status",
                schema: "pos",
                table: "stock_uses",
                columns: new[] { "organization_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_stock_uses_org_idempotency_key",
                schema: "pos",
                table: "stock_uses",
                columns: new[] { "organization_id", "idempotency_key" },
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_stock_uses_org_stock_use_number",
                schema: "pos",
                table: "stock_uses",
                columns: new[] { "organization_id", "stock_use_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_stock_use_lines_inventory_movement_id",
                schema: "pos",
                table: "stock_use_lines",
                column: "inventory_movement_id",
                unique: true,
                filter: "inventory_movement_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_stock_use_lines_stock_use_line_number",
                schema: "pos",
                table: "stock_use_lines",
                columns: new[] { "stock_use_id", "line_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_use_lines_product_id",
                schema: "pos",
                table: "stock_use_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ux_stock_movements_stock_use_source",
                schema: "pos",
                table: "stock_movements",
                columns: new[] { "organization_id", "source_id", "product_id", "movement_type" },
                unique: true,
                filter: "source_type = 'StockUse' AND source_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movements_movement_type",
                schema: "pos",
                table: "stock_movements",
                sql: "movement_type IN ('OpeningStock', 'ManualIncrease', 'ManualDecrease', 'SaleDeduction', 'SaleVoidRestoration', 'PurchaseReceipt', 'StockCountVarianceIncrease', 'StockCountVarianceDecrease', 'SaleReturnRestock', 'TransferOut', 'TransferIn', 'TransferCancelRestore', 'DirectPurchaseReceipt', 'ExpirationInitialization', 'StockUse', 'StockUseVoidRestoration')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movements_source_type",
                schema: "pos",
                table: "stock_movements",
                sql: "source_type IN ('None', 'Sale', 'Manual', 'Opening', 'PurchaseReceipt', 'StockCount', 'SaleReturn', 'InventoryTransfer', 'CustomerOrder', 'DirectPurchase', 'StockUse')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_movements_movement_type",
                schema: "pos",
                table: "stock_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_movements_source_type",
                schema: "pos",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "ux_stock_movements_stock_use_source",
                schema: "pos",
                table: "stock_movements");

            migrationBuilder.DropTable(
                name: "stock_use_lines",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "stock_use_number_sequences",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "stock_uses",
                schema: "pos");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movements_movement_type",
                schema: "pos",
                table: "stock_movements",
                sql: "movement_type IN ('OpeningStock', 'ManualIncrease', 'ManualDecrease', 'SaleDeduction', 'SaleVoidRestoration', 'PurchaseReceipt', 'StockCountVarianceIncrease', 'StockCountVarianceDecrease', 'SaleReturnRestock', 'TransferOut', 'TransferIn', 'TransferCancelRestore', 'DirectPurchaseReceipt', 'ExpirationInitialization')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movements_source_type",
                schema: "pos",
                table: "stock_movements",
                sql: "source_type IN ('None', 'Sale', 'Manual', 'Opening', 'PurchaseReceipt', 'StockCount', 'SaleReturn', 'InventoryTransfer', 'CustomerOrder', 'DirectPurchase')");
        }
    }
}
