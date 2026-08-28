using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductBrands : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "brand_id",
                schema: "pos",
                table: "products",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "product_brands",
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
                    table.PrimaryKey("PK_product_brands", x => x.id);
                    table.CheckConstraint("ck_product_brands_status", "status IN ('Active', 'Inactive')");
                });

            migrationBuilder.CreateIndex(
                name: "IX_products_brand_id",
                schema: "pos",
                table: "products",
                column: "brand_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_org_brand",
                schema: "pos",
                table: "products",
                columns: new[] { "organization_id", "brand_id" });

            migrationBuilder.CreateIndex(
                name: "ix_product_brands_org_name",
                schema: "pos",
                table: "product_brands",
                columns: new[] { "organization_id", "name" });

            migrationBuilder.CreateIndex(
                name: "ux_product_brands_org_active_name",
                schema: "pos",
                table: "product_brands",
                columns: new[] { "organization_id", "normalized_name" },
                unique: true,
                filter: "status = 'Active'");

            migrationBuilder.AddForeignKey(
                name: "fk_products_product_brands",
                schema: "pos",
                table: "products",
                column: "brand_id",
                principalSchema: "pos",
                principalTable: "product_brands",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_products_product_brands",
                schema: "pos",
                table: "products");

            migrationBuilder.DropTable(
                name: "product_brands",
                schema: "pos");

            migrationBuilder.DropIndex(
                name: "IX_products_brand_id",
                schema: "pos",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_products_org_brand",
                schema: "pos",
                table: "products");

            migrationBuilder.DropColumn(
                name: "brand_id",
                schema: "pos",
                table: "products");
        }
    }
}
