using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Org Offer Delivery (default OFF) + per-customer delivery override (null|allow|block).
/// Does not delete or reset existing branch delivery configuration.
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260916180000_AddOrgOfferDeliveryAndCustomerOverride")]
public partial class AddOrgOfferDeliveryAndCustomerOverride : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS pos.organization_fulfillment_settings (
                id uuid NOT NULL,
                organization_id uuid NOT NULL,
                offer_delivery boolean NOT NULL DEFAULT FALSE,
                created_at_utc timestamptz NOT NULL,
                updated_at_utc timestamptz NOT NULL,
                CONSTRAINT "PK_organization_fulfillment_settings" PRIMARY KEY (id)
            );

            CREATE UNIQUE INDEX IF NOT EXISTS ux_organization_fulfillment_settings_org
                ON pos.organization_fulfillment_settings (organization_id);

            ALTER TABLE pos.connected_supplier_relationships
                ADD COLUMN IF NOT EXISTS customer_delivery_override varchar(16) NULL;

            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint
                    WHERE conname = 'ck_connected_supplier_relationships_customer_delivery_override'
                ) THEN
                    ALTER TABLE pos.connected_supplier_relationships
                        ADD CONSTRAINT ck_connected_supplier_relationships_customer_delivery_override
                        CHECK (
                            customer_delivery_override IS NULL
                            OR customer_delivery_override IN ('allow', 'block')
                        );
                END IF;
            END $$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE pos.connected_supplier_relationships
                DROP CONSTRAINT IF EXISTS ck_connected_supplier_relationships_customer_delivery_override;

            ALTER TABLE pos.connected_supplier_relationships
                DROP COLUMN IF EXISTS customer_delivery_override;

            DROP TABLE IF EXISTS pos.organization_fulfillment_settings;
            """);
    }
}
