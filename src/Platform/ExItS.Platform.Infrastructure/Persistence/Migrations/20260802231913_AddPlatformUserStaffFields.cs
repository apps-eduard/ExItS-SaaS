using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformUserStaffFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "created_by_user_id",
                schema: "platform",
                table: "platform_users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "employee_code",
                schema: "platform",
                table: "platform_users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "first_name",
                schema: "platform",
                table: "platform_users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_name",
                schema: "platform",
                table: "platform_users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "phone",
                schema: "platform",
                table: "platform_users",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "staff_number",
                schema: "platform",
                table: "platform_users",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_platform_users_staff_number",
                schema: "platform",
                table: "platform_users",
                column: "staff_number",
                unique: true,
                filter: "staff_number IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_platform_users_staff_number",
                schema: "platform",
                table: "platform_users");

            migrationBuilder.DropColumn(
                name: "created_by_user_id",
                schema: "platform",
                table: "platform_users");

            migrationBuilder.DropColumn(
                name: "employee_code",
                schema: "platform",
                table: "platform_users");

            migrationBuilder.DropColumn(
                name: "first_name",
                schema: "platform",
                table: "platform_users");

            migrationBuilder.DropColumn(
                name: "last_name",
                schema: "platform",
                table: "platform_users");

            migrationBuilder.DropColumn(
                name: "phone",
                schema: "platform",
                table: "platform_users");

            migrationBuilder.DropColumn(
                name: "staff_number",
                schema: "platform",
                table: "platform_users");
        }
    }
}
