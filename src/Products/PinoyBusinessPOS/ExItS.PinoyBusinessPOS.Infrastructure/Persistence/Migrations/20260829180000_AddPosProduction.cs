using System;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(PosDbContext))]
    [Migration("20260829180000_AddPosProduction")]
    public partial class AddPosProduction : Migration
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
                name: "production_run_number_sequences",
                schema: "pos",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    last_value = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_production_run_number_sequences", x => new { x.organization_id, x.business_date });
                    table.CheckConstraint("ck_production_run_number_sequences_last_value_positive", "last_value > 0");
                });

            migrationBuilder.CreateTable(
                name: "production_definitions",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    output_product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    output_product_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    output_quantity_entered = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    output_multiplier_to_base = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    output_base_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    revision = table.Column<int>(type: "integer", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_definitions", x => x.id);
                    table.CheckConstraint("ck_production_definitions_status", "status IN ('Active', 'Inactive')");
                    table.CheckConstraint("ck_production_definitions_output_qty_positive", "output_quantity_entered > 0");
                    table.CheckConstraint("ck_production_definitions_output_multiplier_positive", "output_multiplier_to_base > 0");
                    table.CheckConstraint("ck_production_definitions_output_base_positive", "output_base_quantity > 0");
                    table.CheckConstraint("ck_production_definitions_revision_positive", "revision >= 1");
                    table.ForeignKey(
                        name: "fk_production_definitions_output_products",
                        column: x => x.output_product_id,
                        principalSchema: "pos",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "production_components",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    production_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    material_product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    quantity_entered = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    multiplier_to_base = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    base_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_components", x => x.id);
                    table.CheckConstraint("ck_production_components_quantity_positive", "quantity_entered > 0");
                    table.CheckConstraint("ck_production_components_multiplier_positive", "multiplier_to_base > 0");
                    table.CheckConstraint("ck_production_components_base_positive", "base_quantity > 0");
                    table.ForeignKey(
                        name: "fk_production_components_definitions",
                        column: x => x.production_definition_id,
                        principalSchema: "pos",
                        principalTable: "production_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_production_components_materials",
                        column: x => x.material_product_id,
                        principalSchema: "pos",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "production_runs",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    production_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    reference_number = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    production_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    production_definition_revision = table.Column<int>(type: "integer", nullable: false),
                    production_definition_name_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    output_product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    output_product_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    output_quantity_entered = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    output_multiplier_to_base = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    output_base_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    output_name_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    output_unit_label_snapshot = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    produced_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    output_expiration_date = table.Column<DateOnly>(type: "date", nullable: true),
                    output_lot_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    cost_status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    total_material_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    output_base_unit_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    notes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    voided_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    voided_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    output_inventory_movement_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_runs", x => x.id);
                    table.CheckConstraint("ck_production_runs_status", "status IN ('Posted', 'Voided')");
                    table.CheckConstraint("ck_production_runs_cost_status", "cost_status IN ('Complete', 'Partial', 'Unavailable')");
                    table.CheckConstraint("ck_production_runs_output_qty_positive", "output_quantity_entered > 0");
                    table.CheckConstraint("ck_production_runs_output_multiplier_positive", "output_multiplier_to_base > 0");
                    table.CheckConstraint("ck_production_runs_output_base_positive", "output_base_quantity > 0");
                    table.CheckConstraint("ck_production_runs_revision_positive", "production_definition_revision >= 1");
                    table.ForeignKey(
                        name: "fk_production_runs_definitions",
                        column: x => x.production_definition_id,
                        principalSchema: "pos",
                        principalTable: "production_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_production_runs_output_products",
                        column: x => x.output_product_id,
                        principalSchema: "pos",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "production_run_materials",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    production_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    material_product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    expected_quantity_entered = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    actual_quantity_entered = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    multiplier_to_base = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    expected_base_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    actual_base_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    name_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    unit_label_snapshot = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    unit_cost_snapshot = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    line_cost_snapshot = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    inventory_movement_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_run_materials", x => x.id);
                    table.CheckConstraint("ck_production_run_materials_expected_non_negative", "expected_quantity_entered >= 0");
                    table.CheckConstraint("ck_production_run_materials_actual_positive", "actual_quantity_entered > 0");
                    table.CheckConstraint("ck_production_run_materials_multiplier_positive", "multiplier_to_base > 0");
                    table.CheckConstraint("ck_production_run_materials_expected_base_non_negative", "expected_base_quantity >= 0");
                    table.CheckConstraint("ck_production_run_materials_actual_base_positive", "actual_base_quantity > 0");
                    table.ForeignKey(
                        name: "fk_production_run_materials_runs",
                        column: x => x.production_run_id,
                        principalSchema: "pos",
                        principalTable: "production_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_production_run_materials_products",
                        column: x => x.material_product_id,
                        principalSchema: "pos",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(name: "ix_production_definitions_org_name", schema: "pos", table: "production_definitions", columns: new[] { "organization_id", "name" });
            migrationBuilder.CreateIndex(name: "ix_production_definitions_org_output", schema: "pos", table: "production_definitions", columns: new[] { "organization_id", "output_product_id" });
            migrationBuilder.CreateIndex(name: "ix_production_definitions_org_status", schema: "pos", table: "production_definitions", columns: new[] { "organization_id", "status" });
            migrationBuilder.CreateIndex(name: "IX_production_definitions_output_product_id", schema: "pos", table: "production_definitions", column: "output_product_id");

            migrationBuilder.CreateIndex(name: "ux_production_components_definition_sort", schema: "pos", table: "production_components", columns: new[] { "production_definition_id", "sort_order" }, unique: true);
            migrationBuilder.CreateIndex(name: "ux_production_components_definition_material", schema: "pos", table: "production_components", columns: new[] { "production_definition_id", "material_product_id" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_production_components_material_product_id", schema: "pos", table: "production_components", column: "material_product_id");

            migrationBuilder.CreateIndex(name: "ux_production_runs_org_production_number", schema: "pos", table: "production_runs", columns: new[] { "organization_id", "production_number" }, unique: true);
            migrationBuilder.CreateIndex(name: "ux_production_runs_org_idempotency_key", schema: "pos", table: "production_runs", columns: new[] { "organization_id", "idempotency_key" }, unique: true, filter: "idempotency_key IS NOT NULL");
            migrationBuilder.CreateIndex(name: "ix_production_runs_org_produced_at", schema: "pos", table: "production_runs", columns: new[] { "organization_id", "produced_at_utc" });
            migrationBuilder.CreateIndex(name: "ix_production_runs_org_status", schema: "pos", table: "production_runs", columns: new[] { "organization_id", "status" });
            migrationBuilder.CreateIndex(name: "ix_production_runs_org_output", schema: "pos", table: "production_runs", columns: new[] { "organization_id", "output_product_id" });
            migrationBuilder.CreateIndex(name: "ix_production_runs_org_definition", schema: "pos", table: "production_runs", columns: new[] { "organization_id", "production_definition_id" });
            migrationBuilder.CreateIndex(name: "ux_production_runs_output_inventory_movement_id", schema: "pos", table: "production_runs", column: "output_inventory_movement_id", unique: true, filter: "output_inventory_movement_id IS NOT NULL");
            migrationBuilder.CreateIndex(name: "IX_production_runs_output_product_id", schema: "pos", table: "production_runs", column: "output_product_id");
            migrationBuilder.CreateIndex(name: "IX_production_runs_production_definition_id", schema: "pos", table: "production_runs", column: "production_definition_id");

            migrationBuilder.CreateIndex(name: "ux_production_run_materials_run_line", schema: "pos", table: "production_run_materials", columns: new[] { "production_run_id", "line_number" }, unique: true);
            migrationBuilder.CreateIndex(name: "ux_production_run_materials_inventory_movement_id", schema: "pos", table: "production_run_materials", column: "inventory_movement_id", unique: true, filter: "inventory_movement_id IS NOT NULL");
            migrationBuilder.CreateIndex(name: "IX_production_run_materials_material_product_id", schema: "pos", table: "production_run_materials", column: "material_product_id");

            migrationBuilder.CreateIndex(
                name: "ux_stock_movements_production_source",
                schema: "pos",
                table: "stock_movements",
                columns: new[] { "organization_id", "source_id", "product_id", "movement_type" },
                unique: true,
                filter: "source_type = 'Production' AND source_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movements_movement_type",
                schema: "pos",
                table: "stock_movements",
                sql: "movement_type IN ('OpeningStock', 'ManualIncrease', 'ManualDecrease', 'SaleDeduction', 'SaleVoidRestoration', 'PurchaseReceipt', 'StockCountVarianceIncrease', 'StockCountVarianceDecrease', 'SaleReturnRestock', 'TransferOut', 'TransferIn', 'TransferCancelRestore', 'DirectPurchaseReceipt', 'ExpirationInitialization', 'StockUse', 'StockUseVoidRestoration', 'ProductionMaterialConsumption', 'ProductionMaterialRestoration', 'ProductionOutput', 'ProductionOutputReversal')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movements_source_type",
                schema: "pos",
                table: "stock_movements",
                sql: "source_type IN ('None', 'Sale', 'Manual', 'Opening', 'PurchaseReceipt', 'StockCount', 'SaleReturn', 'InventoryTransfer', 'CustomerOrder', 'DirectPurchase', 'StockUse', 'Production')");
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
                name: "ux_stock_movements_production_source",
                schema: "pos",
                table: "stock_movements");

            migrationBuilder.DropTable(name: "production_run_materials", schema: "pos");
            migrationBuilder.DropTable(name: "production_run_number_sequences", schema: "pos");
            migrationBuilder.DropTable(name: "production_runs", schema: "pos");
            migrationBuilder.DropTable(name: "production_components", schema: "pos");
            migrationBuilder.DropTable(name: "production_definitions", schema: "pos");

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
    }
}
