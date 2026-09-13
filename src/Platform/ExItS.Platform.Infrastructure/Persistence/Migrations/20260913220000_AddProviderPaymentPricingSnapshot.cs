using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PlatformDbContext))]
[Migration("20260913220000_AddProviderPaymentPricingSnapshot")]
public partial class AddProviderPaymentPricingSnapshot : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "plan_key",
            table: "provider_payments",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "billing_cycle",
            table: "provider_payments",
            type: "character varying(32)",
            maxLength: 32,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "base_amount",
            table: "provider_payments",
            type: "numeric(18,2)",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "discount_amount",
            table: "provider_payments",
            type: "numeric(18,2)",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "discount_percent",
            table: "provider_payments",
            type: "numeric(8,2)",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "final_amount",
            table: "provider_payments",
            type: "numeric(18,2)",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "plan_key", table: "provider_payments");
        migrationBuilder.DropColumn(name: "billing_cycle", table: "provider_payments");
        migrationBuilder.DropColumn(name: "base_amount", table: "provider_payments");
        migrationBuilder.DropColumn(name: "discount_amount", table: "provider_payments");
        migrationBuilder.DropColumn(name: "discount_percent", table: "provider_payments");
        migrationBuilder.DropColumn(name: "final_amount", table: "provider_payments");
    }
}
