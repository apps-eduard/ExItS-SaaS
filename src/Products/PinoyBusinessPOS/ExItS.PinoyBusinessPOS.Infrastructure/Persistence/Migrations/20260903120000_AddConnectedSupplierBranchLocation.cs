using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Persists operational supplier source branch on org↔org connected-supplier relationships.
/// Organization remains the relationship anchor; branch is location authority only.
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260903120000_AddConnectedSupplierBranchLocation")]
public partial class AddConnectedSupplierBranchLocation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "supplier_branch_id",
            schema: "pos",
            table: "connected_supplier_relationships",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "supplier_branch_name_snapshot",
            schema: "pos",
            table: "connected_supplier_relationships",
            type: "character varying(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_connected_supplier_relationships_supplier_branch",
            schema: "pos",
            table: "connected_supplier_relationships",
            columns: new[] { "supplier_organization_id", "supplier_branch_id" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_connected_supplier_relationships_supplier_branch",
            schema: "pos",
            table: "connected_supplier_relationships");

        migrationBuilder.DropColumn(
            name: "supplier_branch_name_snapshot",
            schema: "pos",
            table: "connected_supplier_relationships");

        migrationBuilder.DropColumn(
            name: "supplier_branch_id",
            schema: "pos",
            table: "connected_supplier_relationships");
    }
}
