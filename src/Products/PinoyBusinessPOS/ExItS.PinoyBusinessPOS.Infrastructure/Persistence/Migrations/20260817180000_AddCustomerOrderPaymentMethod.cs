using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(PosDbContext))]
    [Migration("20260817180000_AddCustomerOrderPaymentMethod")]
    public partial class AddCustomerOrderPaymentMethod : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "payment_method",
                schema: "pos",
                table: "customer_orders",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Cash");

            migrationBuilder.AddCheckConstraint(
                name: "ck_customer_orders_payment_method",
                schema: "pos",
                table: "customer_orders",
                sql: "payment_method IN ('Cash', 'ManualGCash', 'Utang')");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_customer_orders_payment_method",
                schema: "pos",
                table: "customer_orders");

            migrationBuilder.DropColumn(
                name: "payment_method",
                schema: "pos",
                table: "customer_orders");
        }
    }
}
