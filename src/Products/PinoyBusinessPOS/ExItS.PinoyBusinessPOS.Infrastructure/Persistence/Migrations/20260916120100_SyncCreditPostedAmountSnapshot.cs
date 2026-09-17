using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Snapshot-only sync for <c>credit_posted_amount</c>.
/// DDL is owned by <see cref="AddConnectedPoCreditPostedAmount"/> (IF NOT EXISTS).
/// </summary>
public partial class SyncCreditPostedAmountSnapshot : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
