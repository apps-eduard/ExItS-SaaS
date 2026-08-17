using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(PosDbContext))]
    [Migration("20260817200000_AddProductImages")]
    public partial class AddProductImages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "product_images",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    storage_key = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    thumb_width = table.Column<int>(type: "integer", nullable: false),
                    thumb_height = table.Column<int>(type: "integer", nullable: false),
                    medium_width = table.Column<int>(type: "integer", nullable: false),
                    medium_height = table.Column<int>(type: "integer", nullable: false),
                    content_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_images", x => x.id);
                    table.CheckConstraint("ck_product_images_version_positive", "version >= 1");
                    table.CheckConstraint(
                        "ck_product_images_dimensions_positive",
                        "thumb_width > 0 AND thumb_height > 0 AND medium_width > 0 AND medium_height > 0");
                    table.ForeignKey(
                        name: "fk_product_images_products",
                        columns: x => new { x.product_id, x.organization_id },
                        principalSchema: "pos",
                        principalTable: "products",
                        principalColumns: new[] { "id", "organization_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_product_images_org_product",
                schema: "pos",
                table: "product_images",
                columns: new[] { "organization_id", "product_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_images_product_id_organization_id",
                schema: "pos",
                table: "product_images",
                columns: new[] { "product_id", "organization_id" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_images",
                schema: "pos");
        }
    }
}
