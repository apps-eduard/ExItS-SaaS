using System;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(PosDbContext))]
    [Migration("20260830140000_AddPosSupplierPayables")]
    public partial class AddPosSupplierPayables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "supplier_payables",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    paid_at_receipt_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    paid_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    payment_method_at_receipt = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    voided_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    voided_by = table.Column<Guid>(type: "uuid", nullable: true),
                    void_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplier_payables", x => x.id);
                    table.CheckConstraint("ck_supplier_payables_amounts_non_negative", "original_amount > 0 AND paid_at_receipt_amount >= 0 AND paid_amount >= 0 AND balance >= 0");
                    table.CheckConstraint("ck_supplier_payables_balance_identity", "balance = original_amount - paid_amount");
                    table.CheckConstraint("ck_supplier_payables_paid_at_receipt_le_original", "paid_at_receipt_amount <= original_amount");
                    table.CheckConstraint("ck_supplier_payables_payment_method_at_receipt", "payment_method_at_receipt IS NULL OR payment_method_at_receipt IN ('Cash', 'BankTransfer', 'GCash', 'Other')");
                    table.CheckConstraint("ck_supplier_payables_source_type", "source_type IN ('GoodsReceipt', 'DirectPurchaseReceipt')");
                    table.CheckConstraint("ck_supplier_payables_status", "status IN ('Open', 'PartiallyPaid', 'Paid', 'Voided')");
                    table.CheckConstraint("ck_supplier_payables_void_consistency", "(status <> 'Voided' AND voided_at_utc IS NULL AND voided_by IS NULL AND void_reason IS NULL) OR (status = 'Voided' AND voided_at_utc IS NOT NULL AND voided_by IS NOT NULL AND void_reason IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_supplier_payables_suppliers",
                        column: x => x.supplier_id,
                        principalSchema: "pos",
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_payable_payments",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payable_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    payment_method = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    reference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    notes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    paid_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    recorded_by = table.Column<Guid>(type: "uuid", nullable: false),
                    recorded_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplier_payable_payments", x => x.id);
                    table.CheckConstraint("ck_supplier_payable_payments_amount_positive", "amount > 0");
                    table.CheckConstraint("ck_supplier_payable_payments_payment_method", "payment_method IN ('Cash', 'BankTransfer', 'GCash', 'Other')");
                    table.ForeignKey(
                        name: "fk_supplier_payable_payments_payables",
                        column: x => x.payable_id,
                        principalSchema: "pos",
                        principalTable: "supplier_payables",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_supplier_payable_payments_org_payable",
                schema: "pos",
                table: "supplier_payable_payments",
                columns: new[] { "organization_id", "payable_id" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_payable_payments_payable_id",
                schema: "pos",
                table: "supplier_payable_payments",
                column: "payable_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_payables_org_due_date",
                schema: "pos",
                table: "supplier_payables",
                columns: new[] { "organization_id", "due_date" });

            migrationBuilder.CreateIndex(
                name: "ix_supplier_payables_org_status",
                schema: "pos",
                table: "supplier_payables",
                columns: new[] { "organization_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_supplier_payables_org_supplier_status",
                schema: "pos",
                table: "supplier_payables",
                columns: new[] { "organization_id", "supplier_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_payables_supplier_id",
                schema: "pos",
                table: "supplier_payables",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ux_supplier_payables_org_source",
                schema: "pos",
                table: "supplier_payables",
                columns: new[] { "organization_id", "source_type", "source_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "supplier_payable_payments",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "supplier_payables",
                schema: "pos");
        }
    }
}
