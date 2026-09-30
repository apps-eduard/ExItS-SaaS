using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryLotIdentityCorrections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inventory_lot_identity_corrections",
                schema: "pos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inventory_lot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    old_expiration_date = table.Column<DateOnly>(type: "date", nullable: false),
                    new_expiration_date = table.Column<DateOnly>(type: "date", nullable: false),
                    old_lot_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    new_lot_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    corrected_by = table.Column<Guid>(type: "uuid", nullable: false),
                    corrected_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_lot_identity_corrections", x => x.id);
                    table.ForeignKey(
                        name: "fk_inventory_lot_identity_corrections_lots",
                        column: x => x.inventory_lot_id,
                        principalSchema: "pos",
                        principalTable: "inventory_lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_lot_identity_corrections_inventory_lot_id",
                schema: "pos",
                table: "inventory_lot_identity_corrections",
                column: "inventory_lot_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_lot_identity_corrections_org_lot_corrected",
                schema: "pos",
                table: "inventory_lot_identity_corrections",
                columns: new[] { "organization_id", "inventory_lot_id", "corrected_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_lot_identity_corrections",
                schema: "pos");
        }
    }
}
