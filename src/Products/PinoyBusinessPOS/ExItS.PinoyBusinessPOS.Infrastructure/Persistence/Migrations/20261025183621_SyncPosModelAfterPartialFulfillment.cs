using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Snapshot-alignment only. Schema for partial fulfillment was applied by
/// <c>20261025180000_AddCustomerOrderPartialFulfillment</c>. This migration must stay a no-op
/// so databases that already ran that migration do not attempt duplicate AddColumn operations.
/// </summary>
public partial class SyncPosModelAfterPartialFulfillment : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Intentionally empty — model snapshot sync only.
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentionally empty — paired with no-op Up.
    }
}
