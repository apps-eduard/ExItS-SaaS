using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerOrderPaymentConfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "amount_received",
                schema: "pos",
                table: "customer_orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "payment_confirmed_at_utc",
                schema: "pos",
                table: "customer_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "payment_confirmed_by",
                schema: "pos",
                table: "customer_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_customer_orders_payment_confirmation",
                schema: "pos",
                table: "customer_orders",
                sql: "(payment_status <> 'Paid' AND amount_received IS NULL AND payment_confirmed_at_utc IS NULL AND payment_confirmed_by IS NULL) OR (payment_status = 'Paid' AND amount_received IS NOT NULL AND amount_received >= total AND payment_confirmed_at_utc IS NOT NULL AND payment_confirmed_by IS NOT NULL AND (payment_method <> 'ManualGCash' OR amount_received = total))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_customer_orders_payment_confirmation",
                schema: "pos",
                table: "customer_orders");

            migrationBuilder.DropColumn(
                name: "amount_received",
                schema: "pos",
                table: "customer_orders");

            migrationBuilder.DropColumn(
                name: "payment_confirmed_at_utc",
                schema: "pos",
                table: "customer_orders");

            migrationBuilder.DropColumn(
                name: "payment_confirmed_by",
                schema: "pos",
                table: "customer_orders");
        }
    }
}
