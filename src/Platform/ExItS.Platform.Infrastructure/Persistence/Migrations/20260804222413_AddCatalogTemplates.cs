using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalog_templates",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    icon_reference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    primary_business_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    default_batch_size = table.Column<int>(type: "integer", nullable: false),
                    selection_mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    published_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_template_products",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    catalog_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    global_product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_featured = table.Column<bool>(type: "boolean", nullable: false),
                    is_first_batch = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_template_products", x => x.id);
                    table.ForeignKey(
                        name: "FK_catalog_template_products_catalog_templates_catalog_templat~",
                        column: x => x.catalog_template_id,
                        principalSchema: "catalog",
                        principalTable: "catalog_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_catalog_template_products_global_products_global_product_id",
                        column: x => x.global_product_id,
                        principalSchema: "catalog",
                        principalTable: "global_products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_catalog_template_products_product_id",
                schema: "catalog",
                table: "catalog_template_products",
                column: "global_product_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_template_products_template_sort",
                schema: "catalog",
                table: "catalog_template_products",
                columns: new[] { "catalog_template_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ux_catalog_template_products_template_product",
                schema: "catalog",
                table: "catalog_template_products",
                columns: new[] { "catalog_template_id", "global_product_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_catalog_templates_primary_business_type",
                schema: "catalog",
                table: "catalog_templates",
                column: "primary_business_type");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_templates_status",
                schema: "catalog",
                table: "catalog_templates",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ux_catalog_templates_slug",
                schema: "catalog",
                table: "catalog_templates",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_template_products",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "catalog_templates",
                schema: "catalog");
        }
    }
}
