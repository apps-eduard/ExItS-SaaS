using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Snapshots warehouse acquisition unit cost onto inventory transfer lines for TransferOut/TransferIn.
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260907010000_AddInventoryTransferLineUnitCostSnapshot")]
public partial class AddInventoryTransferLineUnitCostSnapshot : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "unit_cost_snapshot",
            schema: "pos",
            table: "inventory_transfer_lines",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "unit_cost_snapshot",
            schema: "pos",
            table: "inventory_transfer_lines");
    }
}
