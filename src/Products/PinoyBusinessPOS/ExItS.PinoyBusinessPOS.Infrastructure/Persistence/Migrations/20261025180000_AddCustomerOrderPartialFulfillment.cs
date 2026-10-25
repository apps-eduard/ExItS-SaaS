using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(PosDbContext))]
[Migration("20261025180000_AddCustomerOrderPartialFulfillment")]
public partial class AddCustomerOrderPartialFulfillment : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE pos.customer_order_lines
              DROP CONSTRAINT IF EXISTS ck_customer_order_lines_quantity_positive;
            """);

        migrationBuilder.AddColumn<decimal>(
            name: "original_quantity",
            schema: "pos",
            table: "customer_order_lines",
            type: "numeric(18,3)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "original_discount",
            schema: "pos",
            table: "customer_order_lines",
            type: "numeric(18,2)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<string>(
            name: "adjustment_reason",
            schema: "pos",
            table: "customer_order_lines",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "adjustment_note",
            schema: "pos",
            table: "customer_order_lines",
            type: "character varying(512)",
            maxLength: 512,
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE pos.customer_order_lines
            SET original_quantity = quantity,
                original_discount = discount
            WHERE original_quantity = 0;
            """);

        migrationBuilder.Sql(
            """
            ALTER TABLE pos.customer_order_lines
              ADD CONSTRAINT ck_customer_order_lines_quantity_non_negative CHECK (quantity >= 0);
            """);

        migrationBuilder.Sql(
            """
            ALTER TABLE pos.customer_order_lines
              DROP CONSTRAINT IF EXISTS ck_customer_order_lines_amounts_non_negative;
            ALTER TABLE pos.customer_order_lines
              ADD CONSTRAINT ck_customer_order_lines_amounts_non_negative
              CHECK (unit_price >= 0 AND discount >= 0 AND line_total >= 0 AND original_quantity >= 0 AND original_discount >= 0);
            """);

        migrationBuilder.AddColumn<decimal>(
            name: "original_merchandise_subtotal",
            schema: "pos",
            table: "customer_orders",
            type: "numeric(18,2)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "original_total",
            schema: "pos",
            table: "customer_orders",
            type: "numeric(18,2)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<string>(
            name: "cancel_reason",
            schema: "pos",
            table: "customer_orders",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "cancel_notes",
            schema: "pos",
            table: "customer_orders",
            type: "character varying(512)",
            maxLength: 512,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "activity_events_json",
            schema: "pos",
            table: "customer_orders",
            type: "text",
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE pos.customer_orders
            SET original_merchandise_subtotal = merchandise_subtotal,
                original_total = total
            WHERE original_total = 0;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE pos.customer_order_lines
              DROP CONSTRAINT IF EXISTS ck_customer_order_lines_quantity_non_negative;
            ALTER TABLE pos.customer_order_lines
              DROP CONSTRAINT IF EXISTS ck_customer_order_lines_amounts_non_negative;
            """);

        migrationBuilder.DropColumn(name: "adjustment_note", schema: "pos", table: "customer_order_lines");
        migrationBuilder.DropColumn(name: "adjustment_reason", schema: "pos", table: "customer_order_lines");
        migrationBuilder.DropColumn(name: "original_discount", schema: "pos", table: "customer_order_lines");
        migrationBuilder.DropColumn(name: "original_quantity", schema: "pos", table: "customer_order_lines");

        migrationBuilder.DropColumn(name: "activity_events_json", schema: "pos", table: "customer_orders");
        migrationBuilder.DropColumn(name: "cancel_notes", schema: "pos", table: "customer_orders");
        migrationBuilder.DropColumn(name: "cancel_reason", schema: "pos", table: "customer_orders");
        migrationBuilder.DropColumn(name: "original_total", schema: "pos", table: "customer_orders");
        migrationBuilder.DropColumn(name: "original_merchandise_subtotal", schema: "pos", table: "customer_orders");

        migrationBuilder.Sql(
            """
            ALTER TABLE pos.customer_order_lines
              ADD CONSTRAINT ck_customer_order_lines_quantity_positive CHECK (quantity > 0);
            ALTER TABLE pos.customer_order_lines
              ADD CONSTRAINT ck_customer_order_lines_amounts_non_negative
              CHECK (unit_price >= 0 AND discount >= 0 AND line_total >= 0);
            """);
    }
}
