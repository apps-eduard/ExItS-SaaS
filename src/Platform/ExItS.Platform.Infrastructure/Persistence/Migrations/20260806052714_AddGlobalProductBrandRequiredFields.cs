using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGlobalProductBrandRequiredFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "brand",
                schema: "catalog",
                table: "global_products",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE catalog.global_products gp
                SET brand = LEFT(b.tag_value, 120)
                FROM (
                    SELECT p.id,
                           SUBSTRING(tag FROM 7) AS tag_value
                    FROM catalog.global_products p
                    CROSS JOIN LATERAL unnest(p.search_tags) AS tag
                    WHERE tag ILIKE 'brand:%'
                ) b
                WHERE gp.id = b.id
                  AND gp.brand IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_global_products_brand",
                schema: "catalog",
                table: "global_products",
                column: "brand");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_global_products_brand",
                schema: "catalog",
                table: "global_products");

            migrationBuilder.DropColumn(
                name: "brand",
                schema: "catalog",
                table: "global_products");
        }
    }
}
