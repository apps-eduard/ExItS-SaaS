using System;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(PosDbContext))]
    [Migration("20260817120000_AddConnectedPoConfirmationAndPaymentTerms")]
    public partial class AddConnectedPoConfirmationAndPaymentTerms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_connected_purchase_orders_status",
                schema: "pos",
                table: "connected_purchase_orders");

            migrationBuilder.AddColumn<int>(
                name: "payment_term",
                schema: "pos",
                table: "purchase_orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "payment_term",
                schema: "pos",
                table: "connected_purchase_orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "changes_proposed_at_utc",
                schema: "pos",
                table: "connected_purchase_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "changes_proposed_by_user_id",
                schema: "pos",
                table: "connected_purchase_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "buyer_responded_at_utc",
                schema: "pos",
                table: "connected_purchase_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "buyer_responded_by_user_id",
                schema: "pos",
                table: "connected_purchase_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "proposed_qty",
                schema: "pos",
                table: "connected_purchase_order_lines",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "confirmed_qty",
                schema: "pos",
                table: "connected_purchase_order_lines",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "availability",
                schema: "pos",
                table: "connected_purchase_order_lines",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                UPDATE pos.connected_purchase_order_lines AS line
                SET confirmed_qty = line.qty,
                    proposed_qty = line.qty,
                    availability = 1
                FROM pos.connected_purchase_orders AS po
                WHERE line.connected_purchase_order_id = po.id
                  AND po.status IN (1, 3, 4);
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_purchase_orders_payment_term",
                schema: "pos",
                table: "purchase_orders",
                sql: "payment_term BETWEEN 0 AND 2");

            migrationBuilder.AddCheckConstraint(
                name: "ck_connected_purchase_orders_status",
                schema: "pos",
                table: "connected_purchase_orders",
                sql: "status BETWEEN 0 AND 6");

            migrationBuilder.AddCheckConstraint(
                name: "ck_connected_purchase_orders_payment_term",
                schema: "pos",
                table: "connected_purchase_orders",
                sql: "payment_term BETWEEN 0 AND 2");

            migrationBuilder.AddCheckConstraint(
                name: "ck_connected_po_lines_proposed_qty",
                schema: "pos",
                table: "connected_purchase_order_lines",
                sql: "proposed_qty IS NULL OR (proposed_qty >= 0 AND proposed_qty <= qty)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_connected_po_lines_confirmed_qty",
                schema: "pos",
                table: "connected_purchase_order_lines",
                sql: "confirmed_qty IS NULL OR (confirmed_qty >= 0 AND confirmed_qty <= qty)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_connected_po_lines_availability",
                schema: "pos",
                table: "connected_purchase_order_lines",
                sql: "availability BETWEEN 0 AND 2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_purchase_orders_payment_term",
                schema: "pos",
                table: "purchase_orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_connected_purchase_orders_status",
                schema: "pos",
                table: "connected_purchase_orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_connected_purchase_orders_payment_term",
                schema: "pos",
                table: "connected_purchase_orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_connected_po_lines_proposed_qty",
                schema: "pos",
                table: "connected_purchase_order_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_connected_po_lines_confirmed_qty",
                schema: "pos",
                table: "connected_purchase_order_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_connected_po_lines_availability",
                schema: "pos",
                table: "connected_purchase_order_lines");

            migrationBuilder.DropColumn(
                name: "payment_term",
                schema: "pos",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "payment_term",
                schema: "pos",
                table: "connected_purchase_orders");

            migrationBuilder.DropColumn(
                name: "changes_proposed_at_utc",
                schema: "pos",
                table: "connected_purchase_orders");

            migrationBuilder.DropColumn(
                name: "changes_proposed_by_user_id",
                schema: "pos",
                table: "connected_purchase_orders");

            migrationBuilder.DropColumn(
                name: "buyer_responded_at_utc",
                schema: "pos",
                table: "connected_purchase_orders");

            migrationBuilder.DropColumn(
                name: "buyer_responded_by_user_id",
                schema: "pos",
                table: "connected_purchase_orders");

            migrationBuilder.DropColumn(
                name: "proposed_qty",
                schema: "pos",
                table: "connected_purchase_order_lines");

            migrationBuilder.DropColumn(
                name: "confirmed_qty",
                schema: "pos",
                table: "connected_purchase_order_lines");

            migrationBuilder.DropColumn(
                name: "availability",
                schema: "pos",
                table: "connected_purchase_order_lines");

            migrationBuilder.AddCheckConstraint(
                name: "ck_connected_purchase_orders_status",
                schema: "pos",
                table: "connected_purchase_orders",
                sql: "status BETWEEN 0 AND 5");
        }
    }
}
