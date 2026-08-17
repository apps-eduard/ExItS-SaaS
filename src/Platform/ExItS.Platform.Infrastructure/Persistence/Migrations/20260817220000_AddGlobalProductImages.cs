using ExItS.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(PlatformDbContext))]
    [Migration("20260817220000_AddGlobalProductImages")]
    public partial class AddGlobalProductImages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "global_product_images",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    global_product_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_global_product_images", x => x.id);
                    table.CheckConstraint("ck_global_product_images_version_positive", "version >= 1");
                    table.CheckConstraint(
                        "ck_global_product_images_dimensions_positive",
                        "thumb_width > 0 AND thumb_height > 0 AND medium_width > 0 AND medium_height > 0");
                    table.ForeignKey(
                        name: "fk_global_product_images_global_products",
                        column: x => x.global_product_id,
                        principalSchema: "catalog",
                        principalTable: "global_products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_global_product_images_product",
                schema: "catalog",
                table: "global_product_images",
                column: "global_product_id",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "global_product_images",
                schema: "catalog");
        }
    }
}
