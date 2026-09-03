using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Historical supplier source-branch snapshot on purchase orders (connected suppliers).
/// Legacy rows remain null; no backfill.
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260903140000_AddPurchaseOrderSupplierBranchSnapshot")]
public partial class AddPurchaseOrderSupplierBranchSnapshot : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "supplier_branch_id",
            schema: "pos",
            table: "purchase_orders",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "supplier_branch_name_snapshot",
            schema: "pos",
            table: "purchase_orders",
            type: "character varying(128)",
            maxLength: 128,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "supplier_branch_name_snapshot",
            schema: "pos",
            table: "purchase_orders");

        migrationBuilder.DropColumn(
            name: "supplier_branch_id",
            schema: "pos",
            table: "purchase_orders");
    }
}
