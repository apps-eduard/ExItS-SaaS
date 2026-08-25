using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPosSalePriceOverrides : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sale_price_override_adjustments",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sale_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sale_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    baseline_unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    applied_unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    applied_by = table.Column<Guid>(type: "uuid", nullable: false),
                    recorded_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sale_price_override_adjustments", x => x.id);
                    table.CheckConstraint("ck_sale_price_override_adjustments_prices", "baseline_unit_price >= 0 AND applied_unit_price > 0");
                    table.ForeignKey(
                        name: "fk_sale_price_override_adjustments_sale_lines",
                        column: x => x.sale_line_id,
                        principalSchema: "pos",
                        principalTable: "sale_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_sale_price_override_adjustments_sales",
                        column: x => x.sale_id,
                        principalSchema: "pos",
                        principalTable: "sales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sale_price_override_adjustments_org_sale",
                schema: "pos",
                table: "sale_price_override_adjustments",
                columns: new[] { "organization_id", "sale_id" });

            migrationBuilder.CreateIndex(
                name: "IX_sale_price_override_adjustments_sale_id",
                schema: "pos",
                table: "sale_price_override_adjustments",
                column: "sale_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_price_override_adjustments_sale_line",
                schema: "pos",
                table: "sale_price_override_adjustments",
                column: "sale_line_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sale_price_override_adjustments",
                schema: "pos");
        }
    }
}
