using ExItS.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PlatformDbContext))]
[Migration("20261004120000_AddSubscriptionCheckoutProviderFields")]
public sealed class AddSubscriptionCheckoutProviderFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "checkout_url",
            schema: "platform",
            table: "subscription_payment_transactions",
            type: "character varying(512)",
            maxLength: 512,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "provider_event_id",
            schema: "platform",
            table: "subscription_payment_transactions",
            type: "character varying(128)",
            maxLength: 128,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "checkout_url",
            schema: "platform",
            table: "subscription_payment_transactions");

        migrationBuilder.DropColumn(
            name: "provider_event_id",
            schema: "platform",
            table: "subscription_payment_transactions");
    }
}
