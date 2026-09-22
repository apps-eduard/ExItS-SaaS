using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(PosDbContext))]
    [Migration("20260919083000_AddOrgBranchFulfillmentDefaults")]
    public partial class AddOrgBranchFulfillmentDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Safe defaults = false preserves current branch-create behavior.
            // Existing branch Pickup/Delivery/OnlineOrders values are never mass-updated.
            migrationBuilder.AddColumn<bool>(
                name: "default_pickup_enabled",
                schema: "pos",
                table: "organization_fulfillment_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "default_delivery_enabled",
                schema: "pos",
                table: "organization_fulfillment_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "default_online_orders_enabled",
                schema: "pos",
                table: "organization_fulfillment_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "default_pickup_enabled",
                schema: "pos",
                table: "organization_fulfillment_settings");

            migrationBuilder.DropColumn(
                name: "default_delivery_enabled",
                schema: "pos",
                table: "organization_fulfillment_settings");

            migrationBuilder.DropColumn(
                name: "default_online_orders_enabled",
                schema: "pos",
                table: "organization_fulfillment_settings");
        }
    }
}
