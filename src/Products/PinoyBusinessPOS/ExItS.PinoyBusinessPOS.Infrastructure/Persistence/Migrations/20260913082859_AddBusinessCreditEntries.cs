using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessCreditEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_tender_consistency",
                schema: "pos",
                table: "sales");

            migrationBuilder.AddColumn<Guid>(
                name: "linked_business_credit_entry_id",
                schema: "pos",
                table: "sales",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "business_credit_entries",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    buyer_organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    connection_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    remarks = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reversed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reversal_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    current_due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    source_sale_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_business_credit_entries", x => x.id);
                    table.CheckConstraint("ck_business_credit_entries_amount_positive", "amount > 0");
                    table.CheckConstraint("ck_business_credit_entries_status", "status IN ('Active', 'Reversed')");
                });

            migrationBuilder.CreateIndex(
                name: "ux_sales_linked_business_credit_entry_id",
                schema: "pos",
                table: "sales",
                column: "linked_business_credit_entry_id",
                unique: true,
                filter: "linked_business_credit_entry_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_tender_consistency",
                schema: "pos",
                table: "sales",
                sql: "(payment_method = 'Cash' AND amount_tendered IS NOT NULL AND change_amount IS NOT NULL AND amount_tendered >= total AND gcash_reference IS NULL AND linked_credit_entry_id IS NULL AND linked_business_credit_entry_id IS NULL) OR (payment_method = 'ManualGCash' AND amount_tendered IS NULL AND change_amount IS NULL AND linked_credit_entry_id IS NULL AND linked_business_credit_entry_id IS NULL) OR (payment_method IN ('Card', 'GCash') AND amount_tendered IS NULL AND change_amount IS NULL AND linked_credit_entry_id IS NULL AND linked_business_credit_entry_id IS NULL) OR (payment_method = 'Utang' AND amount_tendered IS NULL AND change_amount IS NULL AND gcash_reference IS NULL AND total > 0 AND ((customer_id IS NOT NULL AND linked_credit_entry_id IS NOT NULL AND linked_business_credit_entry_id IS NULL) OR (buyer_party_kind = 'Organization' AND linked_business_credit_entry_id IS NOT NULL AND customer_id IS NULL AND linked_credit_entry_id IS NULL)))");

            migrationBuilder.CreateIndex(
                name: "ix_business_credit_entries_connection_id",
                schema: "pos",
                table: "business_credit_entries",
                column: "connection_id");

            migrationBuilder.CreateIndex(
                name: "ix_business_credit_entries_seller_buyer_created",
                schema: "pos",
                table: "business_credit_entries",
                columns: new[] { "seller_organization_id", "buyer_organization_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_business_credit_entries_seller_buyer_status",
                schema: "pos",
                table: "business_credit_entries",
                columns: new[] { "seller_organization_id", "buyer_organization_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_business_credit_entries_source_sale_id",
                schema: "pos",
                table: "business_credit_entries",
                column: "source_sale_id",
                filter: "source_sale_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "business_credit_entries",
                schema: "pos");

            migrationBuilder.DropIndex(
                name: "ux_sales_linked_business_credit_entry_id",
                schema: "pos",
                table: "sales");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_tender_consistency",
                schema: "pos",
                table: "sales");

            migrationBuilder.DropColumn(
                name: "linked_business_credit_entry_id",
                schema: "pos",
                table: "sales");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_tender_consistency",
                schema: "pos",
                table: "sales",
                sql: "(payment_method = 'Cash' AND amount_tendered IS NOT NULL AND change_amount IS NOT NULL AND amount_tendered >= total AND gcash_reference IS NULL AND linked_credit_entry_id IS NULL) OR (payment_method = 'ManualGCash' AND amount_tendered IS NULL AND change_amount IS NULL AND linked_credit_entry_id IS NULL) OR (payment_method IN ('Card', 'GCash') AND amount_tendered IS NULL AND change_amount IS NULL AND linked_credit_entry_id IS NULL) OR (payment_method = 'Utang' AND amount_tendered IS NULL AND change_amount IS NULL AND gcash_reference IS NULL AND customer_id IS NOT NULL AND linked_credit_entry_id IS NOT NULL AND total > 0)");
        }
    }
}
