using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogProductNormalizedNameIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Add nullable column for backfill.
            migrationBuilder.AddColumn<string>(
                name: "normalized_name",
                schema: "pos",
                table: "products",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            // 2) Backfill with SQL equivalent of CatalogProduct.NormalizeProductName:
            //    NFC → trim → collapse whitespace → uppercase invariant.
            migrationBuilder.Sql(
                """
                UPDATE pos.products
                SET normalized_name = upper(
                    regexp_replace(
                        btrim(normalize(name, NFC)),
                        E'\\s+',
                        ' ',
                        'g'))
                WHERE normalized_name IS NULL;
                """);

            // 3) Abort clearly if unresolved duplicate groups exist (no auto-merge).
            migrationBuilder.Sql(
                """
                DO $guard$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM (
                            SELECT organization_id, normalized_name
                            FROM pos.products
                            WHERE normalized_name IS NOT NULL
                            GROUP BY organization_id, normalized_name
                            HAVING COUNT(*) > 1
                        ) duplicates
                    ) THEN
                        RAISE EXCEPTION
                            'MB2-01C-H1: unresolved duplicate normalized product names detected. Auto-merge is forbidden. Report OrganizationId/NormalizedName/ProductIds and STOP.';
                    END IF;
                END
                $guard$;
                """);

            // 4) NOT NULL
            migrationBuilder.AlterColumn<string>(
                name: "normalized_name",
                schema: "pos",
                table: "products",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            // 5) Organization-wide unique identity (Active+Inactive, Standard+Local).
            migrationBuilder.CreateIndex(
                name: "ux_products_org_normalized_name",
                schema: "pos",
                table: "products",
                columns: new[] { "organization_id", "normalized_name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_products_org_normalized_name",
                schema: "pos",
                table: "products");

            migrationBuilder.DropColumn(
                name: "normalized_name",
                schema: "pos",
                table: "products");
        }
    }
}
