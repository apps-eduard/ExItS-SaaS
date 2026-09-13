using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationPaymentMethodSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_payment_method",
                schema: "pos",
                table: "sales");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_tender_consistency",
                schema: "pos",
                table: "sales");

            migrationBuilder.CreateTable(
                name: "organization_payment_method_settings",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    method_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    display_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    require_reference = table.Column<bool>(type: "boolean", nullable: false),
                    branch_scope = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    selected_branch_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    instructions = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    account_hint = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_payment_method_settings", x => x.id);
                    table.CheckConstraint("ck_org_payment_method_branch_scope", "branch_scope IN ('AllBranches', 'SelectedBranches')");
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_payment_method",
                schema: "pos",
                table: "sales",
                sql: "payment_method IN ('Cash', 'ManualGCash', 'Utang', 'Card', 'GCash', 'BankTransfer', 'Check', 'ManualMaya')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_tender_consistency",
                schema: "pos",
                table: "sales",
                sql: "(payment_method = 'Cash' AND amount_tendered IS NOT NULL AND change_amount IS NOT NULL AND amount_tendered >= total AND gcash_reference IS NULL AND linked_credit_entry_id IS NULL AND linked_business_credit_entry_id IS NULL) OR (payment_method IN ('ManualGCash', 'BankTransfer', 'Check', 'ManualMaya') AND amount_tendered IS NULL AND change_amount IS NULL AND linked_credit_entry_id IS NULL AND linked_business_credit_entry_id IS NULL) OR (payment_method IN ('Card', 'GCash') AND amount_tendered IS NULL AND change_amount IS NULL AND linked_credit_entry_id IS NULL AND linked_business_credit_entry_id IS NULL) OR (payment_method = 'Utang' AND amount_tendered IS NULL AND change_amount IS NULL AND gcash_reference IS NULL AND total > 0 AND ((customer_id IS NOT NULL AND linked_credit_entry_id IS NOT NULL AND linked_business_credit_entry_id IS NULL) OR (buyer_party_kind = 'Organization' AND linked_business_credit_entry_id IS NOT NULL AND customer_id IS NULL AND linked_credit_entry_id IS NULL)))");

            migrationBuilder.CreateIndex(
                name: "ux_org_payment_method_settings_org_method",
                schema: "pos",
                table: "organization_payment_method_settings",
                columns: new[] { "organization_id", "method_code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "organization_payment_method_settings",
                schema: "pos");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_payment_method",
                schema: "pos",
                table: "sales");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_tender_consistency",
                schema: "pos",
                table: "sales");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_payment_method",
                schema: "pos",
                table: "sales",
                sql: "payment_method IN ('Cash', 'ManualGCash', 'Utang', 'Card', 'GCash')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_tender_consistency",
                schema: "pos",
                table: "sales",
                sql: "(payment_method = 'Cash' AND amount_tendered IS NOT NULL AND change_amount IS NOT NULL AND amount_tendered >= total AND gcash_reference IS NULL AND linked_credit_entry_id IS NULL AND linked_business_credit_entry_id IS NULL) OR (payment_method = 'ManualGCash' AND amount_tendered IS NULL AND change_amount IS NULL AND linked_credit_entry_id IS NULL AND linked_business_credit_entry_id IS NULL) OR (payment_method IN ('Card', 'GCash') AND amount_tendered IS NULL AND change_amount IS NULL AND linked_credit_entry_id IS NULL AND linked_business_credit_entry_id IS NULL) OR (payment_method = 'Utang' AND amount_tendered IS NULL AND change_amount IS NULL AND gcash_reference IS NULL AND total > 0 AND ((customer_id IS NOT NULL AND linked_credit_entry_id IS NOT NULL AND linked_business_credit_entry_id IS NULL) OR (buyer_party_kind = 'Organization' AND linked_business_credit_entry_id IS NOT NULL AND customer_id IS NULL AND linked_credit_entry_id IS NULL)))");
        }
    }
}
