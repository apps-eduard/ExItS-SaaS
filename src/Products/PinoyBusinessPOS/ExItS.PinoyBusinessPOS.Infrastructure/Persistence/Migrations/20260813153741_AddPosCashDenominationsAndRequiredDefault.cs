using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPosCashDenominationsAndRequiredDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_operational_setups_cash_count_mode",
                schema: "pos",
                table: "operational_setups");

            migrationBuilder.AlterColumn<string>(
                name: "cash_count_mode",
                schema: "pos",
                table: "operational_setups",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Required",
                oldClrType: typeof(string),
                oldType: "character varying(16)",
                oldMaxLength: 16,
                oldDefaultValue: "Optional");

            // Legacy Off is no longer configurable. Existing org rows become Optional.
            // Historical cashier_shifts.effective_cash_count_mode = Off is unchanged.
            migrationBuilder.Sql(
                """
                UPDATE pos.operational_setups
                SET cash_count_mode = 'Optional'
                WHERE cash_count_mode = 'Off';
                """);

            migrationBuilder.CreateTable(
                name: "cashier_shift_cash_count_lines",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_id = table.Column<Guid>(type: "uuid", nullable: false),
                    count_kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    denomination_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    line_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cashier_shift_cash_count_lines", x => x.id);
                    table.CheckConstraint("ck_cashier_shift_cash_count_lines_kind", "count_kind IN ('Opening', 'Closing')");
                    table.CheckConstraint("ck_cashier_shift_cash_count_lines_line_total_non_negative", "line_total >= 0");
                    table.CheckConstraint("ck_cashier_shift_cash_count_lines_quantity_non_negative", "quantity >= 0");
                    table.CheckConstraint("ck_cashier_shift_cash_count_lines_value_positive", "denomination_value > 0");
                    table.ForeignKey(
                        name: "fk_cashier_shift_cash_count_lines_shifts",
                        column: x => x.shift_id,
                        principalSchema: "pos",
                        principalTable: "cashier_shifts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "organization_cash_denominations",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    display_label = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_cash_denominations", x => x.id);
                    table.CheckConstraint("ck_organization_cash_denominations_sort_order_non_negative", "sort_order >= 0");
                    table.CheckConstraint("ck_organization_cash_denominations_value_positive", "value > 0");
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_operational_setups_cash_count_mode",
                schema: "pos",
                table: "operational_setups",
                sql: "cash_count_mode IN ('Optional', 'Required')");

            migrationBuilder.CreateIndex(
                name: "ix_cashier_shift_cash_count_lines_org_shift_kind",
                schema: "pos",
                table: "cashier_shift_cash_count_lines",
                columns: new[] { "organization_id", "shift_id", "count_kind" });

            migrationBuilder.CreateIndex(
                name: "ux_cashier_shift_cash_count_lines_shift_kind_value",
                schema: "pos",
                table: "cashier_shift_cash_count_lines",
                columns: new[] { "shift_id", "count_kind", "denomination_value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_organization_cash_denominations_org_sort",
                schema: "pos",
                table: "organization_cash_denominations",
                columns: new[] { "organization_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ux_organization_cash_denominations_org_value",
                schema: "pos",
                table: "organization_cash_denominations",
                columns: new[] { "organization_id", "value" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cashier_shift_cash_count_lines",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "organization_cash_denominations",
                schema: "pos");

            migrationBuilder.DropCheckConstraint(
                name: "ck_operational_setups_cash_count_mode",
                schema: "pos",
                table: "operational_setups");

            migrationBuilder.AlterColumn<string>(
                name: "cash_count_mode",
                schema: "pos",
                table: "operational_setups",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Optional",
                oldClrType: typeof(string),
                oldType: "character varying(16)",
                oldMaxLength: 16,
                oldDefaultValue: "Required");

            migrationBuilder.AddCheckConstraint(
                name: "ck_operational_setups_cash_count_mode",
                schema: "pos",
                table: "operational_setups",
                sql: "cash_count_mode IN ('Off', 'Optional', 'Required')");
        }
    }
}
