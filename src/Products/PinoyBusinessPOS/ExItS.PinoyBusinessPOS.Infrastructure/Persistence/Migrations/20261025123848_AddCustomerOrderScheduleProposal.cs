using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerOrderScheduleProposal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "confirmed_fulfillment_at_utc",
                schema: "pos",
                table: "customer_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "confirmed_fulfillment_local",
                schema: "pos",
                table: "customer_orders",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "confirmed_fulfillment_time_zone_id",
                schema: "pos",
                table: "customer_orders",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "pending_schedule_proposal_at_utc",
                schema: "pos",
                table: "customer_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "pending_schedule_proposal_by",
                schema: "pos",
                table: "customer_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "pending_schedule_proposal_id",
                schema: "pos",
                table: "customer_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pending_schedule_proposal_local",
                schema: "pos",
                table: "customer_orders",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pending_schedule_proposal_note",
                schema: "pos",
                table: "customer_orders",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "pending_schedule_proposal_proposed_at_utc",
                schema: "pos",
                table: "customer_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pending_schedule_proposal_time_zone_id",
                schema: "pos",
                table: "customer_orders",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "confirmed_fulfillment_at_utc",
                schema: "pos",
                table: "customer_orders");

            migrationBuilder.DropColumn(
                name: "confirmed_fulfillment_local",
                schema: "pos",
                table: "customer_orders");

            migrationBuilder.DropColumn(
                name: "confirmed_fulfillment_time_zone_id",
                schema: "pos",
                table: "customer_orders");

            migrationBuilder.DropColumn(
                name: "pending_schedule_proposal_at_utc",
                schema: "pos",
                table: "customer_orders");

            migrationBuilder.DropColumn(
                name: "pending_schedule_proposal_by",
                schema: "pos",
                table: "customer_orders");

            migrationBuilder.DropColumn(
                name: "pending_schedule_proposal_id",
                schema: "pos",
                table: "customer_orders");

            migrationBuilder.DropColumn(
                name: "pending_schedule_proposal_local",
                schema: "pos",
                table: "customer_orders");

            migrationBuilder.DropColumn(
                name: "pending_schedule_proposal_note",
                schema: "pos",
                table: "customer_orders");

            migrationBuilder.DropColumn(
                name: "pending_schedule_proposal_proposed_at_utc",
                schema: "pos",
                table: "customer_orders");

            migrationBuilder.DropColumn(
                name: "pending_schedule_proposal_time_zone_id",
                schema: "pos",
                table: "customer_orders");
        }
    }
}
