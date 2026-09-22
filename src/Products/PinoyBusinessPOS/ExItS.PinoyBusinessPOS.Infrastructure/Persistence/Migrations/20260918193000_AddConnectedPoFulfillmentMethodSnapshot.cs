using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(PosDbContext))]
    [Migration("20260918193000_AddConnectedPoFulfillmentMethodSnapshot")]
    public partial class AddConnectedPoFulfillmentMethodSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "fulfillment_method",
                schema: "pos",
                table: "purchase_orders",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "fulfillment_method",
                schema: "pos",
                table: "connected_purchase_orders",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "confirmed_fulfillment_method",
                schema: "pos",
                table: "connected_purchase_orders",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "fulfillment_method",
                schema: "pos",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "confirmed_fulfillment_method",
                schema: "pos",
                table: "connected_purchase_orders");

            migrationBuilder.DropColumn(
                name: "fulfillment_method",
                schema: "pos",
                table: "connected_purchase_orders");
        }
    }
}
