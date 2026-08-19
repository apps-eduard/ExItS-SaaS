using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGovernanceStepUpAndBranchSuspension : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "suspended_at_utc",
                schema: "platform",
                table: "organization_branches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "suspended_by_user_id",
                schema: "platform",
                table: "organization_branches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "suspension_reason",
                schema: "platform",
                table: "organization_branches",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "governance_step_up_grants",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    target_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: true),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    consumed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_governance_step_up_grants", x => x.id);
                    table.ForeignKey(
                        name: "FK_governance_step_up_grants_organizations_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "platform",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_governance_step_up_grants_platform_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "platform",
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_governance_step_up_grants_organization_id",
                schema: "platform",
                table: "governance_step_up_grants",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_governance_step_up_grants_token_hash",
                schema: "platform",
                table: "governance_step_up_grants",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_governance_step_up_grants_user_id_organization_id_action_co~",
                schema: "platform",
                table: "governance_step_up_grants",
                columns: new[] { "user_id", "organization_id", "action_code" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "governance_step_up_grants",
                schema: "platform");

            migrationBuilder.DropColumn(
                name: "suspended_at_utc",
                schema: "platform",
                table: "organization_branches");

            migrationBuilder.DropColumn(
                name: "suspended_by_user_id",
                schema: "platform",
                table: "organization_branches");

            migrationBuilder.DropColumn(
                name: "suspension_reason",
                schema: "platform",
                table: "organization_branches");
        }
    }
}
