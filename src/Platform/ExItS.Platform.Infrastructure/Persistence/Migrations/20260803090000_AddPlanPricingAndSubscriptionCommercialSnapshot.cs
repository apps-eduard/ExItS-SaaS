using ExItS.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PlatformDbContext))]
[Migration("20260803090000_AddPlanPricingAndSubscriptionCommercialSnapshot")]
public partial class AddPlanPricingAndSubscriptionCommercialSnapshot : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "monthly_price",
            schema: "platform",
            table: "plans",
            type: "numeric(18,2)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "annual_price",
            schema: "platform",
            table: "plans",
            type: "numeric(18,2)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<string>(
            name: "currency_code",
            schema: "platform",
            table: "plans",
            type: "character varying(3)",
            maxLength: 3,
            nullable: false,
            defaultValue: "PHP");

        migrationBuilder.AddColumn<string>(
            name: "billing_cycle",
            schema: "platform",
            table: "subscriptions",
            type: "character varying(16)",
            maxLength: 16,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "agreed_price",
            schema: "platform",
            table: "subscriptions",
            type: "numeric(18,2)",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "currency_code",
            schema: "platform",
            table: "subscriptions",
            type: "character varying(3)",
            maxLength: 3,
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "price_effective_from_utc",
            schema: "platform",
            table: "subscriptions",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "pending_plan_id",
            schema: "platform",
            table: "subscriptions",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "pending_plan_effective_at_utc",
            schema: "platform",
            table: "subscriptions",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "provider_payments",
            schema: "platform",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                subscription_id = table.Column<Guid>(type: "uuid", nullable: false),
                amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                provider_reference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                is_test = table.Column<bool>(type: "boolean", nullable: false),
                failure_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                failure_message = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                purpose = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_provider_payments", x => x.id);
                table.ForeignKey(
                    name: "FK_provider_payments_subscriptions_subscription_id",
                    column: x => x.subscription_id,
                    principalSchema: "platform",
                    principalTable: "subscriptions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_provider_payments_idempotency_key",
            schema: "platform",
            table: "provider_payments",
            column: "idempotency_key",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_provider_payments_subscription_id",
            schema: "platform",
            table: "provider_payments",
            column: "subscription_id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "provider_payments", schema: "platform");

        migrationBuilder.DropColumn(name: "monthly_price", schema: "platform", table: "plans");
        migrationBuilder.DropColumn(name: "annual_price", schema: "platform", table: "plans");
        migrationBuilder.DropColumn(name: "currency_code", schema: "platform", table: "plans");

        migrationBuilder.DropColumn(name: "billing_cycle", schema: "platform", table: "subscriptions");
        migrationBuilder.DropColumn(name: "agreed_price", schema: "platform", table: "subscriptions");
        migrationBuilder.DropColumn(name: "currency_code", schema: "platform", table: "subscriptions");
        migrationBuilder.DropColumn(name: "price_effective_from_utc", schema: "platform", table: "subscriptions");
        migrationBuilder.DropColumn(name: "pending_plan_id", schema: "platform", table: "subscriptions");
        migrationBuilder.DropColumn(name: "pending_plan_effective_at_utc", schema: "platform", table: "subscriptions");
    }
}
