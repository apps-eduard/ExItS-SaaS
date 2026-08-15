using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Stores public counterparty display snapshots on connected-supplier relationships
/// so buyer Pending rows and supplier incoming requests can show business names without
/// cross-org Platform lookups.
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260815120000_AddConnectedSupplierCounterpartySnapshots")]
public partial class AddConnectedSupplierCounterpartySnapshots : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE pos.connected_supplier_relationships
                ADD COLUMN IF NOT EXISTS buyer_display_name_snapshot character varying(128) NULL;
            ALTER TABLE pos.connected_supplier_relationships
                ADD COLUMN IF NOT EXISTS buyer_public_organization_id_snapshot character varying(32) NULL;
            ALTER TABLE pos.connected_supplier_relationships
                ADD COLUMN IF NOT EXISTS supplier_display_name_snapshot character varying(128) NULL;
            ALTER TABLE pos.connected_supplier_relationships
                ADD COLUMN IF NOT EXISTS supplier_public_organization_id_snapshot character varying(32) NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE pos.connected_supplier_relationships
                DROP COLUMN IF EXISTS buyer_display_name_snapshot;
            ALTER TABLE pos.connected_supplier_relationships
                DROP COLUMN IF EXISTS buyer_public_organization_id_snapshot;
            ALTER TABLE pos.connected_supplier_relationships
                DROP COLUMN IF EXISTS supplier_display_name_snapshot;
            ALTER TABLE pos.connected_supplier_relationships
                DROP COLUMN IF EXISTS supplier_public_organization_id_snapshot;
            """);
    }
}
