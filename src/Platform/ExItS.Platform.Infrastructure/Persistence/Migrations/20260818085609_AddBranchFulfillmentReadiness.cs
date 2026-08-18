using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchFulfillmentReadiness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "pickup_enabled",
                schema: "platform",
                table: "organization_branches",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "contact_phone",
                schema: "platform",
                table: "organization_branches",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "customer_ordering_enabled",
                schema: "platform",
                table: "organization_branches",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "online_orders_pause_reason",
                schema: "platform",
                table: "organization_branches",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "online_orders_paused",
                schema: "platform",
                table: "organization_branches",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "time_zone_id",
                schema: "platform",
                table: "organization_branches",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "branch_operating_hours",
                schema: "platform",
                columns: table => new
                {
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    day_of_week = table.Column<int>(type: "integer", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_closed = table.Column<bool>(type: "boolean", nullable: false),
                    is_open_24_hours = table.Column<bool>(type: "boolean", nullable: false),
                    open_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    close_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branch_operating_hours", x => new { x.branch_id, x.day_of_week });
                    table.ForeignKey(
                        name: "FK_branch_operating_hours_organization_branches_branch_id_orga~",
                        columns: x => new { x.branch_id, x.organization_id },
                        principalSchema: "platform",
                        principalTable: "organization_branches",
                        principalColumns: new[] { "id", "organization_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_branch_operating_hours_branch_id_organization_id",
                schema: "platform",
                table: "branch_operating_hours",
                columns: new[] { "branch_id", "organization_id" });

            migrationBuilder.CreateIndex(
                name: "IX_branch_operating_hours_organization_id",
                schema: "platform",
                table: "branch_operating_hours",
                column: "organization_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "branch_operating_hours",
                schema: "platform");

            migrationBuilder.DropColumn(
                name: "contact_phone",
                schema: "platform",
                table: "organization_branches");

            migrationBuilder.DropColumn(
                name: "customer_ordering_enabled",
                schema: "platform",
                table: "organization_branches");

            migrationBuilder.DropColumn(
                name: "online_orders_pause_reason",
                schema: "platform",
                table: "organization_branches");

            migrationBuilder.DropColumn(
                name: "online_orders_paused",
                schema: "platform",
                table: "organization_branches");

            migrationBuilder.DropColumn(
                name: "time_zone_id",
                schema: "platform",
                table: "organization_branches");

            migrationBuilder.AlterColumn<bool>(
                name: "pickup_enabled",
                schema: "platform",
                table: "organization_branches",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);
        }
    }
}
