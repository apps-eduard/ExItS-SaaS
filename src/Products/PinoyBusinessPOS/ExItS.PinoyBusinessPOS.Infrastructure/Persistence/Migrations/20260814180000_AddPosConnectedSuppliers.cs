using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Connected ExItS suppliers Phase 1 schema. Up is idempotent so Local Validation volumes that
/// already received columns/tables (partial apply / history gap) can complete MigrateAsync.
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260814180000_AddPosConnectedSuppliers")]
public partial class AddPosConnectedSuppliers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE pos.suppliers
                ADD COLUMN IF NOT EXISTS connection_type integer NOT NULL DEFAULT 0;
            ALTER TABLE pos.suppliers
                ADD COLUMN IF NOT EXISTS connected_relationship_id uuid NULL;

            CREATE TABLE IF NOT EXISTS pos.connected_supplier_relationships (
                id uuid NOT NULL PRIMARY KEY,
                buyer_organization_id uuid NOT NULL,
                supplier_organization_id uuid NOT NULL,
                status integer NOT NULL,
                requested_at_utc timestamp with time zone NOT NULL,
                requested_by_user_id uuid NULL,
                responded_at_utc timestamp with time zone NULL,
                responded_by_user_id uuid NULL,
                disconnected_at_utc timestamp with time zone NULL,
                created_at_utc timestamp with time zone NOT NULL,
                updated_at_utc timestamp with time zone NOT NULL,
                CONSTRAINT ck_connected_supplier_relationships_status CHECK (status BETWEEN 0 AND 3)
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ux_connected_supplier_relationships_open
                ON pos.connected_supplier_relationships (buyer_organization_id, supplier_organization_id)
                WHERE status IN (0, 1);
            CREATE INDEX IF NOT EXISTS ix_connected_supplier_relationships_buyer
                ON pos.connected_supplier_relationships (buyer_organization_id);
            CREATE INDEX IF NOT EXISTS ix_connected_supplier_relationships_supplier
                ON pos.connected_supplier_relationships (supplier_organization_id);

            CREATE TABLE IF NOT EXISTS pos.supplier_product_exposures (
                id uuid NOT NULL PRIMARY KEY,
                supplier_organization_id uuid NOT NULL,
                product_id uuid NOT NULL,
                sku_snapshot varchar(64) NULL,
                name_snapshot varchar(200) NOT NULL,
                category_name_snapshot varchar(128) NULL,
                unit_of_measure_code varchar(32) NOT NULL,
                supplier_order_price numeric(18,2) NOT NULL,
                is_orderable boolean NOT NULL,
                is_exposed boolean NOT NULL,
                sync_version bigint NOT NULL,
                created_at_utc timestamp with time zone NOT NULL,
                updated_at_utc timestamp with time zone NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ux_supplier_product_exposures_product
                ON pos.supplier_product_exposures (supplier_organization_id, product_id);
            CREATE INDEX IF NOT EXISTS ix_supplier_product_exposures_sync
                ON pos.supplier_product_exposures (supplier_organization_id, sync_version);
            CREATE INDEX IF NOT EXISTS ix_supplier_product_exposures_name
                ON pos.supplier_product_exposures (supplier_organization_id, name_snapshot);
            CREATE INDEX IF NOT EXISTS ix_supplier_product_exposures_sku
                ON pos.supplier_product_exposures (supplier_organization_id, sku_snapshot);

            CREATE TABLE IF NOT EXISTS pos.buyer_supplier_product_links (
                id uuid NOT NULL PRIMARY KEY,
                relationship_id uuid NOT NULL,
                buyer_organization_id uuid NOT NULL,
                supplier_organization_id uuid NOT NULL,
                buyer_product_id uuid NOT NULL,
                supplier_product_id uuid NOT NULL,
                supplier_sku_snapshot varchar(64) NULL,
                supplier_name_snapshot varchar(200) NOT NULL,
                unit_of_measure_code varchar(32) NOT NULL,
                last_known_order_price numeric(18,2) NOT NULL,
                is_active boolean NOT NULL,
                sync_version bigint NOT NULL,
                created_at_utc timestamp with time zone NOT NULL,
                updated_at_utc timestamp with time zone NOT NULL,
                CONSTRAINT fk_buyer_supplier_product_links_relationship FOREIGN KEY (relationship_id)
                    REFERENCES pos.connected_supplier_relationships(id) ON DELETE RESTRICT
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ux_buyer_supplier_product_links_active
                ON pos.buyer_supplier_product_links (relationship_id, buyer_product_id) WHERE is_active;
            CREATE INDEX IF NOT EXISTS ix_buyer_supplier_product_links_sync
                ON pos.buyer_supplier_product_links (relationship_id, sync_version);

            CREATE TABLE IF NOT EXISTS pos.connected_purchase_orders (
                id uuid NOT NULL PRIMARY KEY,
                relationship_id uuid NOT NULL,
                buyer_organization_id uuid NOT NULL,
                supplier_organization_id uuid NOT NULL,
                buyer_purchase_order_id uuid NOT NULL,
                buyer_po_number varchar(64) NULL,
                order_date date NOT NULL,
                notes varchar(512) NULL,
                status integer NOT NULL,
                total_amount numeric(18,2) NOT NULL,
                created_at_utc timestamp with time zone NOT NULL,
                updated_at_utc timestamp with time zone NOT NULL,
                accepted_at_utc timestamp with time zone NULL,
                declined_at_utc timestamp with time zone NULL,
                CONSTRAINT ck_connected_purchase_orders_status CHECK (status BETWEEN 0 AND 2),
                CONSTRAINT fk_connected_purchase_orders_relationship FOREIGN KEY (relationship_id)
                    REFERENCES pos.connected_supplier_relationships(id) ON DELETE RESTRICT
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ux_connected_purchase_orders_buyer_po
                ON pos.connected_purchase_orders (buyer_purchase_order_id);
            CREATE INDEX IF NOT EXISTS ix_connected_purchase_orders_supplier_status
                ON pos.connected_purchase_orders (supplier_organization_id, status);

            CREATE TABLE IF NOT EXISTS pos.connected_purchase_order_lines (
                connected_purchase_order_id uuid NOT NULL,
                line_number integer NOT NULL,
                product_id uuid NOT NULL,
                name_snapshot varchar(200) NOT NULL,
                sku_snapshot varchar(64) NULL,
                qty numeric(18,3) NOT NULL,
                unit_price_snapshot numeric(18,2) NOT NULL,
                line_total numeric(18,2) NOT NULL,
                unit_of_measure_code varchar(32) NOT NULL,
                CONSTRAINT pk_connected_purchase_order_lines PRIMARY KEY (connected_purchase_order_id, line_number),
                CONSTRAINT fk_connected_purchase_order_lines_order FOREIGN KEY (connected_purchase_order_id)
                    REFERENCES pos.connected_purchase_orders(id) ON DELETE CASCADE
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE IF EXISTS pos.connected_purchase_order_lines;
            DROP TABLE IF EXISTS pos.connected_purchase_orders;
            DROP TABLE IF EXISTS pos.buyer_supplier_product_links;
            DROP TABLE IF EXISTS pos.supplier_product_exposures;
            DROP TABLE IF EXISTS pos.connected_supplier_relationships;
            ALTER TABLE pos.suppliers DROP COLUMN IF EXISTS connected_relationship_id;
            ALTER TABLE pos.suppliers DROP COLUMN IF EXISTS connection_type;
            """);
    }
}
