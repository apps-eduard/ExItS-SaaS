using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Syncs EF model snapshot for hand-written AddUtangCheckRepayments (already applied via that migration)
/// and adds the unique buyer connected-supplier projection index.
/// Does not re-create repayment columns/tables.
/// </summary>
public partial class SyncPosModelSnapshotAndSupplierConnectedUnique : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Keep the oldest supplier row when historical duplicates exist; clear the rest.
        migrationBuilder.Sql(
            """
            WITH ranked AS (
                SELECT id,
                       ROW_NUMBER() OVER (
                           PARTITION BY organization_id, connected_relationship_id
                           ORDER BY created_at_utc ASC, id ASC) AS rn
                FROM pos.suppliers
                WHERE connected_relationship_id IS NOT NULL
            )
            UPDATE pos.suppliers s
            SET connected_relationship_id = NULL,
                connection_type = 0
            FROM ranked r
            WHERE s.id = r.id
              AND r.rn > 1;

            CREATE UNIQUE INDEX IF NOT EXISTS ux_suppliers_org_connected_relationship
            ON pos.suppliers (organization_id, connected_relationship_id)
            WHERE connected_relationship_id IS NOT NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP INDEX IF EXISTS pos.ux_suppliers_org_connected_relationship;
            """);
    }
}
