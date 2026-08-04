using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGlobalProductCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "catalog");

            migrationBuilder.CreateTable(
                name: "global_categories",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    icon_reference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_global_categories", x => x.id);
                    table.ForeignKey(
                        name: "FK_global_categories_global_categories_parent_id",
                        column: x => x.parent_id,
                        principalSchema: "catalog",
                        principalTable: "global_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "global_category_business_types",
                schema: "catalog",
                columns: table => new
                {
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_global_category_business_types", x => new { x.category_id, x.business_type });
                    table.ForeignKey(
                        name: "FK_global_category_business_types_global_categories_category_id",
                        column: x => x.category_id,
                        principalSchema: "catalog",
                        principalTable: "global_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "global_products",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    sku = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    barcode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    global_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    unit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    suggested_price = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    suggested_cost = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    image_reference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    search_tags = table.Column<string[]>(type: "text[]", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_global_products", x => x.id);
                    table.ForeignKey(
                        name: "FK_global_products_global_categories_global_category_id",
                        column: x => x.global_category_id,
                        principalSchema: "catalog",
                        principalTable: "global_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "global_product_business_types",
                schema: "catalog",
                columns: table => new
                {
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_global_product_business_types", x => new { x.product_id, x.business_type });
                    table.ForeignKey(
                        name: "FK_global_product_business_types_global_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "catalog",
                        principalTable: "global_products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_global_categories_parent_id",
                schema: "catalog",
                table: "global_categories",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_global_categories_status",
                schema: "catalog",
                table: "global_categories",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ux_global_categories_normalized_name_parent",
                schema: "catalog",
                table: "global_categories",
                columns: new[] { "normalized_name", "parent_id" },
                unique: true,
                filter: "parent_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_global_categories_normalized_name_root",
                schema: "catalog",
                table: "global_categories",
                column: "normalized_name",
                unique: true,
                filter: "parent_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_global_category_business_types_type",
                schema: "catalog",
                table: "global_category_business_types",
                column: "business_type");

            migrationBuilder.CreateIndex(
                name: "ix_global_product_business_types_type",
                schema: "catalog",
                table: "global_product_business_types",
                column: "business_type");

            migrationBuilder.CreateIndex(
                name: "ix_global_products_category_id",
                schema: "catalog",
                table: "global_products",
                column: "global_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_global_products_name",
                schema: "catalog",
                table: "global_products",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_global_products_status",
                schema: "catalog",
                table: "global_products",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ux_global_products_barcode",
                schema: "catalog",
                table: "global_products",
                column: "barcode",
                unique: true,
                filter: "barcode IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_global_products_sku",
                schema: "catalog",
                table: "global_products",
                column: "sku",
                unique: true,
                filter: "sku IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "global_category_business_types",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "global_product_business_types",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "global_products",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "global_categories",
                schema: "catalog");
        }
    }
}
