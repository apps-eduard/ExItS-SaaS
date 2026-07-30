using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPosBasicInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inventory_accounts",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_tracked = table.Column<bool>(type: "boolean", nullable: false),
                    reorder_level = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    on_hand_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false, defaultValue: 0m),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_accounts", x => x.id);
                    table.CheckConstraint("ck_inventory_accounts_on_hand_non_negative", "on_hand_quantity >= 0");
                    table.CheckConstraint("ck_inventory_accounts_reorder_level_non_negative", "reorder_level IS NULL OR reorder_level >= 0");
                    table.ForeignKey(
                        name: "fk_inventory_accounts_products",
                        column: x => x.product_id,
                        principalSchema: "pos",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_movements",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inventory_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    movement_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    quantity_effect = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    source_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recorded_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    recorded_by = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_movements", x => x.id);
                    table.CheckConstraint("ck_stock_movements_movement_type", "movement_type IN ('OpeningStock', 'ManualIncrease', 'ManualDecrease', 'SaleDeduction', 'SaleVoidRestoration')");
                    table.CheckConstraint("ck_stock_movements_quantity_effect_nonzero", "quantity_effect <> 0");
                    table.CheckConstraint("ck_stock_movements_source_type", "source_type IN ('None', 'Sale', 'Manual', 'Opening')");
                    table.ForeignKey(
                        name: "fk_stock_movements_inventory_accounts",
                        column: x => x.inventory_account_id,
                        principalSchema: "pos",
                        principalTable: "inventory_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_movements_products",
                        column: x => x.product_id,
                        principalSchema: "pos",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_accounts_org_tracked",
                schema: "pos",
                table: "inventory_accounts",
                columns: new[] { "organization_id", "is_tracked" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_accounts_product_id",
                schema: "pos",
                table: "inventory_accounts",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ux_inventory_accounts_org_product",
                schema: "pos",
                table: "inventory_accounts",
                columns: new[] { "organization_id", "product_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_inventory_account_id",
                schema: "pos",
                table: "stock_movements",
                column: "inventory_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_org_product_recorded",
                schema: "pos",
                table: "stock_movements",
                columns: new[] { "organization_id", "product_id", "recorded_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_product_id",
                schema: "pos",
                table: "stock_movements",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ux_stock_movements_opening_stock",
                schema: "pos",
                table: "stock_movements",
                columns: new[] { "organization_id", "product_id", "movement_type" },
                unique: true,
                filter: "movement_type = 'OpeningStock'");

            migrationBuilder.CreateIndex(
                name: "ux_stock_movements_sale_source",
                schema: "pos",
                table: "stock_movements",
                columns: new[] { "organization_id", "source_id", "product_id", "movement_type" },
                unique: true,
                filter: "source_type = 'Sale' AND source_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stock_movements",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "inventory_accounts",
                schema: "pos");
        }
    }
}
