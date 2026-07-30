using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductBasedUtang : Migration
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

            migrationBuilder.AddColumn<Guid>(
                name: "customer_id",
                schema: "pos",
                table: "sales",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "linked_credit_entry_id",
                schema: "pos",
                table: "sales",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_sale_id",
                schema: "pos",
                table: "credit_entries",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_sales_customer_id",
                schema: "pos",
                table: "sales",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ux_sales_linked_credit_entry_id",
                schema: "pos",
                table: "sales",
                column: "linked_credit_entry_id",
                unique: true,
                filter: "linked_credit_entry_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_payment_method",
                schema: "pos",
                table: "sales",
                sql: "payment_method IN ('Cash', 'ManualGCash', 'Utang')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_tender_consistency",
                schema: "pos",
                table: "sales",
                sql: "(payment_method = 'Cash' AND amount_tendered IS NOT NULL AND change_amount IS NOT NULL AND amount_tendered >= total AND gcash_reference IS NULL AND customer_id IS NULL AND linked_credit_entry_id IS NULL) OR (payment_method = 'ManualGCash' AND amount_tendered IS NULL AND change_amount IS NULL AND customer_id IS NULL AND linked_credit_entry_id IS NULL) OR (payment_method = 'Utang' AND amount_tendered IS NULL AND change_amount IS NULL AND gcash_reference IS NULL AND customer_id IS NOT NULL AND linked_credit_entry_id IS NOT NULL AND total > 0)");

            migrationBuilder.CreateIndex(
                name: "ux_credit_entries_source_sale_id",
                schema: "pos",
                table: "credit_entries",
                column: "source_sale_id",
                unique: true,
                filter: "source_sale_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_credit_entries_source_sale",
                schema: "pos",
                table: "credit_entries",
                column: "source_sale_id",
                principalSchema: "pos",
                principalTable: "sales",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_customers",
                schema: "pos",
                table: "sales",
                column: "customer_id",
                principalSchema: "pos",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_credit_entries_source_sale",
                schema: "pos",
                table: "credit_entries");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_customers",
                schema: "pos",
                table: "sales");

            migrationBuilder.DropIndex(
                name: "ix_sales_customer_id",
                schema: "pos",
                table: "sales");

            migrationBuilder.DropIndex(
                name: "ux_sales_linked_credit_entry_id",
                schema: "pos",
                table: "sales");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_payment_method",
                schema: "pos",
                table: "sales");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_tender_consistency",
                schema: "pos",
                table: "sales");

            // Product-Based Utang rows cannot satisfy the pre-Utang payment-method checks.
            migrationBuilder.Sql(
                """
                DELETE FROM pos.credit_due_date_changes
                WHERE credit_entry_id IN (
                    SELECT id FROM pos.credit_entries WHERE source_sale_id IS NOT NULL);
                DELETE FROM pos.credit_entries WHERE source_sale_id IS NOT NULL;
                DELETE FROM pos.sale_lines
                WHERE sale_id IN (SELECT id FROM pos.sales WHERE payment_method = 'Utang');
                DELETE FROM pos.sales WHERE payment_method = 'Utang';
                """);

            migrationBuilder.DropIndex(
                name: "ux_credit_entries_source_sale_id",
                schema: "pos",
                table: "credit_entries");

            migrationBuilder.DropColumn(
                name: "customer_id",
                schema: "pos",
                table: "sales");

            migrationBuilder.DropColumn(
                name: "linked_credit_entry_id",
                schema: "pos",
                table: "sales");

            migrationBuilder.DropColumn(
                name: "source_sale_id",
                schema: "pos",
                table: "credit_entries");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_payment_method",
                schema: "pos",
                table: "sales",
                sql: "payment_method IN ('Cash', 'ManualGCash')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_tender_consistency",
                schema: "pos",
                table: "sales",
                sql: "(payment_method = 'Cash' AND amount_tendered IS NOT NULL AND change_amount IS NOT NULL AND amount_tendered >= total AND gcash_reference IS NULL) OR (payment_method = 'ManualGCash' AND amount_tendered IS NULL AND change_amount IS NULL)");
        }
    }
}
