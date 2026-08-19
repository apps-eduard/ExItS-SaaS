using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationalActorTraceabilityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                schema: "pos",
                table: "stock_counts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "provider_finalized_by_system",
                schema: "pos",
                table: "payment_attempts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "collected_at_utc",
                schema: "pos",
                table: "customer_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "collected_by",
                schema: "pos",
                table: "customer_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "delivered_at_utc",
                schema: "pos",
                table: "customer_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "delivered_by",
                schema: "pos",
                table: "customer_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "out_for_delivery_at_utc",
                schema: "pos",
                table: "customer_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "out_for_delivery_by",
                schema: "pos",
                table: "customer_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ready_at_utc",
                schema: "pos",
                table: "customer_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ready_by",
                schema: "pos",
                table: "customer_orders",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_by",
                schema: "pos",
                table: "stock_counts");

            migrationBuilder.DropColumn(
                name: "provider_finalized_by_system",
                schema: "pos",
                table: "payment_attempts");

            migrationBuilder.DropColumn(
                name: "collected_at_utc",
                schema: "pos",
                table: "customer_orders");

            migrationBuilder.DropColumn(
                name: "collected_by",
                schema: "pos",
                table: "customer_orders");

            migrationBuilder.DropColumn(
                name: "delivered_at_utc",
                schema: "pos",
                table: "customer_orders");

            migrationBuilder.DropColumn(
                name: "delivered_by",
                schema: "pos",
                table: "customer_orders");

            migrationBuilder.DropColumn(
                name: "out_for_delivery_at_utc",
                schema: "pos",
                table: "customer_orders");

            migrationBuilder.DropColumn(
                name: "out_for_delivery_by",
                schema: "pos",
                table: "customer_orders");

            migrationBuilder.DropColumn(
                name: "ready_at_utc",
                schema: "pos",
                table: "customer_orders");

            migrationBuilder.DropColumn(
                name: "ready_by",
                schema: "pos",
                table: "customer_orders");
        }
    }
}
