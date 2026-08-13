using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPosInventoryLots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_stock_movements_inventory_transfer_source",
                schema: "pos",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "ux_inventory_transfer_lines_transfer_product",
                schema: "pos",
                table: "inventory_transfer_lines");

            migrationBuilder.AddColumn<Guid>(
                name: "inventory_lot_id",
                schema: "pos",
                table: "stock_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "expiration_warning_days",
                schema: "pos",
                table: "products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "tracks_expiration",
                schema: "pos",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "expiration_date",
                schema: "pos",
                table: "inventory_transfer_lines",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "lot_number",
                schema: "pos",
                table: "inventory_transfer_lines",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_lot_id",
                schema: "pos",
                table: "inventory_transfer_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "inventory_lots",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    lot_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    normalized_lot_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: ""),
                    expiration_date = table.Column<DateOnly>(type: "date", nullable: false),
                    quantity_on_hand = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_lots", x => x.id);
                    table.CheckConstraint("ck_inventory_lots_on_hand_non_negative", "quantity_on_hand >= 0");
                    table.ForeignKey(
                        name: "fk_inventory_lots_products",
                        column: x => x.product_id,
                        principalSchema: "pos",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_lot_movements",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    movement_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    quantity_effect = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    source_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: true),
                    stock_movement_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recorded_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    recorded_by = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_lot_movements", x => x.id);
                    table.CheckConstraint("ck_inventory_lot_movements_quantity_effect_nonzero", "quantity_effect <> 0");
                    table.ForeignKey(
                        name: "fk_inventory_lot_movements_lots",
                        column: x => x.lot_id,
                        principalSchema: "pos",
                        principalTable: "inventory_lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_inventory_lot_id",
                schema: "pos",
                table: "stock_movements",
                column: "inventory_lot_id");

            migrationBuilder.CreateIndex(
                name: "ux_stock_movements_inventory_transfer_lot",
                schema: "pos",
                table: "stock_movements",
                columns: new[] { "organization_id", "source_id", "inventory_lot_id", "movement_type" },
                unique: true,
                filter: "source_type = 'InventoryTransfer' AND source_id IS NOT NULL AND inventory_lot_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_stock_movements_inventory_transfer_source",
                schema: "pos",
                table: "stock_movements",
                columns: new[] { "organization_id", "source_id", "product_id", "movement_type" },
                unique: true,
                filter: "source_type = 'InventoryTransfer' AND source_id IS NOT NULL AND inventory_lot_id IS NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_expiration_warning_days",
                schema: "pos",
                table: "products",
                sql: "expiration_warning_days IS NULL OR (expiration_warning_days >= 1 AND expiration_warning_days <= 365)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_expiration_warning_requires_tracking",
                schema: "pos",
                table: "products",
                sql: "tracks_expiration OR expiration_warning_days IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_transfer_lines_source_lot_id",
                schema: "pos",
                table: "inventory_transfer_lines",
                column: "source_lot_id");

            migrationBuilder.CreateIndex(
                name: "ux_inventory_transfer_lines_transfer_line_number",
                schema: "pos",
                table: "inventory_transfer_lines",
                columns: new[] { "transfer_id", "line_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_lot_movements_lot_id",
                schema: "pos",
                table: "inventory_lot_movements",
                column: "lot_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_lot_movements_lot_recorded",
                schema: "pos",
                table: "inventory_lot_movements",
                columns: new[] { "organization_id", "lot_id", "recorded_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_inventory_lot_movements_source_lot",
                schema: "pos",
                table: "inventory_lot_movements",
                columns: new[] { "organization_id", "source_id", "lot_id", "movement_type" },
                unique: true,
                filter: "source_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_lots_org_product_branch_expiry",
                schema: "pos",
                table: "inventory_lots",
                columns: new[] { "organization_id", "product_id", "branch_id", "expiration_date" },
                filter: "quantity_on_hand > 0");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_lots_org_product_expiry",
                schema: "pos",
                table: "inventory_lots",
                columns: new[] { "organization_id", "product_id", "expiration_date" },
                filter: "quantity_on_hand > 0");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_lots_product_id",
                schema: "pos",
                table: "inventory_lots",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ux_inventory_lots_identity_branch",
                schema: "pos",
                table: "inventory_lots",
                columns: new[] { "organization_id", "product_id", "branch_id", "expiration_date", "normalized_lot_number" },
                unique: true,
                filter: "branch_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_inventory_lots_identity_org",
                schema: "pos",
                table: "inventory_lots",
                columns: new[] { "organization_id", "product_id", "expiration_date", "normalized_lot_number" },
                unique: true,
                filter: "branch_id IS NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_transfer_lines_source_lots",
                schema: "pos",
                table: "inventory_transfer_lines",
                column: "source_lot_id",
                principalSchema: "pos",
                principalTable: "inventory_lots",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_movements_inventory_lots",
                schema: "pos",
                table: "stock_movements",
                column: "inventory_lot_id",
                principalSchema: "pos",
                principalTable: "inventory_lots",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_inventory_transfer_lines_source_lots",
                schema: "pos",
                table: "inventory_transfer_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_movements_inventory_lots",
                schema: "pos",
                table: "stock_movements");

            migrationBuilder.DropTable(
                name: "inventory_lot_movements",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "inventory_lots",
                schema: "pos");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_inventory_lot_id",
                schema: "pos",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "ux_stock_movements_inventory_transfer_lot",
                schema: "pos",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "ux_stock_movements_inventory_transfer_source",
                schema: "pos",
                table: "stock_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_products_expiration_warning_days",
                schema: "pos",
                table: "products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_products_expiration_warning_requires_tracking",
                schema: "pos",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_inventory_transfer_lines_source_lot_id",
                schema: "pos",
                table: "inventory_transfer_lines");

            migrationBuilder.DropIndex(
                name: "ux_inventory_transfer_lines_transfer_line_number",
                schema: "pos",
                table: "inventory_transfer_lines");

            migrationBuilder.DropColumn(
                name: "inventory_lot_id",
                schema: "pos",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "expiration_warning_days",
                schema: "pos",
                table: "products");

            migrationBuilder.DropColumn(
                name: "tracks_expiration",
                schema: "pos",
                table: "products");

            migrationBuilder.DropColumn(
                name: "expiration_date",
                schema: "pos",
                table: "inventory_transfer_lines");

            migrationBuilder.DropColumn(
                name: "lot_number",
                schema: "pos",
                table: "inventory_transfer_lines");

            migrationBuilder.DropColumn(
                name: "source_lot_id",
                schema: "pos",
                table: "inventory_transfer_lines");

            migrationBuilder.CreateIndex(
                name: "ux_stock_movements_inventory_transfer_source",
                schema: "pos",
                table: "stock_movements",
                columns: new[] { "organization_id", "source_id", "product_id", "movement_type" },
                unique: true,
                filter: "source_type = 'InventoryTransfer' AND source_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_inventory_transfer_lines_transfer_product",
                schema: "pos",
                table: "inventory_transfer_lines",
                columns: new[] { "transfer_id", "product_id" },
                unique: true);
        }
    }
}
