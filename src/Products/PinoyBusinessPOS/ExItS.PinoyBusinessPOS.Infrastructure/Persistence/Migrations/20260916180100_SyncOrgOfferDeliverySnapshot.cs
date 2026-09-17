using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Snapshot-only sync for org Offer Delivery + customer delivery override.
/// DDL is owned by <see cref="AddOrgOfferDeliveryAndCustomerOverride"/> (IF NOT EXISTS).
/// </summary>
public partial class SyncOrgOfferDeliverySnapshot : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
