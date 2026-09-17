using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Authoritative PO cancellation audit for timeline (CancelledAtUtc / CancelledByUserId).
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260916010000_AddPurchaseOrderCancellationAudit")]
public partial class AddPurchaseOrderCancellationAudit : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "cancelled_at_utc",
            schema: "pos",
            table: "purchase_orders",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "cancelled_by_user_id",
            schema: "pos",
            table: "purchase_orders",
            type: "uuid",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "cancelled_at_utc",
            schema: "pos",
            table: "purchase_orders");

        migrationBuilder.DropColumn(
            name: "cancelled_by_user_id",
            schema: "pos",
            table: "purchase_orders");
    }
}
