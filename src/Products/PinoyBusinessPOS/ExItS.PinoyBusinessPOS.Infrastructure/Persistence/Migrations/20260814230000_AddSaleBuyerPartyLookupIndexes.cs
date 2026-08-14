using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Filtered lookup indexes for sale buyer personal/organization parties.
/// Completes model indexes configured on Sale that were omitted from
/// 20260814220000_AddSaleBuyerPartyAndCustomerExItsLinks.
/// Up is idempotent for Local Validation volumes.
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260814230000_AddSaleBuyerPartyLookupIndexes")]
public partial class AddSaleBuyerPartyLookupIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS ix_sales_org_buyer_organization
                ON pos.sales (organization_id, buyer_organization_id)
                WHERE buyer_organization_id IS NOT NULL;

            CREATE INDEX IF NOT EXISTS ix_sales_org_buyer_personal
                ON pos.sales (organization_id, buyer_personal_public_user_id)
                WHERE buyer_personal_public_user_id IS NOT NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP INDEX IF EXISTS pos.ix_sales_org_buyer_personal;
            DROP INDEX IF EXISTS pos.ix_sales_org_buyer_organization;
            """);
    }
}
