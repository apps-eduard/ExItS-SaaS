using ExItS.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PlatformDbContext))]
[Migration("20261004143000_OneOpenSubscriptionCheckout")]
public sealed class OneOpenSubscriptionCheckout : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "ux_subscription_payment_one_open_personal",
            schema: "platform",
            table: "subscription_payment_transactions",
            columns: ["initiated_by_user_id", "plan_key", "billing_cycle"],
            unique: true,
            filter: "organization_id IS NULL AND status IN ('Pending', 'Processing')");

        migrationBuilder.CreateIndex(
            name: "ux_subscription_payment_one_open_organization",
            schema: "platform",
            table: "subscription_payment_transactions",
            columns: ["initiated_by_user_id", "organization_id", "plan_key", "billing_cycle"],
            unique: true,
            filter: "organization_id IS NOT NULL AND status IN ('Pending', 'Processing')");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ux_subscription_payment_one_open_organization",
            schema: "platform",
            table: "subscription_payment_transactions");

        migrationBuilder.DropIndex(
            name: "ux_subscription_payment_one_open_personal",
            schema: "platform",
            table: "subscription_payment_transactions");
    }
}
