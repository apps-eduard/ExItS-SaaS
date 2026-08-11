using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPosProductSellingMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "selling_mode",
                schema: "pos",
                table: "products",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "PerItem");

            migrationBuilder.AddColumn<string>(
                name: "selling_mode",
                schema: "pos",
                table: "catalog_import_items",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "PerItem");

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_selling_mode",
                schema: "pos",
                table: "products",
                sql: "selling_mode IN ('PerItem','ByWeight')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_selling_mode_unit",
                schema: "pos",
                table: "products",
                sql: "selling_mode <> 'ByWeight' OR unit_of_measure = 'Kilogram'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_import_items_selling_mode",
                schema: "pos",
                table: "catalog_import_items",
                sql: "selling_mode IN ('PerItem','ByWeight')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_products_selling_mode",
                schema: "pos",
                table: "products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_products_selling_mode_unit",
                schema: "pos",
                table: "products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_import_items_selling_mode",
                schema: "pos",
                table: "catalog_import_items");

            migrationBuilder.DropColumn(
                name: "selling_mode",
                schema: "pos",
                table: "products");

            migrationBuilder.DropColumn(
                name: "selling_mode",
                schema: "pos",
                table: "catalog_import_items");
        }
    }
}
