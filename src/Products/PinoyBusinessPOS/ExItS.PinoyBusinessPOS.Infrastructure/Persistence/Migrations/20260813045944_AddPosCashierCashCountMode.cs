using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPosCashierCashCountMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_cashier_shifts_close_consistency",
                schema: "pos",
                table: "cashier_shifts");

            migrationBuilder.AddColumn<string>(
                name: "cash_count_mode",
                schema: "pos",
                table: "operational_setups",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Optional");

            // Existing completed stores already required a physical count; keep that until an admin opts in.
            migrationBuilder.Sql(
                """
                UPDATE pos.operational_setups
                SET cash_count_mode = 'Required'
                WHERE is_completed = TRUE;
                """);

            migrationBuilder.AddColumn<string>(
                name: "effective_cash_count_mode",
                schema: "pos",
                table: "cashier_shifts",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Required");

            migrationBuilder.AddColumn<bool>(
                name: "opening_cash_counted",
                schema: "pos",
                table: "cashier_shifts",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_operational_setups_cash_count_mode",
                schema: "pos",
                table: "operational_setups",
                sql: "cash_count_mode IN ('Off', 'Optional', 'Required')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_cashier_shifts_cash_count_mode",
                schema: "pos",
                table: "cashier_shifts",
                sql: "effective_cash_count_mode IN ('Off', 'Optional', 'Required')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_cashier_shifts_close_consistency",
                schema: "pos",
                table: "cashier_shifts",
                sql: "(status = 'Open' AND closing_cash_amount IS NULL AND expected_cash_amount_snapshot IS NULL AND cash_variance_amount IS NULL AND closed_at_utc IS NULL AND closed_by IS NULL AND cancelled_at_utc IS NULL AND cancelled_by IS NULL) OR (status = 'Closed' AND expected_cash_amount_snapshot IS NOT NULL AND closed_at_utc IS NOT NULL AND closed_by IS NOT NULL AND cancelled_at_utc IS NULL AND cancelled_by IS NULL AND ((closing_cash_amount IS NOT NULL AND cash_variance_amount IS NOT NULL) OR (closing_cash_amount IS NULL AND cash_variance_amount IS NULL))) OR (status = 'Cancelled' AND closing_cash_amount IS NULL AND expected_cash_amount_snapshot IS NULL AND cash_variance_amount IS NULL AND closed_at_utc IS NULL AND closed_by IS NULL AND cancelled_at_utc IS NOT NULL AND cancelled_by IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_operational_setups_cash_count_mode",
                schema: "pos",
                table: "operational_setups");

            migrationBuilder.DropCheckConstraint(
                name: "ck_cashier_shifts_cash_count_mode",
                schema: "pos",
                table: "cashier_shifts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_cashier_shifts_close_consistency",
                schema: "pos",
                table: "cashier_shifts");

            migrationBuilder.DropColumn(
                name: "cash_count_mode",
                schema: "pos",
                table: "operational_setups");

            migrationBuilder.DropColumn(
                name: "effective_cash_count_mode",
                schema: "pos",
                table: "cashier_shifts");

            migrationBuilder.DropColumn(
                name: "opening_cash_counted",
                schema: "pos",
                table: "cashier_shifts");

            migrationBuilder.AddCheckConstraint(
                name: "ck_cashier_shifts_close_consistency",
                schema: "pos",
                table: "cashier_shifts",
                sql: "(status = 'Open' AND closing_cash_amount IS NULL AND expected_cash_amount_snapshot IS NULL AND cash_variance_amount IS NULL AND closed_at_utc IS NULL AND closed_by IS NULL AND cancelled_at_utc IS NULL AND cancelled_by IS NULL) OR (status = 'Closed' AND closing_cash_amount IS NOT NULL AND expected_cash_amount_snapshot IS NOT NULL AND cash_variance_amount IS NOT NULL AND closed_at_utc IS NOT NULL AND closed_by IS NOT NULL AND cancelled_at_utc IS NULL AND cancelled_by IS NULL) OR (status = 'Cancelled' AND closing_cash_amount IS NULL AND expected_cash_amount_snapshot IS NULL AND cash_variance_amount IS NULL AND closed_at_utc IS NULL AND closed_by IS NULL AND cancelled_at_utc IS NOT NULL AND cancelled_by IS NOT NULL)");
        }
    }
}
