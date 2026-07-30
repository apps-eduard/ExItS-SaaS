using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPosSimpleSales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sale_number_sequences",
                schema: "pos",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    last_value = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sale_number_sequences", x => new { x.organization_id, x.business_date });
                    table.CheckConstraint("ck_sale_number_sequences_last_value_positive", "last_value > 0");
                });

            migrationBuilder.CreateTable(
                name: "sales",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sale_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    payment_method = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    amount_tendered = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    change_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    gcash_reference = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    recorded_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    recorded_by = table.Column<Guid>(type: "uuid", nullable: false),
                    voided_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    voided_by = table.Column<Guid>(type: "uuid", nullable: true),
                    void_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales", x => x.id);
                    table.CheckConstraint("ck_sales_payment_method", "payment_method IN ('Cash', 'ManualGCash')");
                    table.CheckConstraint("ck_sales_status", "status IN ('Completed', 'Voided')");
                    table.CheckConstraint("ck_sales_tender_consistency", "(payment_method = 'Cash' AND amount_tendered IS NOT NULL AND change_amount IS NOT NULL AND amount_tendered >= total AND gcash_reference IS NULL) OR (payment_method = 'ManualGCash' AND amount_tendered IS NULL AND change_amount IS NULL)");
                    table.CheckConstraint("ck_sales_totals_non_negative", "subtotal >= 0 AND total >= 0");
                    table.CheckConstraint("ck_sales_void_consistency", "(status = 'Completed' AND voided_at_utc IS NULL AND voided_by IS NULL AND void_reason IS NULL) OR (status = 'Voided' AND voided_at_utc IS NOT NULL AND voided_by IS NOT NULL AND void_reason IS NOT NULL)");
                });

            migrationBuilder.CreateTable(
                name: "sale_lines",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sale_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    name_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    sku_snapshot = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    barcode_snapshot = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    unit_of_measure_snapshot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    line_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sale_lines", x => x.id);
                    table.CheckConstraint("ck_sale_lines_amounts_non_negative", "unit_price >= 0 AND line_total >= 0");
                    table.CheckConstraint("ck_sale_lines_line_number_positive", "line_number > 0");
                    table.CheckConstraint("ck_sale_lines_quantity_positive", "quantity > 0");
                    table.CheckConstraint("ck_sale_lines_unit_of_measure", "unit_of_measure_snapshot IN ('Piece', 'Pack', 'Box', 'Bottle', 'Can', 'Sachet', 'Kilogram', 'Gram', 'Liter', 'Milliliter', 'Meter')");
                    table.ForeignKey(
                        name: "fk_sale_lines_products",
                        column: x => x.product_id,
                        principalSchema: "pos",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_lines_sales",
                        column: x => x.sale_id,
                        principalSchema: "pos",
                        principalTable: "sales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sale_lines_product_id",
                schema: "pos",
                table: "sale_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ux_sale_lines_sale_line_number",
                schema: "pos",
                table: "sale_lines",
                columns: new[] { "sale_id", "line_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sales_org_payment_method",
                schema: "pos",
                table: "sales",
                columns: new[] { "organization_id", "payment_method" });

            migrationBuilder.CreateIndex(
                name: "ix_sales_org_recorded_at",
                schema: "pos",
                table: "sales",
                columns: new[] { "organization_id", "recorded_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_sales_org_status",
                schema: "pos",
                table: "sales",
                columns: new[] { "organization_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_sales_org_sale_number",
                schema: "pos",
                table: "sales",
                columns: new[] { "organization_id", "sale_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sale_lines",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "sale_number_sequences",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "sales",
                schema: "pos");
        }
    }
}
