using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameGlobalProductPriceColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "suggested_price",
                schema: "catalog",
                table: "global_products",
                newName: "selling_price");

            migrationBuilder.RenameColumn(
                name: "suggested_cost",
                schema: "catalog",
                table: "global_products",
                newName: "cost_price");

            migrationBuilder.RenameColumn(
                name: "suggested_price",
                schema: "catalog",
                table: "catalog_import_items",
                newName: "selling_price");

            migrationBuilder.RenameColumn(
                name: "suggested_cost",
                schema: "catalog",
                table: "catalog_import_items",
                newName: "cost_price");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "selling_price",
                schema: "catalog",
                table: "global_products",
                newName: "suggested_price");

            migrationBuilder.RenameColumn(
                name: "cost_price",
                schema: "catalog",
                table: "global_products",
                newName: "suggested_cost");

            migrationBuilder.RenameColumn(
                name: "selling_price",
                schema: "catalog",
                table: "catalog_import_items",
                newName: "suggested_price");

            migrationBuilder.RenameColumn(
                name: "cost_price",
                schema: "catalog",
                table: "catalog_import_items",
                newName: "suggested_cost");
        }
    }
}
