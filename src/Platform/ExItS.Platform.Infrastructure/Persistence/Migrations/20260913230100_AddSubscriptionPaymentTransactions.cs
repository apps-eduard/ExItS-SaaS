using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddSubscriptionPaymentTransactions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "subscription_payment_transactions",
            schema: "platform",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                reference_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                initiated_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                subscription_id = table.Column<Guid>(type: "uuid", nullable: true),
                plan_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                billing_cycle = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                base_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                discount_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                discount_percent = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                final_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                channel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                environment = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                provider_reference = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                card_brand = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                card_last4 = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                failure_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                failure_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                processing_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                paid_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                failed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                cancelled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                expired_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                period_start_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                period_end_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                subscription_activated = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_subscription_payment_transactions", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "subscription_payment_activities",
            schema: "platform",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                event_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                message = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_subscription_payment_activities", x => x.id);
                table.ForeignKey(
                    name: "FK_subscription_payment_activities_subscription_payment_transa~",
                    column: x => x.payment_id,
                    principalSchema: "platform",
                    principalTable: "subscription_payment_transactions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_subscription_payment_activities_payment_id_occurred_at_utc",
            schema: "platform",
            table: "subscription_payment_activities",
            columns: new[] { "payment_id", "occurred_at_utc" });

        migrationBuilder.CreateIndex(
            name: "IX_subscription_payment_transactions_created_at_utc",
            schema: "platform",
            table: "subscription_payment_transactions",
            column: "created_at_utc");

        migrationBuilder.CreateIndex(
            name: "IX_subscription_payment_transactions_organization_id",
            schema: "platform",
            table: "subscription_payment_transactions",
            column: "organization_id");

        migrationBuilder.CreateIndex(
            name: "IX_subscription_payment_transactions_provider_reference",
            schema: "platform",
            table: "subscription_payment_transactions",
            column: "provider_reference",
            unique: true,
            filter: "provider_reference IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_subscription_payment_transactions_reference_number",
            schema: "platform",
            table: "subscription_payment_transactions",
            column: "reference_number",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "subscription_payment_activities",
            schema: "platform");

        migrationBuilder.DropTable(
            name: "subscription_payment_transactions",
            schema: "platform");
    }
}
