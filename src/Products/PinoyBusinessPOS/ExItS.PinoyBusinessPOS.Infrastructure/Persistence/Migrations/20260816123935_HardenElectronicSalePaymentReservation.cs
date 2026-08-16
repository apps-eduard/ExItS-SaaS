using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenElectronicSalePaymentReservation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "stock_reservation_state",
                schema: "pos",
                table: "sales",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_stock_reservation",
                schema: "pos",
                table: "sales",
                sql: "stock_reservation_state IN ('None', 'Reserved', 'Released', 'Consumed')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_stock_reservation",
                schema: "pos",
                table: "sales");

            migrationBuilder.DropColumn(
                name: "stock_reservation_state",
                schema: "pos",
                table: "sales");
        }
    }
}
