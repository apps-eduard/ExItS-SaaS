using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Sale buyer-party snapshots and POS customer ExItS identity link columns.
/// Up is idempotent for Local Validation volumes with partial prior applies.
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260814220000_AddSaleBuyerPartyAndCustomerExItsLinks")]
public partial class AddSaleBuyerPartyAndCustomerExItsLinks : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE pos.sales
                ADD COLUMN IF NOT EXISTS buyer_party_kind character varying(32) NOT NULL DEFAULT 'WalkIn';
            ALTER TABLE pos.sales
                ADD COLUMN IF NOT EXISTS buyer_display_name_snapshot character varying(128) NULL;
            ALTER TABLE pos.sales
                ADD COLUMN IF NOT EXISTS buyer_personal_public_user_id character varying(12) NULL;
            ALTER TABLE pos.sales
                ADD COLUMN IF NOT EXISTS buyer_organization_id uuid NULL;
            ALTER TABLE pos.sales
                ADD COLUMN IF NOT EXISTS buyer_public_organization_id character varying(9) NULL;

            UPDATE pos.sales
            SET buyer_party_kind = 'ExternalCustomer'
            WHERE customer_id IS NOT NULL
              AND buyer_party_kind = 'WalkIn';

            UPDATE pos.sales s
            SET buyer_display_name_snapshot = c.display_name
            FROM pos.customers c
            WHERE s.customer_id IS NOT NULL
              AND c.id = s.customer_id
              AND s.buyer_display_name_snapshot IS NULL;

            UPDATE pos.sales
            SET buyer_party_kind = 'WalkIn'
            WHERE customer_id IS NULL
              AND buyer_party_kind IS DISTINCT FROM 'WalkIn';

            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint
                    WHERE conname = 'ck_sales_buyer_party_kind'
                      AND conrelid = 'pos.sales'::regclass)
                THEN
                    ALTER TABLE pos.sales
                        ADD CONSTRAINT ck_sales_buyer_party_kind
                        CHECK (buyer_party_kind IN ('WalkIn', 'ExternalCustomer', 'Personal', 'Organization'));
                END IF;
            END $$;

            CREATE INDEX IF NOT EXISTS ix_sales_org_buyer_party_kind
                ON pos.sales (organization_id, buyer_party_kind);

            ALTER TABLE pos.customers
                ADD COLUMN IF NOT EXISTS linked_personal_public_user_id character varying(12) NULL;
            ALTER TABLE pos.customers
                ADD COLUMN IF NOT EXISTS linked_buyer_organization_id uuid NULL;
            ALTER TABLE pos.customers
                ADD COLUMN IF NOT EXISTS linked_buyer_public_organization_id character varying(9) NULL;

            CREATE UNIQUE INDEX IF NOT EXISTS ux_customers_org_linked_personal
                ON pos.customers (organization_id, linked_personal_public_user_id)
                WHERE linked_personal_public_user_id IS NOT NULL;

            CREATE UNIQUE INDEX IF NOT EXISTS ux_customers_org_linked_buyer_org
                ON pos.customers (organization_id, linked_buyer_organization_id)
                WHERE linked_buyer_organization_id IS NOT NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP INDEX IF EXISTS pos.ux_customers_org_linked_buyer_org;
            DROP INDEX IF EXISTS pos.ux_customers_org_linked_personal;
            ALTER TABLE pos.customers DROP COLUMN IF EXISTS linked_buyer_public_organization_id;
            ALTER TABLE pos.customers DROP COLUMN IF EXISTS linked_buyer_organization_id;
            ALTER TABLE pos.customers DROP COLUMN IF EXISTS linked_personal_public_user_id;

            DROP INDEX IF EXISTS pos.ix_sales_org_buyer_party_kind;
            ALTER TABLE pos.sales DROP CONSTRAINT IF EXISTS ck_sales_buyer_party_kind;
            ALTER TABLE pos.sales DROP COLUMN IF EXISTS buyer_public_organization_id;
            ALTER TABLE pos.sales DROP COLUMN IF EXISTS buyer_organization_id;
            ALTER TABLE pos.sales DROP COLUMN IF EXISTS buyer_personal_public_user_id;
            ALTER TABLE pos.sales DROP COLUMN IF EXISTS buyer_display_name_snapshot;
            ALTER TABLE pos.sales DROP COLUMN IF EXISTS buyer_party_kind;
            """);
    }
}
