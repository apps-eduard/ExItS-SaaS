using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Opening stock is location-scoped: one OpeningStock movement per product per branch.
/// Legacy NULL-branch OpeningStock remains unique per product (primary-branch compatibility).
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260905210000_ScopeOpeningStockUniqueByBranch")]
public partial class ScopeOpeningStockUniqueByBranch : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ux_stock_movements_opening_stock",
            schema: "pos",
            table: "stock_movements");

        migrationBuilder.CreateIndex(
            name: "ux_stock_movements_opening_stock_branch",
            schema: "pos",
            table: "stock_movements",
            columns: new[] { "organization_id", "product_id", "branch_id", "movement_type" },
            unique: true,
            filter: "movement_type = 'OpeningStock' AND branch_id IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "ux_stock_movements_opening_stock_legacy",
            schema: "pos",
            table: "stock_movements",
            columns: new[] { "organization_id", "product_id", "movement_type" },
            unique: true,
            filter: "movement_type = 'OpeningStock' AND branch_id IS NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ux_stock_movements_opening_stock_branch",
            schema: "pos",
            table: "stock_movements");

        migrationBuilder.DropIndex(
            name: "ux_stock_movements_opening_stock_legacy",
            schema: "pos",
            table: "stock_movements");

        migrationBuilder.CreateIndex(
            name: "ux_stock_movements_opening_stock",
            schema: "pos",
            table: "stock_movements",
            columns: new[] { "organization_id", "product_id", "movement_type" },
            unique: true,
            filter: "movement_type = 'OpeningStock'");
    }
}
