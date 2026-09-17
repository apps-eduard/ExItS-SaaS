using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Tracks Utang amount already converted from connected-PO reservation into Outstanding Utang.
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260916120000_AddConnectedPoCreditPostedAmount")]
public partial class AddConnectedPoCreditPostedAmount : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE pos.connected_purchase_orders
                ADD COLUMN IF NOT EXISTS credit_posted_amount numeric(18,2) NOT NULL DEFAULT 0;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE pos.connected_purchase_orders
                DROP COLUMN IF EXISTS credit_posted_amount;
            """);
    }
}
