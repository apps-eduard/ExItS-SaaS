using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Connected PO material revision fields: proposed/confirmed payment + unit price.
/// Expands payment_term check to include BankTransfer (3).
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260915210000_AddConnectedPoPaymentRevisionFields")]
public partial class AddConnectedPoPaymentRevisionFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE pos.connected_purchase_orders
                DROP CONSTRAINT IF EXISTS ck_connected_purchase_orders_payment_term;

            ALTER TABLE pos.purchase_orders
                DROP CONSTRAINT IF EXISTS ck_purchase_orders_payment_term;

            ALTER TABLE pos.connected_purchase_orders
                ADD COLUMN IF NOT EXISTS proposed_payment_term integer NULL,
                ADD COLUMN IF NOT EXISTS confirmed_payment_term integer NULL;

            ALTER TABLE pos.connected_purchase_orders
                ADD CONSTRAINT ck_connected_purchase_orders_payment_term
                CHECK (payment_term BETWEEN 0 AND 3);

            ALTER TABLE pos.purchase_orders
                ADD CONSTRAINT ck_purchase_orders_payment_term
                CHECK (payment_term BETWEEN 0 AND 3);

            ALTER TABLE pos.connected_purchase_order_lines
                ADD COLUMN IF NOT EXISTS proposed_unit_price numeric(18,2) NULL,
                ADD COLUMN IF NOT EXISTS confirmed_unit_price numeric(18,2) NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE pos.connected_purchase_order_lines
                DROP COLUMN IF EXISTS proposed_unit_price,
                DROP COLUMN IF EXISTS confirmed_unit_price;

            ALTER TABLE pos.connected_purchase_orders
                DROP CONSTRAINT IF EXISTS ck_connected_purchase_orders_payment_term;

            ALTER TABLE pos.purchase_orders
                DROP CONSTRAINT IF EXISTS ck_purchase_orders_payment_term;

            ALTER TABLE pos.connected_purchase_orders
                DROP COLUMN IF EXISTS proposed_payment_term,
                DROP COLUMN IF EXISTS confirmed_payment_term;

            ALTER TABLE pos.connected_purchase_orders
                ADD CONSTRAINT ck_connected_purchase_orders_payment_term
                CHECK (payment_term BETWEEN 0 AND 2);

            ALTER TABLE pos.purchase_orders
                ADD CONSTRAINT ck_purchase_orders_payment_term
                CHECK (payment_term BETWEEN 0 AND 2);
            """);
    }
}
