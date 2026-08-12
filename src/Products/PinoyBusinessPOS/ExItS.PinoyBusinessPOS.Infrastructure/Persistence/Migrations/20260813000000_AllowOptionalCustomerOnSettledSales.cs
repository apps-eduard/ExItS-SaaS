using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AllowOptionalCustomerOnSettledSales : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "ck_sales_tender_consistency",
            schema: "pos",
            table: "sales");

        migrationBuilder.CreateIndex(
            name: "ix_sales_org_customer_recorded_at",
            schema: "pos",
            table: "sales",
            columns: new[] { "organization_id", "customer_id", "recorded_at_utc" },
            filter: "customer_id IS NOT NULL");

        migrationBuilder.AddCheckConstraint(
            name: "ck_sales_tender_consistency",
            schema: "pos",
            table: "sales",
            sql: "(payment_method = 'Cash' AND amount_tendered IS NOT NULL AND change_amount IS NOT NULL AND amount_tendered >= total AND gcash_reference IS NULL AND linked_credit_entry_id IS NULL) OR (payment_method = 'ManualGCash' AND amount_tendered IS NULL AND change_amount IS NULL AND linked_credit_entry_id IS NULL) OR (payment_method IN ('Card', 'GCash') AND amount_tendered IS NULL AND change_amount IS NULL AND linked_credit_entry_id IS NULL) OR (payment_method = 'Utang' AND amount_tendered IS NULL AND change_amount IS NULL AND gcash_reference IS NULL AND customer_id IS NOT NULL AND linked_credit_entry_id IS NOT NULL AND total > 0)");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "ck_sales_tender_consistency",
            schema: "pos",
            table: "sales");

        migrationBuilder.DropIndex(
            name: "ix_sales_org_customer_recorded_at",
            schema: "pos",
            table: "sales");

        migrationBuilder.AddCheckConstraint(
            name: "ck_sales_tender_consistency",
            schema: "pos",
            table: "sales",
            sql: "(payment_method = 'Cash' AND amount_tendered IS NOT NULL AND change_amount IS NOT NULL AND amount_tendered >= total AND gcash_reference IS NULL AND customer_id IS NULL AND linked_credit_entry_id IS NULL) OR (payment_method = 'ManualGCash' AND amount_tendered IS NULL AND change_amount IS NULL AND customer_id IS NULL AND linked_credit_entry_id IS NULL) OR (payment_method IN ('Card', 'GCash') AND amount_tendered IS NULL AND change_amount IS NULL AND customer_id IS NULL AND linked_credit_entry_id IS NULL) OR (payment_method = 'Utang' AND amount_tendered IS NULL AND change_amount IS NULL AND gcash_reference IS NULL AND customer_id IS NOT NULL AND linked_credit_entry_id IS NOT NULL AND total > 0)");
    }
}
