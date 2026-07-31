using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExItS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformRecoveryEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "pending_recovery_normalized_email",
                schema: "platform",
                table: "platform_user_credentials",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "recovery_email_prompt_skipped_at_utc",
                schema: "platform",
                table: "platform_user_credentials",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "recovery_email_verified_at_utc",
                schema: "platform",
                table: "platform_user_credentials",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "recovery_normalized_email",
                schema: "platform",
                table: "platform_user_credentials",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_platform_user_credentials_recovery_normalized_email",
                schema: "platform",
                table: "platform_user_credentials",
                column: "recovery_normalized_email",
                unique: true,
                filter: "recovery_normalized_email IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_platform_user_credentials_recovery_normalized_email",
                schema: "platform",
                table: "platform_user_credentials");

            migrationBuilder.DropColumn(
                name: "pending_recovery_normalized_email",
                schema: "platform",
                table: "platform_user_credentials");

            migrationBuilder.DropColumn(
                name: "recovery_email_prompt_skipped_at_utc",
                schema: "platform",
                table: "platform_user_credentials");

            migrationBuilder.DropColumn(
                name: "recovery_email_verified_at_utc",
                schema: "platform",
                table: "platform_user_credentials");

            migrationBuilder.DropColumn(
                name: "recovery_normalized_email",
                schema: "platform",
                table: "platform_user_credentials");
        }
    }
}
