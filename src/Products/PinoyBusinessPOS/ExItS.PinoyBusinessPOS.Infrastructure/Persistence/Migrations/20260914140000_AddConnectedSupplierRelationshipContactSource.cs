using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <summary>
/// ContactSource + OrganizationMemberId for B2B relationship staff picker.
/// Does not create duplicate membership/user rows — stores a reference + snapshots only.
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260914140000_AddConnectedSupplierRelationshipContactSource")]
public partial class AddConnectedSupplierRelationshipContactSource : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE pos.connected_supplier_relationships
            ADD COLUMN IF NOT EXISTS contact_source integer NOT NULL DEFAULT 0;
            ALTER TABLE pos.connected_supplier_relationships
            ADD COLUMN IF NOT EXISTS organization_member_id uuid NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE pos.connected_supplier_relationships
            DROP COLUMN IF EXISTS organization_member_id;
            ALTER TABLE pos.connected_supplier_relationships
            DROP COLUMN IF EXISTS contact_source;
            """);
    }
}
