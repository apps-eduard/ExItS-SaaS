using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGlobalProductSellingMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "selling_mode",
                schema: "catalog",
                table: "global_products",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "PerItem");

            migrationBuilder.AddCheckConstraint(
                name: "ck_global_products_selling_mode",
                schema: "catalog",
                table: "global_products",
                sql: "selling_mode IN ('PerItem', 'ByWeight')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_global_products_selling_mode_unit",
                schema: "catalog",
                table: "global_products",
                sql: "selling_mode <> 'ByWeight' OR unit = 'Kilogram'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_global_products_selling_mode",
                schema: "catalog",
                table: "global_products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_global_products_selling_mode_unit",
                schema: "catalog",
                table: "global_products");

            migrationBuilder.DropColumn(
                name: "selling_mode",
                schema: "catalog",
                table: "global_products");
        }
    }
}
