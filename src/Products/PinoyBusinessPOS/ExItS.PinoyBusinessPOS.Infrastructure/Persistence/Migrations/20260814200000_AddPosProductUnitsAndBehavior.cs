using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Product usage flags, product-specific units, and conversion snapshots.
/// Up is idempotent for Local Validation volumes with partial prior applies.
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260814200000_AddPosProductUnitsAndBehavior")]
public partial class AddPosProductUnitsAndBehavior : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE pos.products
                ADD COLUMN IF NOT EXISTS can_be_purchased boolean NOT NULL DEFAULT true;
            ALTER TABLE pos.products
                ADD COLUMN IF NOT EXISTS can_be_sold boolean NOT NULL DEFAULT true;
            ALTER TABLE pos.products
                ADD COLUMN IF NOT EXISTS can_be_used_as_ingredient boolean NOT NULL DEFAULT false;
            ALTER TABLE pos.products
                ADD COLUMN IF NOT EXISTS is_produced boolean NOT NULL DEFAULT false;
            ALTER TABLE pos.products
                ADD COLUMN IF NOT EXISTS usage_preset character varying(64) NULL DEFAULT 'BuyAndSell';

            CREATE TABLE IF NOT EXISTS pos.product_units (
                id uuid NOT NULL PRIMARY KEY,
                organization_id uuid NOT NULL,
                product_id uuid NOT NULL,
                kind integer NOT NULL,
                display_name varchar(64) NOT NULL,
                short_label varchar(16) NOT NULL,
                multiplier_to_base numeric(18,3) NOT NULL,
                selling_price numeric(18,2) NULL,
                allows_custom_quantity boolean NOT NULL,
                is_active boolean NOT NULL,
                sort_order integer NOT NULL,
                created_at_utc timestamp with time zone NOT NULL,
                updated_at_utc timestamp with time zone NOT NULL,
                CONSTRAINT ck_product_units_kind CHECK (kind IN (0, 1)),
                CONSTRAINT ck_product_units_multiplier_positive CHECK (multiplier_to_base > 0),
                CONSTRAINT ck_product_units_selling_price_non_negative CHECK (selling_price IS NULL OR selling_price >= 0),
                CONSTRAINT ck_product_units_sort_order_non_negative CHECK (sort_order >= 0),
                CONSTRAINT fk_product_units_products FOREIGN KEY (product_id)
                    REFERENCES pos.products(id) ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS ix_product_units_org_product
                ON pos.product_units (organization_id, product_id);
            CREATE INDEX IF NOT EXISTS ix_product_units_org_product_kind_active
                ON pos.product_units (organization_id, product_id, kind)
                WHERE is_active;

            -- Seed 1:1 purchase + sell units only when a product has none yet.
            INSERT INTO pos.product_units (
                id, organization_id, product_id, kind, display_name, short_label,
                multiplier_to_base, selling_price, allows_custom_quantity, is_active, sort_order,
                created_at_utc, updated_at_utc)
            SELECT
                gen_random_uuid(),
                p.organization_id,
                p.id,
                0,
                p.unit_of_measure,
                LEFT(p.unit_of_measure, 16),
                1,
                NULL,
                false,
                true,
                0,
                COALESCE(p.created_at_utc, NOW()),
                COALESCE(p.updated_at_utc, NOW())
            FROM pos.products p
            WHERE NOT EXISTS (
                SELECT 1 FROM pos.product_units u
                WHERE u.product_id = p.id AND u.kind = 0);

            INSERT INTO pos.product_units (
                id, organization_id, product_id, kind, display_name, short_label,
                multiplier_to_base, selling_price, allows_custom_quantity, is_active, sort_order,
                created_at_utc, updated_at_utc)
            SELECT
                gen_random_uuid(),
                p.organization_id,
                p.id,
                1,
                p.unit_of_measure,
                LEFT(p.unit_of_measure, 16),
                1,
                p.selling_price,
                (p.selling_mode = 'ByWeight'),
                true,
                0,
                COALESCE(p.created_at_utc, NOW()),
                COALESCE(p.updated_at_utc, NOW())
            FROM pos.products p
            WHERE NOT EXISTS (
                SELECT 1 FROM pos.product_units u
                WHERE u.product_id = p.id AND u.kind = 1);

            ALTER TABLE pos.sale_lines
                ADD COLUMN IF NOT EXISTS selling_unit_id uuid NULL;
            ALTER TABLE pos.sale_lines
                ADD COLUMN IF NOT EXISTS selling_unit_name_snapshot character varying(64) NULL;
            ALTER TABLE pos.sale_lines
                ADD COLUMN IF NOT EXISTS entered_quantity numeric(18,3) NULL;
            ALTER TABLE pos.sale_lines
                ADD COLUMN IF NOT EXISTS multiplier_to_base_snapshot numeric(18,3) NULL;

            ALTER TABLE pos.purchase_order_lines
                ADD COLUMN IF NOT EXISTS purchase_unit_id uuid NULL;
            ALTER TABLE pos.purchase_order_lines
                ADD COLUMN IF NOT EXISTS purchase_unit_name_snapshot character varying(64) NULL;
            ALTER TABLE pos.purchase_order_lines
                ADD COLUMN IF NOT EXISTS multiplier_to_base_snapshot numeric(18,3) NOT NULL DEFAULT 1;

            ALTER TABLE pos.goods_receipt_lines
                ADD COLUMN IF NOT EXISTS purchase_unit_id uuid NULL;
            ALTER TABLE pos.goods_receipt_lines
                ADD COLUMN IF NOT EXISTS purchase_unit_name_snapshot character varying(64) NULL;
            ALTER TABLE pos.goods_receipt_lines
                ADD COLUMN IF NOT EXISTS multiplier_to_base_snapshot numeric(18,3) NOT NULL DEFAULT 1;

            ALTER TABLE pos.buyer_supplier_product_links
                ADD COLUMN IF NOT EXISTS buyer_purchase_unit_id uuid NULL;
            ALTER TABLE pos.buyer_supplier_product_links
                ADD COLUMN IF NOT EXISTS multiplier_to_base numeric(18,3) NOT NULL DEFAULT 1;
            ALTER TABLE pos.buyer_supplier_product_links
                ADD COLUMN IF NOT EXISTS package_label character varying(64) NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE pos.buyer_supplier_product_links DROP COLUMN IF EXISTS buyer_purchase_unit_id;
            ALTER TABLE pos.buyer_supplier_product_links DROP COLUMN IF EXISTS multiplier_to_base;
            ALTER TABLE pos.buyer_supplier_product_links DROP COLUMN IF EXISTS package_label;

            ALTER TABLE pos.goods_receipt_lines DROP COLUMN IF EXISTS purchase_unit_id;
            ALTER TABLE pos.goods_receipt_lines DROP COLUMN IF EXISTS purchase_unit_name_snapshot;
            ALTER TABLE pos.goods_receipt_lines DROP COLUMN IF EXISTS multiplier_to_base_snapshot;

            ALTER TABLE pos.purchase_order_lines DROP COLUMN IF EXISTS purchase_unit_id;
            ALTER TABLE pos.purchase_order_lines DROP COLUMN IF EXISTS purchase_unit_name_snapshot;
            ALTER TABLE pos.purchase_order_lines DROP COLUMN IF EXISTS multiplier_to_base_snapshot;

            ALTER TABLE pos.sale_lines DROP COLUMN IF EXISTS selling_unit_id;
            ALTER TABLE pos.sale_lines DROP COLUMN IF EXISTS selling_unit_name_snapshot;
            ALTER TABLE pos.sale_lines DROP COLUMN IF EXISTS entered_quantity;
            ALTER TABLE pos.sale_lines DROP COLUMN IF EXISTS multiplier_to_base_snapshot;

            DROP TABLE IF EXISTS pos.product_units;

            ALTER TABLE pos.products DROP COLUMN IF EXISTS can_be_purchased;
            ALTER TABLE pos.products DROP COLUMN IF EXISTS can_be_sold;
            ALTER TABLE pos.products DROP COLUMN IF EXISTS can_be_used_as_ingredient;
            ALTER TABLE pos.products DROP COLUMN IF EXISTS is_produced;
            ALTER TABLE pos.products DROP COLUMN IF EXISTS usage_preset;
            """);
    }
}
