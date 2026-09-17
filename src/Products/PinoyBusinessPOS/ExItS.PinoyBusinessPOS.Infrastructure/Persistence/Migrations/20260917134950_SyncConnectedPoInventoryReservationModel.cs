using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Syncs EF model snapshot for connected-PO inventory reservations.
/// Idempotent so it is safe if an earlier raw SQL preview already applied the same objects.
/// </summary>
public partial class SyncConnectedPoInventoryReservationModel : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE pos.connected_purchase_orders
                ADD COLUMN IF NOT EXISTS inventory_reservation_state integer NOT NULL DEFAULT 0,
                ADD COLUMN IF NOT EXISTS inventory_reservation_expires_at_utc timestamptz NULL,
                ADD COLUMN IF NOT EXISTS inventory_reservation_revision integer NOT NULL DEFAULT 0;

            ALTER TABLE pos.connected_purchase_orders
                DROP CONSTRAINT IF EXISTS ck_connected_purchase_orders_inv_res_state;
            ALTER TABLE pos.connected_purchase_orders
                ADD CONSTRAINT ck_connected_purchase_orders_inv_res_state
                CHECK (inventory_reservation_state BETWEEN 0 AND 4);

            ALTER TABLE pos.connected_purchase_orders
                DROP CONSTRAINT IF EXISTS ck_connected_purchase_orders_inv_res_revision;
            ALTER TABLE pos.connected_purchase_orders
                ADD CONSTRAINT ck_connected_purchase_orders_inv_res_revision
                CHECK (inventory_reservation_revision >= 0);

            CREATE TABLE IF NOT EXISTS pos.connected_po_inventory_reservations
            (
                id uuid NOT NULL,
                organization_id uuid NOT NULL,
                branch_id uuid NOT NULL,
                product_id uuid NOT NULL,
                connected_purchase_order_id uuid NOT NULL,
                revision integer NOT NULL,
                quantity numeric(18,3) NOT NULL,
                remaining_quantity numeric(18,3) NOT NULL,
                type integer NOT NULL,
                status integer NOT NULL,
                created_at_utc timestamptz NOT NULL,
                expires_at_utc timestamptz NULL,
                released_at_utc timestamptz NULL,
                version integer NOT NULL,
                CONSTRAINT "PK_connected_po_inventory_reservations" PRIMARY KEY (id),
                CONSTRAINT ck_connected_po_inv_res_type CHECK (type BETWEEN 0 AND 1),
                CONSTRAINT ck_connected_po_inv_res_status CHECK (status BETWEEN 0 AND 3),
                CONSTRAINT ck_connected_po_inv_res_qty_positive CHECK (quantity > 0),
                CONSTRAINT ck_connected_po_inv_res_remaining CHECK (remaining_quantity >= 0 AND remaining_quantity <= quantity),
                CONSTRAINT ck_connected_po_inv_res_revision CHECK (revision >= 1)
            );

            CREATE INDEX IF NOT EXISTS ix_connected_po_inv_res_order_status
                ON pos.connected_po_inventory_reservations (connected_purchase_order_id, status);

            CREATE INDEX IF NOT EXISTS ix_connected_po_inv_res_branch_product_status
                ON pos.connected_po_inventory_reservations (organization_id, branch_id, product_id, status);
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TABLE IF EXISTS pos.connected_po_inventory_reservations;

            ALTER TABLE pos.connected_purchase_orders
                DROP CONSTRAINT IF EXISTS ck_connected_purchase_orders_inv_res_state;
            ALTER TABLE pos.connected_purchase_orders
                DROP CONSTRAINT IF EXISTS ck_connected_purchase_orders_inv_res_revision;

            ALTER TABLE pos.connected_purchase_orders
                DROP COLUMN IF EXISTS inventory_reservation_state,
                DROP COLUMN IF EXISTS inventory_reservation_expires_at_utc,
                DROP COLUMN IF EXISTS inventory_reservation_revision;
            """);
    }
}
