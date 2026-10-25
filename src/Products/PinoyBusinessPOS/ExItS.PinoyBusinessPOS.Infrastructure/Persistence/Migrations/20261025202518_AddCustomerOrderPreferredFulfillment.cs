using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerOrderPreferredFulfillment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "preferred_fulfillment_date",
                schema: "pos",
                table: "customer_orders",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "preferred_fulfillment_period",
                schema: "pos",
                table: "customer_orders",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "preferred_fulfillment_time_zone_id",
                schema: "pos",
                table: "customer_orders",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "preferred_fulfillment_window_end_utc",
                schema: "pos",
                table: "customer_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "preferred_fulfillment_window_start_utc",
                schema: "pos",
                table: "customer_orders",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "preferred_fulfillment_date",
                schema: "pos",
                table: "customer_orders");

            migrationBuilder.DropColumn(
                name: "preferred_fulfillment_period",
                schema: "pos",
                table: "customer_orders");

            migrationBuilder.DropColumn(
                name: "preferred_fulfillment_time_zone_id",
                schema: "pos",
                table: "customer_orders");

            migrationBuilder.DropColumn(
                name: "preferred_fulfillment_window_end_utc",
                schema: "pos",
                table: "customer_orders");

            migrationBuilder.DropColumn(
                name: "preferred_fulfillment_window_start_utc",
                schema: "pos",
                table: "customer_orders");
        }
    }
}
