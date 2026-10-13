using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerOrderRequestedPickup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "requested_pickup_at_utc",
                schema: "pos",
                table: "customer_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "requested_pickup_local",
                schema: "pos",
                table: "customer_orders",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "requested_pickup_time_zone_id",
                schema: "pos",
                table: "customer_orders",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_customer_orders_pickup_request",
                schema: "pos",
                table: "customer_orders",
                sql: "(requested_pickup_local IS NULL AND requested_pickup_time_zone_id IS NULL AND requested_pickup_at_utc IS NULL) OR (fulfillment_type = 'Pickup' AND requested_pickup_local IS NOT NULL AND requested_pickup_time_zone_id IS NOT NULL AND requested_pickup_at_utc IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_customer_orders_pickup_request",
                schema: "pos",
                table: "customer_orders");

            migrationBuilder.DropColumn(
                name: "requested_pickup_at_utc",
                schema: "pos",
                table: "customer_orders");

            migrationBuilder.DropColumn(
                name: "requested_pickup_local",
                schema: "pos",
                table: "customer_orders");

            migrationBuilder.DropColumn(
                name: "requested_pickup_time_zone_id",
                schema: "pos",
                table: "customer_orders");
        }
    }
}
