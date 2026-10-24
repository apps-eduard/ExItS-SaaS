using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchScheduledOrderSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "accept_scheduled_orders_while_closed",
                schema: "platform",
                table: "organization_branches",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "max_advance_schedule_days",
                schema: "platform",
                table: "organization_branches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "min_preparation_lead_minutes",
                schema: "platform",
                table: "organization_branches",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "accept_scheduled_orders_while_closed",
                schema: "platform",
                table: "organization_branches");

            migrationBuilder.DropColumn(
                name: "max_advance_schedule_days",
                schema: "platform",
                table: "organization_branches");

            migrationBuilder.DropColumn(
                name: "min_preparation_lead_minutes",
                schema: "platform",
                table: "organization_branches");
        }
    }
}
