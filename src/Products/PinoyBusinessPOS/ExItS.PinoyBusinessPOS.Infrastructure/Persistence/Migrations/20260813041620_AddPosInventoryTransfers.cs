using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPosInventoryTransfers : Migration
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

            migrationBuilder.AddColumn<Guid>(
                name: "branch_id",
                schema: "pos",
                table: "stock_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "inventory_branch_balances",
                schema: "pos",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    on_hand_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_branch_balances", x => new { x.organization_id, x.branch_id, x.product_id });
                    table.CheckConstraint("ck_inventory_branch_balances_on_hand_non_negative", "on_hand_quantity >= 0");
                    table.ForeignKey(
                        name: "fk_inventory_branch_balances_products",
                        column: x => x.product_id,
                        principalSchema: "pos",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_transfer_number_sequences",
                schema: "pos",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    last_value = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_transfer_number_sequences", x => new { x.organization_id, x.business_date });
                    table.CheckConstraint("ck_inventory_transfer_number_sequences_last_value_positive", "last_value > 0");
                });

            migrationBuilder.CreateTable(
                name: "inventory_transfers",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transfer_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    source_branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    destination_branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    notes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    dispatched_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    dispatched_by = table.Column<Guid>(type: "uuid", nullable: true),
                    received_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    received_by = table.Column<Guid>(type: "uuid", nullable: true),
                    cancelled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_transfers", x => x.id);
                    table.CheckConstraint("ck_inventory_transfers_distinct_branches", "source_branch_id <> destination_branch_id");
                    table.CheckConstraint("ck_inventory_transfers_status", "status IN ('Draft', 'InTransit', 'PartiallyReceived', 'Received', 'Cancelled')");
                });

            migrationBuilder.CreateTable(
                name: "inventory_transfer_lines",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    transfer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    name_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    unit_of_measure = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    sent_qty = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    received_qty = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    discrepancy_reason = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    discrepancy_note = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_transfer_lines", x => x.id);
                    table.CheckConstraint("ck_inventory_transfer_lines_received_range", "received_qty >= 0 AND received_qty <= sent_qty");
                    table.CheckConstraint("ck_inventory_transfer_lines_sent_positive", "sent_qty > 0");
                    table.ForeignKey(
                        name: "fk_inventory_transfer_lines_products",
                        column: x => x.product_id,
                        principalSchema: "pos",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_transfer_lines_transfers",
                        column: x => x.transfer_id,
                        principalSchema: "pos",
                        principalTable: "inventory_transfers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_stock_movements_inventory_transfer_source",
                schema: "pos",
                table: "stock_movements",
                columns: new[] { "organization_id", "source_id", "product_id", "movement_type" },
                unique: true,
                filter: "source_type = 'InventoryTransfer' AND source_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movements_movement_type",
                schema: "pos",
                table: "stock_movements",
                sql: "movement_type IN ('OpeningStock', 'ManualIncrease', 'ManualDecrease', 'SaleDeduction', 'SaleVoidRestoration', 'PurchaseReceipt', 'StockCountVarianceIncrease', 'StockCountVarianceDecrease', 'SaleReturnRestock', 'TransferOut', 'TransferIn', 'TransferCancelRestore')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movements_source_type",
                schema: "pos",
                table: "stock_movements",
                sql: "source_type IN ('None', 'Sale', 'Manual', 'Opening', 'PurchaseReceipt', 'StockCount', 'SaleReturn', 'InventoryTransfer')");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_branch_balances_product_id",
                schema: "pos",
                table: "inventory_branch_balances",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_transfer_lines_product",
                schema: "pos",
                table: "inventory_transfer_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ux_inventory_transfer_lines_transfer_product",
                schema: "pos",
                table: "inventory_transfer_lines",
                columns: new[] { "transfer_id", "product_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_inventory_transfers_org_destination",
                schema: "pos",
                table: "inventory_transfers",
                columns: new[] { "organization_id", "destination_branch_id" });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_transfers_org_source",
                schema: "pos",
                table: "inventory_transfers",
                columns: new[] { "organization_id", "source_branch_id" });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_transfers_org_status",
                schema: "pos",
                table: "inventory_transfers",
                columns: new[] { "organization_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_inventory_transfers_org_transfer_number",
                schema: "pos",
                table: "inventory_transfers",
                columns: new[] { "organization_id", "transfer_number" },
                unique: true,
                filter: "transfer_number IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_branch_balances",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "inventory_transfer_lines",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "inventory_transfer_number_sequences",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "inventory_transfers",
                schema: "pos");

            migrationBuilder.DropIndex(
                name: "ux_stock_movements_inventory_transfer_source",
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

            migrationBuilder.DropColumn(
                name: "branch_id",
                schema: "pos",
                table: "stock_movements");

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
        }
    }
}
