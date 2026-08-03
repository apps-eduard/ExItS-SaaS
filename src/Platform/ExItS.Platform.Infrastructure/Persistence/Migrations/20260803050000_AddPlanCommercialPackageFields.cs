using ExItS.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PlatformDbContext))]
[Migration("20260803050000_AddPlanCommercialPackageFields")]
public partial class AddPlanCommercialPackageFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "description",
            schema: "platform",
            table: "plans",
            type: "character varying(2000)",
            maxLength: 2000,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "max_branches",
            schema: "platform",
            table: "plans",
            type: "integer",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.AddColumn<int>(
            name: "max_active_staff",
            schema: "platform",
            table: "plans",
            type: "integer",
            nullable: false,
            defaultValue: 3);

        migrationBuilder.AddColumn<bool>(
            name: "customer_credit_enabled",
            schema: "platform",
            table: "plans",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "advanced_reports_enabled",
            schema: "platform",
            table: "plans",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "export_enabled",
            schema: "platform",
            table: "plans",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "trial_allowed",
            schema: "platform",
            table: "plans",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<int>(
            name: "default_trial_days",
            schema: "platform",
            table: "plans",
            type: "integer",
            nullable: false,
            defaultValue: 14);

        migrationBuilder.AddColumn<int>(
            name: "sort_order",
            schema: "platform",
            table: "plans",
            type: "integer",
            nullable: false,
            defaultValue: 100);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "description", schema: "platform", table: "plans");
        migrationBuilder.DropColumn(name: "max_branches", schema: "platform", table: "plans");
        migrationBuilder.DropColumn(name: "max_active_staff", schema: "platform", table: "plans");
        migrationBuilder.DropColumn(name: "customer_credit_enabled", schema: "platform", table: "plans");
        migrationBuilder.DropColumn(name: "advanced_reports_enabled", schema: "platform", table: "plans");
        migrationBuilder.DropColumn(name: "export_enabled", schema: "platform", table: "plans");
        migrationBuilder.DropColumn(name: "trial_allowed", schema: "platform", table: "plans");
        migrationBuilder.DropColumn(name: "default_trial_days", schema: "platform", table: "plans");
        migrationBuilder.DropColumn(name: "sort_order", schema: "platform", table: "plans");
    }
}
