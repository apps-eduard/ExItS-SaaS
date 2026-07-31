using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPosCashierShifts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "cashier_shift_id",
                schema: "pos",
                table: "sales",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "cashier_shift_number_sequences",
                schema: "pos",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    last_value = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cashier_shift_number_sequences", x => new { x.organization_id, x.business_date });
                    table.CheckConstraint("ck_cashier_shift_number_sequences_last_value_positive", "last_value > 0");
                });

            migrationBuilder.CreateTable(
                name: "cashier_shifts",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    opening_cash_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    opened_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    opened_by = table.Column<Guid>(type: "uuid", nullable: false),
                    closing_cash_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    expected_cash_amount_snapshot = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    cash_variance_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    closing_notes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    closed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    closed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    cancelled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cashier_shifts", x => x.id);
                    table.CheckConstraint("ck_cashier_shifts_close_consistency", "(status = 'Open' AND closing_cash_amount IS NULL AND expected_cash_amount_snapshot IS NULL AND cash_variance_amount IS NULL AND closed_at_utc IS NULL AND closed_by IS NULL AND cancelled_at_utc IS NULL AND cancelled_by IS NULL) OR (status = 'Closed' AND closing_cash_amount IS NOT NULL AND expected_cash_amount_snapshot IS NOT NULL AND cash_variance_amount IS NOT NULL AND closed_at_utc IS NOT NULL AND closed_by IS NOT NULL AND cancelled_at_utc IS NULL AND cancelled_by IS NULL) OR (status = 'Cancelled' AND closing_cash_amount IS NULL AND expected_cash_amount_snapshot IS NULL AND cash_variance_amount IS NULL AND closed_at_utc IS NULL AND closed_by IS NULL AND cancelled_at_utc IS NOT NULL AND cancelled_by IS NOT NULL)");
                    table.CheckConstraint("ck_cashier_shifts_opening_cash_non_negative", "opening_cash_amount >= 0");
                    table.CheckConstraint("ck_cashier_shifts_status", "status IN ('Open', 'Closed', 'Cancelled')");
                });

            migrationBuilder.CreateTable(
                name: "cashier_shift_movements",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    movement_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    reason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    reference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    recorded_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    recorded_by = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cashier_shift_movements", x => x.id);
                    table.CheckConstraint("ck_cashier_shift_movements_amount_positive", "amount > 0");
                    table.CheckConstraint("ck_cashier_shift_movements_type", "movement_type IN ('CashIn', 'CashOut')");
                    table.ForeignKey(
                        name: "fk_cashier_shift_movements_shifts",
                        column: x => x.shift_id,
                        principalSchema: "pos",
                        principalTable: "cashier_shifts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sales_cashier_shift_id",
                schema: "pos",
                table: "sales",
                column: "cashier_shift_id");

            migrationBuilder.CreateIndex(
                name: "ix_cashier_shift_movements_org_shift_recorded",
                schema: "pos",
                table: "cashier_shift_movements",
                columns: new[] { "organization_id", "shift_id", "recorded_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_cashier_shift_movements_shift_id",
                schema: "pos",
                table: "cashier_shift_movements",
                column: "shift_id");

            migrationBuilder.CreateIndex(
                name: "ix_cashier_shifts_org_status_opened",
                schema: "pos",
                table: "cashier_shifts",
                columns: new[] { "organization_id", "status", "opened_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_cashier_shifts_org_actor_open",
                schema: "pos",
                table: "cashier_shifts",
                columns: new[] { "organization_id", "actor_id" },
                unique: true,
                filter: "status = 'Open'");

            migrationBuilder.CreateIndex(
                name: "ux_cashier_shifts_org_shift_number",
                schema: "pos",
                table: "cashier_shifts",
                columns: new[] { "organization_id", "shift_number" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_cashier_shifts",
                schema: "pos",
                table: "sales",
                column: "cashier_shift_id",
                principalSchema: "pos",
                principalTable: "cashier_shifts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM pos.cashier_shift_movements;");
            migrationBuilder.Sql("UPDATE pos.sales SET cashier_shift_id = NULL WHERE cashier_shift_id IS NOT NULL;");
            migrationBuilder.Sql("DELETE FROM pos.cashier_shifts;");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_cashier_shifts",
                schema: "pos",
                table: "sales");

            migrationBuilder.DropTable(
                name: "cashier_shift_movements",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "cashier_shift_number_sequences",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "cashier_shifts",
                schema: "pos");

            migrationBuilder.DropIndex(
                name: "ix_sales_cashier_shift_id",
                schema: "pos",
                table: "sales");

            migrationBuilder.DropColumn(
                name: "cashier_shift_id",
                schema: "pos",
                table: "sales");
        }
    }
}
