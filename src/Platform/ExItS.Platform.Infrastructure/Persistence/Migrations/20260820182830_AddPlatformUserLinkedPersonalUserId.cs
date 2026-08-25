using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformUserLinkedPersonalUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "linked_personal_user_id",
                schema: "platform",
                table: "platform_users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_platform_users_linked_personal_user_id",
                schema: "platform",
                table: "platform_users",
                column: "linked_personal_user_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_platform_users_linked_personal_staff_only",
                schema: "platform",
                table: "platform_users",
                sql: "linked_personal_user_id IS NULL OR (home_organization_id IS NOT NULL AND linked_personal_user_id <> id)");

            migrationBuilder.AddForeignKey(
                name: "fk_platform_users_linked_personal_user",
                schema: "platform",
                table: "platform_users",
                column: "linked_personal_user_id",
                principalSchema: "platform",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_platform_users_linked_personal_user",
                schema: "platform",
                table: "platform_users");

            migrationBuilder.DropIndex(
                name: "ix_platform_users_linked_personal_user_id",
                schema: "platform",
                table: "platform_users");

            migrationBuilder.DropCheckConstraint(
                name: "ck_platform_users_linked_personal_staff_only",
                schema: "platform",
                table: "platform_users");

            migrationBuilder.DropColumn(
                name: "linked_personal_user_id",
                schema: "platform",
                table: "platform_users");
        }
    }
}
