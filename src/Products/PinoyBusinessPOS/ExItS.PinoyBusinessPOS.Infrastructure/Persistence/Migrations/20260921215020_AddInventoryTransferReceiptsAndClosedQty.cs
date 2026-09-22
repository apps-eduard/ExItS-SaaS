using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Multi-receipt transfer lifecycle: closed_qty, receipt tables, ClosedWithDiscrepancy status.
    /// Legacy received transfers keep cumulative received_qty; optional receipt backfill when actor/timestamp exist.
    /// </summary>
    public partial class AddInventoryTransferReceiptsAndClosedQty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_transfers_status",
                schema: "pos",
                table: "inventory_transfers");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_transfer_lines_received_range",
                schema: "pos",
                table: "inventory_transfer_lines");

            migrationBuilder.AddColumn<decimal>(
                name: "closed_qty",
                schema: "pos",
                table: "inventory_transfer_lines",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "inventory_transfer_receipts",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transfer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    received_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    received_by = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_transfer_receipts", x => x.id);
                    table.CheckConstraint("ck_inventory_transfer_receipts_sequence_positive", "sequence >= 1");
                    table.ForeignKey(
                        name: "fk_inventory_transfer_receipts_transfers",
                        column: x => x.transfer_id,
                        principalSchema: "pos",
                        principalTable: "inventory_transfers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inventory_transfer_receipt_lines",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transfer_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity_received = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_transfer_receipt_lines", x => x.id);
                    table.CheckConstraint("ck_inventory_transfer_receipt_lines_qty_positive", "quantity_received > 0");
                    table.ForeignKey(
                        name: "fk_inventory_transfer_receipt_lines_receipts",
                        column: x => x.receipt_id,
                        principalSchema: "pos",
                        principalTable: "inventory_transfer_receipts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_inventory_transfer_receipt_lines_transfer_lines",
                        column: x => x.transfer_line_id,
                        principalSchema: "pos",
                        principalTable: "inventory_transfer_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_transfers_status",
                schema: "pos",
                table: "inventory_transfers",
                sql: "status IN ('Draft', 'InTransit', 'PartiallyReceived', 'Received', 'Cancelled', 'ClosedWithDiscrepancy')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_transfer_lines_received_range",
                schema: "pos",
                table: "inventory_transfer_lines",
                sql: "received_qty >= 0 AND closed_qty >= 0 AND received_qty + closed_qty <= sent_qty");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_transfer_receipt_lines_transfer_line_id",
                schema: "pos",
                table: "inventory_transfer_receipt_lines",
                column: "transfer_line_id");

            migrationBuilder.CreateIndex(
                name: "ux_inventory_transfer_receipt_lines_receipt_line",
                schema: "pos",
                table: "inventory_transfer_receipt_lines",
                columns: new[] { "receipt_id", "transfer_line_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_inventory_transfer_receipts_org_transfer",
                schema: "pos",
                table: "inventory_transfer_receipts",
                columns: new[] { "organization_id", "transfer_id" });

            migrationBuilder.CreateIndex(
                name: "ux_inventory_transfer_receipts_transfer_sequence",
                schema: "pos",
                table: "inventory_transfer_receipts",
                columns: new[] { "transfer_id", "sequence" },
                unique: true);

            // Backfill one synthetic receipt per historical received transfer when actor/timestamp are present.
            migrationBuilder.Sql(
                """
                INSERT INTO pos.inventory_transfer_receipts (id, organization_id, transfer_id, sequence, received_at_utc, received_by)
                SELECT gen_random_uuid(), t.organization_id, t.id, 1, t.received_at_utc, t.received_by
                FROM pos.inventory_transfers t
                WHERE t.status IN ('PartiallyReceived', 'Received')
                  AND t.received_at_utc IS NOT NULL
                  AND t.received_by IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1 FROM pos.inventory_transfer_receipts r WHERE r.transfer_id = t.id);

                INSERT INTO pos.inventory_transfer_receipt_lines (id, receipt_id, transfer_line_id, product_id, quantity_received)
                SELECT gen_random_uuid(), r.id, l.id, l.product_id, l.received_qty
                FROM pos.inventory_transfer_lines l
                INNER JOIN pos.inventory_transfer_receipts r ON r.transfer_id = l.transfer_id AND r.sequence = 1
                WHERE l.received_qty > 0
                  AND NOT EXISTS (
                      SELECT 1 FROM pos.inventory_transfer_receipt_lines rl WHERE rl.receipt_id = r.id);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_transfer_receipt_lines",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "inventory_transfer_receipts",
                schema: "pos");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_transfers_status",
                schema: "pos",
                table: "inventory_transfers");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_transfer_lines_received_range",
                schema: "pos",
                table: "inventory_transfer_lines");

            migrationBuilder.DropColumn(
                name: "closed_qty",
                schema: "pos",
                table: "inventory_transfer_lines");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_transfers_status",
                schema: "pos",
                table: "inventory_transfers",
                sql: "status IN ('Draft', 'InTransit', 'PartiallyReceived', 'Received', 'Cancelled')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_transfer_lines_received_range",
                schema: "pos",
                table: "inventory_transfer_lines",
                sql: "received_qty >= 0 AND received_qty <= sent_qty");
        }
    }
}
