using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConnectedPoLifecycleAndReceivingDiscrepancies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_goods_receipt_lines_received_qty_positive",
                schema: "pos",
                table: "goods_receipt_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_connected_purchase_orders_status",
                schema: "pos",
                table: "connected_purchase_orders");

            migrationBuilder.AddColumn<decimal>(
                name: "closed_short_qty",
                schema: "pos",
                table: "purchase_order_lines",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "damaged_qty",
                schema: "pos",
                table: "goods_receipt_lines",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "discrepancy_kind",
                schema: "pos",
                table: "goods_receipt_lines",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<string>(
                name: "discrepancy_note",
                schema: "pos",
                table: "goods_receipt_lines",
                type: "character varying(280)",
                maxLength: 280,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "rejected_qty",
                schema: "pos",
                table: "goods_receipt_lines",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "short_closed_qty",
                schema: "pos",
                table: "goods_receipt_lines",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "decline_note",
                schema: "pos",
                table: "connected_purchase_orders",
                type: "character varying(280)",
                maxLength: 280,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "decline_reason",
                schema: "pos",
                table: "connected_purchase_orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "fulfilled_at_utc",
                schema: "pos",
                table: "connected_purchase_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "preparing_at_utc",
                schema: "pos",
                table: "connected_purchase_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "withdrawn_at_utc",
                schema: "pos",
                table: "connected_purchase_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_purchase_order_lines_closed_short_qty_nonnegative",
                schema: "pos",
                table: "purchase_order_lines",
                sql: "closed_short_qty >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_goods_receipt_lines_activity_positive",
                schema: "pos",
                table: "goods_receipt_lines",
                sql: "(received_qty + damaged_qty + rejected_qty + short_closed_qty) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_goods_receipt_lines_damaged_qty_nonnegative",
                schema: "pos",
                table: "goods_receipt_lines",
                sql: "damaged_qty >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_goods_receipt_lines_received_qty_nonnegative",
                schema: "pos",
                table: "goods_receipt_lines",
                sql: "received_qty >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_goods_receipt_lines_rejected_qty_nonnegative",
                schema: "pos",
                table: "goods_receipt_lines",
                sql: "rejected_qty >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_goods_receipt_lines_short_closed_qty_nonnegative",
                schema: "pos",
                table: "goods_receipt_lines",
                sql: "short_closed_qty >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_connected_purchase_orders_status",
                schema: "pos",
                table: "connected_purchase_orders",
                sql: "status BETWEEN 0 AND 5");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_purchase_order_lines_closed_short_qty_nonnegative",
                schema: "pos",
                table: "purchase_order_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_goods_receipt_lines_activity_positive",
                schema: "pos",
                table: "goods_receipt_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_goods_receipt_lines_damaged_qty_nonnegative",
                schema: "pos",
                table: "goods_receipt_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_goods_receipt_lines_received_qty_nonnegative",
                schema: "pos",
                table: "goods_receipt_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_goods_receipt_lines_rejected_qty_nonnegative",
                schema: "pos",
                table: "goods_receipt_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_goods_receipt_lines_short_closed_qty_nonnegative",
                schema: "pos",
                table: "goods_receipt_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_connected_purchase_orders_status",
                schema: "pos",
                table: "connected_purchase_orders");

            migrationBuilder.DropColumn(
                name: "closed_short_qty",
                schema: "pos",
                table: "purchase_order_lines");

            migrationBuilder.DropColumn(
                name: "damaged_qty",
                schema: "pos",
                table: "goods_receipt_lines");

            migrationBuilder.DropColumn(
                name: "discrepancy_kind",
                schema: "pos",
                table: "goods_receipt_lines");

            migrationBuilder.DropColumn(
                name: "discrepancy_note",
                schema: "pos",
                table: "goods_receipt_lines");

            migrationBuilder.DropColumn(
                name: "rejected_qty",
                schema: "pos",
                table: "goods_receipt_lines");

            migrationBuilder.DropColumn(
                name: "short_closed_qty",
                schema: "pos",
                table: "goods_receipt_lines");

            migrationBuilder.DropColumn(
                name: "decline_note",
                schema: "pos",
                table: "connected_purchase_orders");

            migrationBuilder.DropColumn(
                name: "decline_reason",
                schema: "pos",
                table: "connected_purchase_orders");

            migrationBuilder.DropColumn(
                name: "fulfilled_at_utc",
                schema: "pos",
                table: "connected_purchase_orders");

            migrationBuilder.DropColumn(
                name: "preparing_at_utc",
                schema: "pos",
                table: "connected_purchase_orders");

            migrationBuilder.DropColumn(
                name: "withdrawn_at_utc",
                schema: "pos",
                table: "connected_purchase_orders");

            migrationBuilder.AddCheckConstraint(
                name: "ck_goods_receipt_lines_received_qty_positive",
                schema: "pos",
                table: "goods_receipt_lines",
                sql: "received_qty > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_connected_purchase_orders_status",
                schema: "pos",
                table: "connected_purchase_orders",
                sql: "status BETWEEN 0 AND 2");
        }
    }
}
