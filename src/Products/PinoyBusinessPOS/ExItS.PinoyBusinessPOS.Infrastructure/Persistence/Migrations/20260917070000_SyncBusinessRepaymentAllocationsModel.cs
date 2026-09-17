using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class SyncBusinessRepaymentAllocationsModel : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Idempotent: a prior hand-written SQL migration may already have created this table.
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS pos.business_repayment_allocations (
                id uuid NOT NULL,
                seller_organization_id uuid NOT NULL,
                repayment_id uuid NOT NULL,
                credit_entry_id uuid NOT NULL,
                amount numeric(18,2) NOT NULL,
                created_at_utc timestamp with time zone NOT NULL,
                CONSTRAINT "PK_business_repayment_allocations" PRIMARY KEY (id),
                CONSTRAINT ck_business_repayment_allocations_amount_positive CHECK (amount > 0)
            );

            CREATE INDEX IF NOT EXISTS ix_business_repayment_allocations_credit
                ON pos.business_repayment_allocations (seller_organization_id, credit_entry_id);

            CREATE INDEX IF NOT EXISTS ix_business_repayment_allocations_repayment
                ON pos.business_repayment_allocations (seller_organization_id, repayment_id);

            CREATE UNIQUE INDEX IF NOT EXISTS ux_business_repayment_allocations_repayment_credit
                ON pos.business_repayment_allocations (repayment_id, credit_entry_id);
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TABLE IF EXISTS pos.business_repayment_allocations;
            """);
    }
}
