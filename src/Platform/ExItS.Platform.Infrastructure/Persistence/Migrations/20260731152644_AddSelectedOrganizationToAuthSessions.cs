using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSelectedOrganizationToAuthSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "selected_organization_id",
                schema: "platform",
                table: "platform_auth_sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_platform_auth_sessions_selected_organization_id",
                schema: "platform",
                table: "platform_auth_sessions",
                column: "selected_organization_id");

            migrationBuilder.AddForeignKey(
                name: "FK_platform_auth_sessions_organizations_selected_organization_~",
                schema: "platform",
                table: "platform_auth_sessions",
                column: "selected_organization_id",
                principalSchema: "platform",
                principalTable: "organizations",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_platform_auth_sessions_organizations_selected_organization_~",
                schema: "platform",
                table: "platform_auth_sessions");

            migrationBuilder.DropIndex(
                name: "IX_platform_auth_sessions_selected_organization_id",
                schema: "platform",
                table: "platform_auth_sessions");

            migrationBuilder.DropColumn(
                name: "selected_organization_id",
                schema: "platform",
                table: "platform_auth_sessions");
        }
    }
}
