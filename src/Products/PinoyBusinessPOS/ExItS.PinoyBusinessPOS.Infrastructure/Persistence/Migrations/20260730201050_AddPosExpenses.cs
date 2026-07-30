using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPosExpenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "expense_categories",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_expense_categories", x => x.id);
                    table.CheckConstraint("ck_expense_categories_status", "status IN ('Active', 'Inactive')");
                });

            migrationBuilder.CreateTable(
                name: "expense_number_sequences",
                schema: "pos",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    last_value = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_expense_number_sequences", x => new { x.organization_id, x.business_date });
                    table.CheckConstraint("ck_expense_number_sequences_last_value_positive", "last_value > 0");
                });

            migrationBuilder.CreateTable(
                name: "expenses",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expense_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    payment_method = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    payee = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    gcash_reference = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    expense_date = table.Column<DateOnly>(type: "date", nullable: false),
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
                    table.PrimaryKey("PK_expenses", x => x.id);
                    table.CheckConstraint("ck_expenses_amount_positive", "amount > 0");
                    table.CheckConstraint("ck_expenses_payment_method", "payment_method IN ('Cash', 'ManualGCash')");
                    table.CheckConstraint("ck_expenses_status", "status IN ('Recorded', 'Voided')");
                    table.CheckConstraint("ck_expenses_tender_consistency", "(payment_method = 'Cash' AND gcash_reference IS NULL) OR (payment_method = 'ManualGCash')");
                    table.CheckConstraint("ck_expenses_void_consistency", "(status = 'Recorded' AND voided_at_utc IS NULL AND voided_by IS NULL AND void_reason IS NULL) OR (status = 'Voided' AND voided_at_utc IS NOT NULL AND voided_by IS NOT NULL AND void_reason IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_expenses_expense_categories",
                        column: x => x.category_id,
                        principalSchema: "pos",
                        principalTable: "expense_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_expense_categories_org_name",
                schema: "pos",
                table: "expense_categories",
                columns: new[] { "organization_id", "name" });

            migrationBuilder.CreateIndex(
                name: "ux_expense_categories_org_active_name",
                schema: "pos",
                table: "expense_categories",
                columns: new[] { "organization_id", "normalized_name" },
                unique: true,
                filter: "status = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_expenses_category_id",
                schema: "pos",
                table: "expenses",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_expenses_org_category_id",
                schema: "pos",
                table: "expenses",
                columns: new[] { "organization_id", "category_id" });

            migrationBuilder.CreateIndex(
                name: "ix_expenses_org_expense_date",
                schema: "pos",
                table: "expenses",
                columns: new[] { "organization_id", "expense_date" });

            migrationBuilder.CreateIndex(
                name: "ix_expenses_org_payment_method",
                schema: "pos",
                table: "expenses",
                columns: new[] { "organization_id", "payment_method" });

            migrationBuilder.CreateIndex(
                name: "ix_expenses_org_status",
                schema: "pos",
                table: "expenses",
                columns: new[] { "organization_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_expenses_org_expense_number",
                schema: "pos",
                table: "expenses",
                columns: new[] { "organization_id", "expense_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "expense_number_sequences",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "expenses",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "expense_categories",
                schema: "pos");
        }
    }
}
