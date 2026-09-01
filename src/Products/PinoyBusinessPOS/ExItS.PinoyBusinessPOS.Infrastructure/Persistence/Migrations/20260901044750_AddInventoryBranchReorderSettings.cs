using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryBranchReorderSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inventory_branch_reorder_settings",
                schema: "pos",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reorder_level = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    reorder_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_branch_reorder_settings", x => new { x.organization_id, x.branch_id, x.product_id });
                    table.ForeignKey(
                        name: "fk_inventory_branch_reorder_settings_products",
                        column: x => x.product_id,
                        principalSchema: "pos",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_branch_reorder_settings_product_id",
                schema: "pos",
                table: "inventory_branch_reorder_settings",
                column: "product_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_branch_reorder_settings",
                schema: "pos");
        }
    }
}
