using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPosOperationalSetup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_totals_non_negative",
                schema: "pos",
                table: "sales");

            migrationBuilder.AddColumn<decimal>(
                name: "tax_amount",
                schema: "pos",
                table: "sales",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "operational_setups",
                schema: "pos",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_display_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    tax_pricing_mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    tax_rate_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    receipt_header = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    receipt_footer = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    business_address = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    contact_phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    default_register_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_completed = table.Column<bool>(type: "boolean", nullable: false),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_operational_setups", x => x.organization_id);
                    table.CheckConstraint("ck_operational_setups_completed_consistency", "(is_completed = FALSE AND completed_at_utc IS NULL) OR (is_completed = TRUE AND completed_at_utc IS NOT NULL)");
                    table.CheckConstraint("ck_operational_setups_tax_pricing_mode", "tax_pricing_mode IN ('TaxExclusive', 'TaxInclusive')");
                    table.CheckConstraint("ck_operational_setups_tax_rate_range", "tax_rate_percent >= 0 AND tax_rate_percent <= 100");
                    table.ForeignKey(
                        name: "fk_operational_setups_default_register",
                        column: x => x.default_register_id,
                        principalSchema: "pos",
                        principalTable: "registers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_totals_non_negative",
                schema: "pos",
                table: "sales",
                sql: "subtotal >= 0 AND total >= 0 AND tax_amount >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_operational_setups_default_register_id",
                schema: "pos",
                table: "operational_setups",
                column: "default_register_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "operational_setups",
                schema: "pos");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_totals_non_negative",
                schema: "pos",
                table: "sales");

            migrationBuilder.DropColumn(
                name: "tax_amount",
                schema: "pos",
                table: "sales");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_totals_non_negative",
                schema: "pos",
                table: "sales",
                sql: "subtotal >= 0 AND total >= 0");
        }
    }
}
