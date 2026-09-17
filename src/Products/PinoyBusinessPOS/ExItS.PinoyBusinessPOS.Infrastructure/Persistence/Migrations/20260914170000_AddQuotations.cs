using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Seller quotations MVP: quotations, lines, number sequences, and optional sale.source_quotation_id.
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260914170000_AddQuotations")]
public partial class AddQuotations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "source_quotation_id",
            schema: "pos",
            table: "sales",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "quotation_number_sequences",
            schema: "pos",
            columns: table => new
            {
                organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                business_date = table.Column<DateOnly>(type: "date", nullable: false),
                last_value = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_quotation_number_sequences", x => new { x.organization_id, x.business_date });
                table.CheckConstraint("ck_quotation_number_sequences_last_value_positive", "last_value > 0");
            });

        migrationBuilder.CreateTable(
            name: "quotations",
            schema: "pos",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                quotation_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                customer_display_name_snapshot = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                customer_mobile_number_snapshot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                customer_address_snapshot = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                customer_notes_snapshot = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                prepared_by = table.Column<Guid>(type: "uuid", nullable: false),
                valid_until = table.Column<DateOnly>(type: "date", nullable: true),
                reference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                notes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                terms = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                converted_sale_id = table.Column<Guid>(type: "uuid", nullable: true),
                is_customer_visible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                issued_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_quotations", x => x.id);
                table.CheckConstraint(
                    "ck_quotations_status",
                    "status IN ('Draft', 'Sent', 'Accepted', 'Declined', 'Expired', 'Converted', 'Cancelled')");
                table.ForeignKey(
                    name: "fk_quotations_customers",
                    column: x => x.customer_id,
                    principalSchema: "pos",
                    principalTable: "customers",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "quotation_lines",
            schema: "pos",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                quotation_id = table.Column<Guid>(type: "uuid", nullable: false),
                organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                product_id = table.Column<Guid>(type: "uuid", nullable: false),
                line_number = table.Column<int>(type: "integer", nullable: false),
                name_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                sku_snapshot = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                uom_snapshot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                discount_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                line_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_quotation_lines", x => x.id);
                table.CheckConstraint("ck_quotation_lines_quantity_positive", "quantity > 0");
                table.CheckConstraint("ck_quotation_lines_unit_price_nonnegative", "unit_price >= 0");
                table.CheckConstraint(
                    "ck_quotation_lines_discount_nonnegative",
                    "discount_amount IS NULL OR discount_amount >= 0");
                table.ForeignKey(
                    name: "fk_quotation_lines_quotations",
                    column: x => x.quotation_id,
                    principalSchema: "pos",
                    principalTable: "quotations",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_quotations_org_branch",
            schema: "pos",
            table: "quotations",
            columns: new[] { "organization_id", "branch_id" });

        migrationBuilder.CreateIndex(
            name: "ix_quotations_org_customer",
            schema: "pos",
            table: "quotations",
            columns: new[] { "organization_id", "customer_id" });

        migrationBuilder.CreateIndex(
            name: "ix_quotations_org_status",
            schema: "pos",
            table: "quotations",
            columns: new[] { "organization_id", "status" });

        migrationBuilder.CreateIndex(
            name: "ux_quotations_org_quotation_number",
            schema: "pos",
            table: "quotations",
            columns: new[] { "organization_id", "quotation_number" },
            unique: true,
            filter: "quotation_number IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_quotations_customer_id",
            schema: "pos",
            table: "quotations",
            column: "customer_id");

        migrationBuilder.CreateIndex(
            name: "ux_quotation_lines_quotation_line_number",
            schema: "pos",
            table: "quotation_lines",
            columns: new[] { "quotation_id", "line_number" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ux_quotation_lines_quotation_product",
            schema: "pos",
            table: "quotation_lines",
            columns: new[] { "quotation_id", "product_id" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "quotation_lines",
            schema: "pos");

        migrationBuilder.DropTable(
            name: "quotation_number_sequences",
            schema: "pos");

        migrationBuilder.DropTable(
            name: "quotations",
            schema: "pos");

        migrationBuilder.DropColumn(
            name: "source_quotation_id",
            schema: "pos",
            table: "sales");
    }
}
