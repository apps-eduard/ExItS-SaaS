using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Explicit additional supplier-branch visibility for Business Customers.
/// Home remains supplier_branch_id; shared_supplier_branch_ids never stores Area ids.
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260914110000_AddConnectedSupplierSharedBranchVisibility")]
public partial class AddConnectedSupplierSharedBranchVisibility : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid[]>(
            name: "shared_supplier_branch_ids",
            schema: "pos",
            table: "connected_supplier_relationships",
            type: "uuid[]",
            nullable: false,
            defaultValueSql: "'{}'::uuid[]");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "shared_supplier_branch_ids",
            schema: "pos",
            table: "connected_supplier_relationships");
    }
}
