using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(PosDbContext))]
    [Migration("20260919220000_AddBuyerPrepaymentProof")]
    public partial class AddBuyerPrepaymentProof : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "buyer_prepayment_submitted_at_utc",
                schema: "pos",
                table: "purchase_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "buyer_prepayment_method",
                schema: "pos",
                table: "purchase_orders",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "buyer_prepayment_reference",
                schema: "pos",
                table: "purchase_orders",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "buyer_prepayment_details",
                schema: "pos",
                table: "purchase_orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "buyer_prepayment_submitted_at_utc",
                schema: "pos",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "buyer_prepayment_method",
                schema: "pos",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "buyer_prepayment_reference",
                schema: "pos",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "buyer_prepayment_details",
                schema: "pos",
                table: "purchase_orders");
        }
    }
}
