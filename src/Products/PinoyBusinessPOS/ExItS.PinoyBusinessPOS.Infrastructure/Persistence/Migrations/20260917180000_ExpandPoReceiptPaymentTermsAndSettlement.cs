using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(PosDbContext))]
[Migration("20260917180000_ExpandPoReceiptPaymentTermsAndSettlement")]
public partial class ExpandPoReceiptPaymentTermsAndSettlement : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE pos.purchase_orders
                DROP CONSTRAINT IF EXISTS ck_purchase_orders_payment_term;

            ALTER TABLE pos.purchase_orders
                ADD CONSTRAINT ck_purchase_orders_payment_term
                CHECK (payment_term BETWEEN 0 AND 5);

            ALTER TABLE pos.connected_purchase_orders
                DROP CONSTRAINT IF EXISTS ck_connected_purchase_orders_payment_term;

            ALTER TABLE pos.connected_purchase_orders
                ADD CONSTRAINT ck_connected_purchase_orders_payment_term
                CHECK (payment_term BETWEEN 0 AND 5);

            ALTER TABLE pos.supplier_payables
                DROP CONSTRAINT IF EXISTS ck_supplier_payables_payment_method_at_receipt;

            ALTER TABLE pos.supplier_payables
                ADD CONSTRAINT ck_supplier_payables_payment_method_at_receipt
                CHECK (payment_method_at_receipt IS NULL OR payment_method_at_receipt IN ('Cash', 'BankTransfer', 'GCash', 'Other', 'BankDeposit', 'Check'));

            ALTER TABLE pos.supplier_payable_payments
                DROP CONSTRAINT IF EXISTS ck_supplier_payable_payments_payment_method;

            ALTER TABLE pos.supplier_payable_payments
                ADD CONSTRAINT ck_supplier_payable_payments_payment_method
                CHECK (payment_method IN ('Cash', 'BankTransfer', 'GCash', 'Other', 'BankDeposit', 'Check'));
            """);

        migrationBuilder.AddColumn<string>(
            name: "gcash_reference",
            schema: "pos",
            table: "goods_receipts",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "bank_name",
            schema: "pos",
            table: "goods_receipts",
            type: "character varying(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "transfer_or_deposit_reference",
            schema: "pos",
            table: "goods_receipts",
            type: "character varying(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.AddColumn<DateOnly>(
            name: "settlement_date",
            schema: "pos",
            table: "goods_receipts",
            type: "date",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "check_number",
            schema: "pos",
            table: "goods_receipts",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<DateOnly>(
            name: "check_date",
            schema: "pos",
            table: "goods_receipts",
            type: "date",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "settlement_notes",
            schema: "pos",
            table: "goods_receipts",
            type: "character varying(512)",
            maxLength: 512,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "check_clearing_status",
            schema: "pos",
            table: "goods_receipts",
            type: "character varying(32)",
            maxLength: 32,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "gcash_reference", schema: "pos", table: "goods_receipts");
        migrationBuilder.DropColumn(name: "bank_name", schema: "pos", table: "goods_receipts");
        migrationBuilder.DropColumn(name: "transfer_or_deposit_reference", schema: "pos", table: "goods_receipts");
        migrationBuilder.DropColumn(name: "settlement_date", schema: "pos", table: "goods_receipts");
        migrationBuilder.DropColumn(name: "check_number", schema: "pos", table: "goods_receipts");
        migrationBuilder.DropColumn(name: "check_date", schema: "pos", table: "goods_receipts");
        migrationBuilder.DropColumn(name: "settlement_notes", schema: "pos", table: "goods_receipts");
        migrationBuilder.DropColumn(name: "check_clearing_status", schema: "pos", table: "goods_receipts");

        migrationBuilder.Sql(
            """
            ALTER TABLE pos.purchase_orders
                DROP CONSTRAINT IF EXISTS ck_purchase_orders_payment_term;

            ALTER TABLE pos.purchase_orders
                ADD CONSTRAINT ck_purchase_orders_payment_term
                CHECK (payment_term BETWEEN 0 AND 3);

            ALTER TABLE pos.connected_purchase_orders
                DROP CONSTRAINT IF EXISTS ck_connected_purchase_orders_payment_term;

            ALTER TABLE pos.connected_purchase_orders
                ADD CONSTRAINT ck_connected_purchase_orders_payment_term
                CHECK (payment_term BETWEEN 0 AND 3);

            ALTER TABLE pos.supplier_payables
                DROP CONSTRAINT IF EXISTS ck_supplier_payables_payment_method_at_receipt;

            ALTER TABLE pos.supplier_payables
                ADD CONSTRAINT ck_supplier_payables_payment_method_at_receipt
                CHECK (payment_method_at_receipt IS NULL OR payment_method_at_receipt IN ('Cash', 'BankTransfer', 'GCash', 'Other'));

            ALTER TABLE pos.supplier_payable_payments
                DROP CONSTRAINT IF EXISTS ck_supplier_payable_payments_payment_method;

            ALTER TABLE pos.supplier_payable_payments
                ADD CONSTRAINT ck_supplier_payable_payments_payment_method
                CHECK (payment_method IN ('Cash', 'BankTransfer', 'GCash', 'Other'));
            """);
    }
}
