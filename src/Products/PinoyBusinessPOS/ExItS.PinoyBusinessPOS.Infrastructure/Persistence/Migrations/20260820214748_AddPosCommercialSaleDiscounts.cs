using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPosCommercialSaleDiscounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "discount_total",
                schema: "pos",
                table: "sales",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "gross_subtotal",
                schema: "pos",
                table: "sales",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "line_discount_total",
                schema: "pos",
                table: "sales",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "sale_discount_total",
                schema: "pos",
                table: "sales",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "gross_line_total",
                schema: "pos",
                table: "sale_lines",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "line_discount_amount",
                schema: "pos",
                table: "sale_lines",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "sale_discount_allocated_amount",
                schema: "pos",
                table: "sale_lines",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // Backfill before the reconciliation constraints are added. Sales recorded before
            // commercial discounts existed carry no discount, so their gross amounts are exactly
            // their existing net amounts. Without this the new checks would reject every legacy row.
            migrationBuilder.Sql(
                "UPDATE pos.sales SET gross_subtotal = subtotal WHERE gross_subtotal = 0 AND subtotal <> 0;");
            migrationBuilder.Sql(
                "UPDATE pos.sale_lines SET gross_line_total = line_total WHERE gross_line_total = 0 AND line_total <> 0;");

            migrationBuilder.CreateTable(
                name: "sale_commercial_discount_adjustments",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sale_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    method = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    source = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    requested_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    calculated_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    sale_line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    applied_by = table.Column<Guid>(type: "uuid", nullable: false),
                    recorded_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sale_commercial_discount_adjustments", x => x.id);
                    table.CheckConstraint("ck_sale_commercial_discount_adjustments_amounts", "requested_value > 0 AND calculated_amount >= 0");
                    table.CheckConstraint("ck_sale_commercial_discount_adjustments_line_scope", "(scope = 'Line' AND sale_line_id IS NOT NULL) OR (scope = 'Sale' AND sale_line_id IS NULL)");
                    table.CheckConstraint("ck_sale_commercial_discount_adjustments_method", "method IN ('Percentage', 'FixedAmount')");
                    table.CheckConstraint("ck_sale_commercial_discount_adjustments_scope", "scope IN ('Line', 'Sale')");
                    table.CheckConstraint("ck_sale_commercial_discount_adjustments_source", "source = 'Manual'");
                    table.ForeignKey(
                        name: "fk_sale_commercial_discount_adjustments_sale_lines",
                        column: x => x.sale_line_id,
                        principalSchema: "pos",
                        principalTable: "sale_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_sale_commercial_discount_adjustments_sales",
                        column: x => x.sale_id,
                        principalSchema: "pos",
                        principalTable: "sales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_discount_reconciliation",
                schema: "pos",
                table: "sales",
                sql: "discount_total = line_discount_total + sale_discount_total AND gross_subtotal - discount_total = subtotal");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_discount_totals_non_negative",
                schema: "pos",
                table: "sales",
                sql: "gross_subtotal >= 0 AND line_discount_total >= 0 AND sale_discount_total >= 0 AND discount_total >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_lines_discount_amounts_non_negative",
                schema: "pos",
                table: "sale_lines",
                sql: "gross_line_total >= 0 AND line_discount_amount >= 0 AND sale_discount_allocated_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_lines_discount_reconciliation",
                schema: "pos",
                table: "sale_lines",
                sql: "gross_line_total - line_discount_amount - sale_discount_allocated_amount = line_total");

            migrationBuilder.CreateIndex(
                name: "ix_sale_commercial_discount_adjustments_org_sale",
                schema: "pos",
                table: "sale_commercial_discount_adjustments",
                columns: new[] { "organization_id", "sale_id" });

            migrationBuilder.CreateIndex(
                name: "IX_sale_commercial_discount_adjustments_sale_id",
                schema: "pos",
                table: "sale_commercial_discount_adjustments",
                column: "sale_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_commercial_discount_adjustments_sale_line",
                schema: "pos",
                table: "sale_commercial_discount_adjustments",
                column: "sale_line_id",
                filter: "sale_line_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sale_commercial_discount_adjustments",
                schema: "pos");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_discount_reconciliation",
                schema: "pos",
                table: "sales");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_discount_totals_non_negative",
                schema: "pos",
                table: "sales");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_lines_discount_amounts_non_negative",
                schema: "pos",
                table: "sale_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_lines_discount_reconciliation",
                schema: "pos",
                table: "sale_lines");

            migrationBuilder.DropColumn(
                name: "discount_total",
                schema: "pos",
                table: "sales");

            migrationBuilder.DropColumn(
                name: "gross_subtotal",
                schema: "pos",
                table: "sales");

            migrationBuilder.DropColumn(
                name: "line_discount_total",
                schema: "pos",
                table: "sales");

            migrationBuilder.DropColumn(
                name: "sale_discount_total",
                schema: "pos",
                table: "sales");

            migrationBuilder.DropColumn(
                name: "gross_line_total",
                schema: "pos",
                table: "sale_lines");

            migrationBuilder.DropColumn(
                name: "line_discount_amount",
                schema: "pos",
                table: "sale_lines");

            migrationBuilder.DropColumn(
                name: "sale_discount_allocated_amount",
                schema: "pos",
                table: "sale_lines");
        }
    }
}
