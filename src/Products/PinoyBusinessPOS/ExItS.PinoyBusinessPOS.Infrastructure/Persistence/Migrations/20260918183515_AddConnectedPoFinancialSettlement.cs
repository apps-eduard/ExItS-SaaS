using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConnectedPoFinancialSettlement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "financial_settlement_status",
                schema: "pos",
                table: "purchase_orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "financially_settled_at_utc",
                schema: "pos",
                table: "purchase_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "financially_settled_by",
                schema: "pos",
                table: "purchase_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "seller_settlement_remarks",
                schema: "pos",
                table: "purchase_orders",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "financial_settlement_status",
                schema: "pos",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "financially_settled_at_utc",
                schema: "pos",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "financially_settled_by",
                schema: "pos",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "seller_settlement_remarks",
                schema: "pos",
                table: "purchase_orders");
        }
    }
}
