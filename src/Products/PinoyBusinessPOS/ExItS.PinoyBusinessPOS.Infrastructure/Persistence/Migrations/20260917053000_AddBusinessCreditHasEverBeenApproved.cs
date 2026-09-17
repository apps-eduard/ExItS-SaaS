using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Durable HasEverBeenApproved on business customer credit policies.
/// Distinguishes Paused (once Active) from Credit unavailable (never activated).
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260917053000_AddBusinessCreditHasEverBeenApproved")]
public partial class AddBusinessCreditHasEverBeenApproved : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE pos.business_customer_credit_policies
                ADD COLUMN IF NOT EXISTS has_ever_been_approved boolean NOT NULL DEFAULT FALSE;

            -- Current Approved rows
            UPDATE pos.business_customer_credit_policies
            SET has_ever_been_approved = TRUE
            WHERE status = 2;

            -- Rows that still retain an approval timestamp (Disable keeps it; UpdateTerms clears it)
            UPDATE pos.business_customer_credit_policies
            SET has_ever_been_approved = TRUE
            WHERE approved_at_utc IS NOT NULL;

            -- Historical approve actions (covers reconfigured policies that cleared approved_at_utc)
            UPDATE pos.business_customer_credit_policies p
            SET has_ever_been_approved = TRUE
            WHERE EXISTS (
                SELECT 1
                FROM pos.business_customer_credit_policy_changes c
                WHERE c.business_customer_credit_policy_id = p.id
                  AND c.action = 4
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE pos.business_customer_credit_policies
                DROP COLUMN IF EXISTS has_ever_been_approved;
            """);
    }
}
