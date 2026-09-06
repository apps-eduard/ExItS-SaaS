using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Optional intended receiving branch on purchase orders for warehouse restock routing.
/// Null intended branch preserves current receive-at-acting-branch behavior.
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260907020000_AddPurchaseOrderIntendedReceivingBranch")]
public partial class AddPurchaseOrderIntendedReceivingBranch : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "intended_receiving_branch_id",
            schema: "pos",
            table: "purchase_orders",
            type: "uuid",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "intended_receiving_branch_id",
            schema: "pos",
            table: "purchase_orders");
    }
}
