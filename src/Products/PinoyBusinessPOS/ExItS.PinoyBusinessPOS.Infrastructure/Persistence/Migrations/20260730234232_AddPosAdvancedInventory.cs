using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPosAdvancedInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.AddColumn<decimal>(
                name: "reorder_quantity",
                schema: "pos",
                table: "inventory_accounts",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "inventory_reorder_changes",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inventory_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_reorder_level = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    new_reorder_level = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    previous_reorder_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    new_reorder_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    changed_by = table.Column<Guid>(type: "uuid", nullable: false),
                    changed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_reorder_changes", x => x.id);
                    table.ForeignKey(
                        name: "fk_inventory_reorder_changes_accounts",
                        column: x => x.inventory_account_id,
                        principalSchema: "pos",
                        principalTable: "inventory_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_count_number_sequences",
                schema: "pos",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    last_value = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_count_number_sequences", x => new { x.organization_id, x.business_date });
                    table.CheckConstraint("ck_stock_count_number_sequences_last_value_positive", "last_value > 0");
                });

            migrationBuilder.CreateTable(
                name: "stock_counts",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    count_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    notes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    started_by = table.Column<Guid>(type: "uuid", nullable: true),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    cancelled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_counts", x => x.id);
                    table.CheckConstraint("ck_stock_counts_status", "status IN ('Draft', 'InProgress', 'Completed', 'Cancelled')");
                });

            migrationBuilder.CreateTable(
                name: "stock_count_lines",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_count_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    system_on_hand_snapshot = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    counted_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_count_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_stock_count_lines_counts",
                        column: x => x.stock_count_id,
                        principalSchema: "pos",
                        principalTable: "stock_counts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_stock_count_lines_products",
                        column: x => x.product_id,
                        principalSchema: "pos",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_stock_movements_stock_count_source",
                schema: "pos",
                table: "stock_movements",
                columns: new[] { "organization_id", "source_id", "product_id", "movement_type" },
                unique: true,
                filter: "source_type = 'StockCount' AND source_id IS NOT NULL");

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
                sql: "movement_type IN ('OpeningStock', 'ManualIncrease', 'ManualDecrease', 'SaleDeduction', 'SaleVoidRestoration', 'PurchaseReceipt', 'StockCountVarianceIncrease', 'StockCountVarianceDecrease')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movements_source_type",
                schema: "pos",
                table: "stock_movements",
                sql: "source_type IN ('None', 'Sale', 'Manual', 'Opening', 'PurchaseReceipt', 'StockCount')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_accounts_reorder_quantity_positive",
                schema: "pos",
                table: "inventory_accounts",
                sql: "reorder_quantity IS NULL OR reorder_quantity > 0");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reorder_changes_inventory_account_id",
                schema: "pos",
                table: "inventory_reorder_changes",
                column: "inventory_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_reorder_changes_org_product_changed",
                schema: "pos",
                table: "inventory_reorder_changes",
                columns: new[] { "organization_id", "product_id", "changed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_count_lines_product_id",
                schema: "pos",
                table: "stock_count_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ux_stock_count_lines_count_product",
                schema: "pos",
                table: "stock_count_lines",
                columns: new[] { "stock_count_id", "product_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_counts_org_status_updated",
                schema: "pos",
                table: "stock_counts",
                columns: new[] { "organization_id", "status", "updated_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_stock_counts_org_count_number",
                schema: "pos",
                table: "stock_counts",
                columns: new[] { "organization_id", "count_number" },
                unique: true,
                filter: "count_number IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_reorder_changes",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "stock_count_lines",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "stock_count_number_sequences",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "stock_counts",
                schema: "pos");

            migrationBuilder.DropIndex(
                name: "ux_stock_movements_stock_count_source",
                schema: "pos",
                table: "stock_movements");

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

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_accounts_reorder_quantity_positive",
                schema: "pos",
                table: "inventory_accounts");

            migrationBuilder.DropColumn(
                name: "reorder_quantity",
                schema: "pos",
                table: "inventory_accounts");

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
        }
    }
}
