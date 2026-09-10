using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Adds InitiatedByParty so seller-initiated Business Customer invitations can be distinguished
/// from classic buyer→supplier requests. Existing rows backfill as Buyer (0).
/// </summary>
public partial class AddConnectedSupplierInitiatedByParty : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "initiated_by_party",
            schema: "pos",
            table: "connected_supplier_relationships",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddCheckConstraint(
            name: "ck_connected_supplier_relationships_initiated_by_party",
            schema: "pos",
            table: "connected_supplier_relationships",
            sql: "initiated_by_party BETWEEN 0 AND 1");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "ck_connected_supplier_relationships_initiated_by_party",
            schema: "pos",
            table: "connected_supplier_relationships");

        migrationBuilder.DropColumn(
            name: "initiated_by_party",
            schema: "pos",
            table: "connected_supplier_relationships");
    }
}
