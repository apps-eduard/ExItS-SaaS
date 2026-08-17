using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(PosDbContext))]
    [Migration("20260817210000_AddProductPlatformBarcodeAndImageVersion")]
    public partial class AddProductPlatformBarcodeAndImageVersion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "platform_barcode",
                schema: "pos",
                table: "products",
                type: "character varying(14)",
                maxLength: 14,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "platform_image_version",
                schema: "pos",
                table: "products",
                type: "integer",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "platform_barcode",
                schema: "pos",
                table: "products");

            migrationBuilder.DropColumn(
                name: "platform_image_version",
                schema: "pos",
                table: "products");
        }
    }
}
