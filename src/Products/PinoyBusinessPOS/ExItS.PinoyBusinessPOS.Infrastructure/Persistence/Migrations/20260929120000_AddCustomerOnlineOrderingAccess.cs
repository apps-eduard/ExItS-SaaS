using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Organization-owned Personal online-ordering access override on POS customers
/// (Default | Allowed | Blocked). Existing rows default to Default.
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260929120000_AddCustomerOnlineOrderingAccess")]
public partial class AddCustomerOnlineOrderingAccess : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE pos.customers
                ADD COLUMN IF NOT EXISTS online_ordering_access character varying(16) NOT NULL DEFAULT 'Default';

            ALTER TABLE pos.customers
                ADD COLUMN IF NOT EXISTS online_ordering_access_updated_by_user_id uuid NULL;

            ALTER TABLE pos.customers
                ADD COLUMN IF NOT EXISTS online_ordering_access_updated_at_utc timestamptz NULL;

            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint
                    WHERE conname = 'ck_customers_online_ordering_access'
                ) THEN
                    ALTER TABLE pos.customers
                        ADD CONSTRAINT ck_customers_online_ordering_access
                        CHECK (online_ordering_access IN ('Default', 'Allowed', 'Blocked'));
                END IF;
            END $$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE pos.customers
                DROP CONSTRAINT IF EXISTS ck_customers_online_ordering_access;

            ALTER TABLE pos.customers
                DROP COLUMN IF EXISTS online_ordering_access_updated_at_utc;

            ALTER TABLE pos.customers
                DROP COLUMN IF EXISTS online_ordering_access_updated_by_user_id;

            ALTER TABLE pos.customers
                DROP COLUMN IF EXISTS online_ordering_access;
            """);
    }
}
