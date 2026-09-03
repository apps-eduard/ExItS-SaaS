using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationNotificationBranchTarget : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "branch_id",
                schema: "platform",
                table: "organization_in_app_notifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_organization_in_app_notifications_org_recipient_branch",
                schema: "platform",
                table: "organization_in_app_notifications",
                columns: new[] { "organization_id", "recipient_user_identity_id", "branch_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_organization_in_app_notifications_org_recipient_branch",
                schema: "platform",
                table: "organization_in_app_notifications");

            migrationBuilder.DropColumn(
                name: "branch_id",
                schema: "platform",
                table: "organization_in_app_notifications");
        }
    }
}
