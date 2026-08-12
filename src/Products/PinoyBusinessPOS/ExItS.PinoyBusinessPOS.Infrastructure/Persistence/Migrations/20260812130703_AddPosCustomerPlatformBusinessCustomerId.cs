using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPosCustomerPlatformBusinessCustomerId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "platform_business_customer_id",
                schema: "pos",
                table: "customers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_customers_org_platform_business_customer",
                schema: "pos",
                table: "customers",
                columns: new[] { "organization_id", "platform_business_customer_id" },
                unique: true,
                filter: "platform_business_customer_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_customers_org_platform_business_customer",
                schema: "pos",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "platform_business_customer_id",
                schema: "pos",
                table: "customers");
        }
    }
}
