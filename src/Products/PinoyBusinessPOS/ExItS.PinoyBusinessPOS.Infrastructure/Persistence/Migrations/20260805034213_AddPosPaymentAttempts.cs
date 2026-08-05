using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPosPaymentAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_payment_method",
                schema: "pos",
                table: "sales");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_status",
                schema: "pos",
                table: "sales");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_tender_consistency",
                schema: "pos",
                table: "sales");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_void_consistency",
                schema: "pos",
                table: "sales");

            migrationBuilder.CreateTable(
                name: "payment_attempts",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sale_id = table.Column<Guid>(type: "uuid", nullable: false),
                    method = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    provider_reference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    external_reference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    checkout_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    deep_link = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    qr_payload = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    card_brand = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    card_last_four = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    failure_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    failure_message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    verified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    verification_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    provider_event_sequence = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_attempts", x => x.id);
                    table.CheckConstraint("ck_payment_attempts_amount_positive", "amount > 0");
                    table.CheckConstraint("ck_payment_attempts_method", "method IN ('Cash', 'Card', 'GCash', 'ManualGCashTransfer')");
                    table.CheckConstraint("ck_payment_attempts_provider", "provider IN ('None', 'Fake', 'Manual')");
                    table.CheckConstraint("ck_payment_attempts_status", "status IN ('Created', 'Pending', 'RequiresCustomerAction', 'Processing', 'Paid', 'Failed', 'Cancelled', 'Expired', 'Refunded', 'PendingManualVerification')");
                    table.ForeignKey(
                        name: "fk_payment_attempts_sales",
                        column: x => x.sale_id,
                        principalSchema: "pos",
                        principalTable: "sales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_payment_method",
                schema: "pos",
                table: "sales",
                sql: "payment_method IN ('Cash', 'ManualGCash', 'Utang', 'Card', 'GCash')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_status",
                schema: "pos",
                table: "sales",
                sql: "status IN ('Completed', 'Voided', 'AwaitingPayment')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_tender_consistency",
                schema: "pos",
                table: "sales",
                sql: "(payment_method = 'Cash' AND amount_tendered IS NOT NULL AND change_amount IS NOT NULL AND amount_tendered >= total AND gcash_reference IS NULL AND customer_id IS NULL AND linked_credit_entry_id IS NULL) OR (payment_method = 'ManualGCash' AND amount_tendered IS NULL AND change_amount IS NULL AND customer_id IS NULL AND linked_credit_entry_id IS NULL) OR (payment_method IN ('Card', 'GCash') AND amount_tendered IS NULL AND change_amount IS NULL AND customer_id IS NULL AND linked_credit_entry_id IS NULL) OR (payment_method = 'Utang' AND amount_tendered IS NULL AND change_amount IS NULL AND gcash_reference IS NULL AND customer_id IS NOT NULL AND linked_credit_entry_id IS NOT NULL AND total > 0)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_void_consistency",
                schema: "pos",
                table: "sales",
                sql: "(status IN ('Completed', 'AwaitingPayment') AND voided_at_utc IS NULL AND voided_by IS NULL AND void_reason IS NULL) OR (status = 'Voided' AND voided_at_utc IS NOT NULL AND voided_by IS NOT NULL AND void_reason IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "ix_payment_attempts_org_sale_status",
                schema: "pos",
                table: "payment_attempts",
                columns: new[] { "organization_id", "sale_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_attempts_organization_id",
                schema: "pos",
                table: "payment_attempts",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_attempts_sale_id",
                schema: "pos",
                table: "payment_attempts",
                column: "sale_id");

            migrationBuilder.CreateIndex(
                name: "ux_payment_attempts_org_external_reference",
                schema: "pos",
                table: "payment_attempts",
                columns: new[] { "organization_id", "external_reference" },
                unique: true,
                filter: "external_reference IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_payment_attempts_org_idempotency",
                schema: "pos",
                table: "payment_attempts",
                columns: new[] { "organization_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_payment_attempts_provider_reference",
                schema: "pos",
                table: "payment_attempts",
                columns: new[] { "provider", "provider_reference" },
                unique: true,
                filter: "provider_reference IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment_attempts",
                schema: "pos");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_payment_method",
                schema: "pos",
                table: "sales");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_status",
                schema: "pos",
                table: "sales");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_tender_consistency",
                schema: "pos",
                table: "sales");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_void_consistency",
                schema: "pos",
                table: "sales");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_payment_method",
                schema: "pos",
                table: "sales",
                sql: "payment_method IN ('Cash', 'ManualGCash', 'Utang')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_status",
                schema: "pos",
                table: "sales",
                sql: "status IN ('Completed', 'Voided')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_tender_consistency",
                schema: "pos",
                table: "sales",
                sql: "(payment_method = 'Cash' AND amount_tendered IS NOT NULL AND change_amount IS NOT NULL AND amount_tendered >= total AND gcash_reference IS NULL AND customer_id IS NULL AND linked_credit_entry_id IS NULL) OR (payment_method = 'ManualGCash' AND amount_tendered IS NULL AND change_amount IS NULL AND customer_id IS NULL AND linked_credit_entry_id IS NULL) OR (payment_method = 'Utang' AND amount_tendered IS NULL AND change_amount IS NULL AND gcash_reference IS NULL AND customer_id IS NOT NULL AND linked_credit_entry_id IS NOT NULL AND total > 0)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_void_consistency",
                schema: "pos",
                table: "sales",
                sql: "(status = 'Completed' AND voided_at_utc IS NULL AND voided_by IS NULL AND void_reason IS NULL) OR (status = 'Voided' AND voided_at_utc IS NOT NULL AND voided_by IS NOT NULL AND void_reason IS NOT NULL)");
        }
    }
}
