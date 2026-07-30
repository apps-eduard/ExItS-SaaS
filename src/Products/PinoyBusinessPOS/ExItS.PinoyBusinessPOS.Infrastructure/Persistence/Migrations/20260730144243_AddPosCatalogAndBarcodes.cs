using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPosCatalogAndBarcodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "product_categories",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_categories", x => x.id);
                    table.CheckConstraint("ck_product_categories_status", "status IN ('Active', 'Inactive')");
                });

            migrationBuilder.CreateTable(
                name: "products",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    sku = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    normalized_sku = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    barcode = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    unit_of_measure = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    selling_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.id);
                    table.CheckConstraint("ck_products_barcode_digits", "barcode IS NULL OR barcode ~ '^[0-9]{8,14}$'");
                    table.CheckConstraint("ck_products_selling_price_non_negative", "selling_price >= 0");
                    table.CheckConstraint("ck_products_status", "status IN ('Active', 'Inactive')");
                    table.CheckConstraint("ck_products_unit_of_measure", "unit_of_measure IN ('Piece', 'Pack', 'Box', 'Bottle', 'Can', 'Sachet', 'Kilogram', 'Gram', 'Liter', 'Milliliter', 'Meter')");
                    table.ForeignKey(
                        name: "fk_products_product_categories",
                        column: x => x.category_id,
                        principalSchema: "pos",
                        principalTable: "product_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_product_categories_org_name",
                schema: "pos",
                table: "product_categories",
                columns: new[] { "organization_id", "name" });

            migrationBuilder.CreateIndex(
                name: "ux_product_categories_org_active_name",
                schema: "pos",
                table: "product_categories",
                columns: new[] { "organization_id", "normalized_name" },
                unique: true,
                filter: "status = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_products_category_id",
                schema: "pos",
                table: "products",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_org_category",
                schema: "pos",
                table: "products",
                columns: new[] { "organization_id", "category_id" });

            migrationBuilder.CreateIndex(
                name: "ix_products_org_name",
                schema: "pos",
                table: "products",
                columns: new[] { "organization_id", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_products_org_status",
                schema: "pos",
                table: "products",
                columns: new[] { "organization_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_products_org_barcode",
                schema: "pos",
                table: "products",
                columns: new[] { "organization_id", "barcode" },
                unique: true,
                filter: "barcode IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_products_org_normalized_sku",
                schema: "pos",
                table: "products",
                columns: new[] { "organization_id", "normalized_sku" },
                unique: true,
                filter: "normalized_sku IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "products",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "product_categories",
                schema: "pos");
        }
    }
}
