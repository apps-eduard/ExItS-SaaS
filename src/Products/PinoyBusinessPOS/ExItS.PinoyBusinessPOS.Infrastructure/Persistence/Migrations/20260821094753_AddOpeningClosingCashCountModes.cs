using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOpeningClosingCashCountModes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.AddColumn<string>(
                name: "closing_cash_count_mode",
                schema: "pos",
                table: "operational_setups",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Optional");

            migrationBuilder.AddColumn<string>(
                name: "opening_cash_count_mode",
                schema: "pos",
                table: "operational_setups",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Optional");

            migrationBuilder.Sql(
                """
                UPDATE pos.operational_setups
                SET opening_cash_count_mode = cash_count_mode,
                    closing_cash_count_mode = cash_count_mode;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "effective_cash_count_mode",
                schema: "pos",
                table: "cashier_shifts",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Optional",
                oldClrType: typeof(string),
                oldType: "character varying(16)",
                oldMaxLength: 16,
                oldDefaultValue: "Required");

            migrationBuilder.AddColumn<string>(
                name: "effective_closing_cash_count_mode",
                schema: "pos",
                table: "cashier_shifts",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Optional");

            migrationBuilder.AddColumn<string>(
                name: "effective_opening_cash_count_mode",
                schema: "pos",
                table: "cashier_shifts",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Optional");

            migrationBuilder.Sql(
                """
                UPDATE pos.cashier_shifts
                SET effective_opening_cash_count_mode = effective_cash_count_mode,
                    effective_closing_cash_count_mode = effective_cash_count_mode;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_operational_setups_closing_cash_count_mode",
                schema: "pos",
                table: "operational_setups",
                sql: "closing_cash_count_mode IN ('Optional', 'Required')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_operational_setups_opening_cash_count_mode",
                schema: "pos",
                table: "operational_setups",
                sql: "opening_cash_count_mode IN ('Optional', 'Required')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_cashier_shifts_closing_cash_count_mode",
                schema: "pos",
                table: "cashier_shifts",
                sql: "effective_closing_cash_count_mode IN ('Off', 'Optional', 'Required')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_cashier_shifts_opening_cash_count_mode",
                schema: "pos",
                table: "cashier_shifts",
                sql: "effective_opening_cash_count_mode IN ('Off', 'Optional', 'Required')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_operational_setups_closing_cash_count_mode",
                schema: "pos",
                table: "operational_setups");

            migrationBuilder.DropCheckConstraint(
                name: "ck_operational_setups_opening_cash_count_mode",
                schema: "pos",
                table: "operational_setups");

            migrationBuilder.DropCheckConstraint(
                name: "ck_cashier_shifts_closing_cash_count_mode",
                schema: "pos",
                table: "cashier_shifts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_cashier_shifts_opening_cash_count_mode",
                schema: "pos",
                table: "cashier_shifts");

            migrationBuilder.DropColumn(
                name: "closing_cash_count_mode",
                schema: "pos",
                table: "operational_setups");

            migrationBuilder.DropColumn(
                name: "opening_cash_count_mode",
                schema: "pos",
                table: "operational_setups");

            migrationBuilder.DropColumn(
                name: "effective_closing_cash_count_mode",
                schema: "pos",
                table: "cashier_shifts");

            migrationBuilder.DropColumn(
                name: "effective_opening_cash_count_mode",
                schema: "pos",
                table: "cashier_shifts");

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

            migrationBuilder.AlterColumn<string>(
                name: "effective_cash_count_mode",
                schema: "pos",
                table: "cashier_shifts",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Required",
                oldClrType: typeof(string),
                oldType: "character varying(16)",
                oldMaxLength: 16,
                oldDefaultValue: "Optional");
        }
    }
}
